using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class CellRouteCatalogTests
{
    [Fact]
    public void BuildCollectsCellRoutesAndProjectsTechnologySurface()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "Microservice"));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
            engine.AddModule(new CellRouteCatalogTestModule());
            engine.AddCellBoundary(new CellBoundaryDescriptor(
                id: "platform-control",
                sourceModuleId: "platform",
                displayName: "Platform Control Cell",
                description: "Keeps control-plane routing and governance in one shared boundary.",
                blastRadius: "shared-control",
                routingStrategy: "local-preferred",
                moduleIds: ["platform"]));
            engine.AddCellRoute(new CellRouteDescriptor(
                id: "platform-to-orders",
                sourceModuleId: "platform",
                sourceCellId: "platform-control",
                targetCellId: "orders-cell",
                displayName: "Platform to Orders",
                description: "Lets platform control-plane flows reach the orders cell through governed REST calls.",
                routingStrategy: "sync-request",
                governanceMode: "capability-gated",
                transportIds: ["rest-api"],
                requiredCapabilityKey: "platform.orders.read",
                metadata: new Dictionary<string, string>
                {
                    ["owner"] = "platform"
                }));
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var catalog = provider.GetRequiredService<ICellRouteCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        Assert.Equal(3, catalog.Routes.Count);
        Assert.Contains(runtime.Manifest.AppProfile.Technologies, technology => technology.Id == "cell-based-architecture");

        var platformRoute = catalog.GetById("platform-to-orders");
        Assert.NotNull(platformRoute);
        Assert.Equal("platform-control", platformRoute.SourceCellId);
        Assert.Equal("orders-cell", platformRoute.TargetCellId);
        Assert.Equal("capability-gated", platformRoute.GovernanceMode);
        Assert.Equal("platform.orders.read", platformRoute.RequiredCapabilityKey);

        var moduleRoutes = catalog.GetBySourceModule("cell-route-tests");
        Assert.Equal(2, moduleRoutes.Count);
        Assert.Contains(moduleRoutes, route => route.Id == "orders-to-reporting");
        Assert.Contains(moduleRoutes, route => route.Id == "orders-to-platform");

        var sourceCellRoutes = catalog.GetBySourceCellId("orders-cell");
        Assert.Equal(2, sourceCellRoutes.Count);
        Assert.Contains(sourceCellRoutes, route => route.Id == "orders-to-platform");

        var targetCellRoutes = catalog.GetByTargetCellId("platform-control");
        Assert.Single(targetCellRoutes);
        Assert.Equal("orders-to-platform", targetCellRoutes[0].Id);

        Assert.Equal(3, snapshot.CellRoutes.Count);
        Assert.Contains(snapshot.CellRoutes, route => route.Id == "orders-to-reporting");

        var surface = Assert.Single(
            technologyCatalog.GetByTechnology("cell-based-architecture"),
            static candidate => candidate.SurfaceId == "cell-routes");
        Assert.Contains(surface.Entries, entry =>
            entry.Id == "platform-to-orders" &&
            entry.Metadata["sourceModuleId"] == "platform" &&
            entry.Metadata["sourceCellId"] == "platform-control" &&
            entry.Metadata["targetCellId"] == "orders-cell" &&
            entry.Metadata["requiredCapabilityKey"] == "platform.orders.read");
        Assert.Contains(surface.Entries, entry =>
            entry.Id == "orders-to-reporting" &&
            entry.Metadata["governanceMode"] == "async-governed" &&
            entry.Metadata["transportIds"] == "server-sent-events,websocket");
    }

    [Fact]
    public void BuildFailsWhenCellRouteReferencesUnknownTargetCell()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(blueprint: "Microservice"));
        builder.AddModule(new PlatformTestModule());
        builder.AddCellBoundary(new CellBoundaryDescriptor(
            id: "platform-control",
            sourceModuleId: "platform",
            displayName: "Platform Control Cell",
            description: "Provides one valid source cell.",
            blastRadius: "shared-control",
            routingStrategy: "local-preferred",
            moduleIds: ["platform"]));
        builder.AddCellRoute(new CellRouteDescriptor(
            id: "invalid-route",
            sourceModuleId: "platform",
            sourceCellId: "platform-control",
            targetCellId: "missing-cell",
            displayName: "Invalid Route",
            description: "References one target cell that is not active.",
            routingStrategy: "sync-request",
            governanceMode: "capability-gated"));

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("invalid-route", exception.Message, StringComparison.Ordinal);
        Assert.Contains("missing-cell", exception.Message, StringComparison.Ordinal);
    }

    private sealed class CellRouteCatalogTestModule :
        ModuleBase,
        ICellBoundaryContributor,
        ICellRouteContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "cell-route-tests",
            displayName: "Cell Route Tests",
            description: "Provides cell boundaries and routes for runtime catalog tests.",
            dependsOn: [typeof(PlatformTestModule), typeof(DiscoveryTestModule)]);

        public void RegisterCellBoundaries(ICellBoundaryRegistry cells)
        {
            cells.Add(new CellBoundaryDescriptor(
                id: "orders-cell",
                sourceModuleId: "cell-route-tests",
                displayName: "Orders Cell",
                description: "Owns order-facing API and coordination workflows.",
                blastRadius: "regional",
                routingStrategy: "local-first",
                moduleIds: ["cell-route-tests", "discovery"]));
            cells.Add(new CellBoundaryDescriptor(
                id: "reporting-cell",
                sourceModuleId: "cell-route-tests",
                displayName: "Reporting Cell",
                description: "Owns reporting and replicated analytics workloads.",
                blastRadius: "analytics-only",
                routingStrategy: "async-replica"));
        }

        public void RegisterCellRoutes(ICellRouteRegistry routes)
        {
            routes.Add(new CellRouteDescriptor(
                id: "orders-to-reporting",
                sourceModuleId: "cell-route-tests",
                sourceCellId: "orders-cell",
                targetCellId: "reporting-cell",
                displayName: "Orders to Reporting",
                description: "Projects order changes into the reporting cell asynchronously.",
                routingStrategy: "async-event",
                governanceMode: "async-governed",
                transportIds: ["server-sent-events", "websocket"]));
            routes.Add(new CellRouteDescriptor(
                id: "orders-to-platform",
                sourceModuleId: "cell-route-tests",
                sourceCellId: "orders-cell",
                targetCellId: "platform-control",
                displayName: "Orders to Platform",
                description: "Publishes platform governance events from the orders cell.",
                routingStrategy: "governed-egress",
                governanceMode: "internal-only",
                transportIds: ["json-rpc"]));
        }
    }
}
