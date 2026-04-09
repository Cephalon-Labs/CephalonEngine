using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Builders;
using Cephalon.Behaviors.Http.Hosting;
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

    [AppBehavior("orders.annotation-rest")]
    [BehaviorAllowedTransports("http.rest")]
    private sealed class AnnotationDrivenRestBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(input);
    }

    [AppBehavior("orders.duplicate-rest")]
    [BehaviorAllowedTransports("http.rest")]
    private sealed class DuplicateRestDeclarationBehavior : IAppBehavior<string, string>
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
        b.ViaHttpRest().ViaRabbitMq().ViaKafka().ViaInMemory().ViaGrpc();
        var desc = b.Build("test");

        Assert.Contains("http.rest", desc.TransportIds);
        Assert.Contains("rabbitmq", desc.TransportIds);
        Assert.Contains("kafka", desc.TransportIds);
        Assert.Contains("in-memory", desc.TransportIds);
        Assert.Contains("grpc", desc.TransportIds);
    }

    [Fact]
    public void BehaviorTopologyBuilderAllTransportIdsAreCorrect()
    {
        var b = new BehaviorTopologyBuilder();
        b.ViaHttpRest()
         .ViaHttpJsonRpc()
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

        Assert.Contains("http.rest", desc.TransportIds);
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
        Assert.Equal(11, desc.TransportIds.Count);
    }

    [Fact]
    public void BehaviorTopologyBuilderSetsDefaultPatternToDirect()
    {
        var desc = new BehaviorTopologyBuilder().Build("x");
        Assert.Equal("direct", desc.Pattern);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BehaviorTopologyResolver — 4-layer priority
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BehaviorTopologyResolverInheritsDefaultsWhenBehaviorEntryEmpty()
    {
        var options = new BehaviorOptions
        {
            BehaviorDefaults = new BehaviorDefaultsOptions
            {
                Pattern = "cqrs",
                Transport = ["http.rest"]
            },
            Behaviors = new Dictionary<string, BehaviorEntryOptions>(StringComparer.OrdinalIgnoreCase)
            {
                // Entry exists but has no pattern or transport overrides
                ["my-behavior"] = new BehaviorEntryOptions()
            }
        };

        var resolver = new BehaviorTopologyResolver(options, new Dictionary<string, BehaviorTopologyDescriptor>());
        var desc = resolver.Resolve("my-behavior");

        Assert.Equal("cqrs", desc.Pattern);
        Assert.Contains("http.rest", desc.TransportIds);
    }

    [Fact]
    public void BehaviorTopologyResolverOverridesTransportWhenBehaviorEntrySpecifiesTransport()
    {
        var options = new BehaviorOptions
        {
            BehaviorDefaults = new BehaviorDefaultsOptions
            {
                Transport = ["http.rest"]
            },
            Behaviors = new Dictionary<string, BehaviorEntryOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["my-behavior"] = new BehaviorEntryOptions { Transport = ["rabbitmq"] }
            }
        };

        var resolver = new BehaviorTopologyResolver(options, new Dictionary<string, BehaviorTopologyDescriptor>());
        var desc = resolver.Resolve("my-behavior");

        Assert.Contains("rabbitmq", desc.TransportIds);
        Assert.DoesNotContain("http.rest", desc.TransportIds);
    }

    [Fact]
    public void BehaviorTopologyResolverFluentOverrideWinsOverConfig()
    {
        var options = new BehaviorOptions
        {
            BehaviorDefaults = new BehaviorDefaultsOptions { Pattern = "cqrs" }
        };

        var fluentDescriptor = new BehaviorTopologyDescriptor("my-behavior", "event-driven", ["rabbitmq"]);
        var fluentOverrides = new Dictionary<string, BehaviorTopologyDescriptor>(StringComparer.OrdinalIgnoreCase)
        {
            ["my-behavior"] = fluentDescriptor
        };

        var resolver = new BehaviorTopologyResolver(options, fluentOverrides);
        var desc = resolver.Resolve("my-behavior");

        Assert.Equal("event-driven", desc.Pattern);
        Assert.Contains("rabbitmq", desc.TransportIds);
    }

    [Fact]
    public void BehaviorTopologyResolverUsesCompiledDefaultsWhenNoConfig()
    {
        var options = new BehaviorOptions();
        var resolver = new BehaviorTopologyResolver(options, new Dictionary<string, BehaviorTopologyDescriptor>());
        var desc = resolver.Resolve("unknown");

        Assert.Equal("direct", desc.Pattern);
        Assert.Empty(desc.TransportIds);
    }

    // ─────────────────────────────────────────────────────────────────────────
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
        var desc = new BehaviorTopologyDescriptor("s", "saga-step", ["http.rest"]);
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
        var desc = new BehaviorTopologyDescriptor("s", "cqrs", ["http.rest"]);
        Assert.Null(rule.Check(desc));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Compatibility matrix — ABT-002
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CompatibilityMatrixAbt002WarningEventDrivenWithHttpRest()
    {
        var rule = new Abt002EventDrivenWithHttpRestRule();
        var desc = new BehaviorTopologyDescriptor("e", "event-driven", ["http.rest"]);
        var v = rule.Check(desc);

        Assert.NotNull(v);
        Assert.Equal(CompatibilitySeverity.Warning, v!.Severity);
    }

    [Fact]
    public void CompatibilityMatrixAbt002NoViolationEventDrivenWithSse()
    {
        var rule = new Abt002EventDrivenWithHttpRestRule();
        var desc = new BehaviorTopologyDescriptor("e", "event-driven", ["http.sse"]);
        Assert.Null(rule.Check(desc));
    }

    [Fact]
    public void CompatibilityMatrixAbt002NoViolationCqrsWithHttpRest()
    {
        var rule = new Abt002EventDrivenWithHttpRestRule();
        var desc = new BehaviorTopologyDescriptor("e", "cqrs", ["http.rest"]);
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

        var descriptor = new BehaviorTopologyDescriptor("greeting.direct", "direct", []);
        var contributor = new FluentBehaviorContributor(descriptor);
        services.AddSingleton<IBehaviorContributor>(contributor);
        services.AddSingleton<IBehaviorTypeRegistry>(typeRegistry);
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
    // BehaviorExecutionSlot — ForType factory
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BehaviorExecutionSlotForTypeCompilesAndInvokes()
    {
        var slot = BehaviorExecutionSlot.ForType(typeof(DirectGreetingBehavior));
        var behavior = new DirectGreetingBehavior();
        var ctx = new TestBehaviorContext("greeting.direct", isDirect: true);

        var result = await slot.InvokeAsync(behavior, "Claude", ctx);
        Assert.Equal("Hello, Claude!", result);
    }

    [Fact]
    public void BehaviorExecutionSlotForTypeThrowsWhenTypeDoesNotImplementInterface()
    {
        Assert.Throws<InvalidOperationException>(() =>
            BehaviorExecutionSlot.ForType(typeof(string)));
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

        builder.Register<DirectGreetingBehavior>(b => b.ViaHttpRest().ViaInMemory());

        var provider = services.BuildServiceProvider();
        var contributors = provider.GetServices<IBehaviorContributor>().ToList();
        Assert.NotEmpty(contributors);

        var catalog = new BehaviorCatalog(contributors);
        var desc = catalog.FindById("greeting.direct");
        Assert.NotNull(desc);
        Assert.Contains("http.rest", desc!.TransportIds);
        Assert.Contains("in-memory", desc.TransportIds);
    }

    [Fact]
    public void BehaviorCollectionBuilderRegisterAutoActivatesRestWhenAnnotationDeclaresHttpRest()
    {
        var services = new ServiceCollection();
        var typeRegistry = new BehaviorTypeRegistry();
        var builder = new BehaviorCollectionBuilder(services, typeRegistry);

        builder.Register<AnnotationDrivenRestBehavior>();

        var provider = services.BuildServiceProvider();
        var contributors = provider.GetServices<IBehaviorContributor>().ToList();
        var catalog = new BehaviorCatalog(contributors);
        var descriptor = catalog.FindById("orders.annotation-rest");

        Assert.NotNull(descriptor);
        Assert.Contains("http.rest", descriptor!.TransportIds);
    }

    [Fact]
    public void BehaviorCollectionBuilderRegisterThrowsWhenRestIsDeclaredByAttributeAndFluentTopology()
    {
        var services = new ServiceCollection();
        var typeRegistry = new BehaviorTypeRegistry();
        var builder = new BehaviorCollectionBuilder(services, typeRegistry);

        Assert.Throws<BehaviorSecurityException>(() =>
            builder.Register<DuplicateRestDeclarationBehavior>(topology => topology.ViaHttpRest()));
    }

    [Fact]
    public void BehaviorCollectionBuilderRegisterSupportsExplicitGenericRestContract()
    {
        var services = new ServiceCollection();
        var typeRegistry = new BehaviorTypeRegistry();
        var builder = new BehaviorCollectionBuilder(services, typeRegistry);

        builder.Register<DirectGreetingBehavior>(topology => topology
            .ViaHttpRest(rest => rest
                .MapGet("greetings/{name}")
                .BindRoute("name", "input")));

        var provider = services.BuildServiceProvider();
        var contributors = provider.GetServices<IBehaviorContributor>().ToList();
        var catalog = new BehaviorCatalog(contributors);
        var descriptor = catalog.FindById("greeting.direct");

        Assert.NotNull(descriptor);
        Assert.Contains("http.rest", descriptor!.TransportIds);
        Assert.Equal("GET", descriptor.Metadata["cephalon.http.rest.method"]);
        Assert.Equal("greetings/{name}", descriptor.Metadata["cephalon.http.rest.route-template"]);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // M1 coverage gap — 5 additional tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BehaviorCatalogGetByPatternReturnsCqrsDescriptor()
    {
        var d = new BehaviorTopologyDescriptor("greeting.cqrs", "cqrs", ["http.rest"]);
        var catalog = new BehaviorCatalog([new FluentBehaviorContributor(d)]);

        var results = catalog.GetByPattern("cqrs");

        Assert.Single(results);
        Assert.Equal("greeting.cqrs", results[0].Id);
    }

    [Fact]
    public void BehaviorCatalogGetByTransportReturnsHttpRestDescriptor()
    {
        var d = new BehaviorTopologyDescriptor("greeting.direct", "direct", ["http.rest"]);
        var catalog = new BehaviorCatalog([new FluentBehaviorContributor(d)]);

        var results = catalog.GetByTransport("http.rest");

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
        var desc = new BehaviorTopologyDescriptor("order.query", "cqrs", ["http.rest", "grpc"]);

        var violation = rule.Check(desc);

        Assert.NotNull(violation);
        Assert.Equal(CompatibilitySeverity.Advisory, violation!.Severity);
        Assert.Equal("ABT-004", violation.RuleId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BehaviorOptions — config binding from IConfiguration
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BehaviorOptionsDefaultsAutoRegisterToTrue()
    {
        var options = new BehaviorOptions();
        Assert.True(options.AutoRegister);
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
