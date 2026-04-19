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
        var migrationCatalog = provider.GetRequiredService<IStranglerFigMigrationRuntimeCatalog>();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        Assert.Equal(3, catalog.Routes.Count);
        Assert.Equal(3, migrationCatalog.Routes.Count);
        Assert.Contains(runtime.Manifest.AppProfile.Patterns, pattern => pattern.Id == "strangler-fig");
        Assert.Contains(snapshot.StranglerFigRoutes, route => route.Id == "platform-status");
        Assert.Contains(snapshot.StranglerFigRoutePolicies, route => route.RouteId == "platform-status");
        Assert.Equal("modern://platform-status", catalog.GetById("platform-status")?.ModernEndpoint);

        var platformRoutes = catalog.GetBySourceModule("platform");
        Assert.Single(platformRoutes);
        Assert.Equal("/legacy/platform", platformRoutes[0].PathPrefix);

        var moduleRoutes = catalog.GetBySourceModule("strangler-fig-tests");
        Assert.Equal(2, moduleRoutes.Count);
        Assert.Contains(moduleRoutes, route => route.Id == "orders-modern");
        Assert.Contains(moduleRoutes, route => route.Id == "reports-fallback");

        var platformPolicy = migrationCatalog.GetById("platform-status");
        Assert.NotNull(platformPolicy);
        Assert.Equal("authored-route", platformPolicy.RequestedTargetSource);
        Assert.Equal(StranglerFigTarget.Modern, platformPolicy.RequestedTarget);
        Assert.Equal("not-started", platformPolicy.ProgressState);
        Assert.Equal(0, platformPolicy.ProgressPercent);
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

    [Fact]
    public async Task BuildProjectsConfiguredMigrationPolicyIntoRuntimeCatalogSnapshotAndRouter()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                migration: new MigrationSettings(new StranglerFigMigrationSettings(
                    defaultTarget: StranglerFigTarget.Legacy,
                    defaultProgressState: "assessing",
                    defaultProgressPercent: 25,
                    routes:
                    [
                        new StranglerFigRoutePolicySettings(
                            routeId: "orders-modern",
                            target: StranglerFigTarget.Modern,
                            progressState: "cutover",
                            progressPercent: 90,
                            notes: "Ready for the final traffic shift."),
                        new StranglerFigRoutePolicySettings(
                            routeId: "reports-fallback",
                            progressState: "validating",
                            progressPercent: 40)
                    ]))));
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
        var catalog = provider.GetRequiredService<IStranglerFigMigrationRuntimeCatalog>();
        var router = provider.GetRequiredService<IStranglerFigRouter>();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        var platformStatus = catalog.GetById("platform-status");
        var ordersModern = catalog.GetById("orders-modern");
        var reportsFallback = catalog.GetById("reports-fallback");
        var reportsResolution = await router.ResolveAsync(new StranglerFigRequest(
            path: "/reports/daily",
            method: "GET"));

        Assert.NotNull(platformStatus);
        Assert.Equal(StranglerFigTarget.Legacy, platformStatus.RequestedTarget);
        Assert.Equal(StranglerFigTarget.Legacy, platformStatus.EffectiveTarget);
        Assert.Equal("migration-default", platformStatus.RequestedTargetSource);
        Assert.Equal("assessing", platformStatus.ProgressState);
        Assert.Equal(25, platformStatus.ProgressPercent);

        Assert.NotNull(ordersModern);
        Assert.Equal(StranglerFigTarget.Modern, ordersModern.RequestedTarget);
        Assert.Equal(StranglerFigTarget.Modern, ordersModern.EffectiveTarget);
        Assert.Equal("migration-route", ordersModern.RequestedTargetSource);
        Assert.Equal("cutover", ordersModern.ProgressState);
        Assert.Equal(90, ordersModern.ProgressPercent);
        Assert.Equal("Ready for the final traffic shift.", ordersModern.RuntimeMetadata["note"]);

        Assert.NotNull(reportsFallback);
        Assert.Equal(StranglerFigTarget.Legacy, reportsFallback.RequestedTarget);
        Assert.Equal(StranglerFigTarget.Legacy, reportsFallback.EffectiveTarget);
        Assert.Equal("migration-default", reportsFallback.RequestedTargetSource);
        Assert.Equal("validating", reportsFallback.ProgressState);
        Assert.Equal(40, reportsFallback.ProgressPercent);

        Assert.NotNull(reportsResolution);
        Assert.Equal("reports-fallback", reportsResolution.RouteId);
        Assert.Equal(StranglerFigTarget.Legacy, reportsResolution.SelectedTarget);
        Assert.Equal("legacy://reports", reportsResolution.SelectedEndpoint);
        Assert.Equal("configured-target", reportsResolution.ResolutionMode);
        Assert.Equal("legacy", reportsResolution.Metadata["migrationRequestedTarget"]);
        Assert.Equal("migration-default", reportsResolution.Metadata["migrationRequestedTargetSource"]);
        Assert.Equal("validating", reportsResolution.Metadata["migrationProgressState"]);
        Assert.Equal("40", reportsResolution.Metadata["migrationProgressPercent"]);

        Assert.Contains(snapshot.StranglerFigRoutePolicies, route =>
            route.RouteId == "orders-modern" &&
            route.RequestedTargetSource == "migration-route" &&
            route.ProgressState == "cutover" &&
            route.ProgressPercent == 90);
        Assert.Contains(snapshot.StranglerFigRoutePolicies, route =>
            route.RouteId == "platform-status" &&
            route.RequestedTargetSource == "migration-default" &&
            route.ProgressState == "assessing" &&
            route.ProgressPercent == 25);
    }

    [Fact]
    public void BuildFailsWhenMigrationPolicyReferencesUnknownRoute()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                migration: new MigrationSettings(new StranglerFigMigrationSettings(
                    routes:
                    [
                        new StranglerFigRoutePolicySettings(
                            routeId: "unknown-route",
                            target: StranglerFigTarget.Modern)
                    ]))));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
            engine.AddModule(new StranglerFigCatalogTestModule());
        });

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IStranglerFigMigrationRuntimeCatalog>());
        Assert.Contains("unknown route ids", exception.Message, StringComparison.Ordinal);
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
