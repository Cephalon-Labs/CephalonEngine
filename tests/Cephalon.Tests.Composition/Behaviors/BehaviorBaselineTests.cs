using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Builders;
using Cephalon.Behaviors.Compatibility;
using Cephalon.Behaviors.Configuration;
using Cephalon.Behaviors.Services;
using Cephalon.Behaviors.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Behaviors;

public sealed class BehaviorBaselineTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Fixtures
    // ─────────────────────────────────────────────────────────────────────────

    [AppBehavior("greeting.direct")]
    private sealed class DirectGreetingBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult($"Hello, {input}!");
    }

    [AppBehavior("greeting.cqrs")]
    [BehaviorAllowedPatterns("cqrs", "direct")]
    private sealed class CqrsGreetingBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult($"CQRS: {input}");
    }

    [AppBehavior("greeting.restricted")]
    [BehaviorAllowedPatterns("cqrs", "direct")]
    private sealed class AllowlistViolatingBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(input);
    }

    [AppBehavior("greeting.no-allowlist")]
    private sealed class NoAllowlistBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(input);
    }

    [AppBehavior("orders.attribute-only-cqrs")]
    [BehaviorAllowedPatterns("cqrs")]
    [BehaviorAllowedTransports("http.jsonrpc", "http.grpc")]
    private sealed class AttributeOnlyCqrsBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(input);
    }

    [AppBehavior("orders.grpc-alias")]
    [BehaviorAllowedTransports("http.grpc")]
    private sealed class GrpcAliasAllowlistBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(input);
    }

    [AppBehavior("orders.attribute-only-ambiguous")]
    [BehaviorAllowedPatterns("cqrs", "event-driven")]
    [BehaviorAllowedTransports("http.jsonrpc")]
    private sealed class AttributeOnlyAmbiguousPatternBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(input);
    }

    [AppBehavior("orders.rest-annotation")]
    [BehaviorAllowedTransports("http.rest")]
    private sealed class RestAnnotationBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(input);
    }

    [AppBehavior("orders.rest-topology")]
    private sealed class RestTopologyBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(input);
    }

    private sealed class NoAttributeBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(input);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BehaviorTopologyBuilder / Via* transport IDs
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ViaTransportMethodsProduceCorrectIds()
    {
        var b = new BehaviorTopologyBuilder();
        b.ViaHttpJsonRpc().ViaRabbitMq().ViaKafka().ViaInMemory().ViaGrpc();
        var desc = b.Build("test");

        Assert.Contains("http.jsonrpc", desc.TransportIds);
        Assert.Contains("rabbitmq", desc.TransportIds);
        Assert.Contains("kafka", desc.TransportIds);
        Assert.Contains("in-memory", desc.TransportIds);
        Assert.Contains("grpc", desc.TransportIds);
    }

    [Fact]
    public void BehaviorTopologyBuilderAllTransportIdsAreCorrect()
    {
        var b = new BehaviorTopologyBuilder();
        b.ViaHttpJsonRpc()
         .ViaHttpGraphQl()
         .ViaHttpGraphQlSse()
         .ViaHttpGraphQlWs()
         .ViaHttpSse()
         .ViaWebSocket()
         .ViaRabbitMq()
         .ViaKafka()
         .ViaInMemory()
         .ViaGrpc();

        var desc = b.Build("all-transports");

        Assert.Contains("http.jsonrpc", desc.TransportIds);
        Assert.Contains("http.graphql", desc.TransportIds);
        Assert.Contains("http.graphql-sse", desc.TransportIds);
        Assert.Contains("http.graphql-ws", desc.TransportIds);
        Assert.Contains("http.sse", desc.TransportIds);
        Assert.Contains("http.ws", desc.TransportIds);
        Assert.Contains("rabbitmq", desc.TransportIds);
        Assert.Contains("kafka", desc.TransportIds);
        Assert.Contains("in-memory", desc.TransportIds);
        Assert.Contains("grpc", desc.TransportIds);
        Assert.Equal(10, desc.TransportIds.Count);
    }

    [Fact]
    public void BehaviorTopologyBuilderSetsDefaultPatternToDirect()
    {
        var desc = new BehaviorTopologyBuilder().Build("x");
        Assert.Equal("direct", desc.Pattern);
    }

    [Fact]
    public void BehaviorTopologyBuilderSupportsSagaChoreographyPattern()
    {
        var desc = new BehaviorTopologyBuilder()
            .AsSagaChoreography()
            .Build("x");

        Assert.Equal("saga-choreography", desc.Pattern);
    }

    [Fact]
    public void BehaviorTopologyBuilderSupportsDurableExecutionPattern()
    {
        var desc = new BehaviorTopologyBuilder()
            .AsDurableExecution()
            .Build("x");

        Assert.Equal("durable-execution", desc.Pattern);
    }

    // BehaviorAllowlistValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BehaviorAllowlistValidatorPassesWhenPatternInAllowlist()
    {
        var desc = new BehaviorTopologyDescriptor("greeting.cqrs", "cqrs", []);
        BehaviorAllowlistValidator.Validate(desc, typeof(CqrsGreetingBehavior));
    }

    [Fact]
    public void BehaviorAllowlistValidatorThrowsWhenPatternNotInAllowlist()
    {
        // AllowlistViolatingBehavior allows ["cqrs","direct"] but resolved pattern is "event-driven"
        var desc = new BehaviorTopologyDescriptor("greeting.restricted", "event-driven", []);

        Assert.Throws<BehaviorSecurityException>(() =>
            BehaviorAllowlistValidator.Validate(desc, typeof(AllowlistViolatingBehavior)));
    }

    [Fact]
    public void BehaviorAllowlistValidatorPassesWhenNoAllowlistDefined()
    {
        var desc = new BehaviorTopologyDescriptor("greeting.no-allowlist", "saga-step", []);
        BehaviorAllowlistValidator.Validate(desc, typeof(NoAllowlistBehavior));
    }

    [Fact]
    public void BehaviorAllowlistValidatorPassesWhenBehaviorTypeIsNull()
    {
        var desc = new BehaviorTopologyDescriptor("x", "direct", []);
        BehaviorAllowlistValidator.Validate(desc, null);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Compatibility matrix — ABT-001
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CompatibilityMatrixAbt001ErrorSagaStepWithNoStatefulTransport()
    {
        var rule = new Abt001SagaStepStatefulTransportRule();
        var desc = new BehaviorTopologyDescriptor("s", "saga-step", ["http.jsonrpc"]);
        var violation = rule.Check(desc);

        Assert.NotNull(violation);
        Assert.Equal(CompatibilitySeverity.Error, violation!.Severity);
        Assert.Equal("ABT-001", violation.RuleId);
    }

    [Fact]
    public void CompatibilityMatrixAbt001PassesSagaStepWithRabbitMq()
    {
        var rule = new Abt001SagaStepStatefulTransportRule();
        var desc = new BehaviorTopologyDescriptor("s", "saga-step", ["rabbitmq"]);
        Assert.Null(rule.Check(desc));
    }

    [Fact]
    public void CompatibilityMatrixAbt001PassesSagaStepWithKafka()
    {
        var rule = new Abt001SagaStepStatefulTransportRule();
        var desc = new BehaviorTopologyDescriptor("s", "saga-step", ["kafka"]);
        Assert.Null(rule.Check(desc));
    }

    [Fact]
    public void CompatibilityMatrixAbt001PassesSagaStepWithInMemory()
    {
        var rule = new Abt001SagaStepStatefulTransportRule();
        var desc = new BehaviorTopologyDescriptor("s", "saga-step", ["in-memory"]);
        Assert.Null(rule.Check(desc));
    }

    [Fact]
    public void CompatibilityMatrixAbt001PassesNonSagaPattern()
    {
        var rule = new Abt001SagaStepStatefulTransportRule();
        var desc = new BehaviorTopologyDescriptor("s", "cqrs", ["http.jsonrpc"]);
        Assert.Null(rule.Check(desc));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BehaviorDispatcher — end-to-end dispatch
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BehaviorDispatcherDispatchesKnownBehavior()
    {
        var services = new ServiceCollection();
        services.AddTransient<DirectGreetingBehavior>();

        var typeRegistry = new BehaviorTypeRegistry();
        typeRegistry.Register("greeting.direct", typeof(DirectGreetingBehavior));
        var slotRegistry = new BehaviorExecutionSlotRegistry();
        slotRegistry.Register(
            "greeting.direct",
            typeof(DirectGreetingBehavior),
            BehaviorExecutionSlot.For<DirectGreetingBehavior, string, string>());

        var descriptor = new BehaviorTopologyDescriptor("greeting.direct", "direct", []);
        var contributor = new FluentBehaviorContributor(descriptor);
        services.AddSingleton<IBehaviorContributor>(contributor);
        services.AddSingleton<IBehaviorTypeRegistry>(typeRegistry);
        services.AddSingleton(slotRegistry);
        services.AddSingleton<IBehaviorCatalog>(sp =>
            new BehaviorCatalog(sp.GetServices<IBehaviorContributor>()));

        var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IBehaviorCatalog>();
        var dispatcher = new BehaviorDispatcher(catalog, typeRegistry, provider);

        var ctx = new TestBehaviorContext("greeting.direct", isDirect: true);
        var result = await dispatcher.DispatchAsync("greeting.direct", "World", ctx);

        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void BehaviorDispatcherThrowsWhenExecutionSlotIsMissing()
    {
        var services = new ServiceCollection();
        services.AddTransient<DirectGreetingBehavior>();

        var typeRegistry = new BehaviorTypeRegistry();
        typeRegistry.Register("greeting.direct", typeof(DirectGreetingBehavior));

        var descriptor = new BehaviorTopologyDescriptor("greeting.direct", "direct", []);
        var contributor = new FluentBehaviorContributor(descriptor);
        services.AddSingleton<IBehaviorContributor>(contributor);
        services.AddSingleton<IBehaviorTypeRegistry>(typeRegistry);
        services.AddSingleton<IBehaviorCatalog>(sp =>
            new BehaviorCatalog(sp.GetServices<IBehaviorContributor>()));

        var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IBehaviorCatalog>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new BehaviorDispatcher(catalog, typeRegistry, provider));

        Assert.Contains("no source-generated or explicitly registered BehaviorExecutionSlot", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Register<TBehavior, TInput, TOutput>", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BehaviorDispatcherThrowsBehaviorNotFoundExceptionForUnknownId()
    {
        var services = new ServiceCollection();
        var typeRegistry = new BehaviorTypeRegistry();
        services.AddSingleton<IBehaviorCatalog>(new BehaviorCatalog([]));

        var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IBehaviorCatalog>();
        var dispatcher = new BehaviorDispatcher(catalog, typeRegistry, provider);

        var ctx = new TestBehaviorContext("missing");
        await Assert.ThrowsAsync<BehaviorNotFoundException>(() =>
            dispatcher.DispatchAsync("missing", "input", ctx));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TestBehaviorContext — direct pattern throws NotSupportedException
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TestBehaviorContextReplyAsyncThrowsNotSupportedExceptionForDirectPattern()
    {
        var ctx = new TestBehaviorContext("greeting.direct", isDirect: true);
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            ctx.ReplyAsync("reply"));
    }

    [Fact]
    public async Task TestBehaviorContextReplyAsyncSucceedsForNonDirectPattern()
    {
        var ctx = new TestBehaviorContext("greeting.cqrs", isDirect: false);
        await ctx.ReplyAsync("reply");
        Assert.Single(ctx.Replies);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BehaviorExecutionSlot — closed generic factory
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BehaviorExecutionSlotForCompilesAndInvokes()
    {
        var slot = BehaviorExecutionSlot.For<DirectGreetingBehavior, string, string>();
        var behavior = new DirectGreetingBehavior();
        var ctx = new TestBehaviorContext("greeting.direct", isDirect: true);

        var result = await slot.InvokeAsync(behavior, "Claude", ctx);
        Assert.Equal("Hello, Claude!", result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BehaviorCatalog
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BehaviorCatalogReturnsAllContributedOrderedById()
    {
        var d1 = new BehaviorTopologyDescriptor("z-behavior", "direct", []);
        var d2 = new BehaviorTopologyDescriptor("a-behavior", "cqrs", []);
        var catalog = new BehaviorCatalog(
        [
            new FluentBehaviorContributor(d1),
            new FluentBehaviorContributor(d2)
        ]);

        Assert.Equal(2, catalog.All.Count);
        Assert.Equal("a-behavior", catalog.All[0].Id);
        Assert.Equal("z-behavior", catalog.All[1].Id);
    }

    [Fact]
    public void BehaviorCatalogFindByIdReturnsNullForUnknownId()
    {
        var catalog = new BehaviorCatalog([]);
        Assert.Null(catalog.FindById("unknown"));
    }

    [Fact]
    public void BehaviorCatalogFindByIdIsCaseInsensitive()
    {
        var desc = new BehaviorTopologyDescriptor("My-Behavior", "direct", []);
        var catalog = new BehaviorCatalog([new FluentBehaviorContributor(desc)]);

        Assert.NotNull(catalog.FindById("MY-BEHAVIOR"));
        Assert.NotNull(catalog.FindById("my-behavior"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AppBehaviorAttribute
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AppBehaviorAttributeStoresId()
    {
        var attr = new AppBehaviorAttribute("my.behavior");
        Assert.Equal("my.behavior", attr.Id);
    }

    [Fact]
    public void AppBehaviorAttributeThrowsWhenIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new AppBehaviorAttribute(""));
        Assert.Throws<ArgumentException>(() => new AppBehaviorAttribute("  "));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BehaviorCollectionBuilder — Register wires up DI + type registry
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BehaviorCollectionBuilderRegisterWiresDiAndTypeRegistry()
    {
        var services = new ServiceCollection();
        var typeRegistry = new BehaviorTypeRegistry();
        var builder = new BehaviorCollectionBuilder(services, typeRegistry);

        builder.Register<DirectGreetingBehavior>();

        Assert.True(typeRegistry.TryGetType("greeting.direct", out var type));
        Assert.Equal(typeof(DirectGreetingBehavior), type);

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetService<DirectGreetingBehavior>();
        Assert.NotNull(resolved);
    }

    [Fact]
    public async Task BehaviorCollectionBuilderTypedRegisterWiresExecutionSlot()
    {
        var services = new ServiceCollection();
        var typeRegistry = new BehaviorTypeRegistry();
        var builder = new BehaviorCollectionBuilder(services, typeRegistry);

        builder.Register<DirectGreetingBehavior, string, string>();

        services.AddSingleton<IBehaviorTypeRegistry>(typeRegistry);
        services.AddSingleton<IBehaviorCatalog>(sp =>
            new BehaviorCatalog(sp.GetServices<IBehaviorContributor>()));

        var provider = services.BuildServiceProvider();
        var dispatcher = new BehaviorDispatcher(
            provider.GetRequiredService<IBehaviorCatalog>(),
            typeRegistry,
            provider);
        var ctx = new TestBehaviorContext("greeting.direct", isDirect: true);

        var result = await dispatcher.DispatchAsync("greeting.direct", "Builder", ctx);

        Assert.Equal("Hello, Builder!", result);
    }

    [Fact]
    public void BehaviorCollectionBuilderRegisterThrowsWhenNoAppBehaviorAttribute()
    {
        var services = new ServiceCollection();
        var typeRegistry = new BehaviorTypeRegistry();
        var builder = new BehaviorCollectionBuilder(services, typeRegistry);

        Assert.Throws<InvalidOperationException>(() =>
            builder.Register<NoAttributeBehavior>());
    }

    [Fact]
    public void BehaviorCollectionBuilderRegisterWithFluentTopologyAddsContributor()
    {
        var services = new ServiceCollection();
        var typeRegistry = new BehaviorTypeRegistry();
        var builder = new BehaviorCollectionBuilder(services, typeRegistry);

        builder.Register<DirectGreetingBehavior>(b => b.ViaHttpJsonRpc().ViaInMemory());

        var provider = services.BuildServiceProvider();
        var contributors = provider.GetServices<IBehaviorContributor>().ToList();
        Assert.NotEmpty(contributors);

        var catalog = new BehaviorCatalog(contributors);
        var desc = catalog.FindById("greeting.direct");
        Assert.NotNull(desc);
        Assert.Contains("http.jsonrpc", desc!.TransportIds);
        Assert.Contains("in-memory", desc.TransportIds);
    }

    [Fact]
    public void BehaviorCollectionBuilderRegisterThrowsWhenRestIsDeclaredByAttribute()
    {
        var services = new ServiceCollection();
        var typeRegistry = new BehaviorTypeRegistry();
        var builder = new BehaviorCollectionBuilder(services, typeRegistry);

        var exception = Assert.Throws<BehaviorSecurityException>(() =>
            builder.Register<RestAnnotationBehavior>());

        Assert.Contains("module-owned only", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BehaviorCollectionBuilderRegisterUsesSingleAllowedPatternAndDeclaredTransportsWhenNoTopologyExists()
    {
        var services = new ServiceCollection();
        var typeRegistry = new BehaviorTypeRegistry();
        var builder = new BehaviorCollectionBuilder(services, typeRegistry);

        builder.Register<AttributeOnlyCqrsBehavior>();

        var provider = services.BuildServiceProvider();
        var contributors = provider.GetServices<IBehaviorContributor>().ToList();
        var catalog = new BehaviorCatalog(contributors);
        var descriptor = catalog.FindById("orders.attribute-only-cqrs");

        Assert.NotNull(descriptor);
        Assert.Equal("cqrs", descriptor!.Pattern);
        Assert.Contains("grpc", descriptor.TransportIds);
        Assert.Contains("http.jsonrpc", descriptor.TransportIds);
    }

    [Fact]
    public void BehaviorCollectionBuilderRegisterThrowsWhenMultipleAllowedPatternsNeedAnExplicitSelection()
    {
        var services = new ServiceCollection();
        var typeRegistry = new BehaviorTypeRegistry();
        var builder = new BehaviorCollectionBuilder(services, typeRegistry);

        var exception = Assert.Throws<BehaviorSecurityException>(() =>
            builder.Register<AttributeOnlyAmbiguousPatternBehavior>());

        Assert.Contains("multiple allowed patterns", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ConfigureTopology", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BehaviorCollectionBuilderRegisterThrowsWhenRestIsDeclaredByTopology()
    {
        var services = new ServiceCollection();
        var typeRegistry = new BehaviorTypeRegistry();
        var builder = new BehaviorCollectionBuilder(services, typeRegistry);

        var restDescriptor = new BehaviorTopologyDescriptor("orders.rest-topology", "direct", ["http.rest"]);
        var exception = Assert.Throws<BehaviorSecurityException>(() =>
            BehaviorAttributeTopologyResolver.Resolve("orders.rest-topology", typeof(RestTopologyBehavior), restDescriptor));

        Assert.Contains("MapBehaviorRestGroup", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BehaviorCollectionBuilderRegisterAcceptsHttpGrpcAliasWhenTopologyUsesGrpc()
    {
        var services = new ServiceCollection();
        var typeRegistry = new BehaviorTypeRegistry();
        var builder = new BehaviorCollectionBuilder(services, typeRegistry);

        builder.Register<GrpcAliasAllowlistBehavior>(topology => topology.ViaGrpc());

        var provider = services.BuildServiceProvider();
        var contributors = provider.GetServices<IBehaviorContributor>().ToList();
        var catalog = new BehaviorCatalog(contributors);
        var descriptor = catalog.FindById("orders.grpc-alias");

        Assert.NotNull(descriptor);
        Assert.Contains("grpc", descriptor!.TransportIds);
    }

    [Fact]
    public void BehaviorCollectionBuilderRegisterSupportsAttributeOnlyNonRestTopology()
    {
        var services = new ServiceCollection();
        var typeRegistry = new BehaviorTypeRegistry();
        var builder = new BehaviorCollectionBuilder(services, typeRegistry);

        builder.Register<AttributeOnlyCqrsBehavior>();

        var provider = services.BuildServiceProvider();
        var contributors = provider.GetServices<IBehaviorContributor>().ToList();
        var catalog = new BehaviorCatalog(contributors);
        var descriptor = catalog.FindById("orders.attribute-only-cqrs");

        Assert.NotNull(descriptor);
        Assert.Contains("http.jsonrpc", descriptor!.TransportIds);
        Assert.Contains("grpc", descriptor.TransportIds);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // M1 coverage gap — 5 additional tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BehaviorCatalogGetByPatternReturnsCqrsDescriptor()
    {
        var d = new BehaviorTopologyDescriptor("greeting.cqrs", "cqrs", ["http.jsonrpc"]);
        var catalog = new BehaviorCatalog([new FluentBehaviorContributor(d)]);

        var results = catalog.GetByPattern("cqrs");

        Assert.Single(results);
        Assert.Equal("greeting.cqrs", results[0].Id);
    }

    [Fact]
    public void BehaviorCatalogGetByTransportReturnsJsonRpcDescriptor()
    {
        var d = new BehaviorTopologyDescriptor("greeting.direct", "direct", ["http.jsonrpc"]);
        var catalog = new BehaviorCatalog([new FluentBehaviorContributor(d)]);

        var results = catalog.GetByTransport("http.jsonrpc");

        Assert.Single(results);
        Assert.Equal("greeting.direct", results[0].Id);
    }

    [Fact]
    public void BehaviorAllowlistValidatorThrowsWhenPatternViolatesAllowlist()
    {
        // AllowlistViolatingBehavior declares [BehaviorAllowedPatterns("cqrs","direct")]
        // but the resolved descriptor uses "saga-step" — must throw BehaviorSecurityException.
        var desc = new BehaviorTopologyDescriptor("greeting.restricted", "saga-step", []);

        Assert.Throws<BehaviorSecurityException>(() =>
            BehaviorAllowlistValidator.Validate(desc, typeof(AllowlistViolatingBehavior)));
    }

    [Fact]
    public void CompatibilityMatrixAbt003ProcessManagerWithoutInboxReturnsError()
    {
        var rule = new Abt003ProcessManagerRequiresInboxRule();
        // InboxEnabled defaults to false
        var desc = new BehaviorTopologyDescriptor("pm.order", "process-manager", ["rabbitmq"]);

        var violation = rule.Check(desc);

        Assert.NotNull(violation);
        Assert.Equal(CompatibilitySeverity.Error, violation!.Severity);
        Assert.Equal("ABT-003", violation.RuleId);
    }

    [Fact]
    public void CompatibilityMatrixAbt004CqrsMultipleTransportsReturnsAdvisory()
    {
        var rule = new Abt004CqrsMultipleTransportsRule();
        var desc = new BehaviorTopologyDescriptor("order.query", "cqrs", ["http.jsonrpc", "grpc"]);

        var violation = rule.Check(desc);

        Assert.NotNull(violation);
        Assert.Equal(CompatibilitySeverity.Advisory, violation!.Severity);
        Assert.Equal("ABT-004", violation.RuleId);
    }

    [Fact]
    public void CompatibilityMatrixAbt005SagaChoreographyWithoutOutboxReturnsAdvisory()
    {
        var rule = new Abt005SagaChoreographyOutboxRule();
        var desc = new BehaviorTopologyDescriptor("order.fulfillment", "saga-choreography", ["rabbitmq"]);

        var violation = rule.Check(desc);

        Assert.NotNull(violation);
        Assert.Equal(CompatibilitySeverity.Advisory, violation!.Severity);
        Assert.Equal("ABT-005", violation.RuleId);
    }

    [Fact]
    public void CompatibilityMatrixAbt006DurableExecutionWithoutEventSourcingReturnsError()
    {
        var rule = new Abt006DurableExecutionRequiresEventSourcingRule();
        var desc = new BehaviorTopologyDescriptor("order.workflow", "durable-execution", ["rabbitmq"]);

        var violation = rule.Check(desc);

        Assert.NotNull(violation);
        Assert.Equal(CompatibilitySeverity.Error, violation!.Severity);
        Assert.Equal("ABT-006", violation.RuleId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BehaviorOptions — config binding from IConfiguration
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BehaviorOptionsDefaultsAutoRegisterToFalse()
    {
        var options = new BehaviorOptions();
        Assert.False(options.AutoRegister);
    }

    [Fact]
    public void BehaviorOptionsBindsAutoRegisterFalseFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Behaviors:AutoRegister"] = "false"
            })
            .Build();

        var options = new BehaviorOptions();
        config.GetSection("Engine:Behaviors").Bind(options);

        Assert.False(options.AutoRegister);
    }

    [Fact]
    public void BehaviorOptionsBindsAutoRegisterTrueFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Behaviors:AutoRegister"] = "true"
            })
            .Build();

        var options = new BehaviorOptions();
        config.GetSection("Engine:Behaviors").Bind(options);

        Assert.True(options.AutoRegister);
    }

    [Fact]
    public void BehaviorOptionsBindsExcludeAssemblyPrefixesFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Behaviors:AutoRegisterExcludeAssemblyPrefixes:0"] = "MyCompany.Shared.",
                ["Engine:Behaviors:AutoRegisterExcludeAssemblyPrefixes:1"] = "ThirdParty."
            })
            .Build();

        var options = new BehaviorOptions();
        config.GetSection("Engine:Behaviors").Bind(options);

        Assert.Equal(2, options.AutoRegisterExcludeAssemblyPrefixes.Count);
        Assert.Contains("MyCompany.Shared.", options.AutoRegisterExcludeAssemblyPrefixes);
        Assert.Contains("ThirdParty.", options.AutoRegisterExcludeAssemblyPrefixes);
    }

    [Fact]
    public void BehaviorOptionsBindsAutoRegisterAssembliesFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Behaviors:AutoRegisterAssemblies:0"] = "MyApp.Domain",
                ["Engine:Behaviors:AutoRegisterAssemblies:1"] = "MyApp.Orders"
            })
            .Build();

        var options = new BehaviorOptions();
        config.GetSection("Engine:Behaviors").Bind(options);

        Assert.Equal(2, options.AutoRegisterAssemblies.Count);
        Assert.Contains("MyApp.Domain", options.AutoRegisterAssemblies);
        Assert.Contains("MyApp.Orders", options.AutoRegisterAssemblies);
    }

    [Fact]
    public void BehaviorOptionsCodeOverrideWinsOverConfig()
    {
        // Config says AutoRegister = true
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Behaviors:AutoRegister"] = "true"
            })
            .Build();

        var options = new BehaviorOptions();
        config.GetSection("Engine:Behaviors").Bind(options);

        // Code override says false → code wins
        options.AutoRegister = false;

        Assert.False(options.AutoRegister);
    }

    [Fact]
    public void BehaviorOptionsAutoRegisterFalseReturnsEmptyAssemblies()
    {
        var options = new BehaviorOptions { AutoRegister = false };
        var assemblies = options.ResolveAutoRegisterAssemblies();

        Assert.Empty(assemblies);
    }
}
