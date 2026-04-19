using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Patterns;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class BackendForFrontendRuntimeCatalogTests
{
    [Fact]
    public void BuildCollectsClientBindingsAndSelectsPatternWhenBindingsExist()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "Microservice"));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
            engine.AddModule(new BackendForFrontendCatalogTestModule());
            engine.AddBackendForFrontendClientBinding(new BackendForFrontendClientBindingDescriptor(
                id: "storefront-rest",
                clientId: "storefront",
                sourceModuleId: "platform",
                displayName: "Storefront REST",
                description: "Projects the storefront REST surface through the platform module.",
                transportId: "rest-api",
                entryPoint: "/api/storefront",
                behaviorFilter: new BackendForFrontendBehaviorFilterDescriptor(
                    includedBehaviorIds: ["tests.storefront.lookup"],
                    includedTags: ["storefront"]),
                metadata: new Dictionary<string, string>
                {
                    ["audience"] = "public"
                }));
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var catalog = provider.GetRequiredService<IBackendForFrontendRuntimeCatalog>();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        Assert.Equal(3, catalog.Bindings.Count);
        Assert.Contains(runtime.Manifest.AppProfile.Patterns, pattern => pattern.Id == "backend-for-frontend");
        Assert.Contains(snapshot.BackendForFrontendBindings, binding => binding.Id == "storefront-rest");

        var storefrontBindings = catalog.GetByClientId("storefront");
        Assert.Equal(2, storefrontBindings.Count);
        Assert.Contains(storefrontBindings, binding => binding.Id == "storefront-rest");
        Assert.Contains(storefrontBindings, binding => binding.Id == "storefront-graphql");

        var platformBindings = catalog.GetBySourceModule("platform");
        Assert.Single(platformBindings);
        Assert.Equal("rest-api", platformBindings[0].TransportId);
        Assert.Equal("/api/storefront", platformBindings[0].EntryPoint);
        Assert.Equal("public", platformBindings[0].Metadata["audience"]);

        var restBindings = catalog.GetByTransportId("rest-api");
        Assert.Equal(2, restBindings.Count);
        Assert.Contains(restBindings, binding => binding.Id == "mobile-rest");
        Assert.Contains(restBindings, binding => binding.Id == "storefront-rest");
    }

    [Fact]
    public void BuildProjectsConfiguredBackendForFrontendSettingsIntoRuntimeCatalog()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                backendForFrontend: new BackendForFrontendSettings(
                [
                    new BackendForFrontendClientBindingSettings(
                        id: "mobile-rest",
                        clientId: "mobile",
                        sourceModuleId: "platform",
                        displayName: "Mobile REST",
                        description: "Projects the mobile REST experience through the platform module.",
                        transportId: "rest-api",
                        entryPoint: "/api/mobile",
                        behaviorFilter: new BackendForFrontendBehaviorFilterSettings(
                            includedBehaviorIds: ["tests.mobile.lookup"],
                            includedTags: ["mobile"]),
                        metadata: new Dictionary<string, string>
                        {
                            ["audience"] = "mobile"
                        }),
                    new BackendForFrontendClientBindingSettings(
                        id: "storefront-graphql",
                        clientId: "storefront",
                        sourceModuleId: "discovery",
                        displayName: "Storefront GraphQL",
                        description: "Projects the storefront GraphQL experience through the discovery module.",
                        transportId: "graphql",
                        entryPoint: "/graphql/storefront",
                        behaviorFilter: new BackendForFrontendBehaviorFilterSettings(
                            includedCapabilityKeys: ["discovery.greetings"],
                            excludedTags: ["admin"]),
                        metadata: new Dictionary<string, string>
                        {
                            ["document"] = "storefront"
                        })
                ])));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var catalog = provider.GetRequiredService<IBackendForFrontendRuntimeCatalog>();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        Assert.Equal(2, catalog.Bindings.Count);
        Assert.Contains(runtime.Manifest.AppProfile.Patterns, pattern => pattern.Id == "backend-for-frontend");
        Assert.Equal(2, snapshot.BackendForFrontendBindings.Count);

        var mobileBinding = catalog.GetById("mobile-rest");
        Assert.NotNull(mobileBinding);
        Assert.Equal("mobile", mobileBinding.ClientId);
        Assert.Equal("rest-api", mobileBinding.TransportId);
        Assert.Equal("/api/mobile", mobileBinding.EntryPoint);
        Assert.Contains("tests.mobile.lookup", mobileBinding.BehaviorFilter.IncludedBehaviorIds);
        Assert.Contains("mobile", mobileBinding.BehaviorFilter.IncludedTags);
        Assert.Equal("mobile", mobileBinding.Metadata["audience"]);

        var graphqlBindings = catalog.GetByTransportId("graphql");
        var graphqlBinding = Assert.Single(graphqlBindings);
        Assert.Equal("storefront-graphql", graphqlBinding.Id);
        Assert.Contains("discovery.greetings", graphqlBinding.BehaviorFilter.IncludedCapabilityKeys);
        Assert.Contains("admin", graphqlBinding.BehaviorFilter.ExcludedTags);
        Assert.Equal("storefront", graphqlBinding.Metadata["document"]);
    }

    [Fact]
    public void BuildFailsWhenDuplicateBackendForFrontendBindingIdsExist()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "Microservice"));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
            engine.AddModule(new BackendForFrontendCatalogTestModule());
            engine.AddBackendForFrontendClientBinding(new BackendForFrontendClientBindingDescriptor(
                id: "mobile-rest",
                clientId: "mobile",
                sourceModuleId: "platform",
                displayName: "Duplicate mobile REST",
                description: "Conflicts with the module-contributed mobile REST binding.",
                transportId: "rest-api",
                entryPoint: "/api/mobile/duplicate"));
        });

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IBackendForFrontendRuntimeCatalog>());
        Assert.Contains("registered multiple times", exception.Message, StringComparison.Ordinal);
    }

    private sealed class BackendForFrontendCatalogTestModule : ModuleBase, IBackendForFrontendClientBindingContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "backend-for-frontend-tests",
            displayName: "Backend for Frontend Tests",
            description: "Provides backend-for-frontend bindings for runtime catalog tests.",
            dependsOn: [typeof(PlatformTestModule), typeof(DiscoveryTestModule)]);

        public void RegisterClientBindings(IBackendForFrontendClientBindingRegistry bindings)
        {
            bindings.Add(new BackendForFrontendClientBindingDescriptor(
                id: "mobile-rest",
                clientId: "mobile",
                sourceModuleId: "backend-for-frontend-tests",
                displayName: "Mobile REST",
                description: "Projects the mobile REST experience through the backend-for-frontend test module.",
                transportId: "rest-api",
                entryPoint: "/api/mobile",
                behaviorFilter: new BackendForFrontendBehaviorFilterDescriptor(
                    includedBehaviorIds: ["tests.mobile.lookup"],
                    includedTags: ["mobile"])));
            bindings.Add(new BackendForFrontendClientBindingDescriptor(
                id: "storefront-graphql",
                clientId: "storefront",
                sourceModuleId: "backend-for-frontend-tests",
                displayName: "Storefront GraphQL",
                description: "Projects the storefront GraphQL experience through the backend-for-frontend test module.",
                transportId: "graphql",
                entryPoint: "/graphql/storefront",
                behaviorFilter: new BackendForFrontendBehaviorFilterDescriptor(
                    includedCapabilityKeys: ["discovery.greetings"],
                    excludedTags: ["admin"])));
        }
    }
}
