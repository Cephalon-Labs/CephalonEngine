using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Publishers;
using Cephalon.Behaviors.Patterns.Strategies;
using Cephalon.Behaviors.Services;
using Cephalon.Tests.Behaviors;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cephalon.Tests.Behaviors.Execution;

/// <summary>Tests for <see cref="ChoreographySagaExecutionStrategy"/>.</summary>
public sealed class ChoreographySagaExecutionStrategyTests
{
    private sealed record OrderPlaced(string OrderId, decimal Amount);
    private sealed record PaymentRequested(string OrderId, decimal Amount);
    private sealed record OrderCancelled(string OrderId, string Reason);

    [AppBehavior("saga.choreography.single")]
    private sealed class SinglePublicationBehavior : IAppBehavior<string, SagaChoreographyPublication>
    {
        public Task<SagaChoreographyPublication> HandleAsync(
            string input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SagaChoreographyPublication(
                id: "publication-1",
                channelId: "orders.events",
                eventType: "order-submitted",
                payload: $"{{\"value\":\"{input}\"}}",
                occurredAtUtc: DateTimeOffset.UtcNow));
        }
    }

    [AppBehavior("saga.choreography.combined")]
    private sealed class CombinedResultBehavior : IAppBehavior<string, SagaChoreographyStepResult>
    {
        public Task<SagaChoreographyStepResult> HandleAsync(
            string input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SagaChoreographyStepResult(
                output: $"accepted:{input}",
                publications:
                [
                    new SagaChoreographyPublication(
                        id: "publication-2",
                        channelId: "payments.events",
                        eventType: "payment-requested",
                        payload: $"{{\"value\":\"{input}\"}}",
                        occurredAtUtc: DateTimeOffset.UtcNow,
                        isCompensation: true)
                ]));
        }
    }

    [AppBehavior("saga.choreography.local-only")]
    private sealed class LocalOnlyBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult($"local:{input}");
    }

    [AppBehavior("saga.choreography.reactor")]
    private sealed class ReactorBehavior : ISagaEventReactor<OrderPlaced>
    {
        public Task<SagaChoreographyStepResult> ReactAsync(
            OrderPlaced input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new SagaChoreographyStepResult(
                publications:
                [
                    SagaChoreographyPublication.CreateJson(
                        id: $"publication-{input.OrderId}",
                        channelId: "payments.events",
                        eventType: "payment-requested",
                        payload: new PaymentRequested(input.OrderId, input.Amount),
                        occurredAtUtc: new DateTimeOffset(2026, 4, 19, 0, 0, 0, TimeSpan.Zero),
                        metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["stage"] = "continuation"
                        })
                ]));
        }
    }

    [AppBehavior("saga.choreography.reactor.output")]
    private sealed class ReactorWithOutputBehavior : ISagaEventReactor<string, string>
    {
        public Task<SagaChoreographyStepResult<string>> ReactAsync(
            string input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new SagaChoreographyStepResult<string>(
                output: $"accepted:{input}",
                publications:
                [
                    SagaChoreographyPublication.CreateCompensationJson(
                        id: "publication-reactor-output",
                        channelId: "orders.events",
                        eventType: "order-cancelled",
                        payload: new OrderCancelled(input, "payment-timeout"),
                        occurredAtUtc: new DateTimeOffset(2026, 4, 19, 1, 0, 0, TimeSpan.Zero))
                ]));
        }
    }

    private static BehaviorExecutionContext MakeContext<TBehavior>(TBehavior behavior, object input, IBehaviorContext behaviorContext)
        where TBehavior : class
    {
        var descriptor = new BehaviorTopologyDescriptor(typeof(TBehavior).Name, "saga-choreography", ["in-memory"]);
        var slot = BehaviorExecutionTestSlots.For(behavior);
        return new BehaviorExecutionContext
        {
            Descriptor = descriptor,
            BehaviorInstance = behavior,
            Slot = slot,
            Input = input,
            BehaviorContext = behaviorContext
        };
    }

    private static ChoreographySagaExecutionStrategy MakeStrategy(ISagaChoreographyPublisher publisher)
        => new(publisher, NullLogger<ChoreographySagaExecutionStrategy>.Instance);

    [Fact]
    public async Task ChoreographySagaExecutionStrategy_SinglePublication_PublishesAndReturnsAccepted()
    {
        var publisher = new InMemorySagaChoreographyPublisher();
        var strategy = MakeStrategy(publisher);
        var behaviorContext = new TestBehaviorContext(
            "saga.choreography.single",
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["TenantId"] = "tenant-a"
            },
            correlationId: "corr-123");
        var executionContext = MakeContext(new SinglePublicationBehavior(), "value", behaviorContext);

        var result = await strategy.ExecuteAsync(executionContext);

        Assert.Equal(202, result.HttpStatusCode);
        Assert.False(result.IsFireAndForget);
        Assert.IsType<SagaChoreographyPublication>(result.Output);
        var published = Assert.Single(publisher.PublishedPublications);
        Assert.Equal("corr-123", published.CorrelationId);
        Assert.Equal("tenant-a", published.TenantId);
    }

    [Fact]
    public async Task ChoreographySagaExecutionStrategy_CombinedResult_PublishesAndPreservesLocalOutput()
    {
        var publisher = new InMemorySagaChoreographyPublisher();
        var strategy = MakeStrategy(publisher);
        var executionContext = MakeContext(
            new CombinedResultBehavior(),
            "checkout",
            new TestBehaviorContext("saga.choreography.combined", correlationId: "corr-456"));

        var result = await strategy.ExecuteAsync(executionContext);

        Assert.Equal(202, result.HttpStatusCode);
        Assert.Equal("accepted:checkout", result.Output);
        var published = Assert.Single(publisher.PublishedPublications);
        Assert.True(published.IsCompensation);
        Assert.Equal("corr-456", published.CorrelationId);
    }

    [Fact]
    public async Task ChoreographySagaExecutionStrategy_LocalOnlyOutput_Returns200WithoutPublication()
    {
        var publisher = new InMemorySagaChoreographyPublisher();
        var strategy = MakeStrategy(publisher);
        var executionContext = MakeContext(
            new LocalOnlyBehavior(),
            "checkout",
            new TestBehaviorContext("saga.choreography.local-only", correlationId: "corr-789"));

        var result = await strategy.ExecuteAsync(executionContext);

        Assert.Equal(200, result.HttpStatusCode);
        Assert.Equal("local:checkout", result.Output);
        Assert.Empty(publisher.PublishedPublications);
    }

    [Fact]
    public async Task ChoreographySagaExecutionStrategy_ReactorContract_PublishesTypedJsonPayload()
    {
        var publisher = new InMemorySagaChoreographyPublisher();
        var strategy = MakeStrategy(publisher);
        var behaviorContext = new TestBehaviorContext(
            "saga.choreography.reactor",
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["tenant-id"] = "tenant-reactor"
            },
            correlationId: "corr-reactor");
        var executionContext = MakeContext(
            new ReactorBehavior(),
            new OrderPlaced("order-42", 149.95m),
            behaviorContext);

        var result = await strategy.ExecuteAsync(executionContext);

        Assert.Equal(202, result.HttpStatusCode);
        Assert.Null(result.Output);

        var published = Assert.Single(publisher.PublishedPublications);
        Assert.Equal("application/json", published.ContentType);
        Assert.Equal("corr-reactor", published.CorrelationId);
        Assert.Equal("tenant-reactor", published.TenantId);
        Assert.Contains("\"orderId\":\"order-42\"", published.Payload, StringComparison.Ordinal);
        Assert.Contains("\"amount\":149.95", published.Payload, StringComparison.Ordinal);
        Assert.Equal("continuation", published.Metadata["stage"]);
    }

    [Fact]
    public async Task ChoreographySagaExecutionStrategy_TypedReactorOutput_PreservesOutputAndCompensationFlag()
    {
        var publisher = new InMemorySagaChoreographyPublisher();
        var strategy = MakeStrategy(publisher);
        var executionContext = MakeContext(
            new ReactorWithOutputBehavior(),
            "checkout-17",
            new TestBehaviorContext("saga.choreography.reactor.output", correlationId: "corr-reactor-output"));

        var result = await strategy.ExecuteAsync(executionContext);

        Assert.Equal(202, result.HttpStatusCode);
        Assert.Equal("accepted:checkout-17", result.Output);

        var published = Assert.Single(publisher.PublishedPublications);
        Assert.True(published.IsCompensation);
        Assert.Equal("application/json", published.ContentType);
        Assert.Equal("corr-reactor-output", published.CorrelationId);
        Assert.Contains("\"orderId\":\"checkout-17\"", published.Payload, StringComparison.Ordinal);
        Assert.Contains("\"reason\":\"payment-timeout\"", published.Payload, StringComparison.Ordinal);
    }

    [Fact]
    public void SagaChoreographyPublication_CreateCompensationJson_UsesJsonDefaults()
    {
        var publication = SagaChoreographyPublication.CreateCompensationJson(
            id: "publication-contract",
            channelId: "orders.events",
            eventType: "order-cancelled",
            payload: new OrderCancelled("order-77", "inventory-mismatch"),
            occurredAtUtc: new DateTimeOffset(2026, 4, 19, 2, 0, 0, TimeSpan.Zero));

        Assert.True(publication.IsCompensation);
        Assert.Equal("application/json", publication.ContentType);
        Assert.Contains("\"orderId\":\"order-77\"", publication.Payload, StringComparison.Ordinal);
        Assert.Contains("\"reason\":\"inventory-mismatch\"", publication.Payload, StringComparison.Ordinal);
    }

    [Fact]
    public void ChoreographySagaExecutionStrategy_Pattern_IsSagaChoreographyLiteral()
    {
        var strategy = MakeStrategy(new InMemorySagaChoreographyPublisher());

        Assert.Equal("saga-choreography", strategy.Pattern);
    }
}
