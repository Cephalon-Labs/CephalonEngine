using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Modules;
using Cephalon.Behaviors.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Sample.Showcase.Domain.Cart.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Behaviors;

public sealed class BehaviorOwnerModuleTests
{
    [Fact]
    public async Task BehaviorModuleBaseRegistersOwnedBehaviorsWhenAutoRegisterIsDisabled()
    {
        var services = new ServiceCollection();
        var builder = new EngineBuilder(services);
        builder.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
        builder.AddBehaviors(options => options.AutoRegister = false);
        builder.AddModule(new OwnedGreetingModule());

        builder.Build();

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<BehaviorDispatcher>();

        var result = await dispatcher.DispatchAsync(
            "tests.owned.greeting",
            "Cephalon",
            new TestBehaviorContext("tests.owned.greeting", isDirect: true));

        Assert.Equal("Owned hello, Cephalon!", result);
    }

    [Fact]
    public void EngineBuilderRejectsBehaviorOwnershipAcrossMultipleModules()
    {
        var services = new ServiceCollection();
        var builder = new EngineBuilder(services);
        builder.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
        builder.AddBehaviors(options => options.AutoRegister = false);
        builder.AddModule(new OwnedGreetingModule());
        builder.AddModule(new DuplicateGreetingOwnerModule());

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("owned by multiple modules", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tests.owned.greeting", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BehaviorModuleBaseOwnedTopologyKeepsPrecedenceWhenAutoRegisterIsEnabled()
    {
        var services = new ServiceCollection();
        var builder = new EngineBuilder(services);
        builder.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
        builder.AddBehaviors(configureOptions: options =>
        {
            options.AutoRegister = true;
            options.AutoRegisterAssemblies = [typeof(GetCartBehavior).Assembly.GetName().Name!];
        });
        builder.AddModule(new ShowcaseOwnedBehaviorModule());

        builder.Build();

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IBehaviorCatalog>();
        var descriptor = catalog.FindById("cart.get");

        Assert.NotNull(descriptor);
        Assert.Equal("cqrs", descriptor!.Pattern);
        Assert.Contains("http.ws", descriptor.TransportIds, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("http.graphql", descriptor.TransportIds, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("http.sse", descriptor.TransportIds, StringComparer.OrdinalIgnoreCase);
    }

    [AppBehavior("tests.owned.greeting")]
    [BehaviorAllowedPatterns("direct")]
    [BehaviorAllowedTransports("http.jsonrpc", "http.sse")]
    private sealed class OwnedGreetingBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(
            string input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult($"Owned hello, {input}!");
    }

    private sealed class OwnedGreetingModule : BehaviorModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.behavior-owner",
            displayName: "Behavior Owner",
            description: "Test module that explicitly owns a behavior.",
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
        {
            behaviors.Add<OwnedGreetingBehavior>(topology => topology
                .AsDirect()
                .ViaHttpJsonRpc());
        }
    }

    private sealed class DuplicateGreetingOwnerModule : BehaviorModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.behavior-owner-duplicate",
            displayName: "Behavior Owner Duplicate",
            description: "Conflicting test module that tries to own the same behavior.",
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
        {
            behaviors.Add<OwnedGreetingBehavior>(topology => topology
                .AsDirect()
                .ViaHttpJsonRpc());
        }
    }

    private sealed class ShowcaseOwnedBehaviorModule : BehaviorModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.showcase-owner",
            displayName: "Showcase Owner",
            description: "Test module that explicitly owns a sample showcase behavior.",
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
        {
            behaviors.Add<GetCartBehavior>(topology => topology
                .AsCqrs()
                .ViaWebSocket());
        }
    }
}
