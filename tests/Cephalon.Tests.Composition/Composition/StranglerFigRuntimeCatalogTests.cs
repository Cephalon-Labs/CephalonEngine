using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Patterns;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class StranglerFigRuntimeCatalogTests
{
    [Fact]
    public void BuildCollectsStranglerFigRoutesAndSelectsPatternWhenRoutesExist()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "Microservice"));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
            engine.AddModule(new StranglerFigCatalogTestModule());
            engine.AddStranglerFigRoute(new StranglerFigRouteDescriptor(
                id: "platform-status",
                sourceModuleId: "platform",
                displayName: "Platform status migration",
                description: "Routes platform status requests through the migration boundary.",
                pathPrefix: "/legacy/platform",
                preferredTarget: StranglerFigTarget.Modern,
                legacyEndpoint: "legacy://platform-status",
                modernEndpoint: "modern://platform-status"));
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var catalog = provider.GetRequiredService<IStranglerFigRuntimeCatalog>();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        Assert.Equal(3, catalog.Routes.Count);
        Assert.Contains(runtime.Manifest.AppProfile.Patterns, pattern => pattern.Id == "strangler-fig");
        Assert.Contains(snapshot.StranglerFigRoutes, route => route.Id == "platform-status");
        Assert.Equal("modern://platform-status", catalog.GetById("platform-status")?.ModernEndpoint);

        var platformRoutes = catalog.GetBySourceModule("platform");
        Assert.Single(platformRoutes);
        Assert.Equal("/legacy/platform", platformRoutes[0].PathPrefix);

        var moduleRoutes = catalog.GetBySourceModule("strangler-fig-tests");
        Assert.Equal(2, moduleRoutes.Count);
        Assert.Contains(moduleRoutes, route => route.Id == "orders-modern");
        Assert.Contains(moduleRoutes, route => route.Id == "reports-fallback");
    }

    [Fact]
    public async Task RouterUsesLongestPrefixAndFallsBackWhenPreferredEndpointIsUnavailable()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "Microservice"));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
            engine.AddModule(new StranglerFigCatalogTestModule());
        });

        using var provider = services.BuildServiceProvider();
        var router = provider.GetRequiredService<IStranglerFigRouter>();

        var specificMatch = await router.ResolveAsync(new StranglerFigRequest(
            path: "/checkout/orders/42?includeHistory=true",
            method: "get"));
        var fallbackMatch = await router.ResolveAsync(new StranglerFigRequest(
            path: "https://legacy.example.local/reports/daily",
            method: "GET"));

        Assert.NotNull(specificMatch);
        Assert.Equal("orders-modern", specificMatch.RouteId);
        Assert.Equal("Orders modernization", specificMatch.RouteDisplayName);
        Assert.Equal("strangler-fig-tests", specificMatch.SourceModuleId);
        Assert.Equal("/checkout/orders/42", specificMatch.RequestedPath);
        Assert.Equal("/checkout/orders", specificMatch.MatchedPathPrefix);
        Assert.Equal(StranglerFigTarget.Modern, specificMatch.SelectedTarget);
        Assert.Equal("modern://orders", specificMatch.SelectedEndpoint);
        Assert.Equal("preferred-target", specificMatch.ResolutionMode);

        Assert.NotNull(fallbackMatch);
        Assert.Equal("reports-fallback", fallbackMatch.RouteId);
        Assert.Equal(StranglerFigTarget.Legacy, fallbackMatch.SelectedTarget);
        Assert.Equal("legacy://reports", fallbackMatch.SelectedEndpoint);
        Assert.Equal("fallback-target", fallbackMatch.ResolutionMode);
    }

    private sealed class StranglerFigCatalogTestModule : ModuleBase, IStranglerFigRouteContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "strangler-fig-tests",
            displayName: "Strangler Fig Tests",
            description: "Provides strangler-fig routes for runtime catalog tests.",
            dependsOn: [typeof(PlatformTestModule)]);

        public void RegisterRoutes(IStranglerFigRouteRegistry routes)
        {
            routes.Add(new StranglerFigRouteDescriptor(
                id: "orders-modern",
                sourceModuleId: "strangler-fig-tests",
                displayName: "Orders modernization",
                description: "Routes order workflows to the modern boundary first.",
                pathPrefix: "/checkout/orders",
                preferredTarget: StranglerFigTarget.Modern,
                legacyEndpoint: "legacy://orders",
                modernEndpoint: "modern://orders",
                methods: ["GET", "POST"]));
            routes.Add(new StranglerFigRouteDescriptor(
                id: "reports-fallback",
                sourceModuleId: "strangler-fig-tests",
                displayName: "Reports fallback",
                description: "Keeps reports on the legacy boundary until the modern endpoint exists.",
                pathPrefix: "/reports",
                preferredTarget: StranglerFigTarget.Modern,
                legacyEndpoint: "legacy://reports"));
        }
    }
}
