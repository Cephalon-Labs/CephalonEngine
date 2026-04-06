using System.Diagnostics.CodeAnalysis;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Rules;
using Cephalon.Behaviors.Services;

namespace Cephalon.Tests.Behaviors;

public sealed class BehaviorBaselineTests
{
    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Scenario_result naming improves test readability.")]
    public void AppBehaviorAttribute_RejectsEmptyId()
    {
        Assert.Throws<ArgumentException>(() => new AppBehaviorAttribute(""));
        Assert.Throws<ArgumentException>(() => new AppBehaviorAttribute("  "));
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Scenario_result naming improves test readability.")]
    public void BehaviorTopologyDescriptor_StoresValues()
    {
        var d = new BehaviorTopologyDescriptor("order.place", "cqrs", ["http.rest"]);
        Assert.Equal("order.place", d.Id);
        Assert.Equal("cqrs", d.Pattern);
        Assert.Contains("http.rest", d.TransportIds);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Scenario_result naming improves test readability.")]
    public void BehaviorTopologyBuilder_AsCqrs_SetsPattern()
    {
        var builder = new BehaviorTopologyBuilder();
        builder.AsCqrs().ViaHttpRest().ViaRabbitMq();
        var desc = builder.Build("order.place");
        Assert.Equal("cqrs", desc.Pattern);
        Assert.Contains("http.rest", desc.TransportIds);
        Assert.Contains("rabbitmq", desc.TransportIds);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Scenario_result naming improves test readability.")]
    public void BehaviorTopologyBuilder_AsDirect_SetsPattern()
    {
        var builder = new BehaviorTopologyBuilder();
        builder.AsDirect();
        var desc = builder.Build("ping");
        Assert.Equal("direct", desc.Pattern);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Scenario_result naming improves test readability.")]
    public void CompatibilityMatrix_Abt001_Error_SagaWithoutStatefulTransport()
    {
        var rule = new Abt001SagaRequiresStatefulTransportRule();
        var desc = new BehaviorTopologyDescriptor("s", "saga-step", ["http.rest"]);
        var violation = rule.Check(desc);
        Assert.NotNull(violation);
        Assert.Equal(CompatibilitySeverity.Error, violation!.Severity);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Scenario_result naming improves test readability.")]
    public void CompatibilityMatrix_Abt001_NoViolation_SagaWithRabbitMq()
    {
        var rule = new Abt001SagaRequiresStatefulTransportRule();
        var desc = new BehaviorTopologyDescriptor("s", "saga-step", ["rabbitmq"]);
        Assert.Null(rule.Check(desc));
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Scenario_result naming improves test readability.")]
    public void CompatibilityMatrix_Abt003_Error_ProcessManagerWithoutInbox()
    {
        var rule = new Abt003ProcessManagerRequiresInboxRule();
        var desc = new BehaviorTopologyDescriptor("pm", "process-manager", ["http.rest"], inboxEnabled: false);
        var violation = rule.Check(desc);
        Assert.NotNull(violation);
        Assert.Equal(CompatibilitySeverity.Error, violation!.Severity);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Scenario_result naming improves test readability.")]
    public async Task BehaviorExecutionSlot_InvokesHandler()
    {
        var slot = BehaviorExecutionSlot.For<TestDirectBehavior, string, string>();
        var ctx = new TestBehaviorContext();
        var result = await slot.InvokeAsync(new TestDirectBehavior(), "hello", ctx, CancellationToken.None);
        Assert.Equal("HELLO", result);
    }

    // Private test support types (must be private — TestHarnessSurfaceTests convention)
    private sealed class TestDirectBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken ct = default)
            => Task.FromResult(input.ToUpperInvariant());
    }

    private sealed class TestBehaviorContext : IBehaviorContext
    {
        public string BehaviorId => "test";
        public string? CorrelationId => null;
        public string? TenantId => null;
        public string? UserId => null;
        public string? TraceId => null;
        public CancellationToken CancellationToken => CancellationToken.None;
        public BehaviorFault? Fault => null;
        public IReadOnlyDictionary<string, string> Metadata => new Dictionary<string, string>();
        public Task PublishAsync<TEvent>(TEvent evt, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default) => Task.CompletedTask;
        public Task ReplyAsync<TResult>(TResult result, CancellationToken ct = default)
            => throw new NotSupportedException("ReplyAsync is not supported in direct pattern.");
        public T? GetSagaState<T>() => default;
        public void SetSagaState<T>(T state) { }
    }
}
