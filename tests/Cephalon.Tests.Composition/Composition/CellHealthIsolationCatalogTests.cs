using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class CellHealthIsolationCatalogTests
{
    [Fact]
    public void BuildCollectsCellHealthIsolationsAndProjectsTechnologySurface()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "Microservice"));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
            engine.AddModule(new CellHealthIsolationCatalogTestModule());
            engine.AddCellBoundary(new CellBoundaryDescriptor(
                id: "platform-control",
                sourceModuleId: "platform",
                displayName: "Platform Control Cell",
                description: "Keeps control-plane health and governance flows in one shared boundary.",
                blastRadius: "shared-control",
                routingStrategy: "local-preferred",
                moduleIds: ["platform"]));
            engine.AddCellHealthIsolation(new CellHealthIsolationDescriptor(
                id: "platform-control-health",
                sourceModuleId: "platform",
                cellId: "platform-control",
                displayName: "Platform Control Health Isolation",
                description: "Contains control-plane dependency failures without leaking them into product cells.",
                failureIsolationMode: "fail-closed",
                readinessScope: "dependency-aware",
                restartScope: "host-coordinated",
                dependencyIds: ["consul-control", "postgres-control"],
                metadata: new Dictionary<string, string>
                {
                    ["tier"] = "control-plane"
                }));
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var catalog = provider.GetRequiredService<ICellHealthIsolationCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        Assert.Equal(3, catalog.HealthIsolations.Count);
        Assert.Contains(runtime.Manifest.AppProfile.Technologies, technology => technology.Id == "cell-based-architecture");

        var platformIsolation = catalog.GetById("platform-control-health");
        Assert.NotNull(platformIsolation);
        Assert.Equal("platform-control", platformIsolation.CellId);
        Assert.Equal("fail-closed", platformIsolation.FailureIsolationMode);
        Assert.Equal("dependency-aware", platformIsolation.ReadinessScope);
        Assert.Equal("host-coordinated", platformIsolation.RestartScope);

        var moduleIsolations = catalog.GetBySourceModule("cell-health-tests");
        Assert.Equal(2, moduleIsolations.Count);
        Assert.Contains(moduleIsolations, isolation => isolation.Id == "orders-cell-health");
        Assert.Contains(moduleIsolations, isolation => isolation.Id == "reporting-cell-health");

        var cellIsolations = catalog.GetByCellId("orders-cell");
        var ordersIsolation = Assert.Single(cellIsolations);
        Assert.Equal("cell-only", ordersIsolation.RestartScope);

        var dependencyIsolations = catalog.GetByDependencyId("orders-db");
        var dependencyIsolation = Assert.Single(dependencyIsolations);
        Assert.Equal("orders-cell-health", dependencyIsolation.Id);

        Assert.Equal(3, snapshot.CellHealthIsolations.Count);
        Assert.Contains(snapshot.CellHealthIsolations, isolation => isolation.Id == "reporting-cell-health");

        var surface = Assert.Single(
            technologyCatalog.GetByTechnology("cell-based-architecture"),
            static candidate => candidate.SurfaceId == "cell-health-isolations");
        Assert.Contains(surface.Entries, entry =>
            entry.Id == "platform-control-health" &&
            entry.Metadata["sourceModuleId"] == "platform" &&
            entry.Metadata["cellId"] == "platform-control" &&
            entry.Metadata["tier"] == "control-plane");
        Assert.Contains(surface.Entries, entry =>
            entry.Id == "orders-cell-health" &&
            entry.Metadata["failureIsolationMode"] == "cell-quarantine" &&
            entry.Metadata["dependencyIds"] == "orders-cache,orders-db");
    }

    [Fact]
    public void BuildFailsWhenCellHealthIsolationReferencesUnknownCell()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(blueprint: "Microservice"));
        builder.AddModule(new PlatformTestModule());
        builder.AddCellBoundary(new CellBoundaryDescriptor(
            id: "platform-control",
            sourceModuleId: "platform",
            displayName: "Platform Control Cell",
            description: "Provides one valid platform-owned cell boundary.",
            blastRadius: "shared-control",
            routingStrategy: "local-preferred",
            moduleIds: ["platform"]));
        builder.AddCellHealthIsolation(new CellHealthIsolationDescriptor(
            id: "invalid-health",
            sourceModuleId: "platform",
            cellId: "missing-cell",
            displayName: "Invalid Health Isolation",
            description: "References one cell that is not active.",
            failureIsolationMode: "fail-closed",
            readinessScope: "dependency-aware",
            restartScope: "host-coordinated"));

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("invalid-health", exception.Message, StringComparison.Ordinal);
        Assert.Contains("missing-cell", exception.Message, StringComparison.Ordinal);
    }

    private sealed class CellHealthIsolationCatalogTestModule :
        ModuleBase,
        ICellBoundaryContributor,
        ICellHealthIsolationContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "cell-health-tests",
            displayName: "Cell Health Tests",
            description: "Provides cell boundaries and health isolation answers for runtime catalog tests.",
            dependsOn: [typeof(PlatformTestModule), typeof(DiscoveryTestModule)]);

        public void RegisterCellBoundaries(ICellBoundaryRegistry cells)
        {
            cells.Add(new CellBoundaryDescriptor(
                id: "orders-cell",
                sourceModuleId: "cell-health-tests",
                displayName: "Orders Cell",
                description: "Owns order-facing API and coordination workflows.",
                blastRadius: "regional",
                routingStrategy: "local-first",
                moduleIds: ["cell-health-tests", "discovery"]));
            cells.Add(new CellBoundaryDescriptor(
                id: "reporting-cell",
                sourceModuleId: "cell-health-tests",
                displayName: "Reporting Cell",
                description: "Owns reporting and replicated analytics workloads.",
                blastRadius: "analytics-only",
                routingStrategy: "async-replica"));
        }

        public void RegisterCellHealthIsolations(ICellHealthIsolationRegistry healthIsolations)
        {
            healthIsolations.Add(new CellHealthIsolationDescriptor(
                id: "orders-cell-health",
                sourceModuleId: "cell-health-tests",
                cellId: "orders-cell",
                displayName: "Orders Cell Health Isolation",
                description: "Quarantines order-serving failures to the orders cell while preserving surrounding topology.",
                failureIsolationMode: "cell-quarantine",
                readinessScope: "dependency-aware",
                restartScope: "cell-only",
                dependencyIds: ["orders-cache", "orders-db"]));
            healthIsolations.Add(new CellHealthIsolationDescriptor(
                id: "reporting-cell-health",
                sourceModuleId: "cell-health-tests",
                cellId: "reporting-cell",
                displayName: "Reporting Cell Health Isolation",
                description: "Lets reporting workloads degrade independently from interactive cells.",
                failureIsolationMode: "degraded-serving",
                readinessScope: "best-effort",
                restartScope: "manual"));
        }
    }
}
