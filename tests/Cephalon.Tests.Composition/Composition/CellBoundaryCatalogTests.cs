using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class CellBoundaryCatalogTests
{
    [Fact]
    public void BuildCollectsCellBoundariesAndProjectsTechnologySurface()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "Microservice"));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
            engine.AddModule(new CellBoundaryCatalogTestModule());
            engine.AddCellBoundary(new CellBoundaryDescriptor(
                id: "platform-control",
                sourceModuleId: "platform",
                displayName: "Platform Control Cell",
                description: "Keeps control-plane flows isolated from product cells.",
                blastRadius: "shared-control",
                routingStrategy: "local-preferred",
                moduleIds: ["platform"],
                metadata: new Dictionary<string, string>
                {
                    ["region"] = "global"
                }));
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var catalog = provider.GetRequiredService<ICellBoundaryCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        Assert.Equal(3, catalog.CellBoundaries.Count);
        Assert.Contains(runtime.Manifest.AppProfile.Technologies, technology => technology.Id == "cell-based-architecture");

        var platformCell = catalog.GetById("platform-control");
        Assert.NotNull(platformCell);
        Assert.Equal("shared-control", platformCell.BlastRadius);
        Assert.Equal("local-preferred", platformCell.RoutingStrategy);

        var owningModuleCells = catalog.GetByModule("cell-tests");
        Assert.Equal(2, owningModuleCells.Count);
        Assert.Contains(owningModuleCells, cell => cell.Id == "orders-cell");
        Assert.Contains(owningModuleCells, cell => cell.Id == "reporting-cell");

        var reportingCell = catalog.GetById("reporting-cell");
        Assert.NotNull(reportingCell);
        Assert.Equal(["cell-tests"], reportingCell.ModuleIds);

        Assert.Equal(3, snapshot.CellBoundaries.Count);
        Assert.Contains(snapshot.CellBoundaries, cell => cell.Id == "orders-cell");

        var surface = Assert.Single(technologyCatalog.GetByTechnology("cell-based-architecture"));
        Assert.Equal("cell-boundaries", surface.SurfaceId);
        Assert.Contains(surface.Entries, entry =>
            entry.Id == "platform-control" &&
            entry.Metadata["sourceModuleId"] == "platform" &&
            entry.Metadata["region"] == "global");
        Assert.Contains(surface.Entries, entry =>
            entry.Id == "orders-cell" &&
            entry.Metadata["sourceModuleId"] == "cell-tests" &&
            entry.Metadata["blastRadius"] == "regional" &&
            entry.Metadata["routingStrategy"] == "local-first" &&
            entry.Metadata["moduleIds"] == "cell-tests,discovery");
    }

    [Fact]
    public void BuildFailsWhenCellBoundaryReferencesUnknownModule()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(blueprint: "Microservice"));
        builder.AddModule(new PlatformTestModule());
        builder.AddCellBoundary(new CellBoundaryDescriptor(
            id: "invalid-cell",
            sourceModuleId: "platform",
            displayName: "Invalid Cell",
            description: "References one module that is not active.",
            blastRadius: "regional",
            routingStrategy: "local-preferred",
            moduleIds: ["platform", "missing-module"]));

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("invalid-cell", exception.Message, StringComparison.Ordinal);
        Assert.Contains("missing-module", exception.Message, StringComparison.Ordinal);
    }

    private sealed class CellBoundaryCatalogTestModule : ModuleBase, ICellBoundaryContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "cell-tests",
            displayName: "Cell Tests",
            description: "Provides cell boundaries for runtime catalog tests.",
            dependsOn: [typeof(PlatformTestModule), typeof(DiscoveryTestModule)]);

        public void RegisterCellBoundaries(ICellBoundaryRegistry cells)
        {
            cells.Add(new CellBoundaryDescriptor(
                id: "orders-cell",
                sourceModuleId: "cell-tests",
                displayName: "Orders Cell",
                description: "Keeps order processing close to its product workflows.",
                blastRadius: "regional",
                routingStrategy: "local-first",
                moduleIds: ["cell-tests", "discovery"]));
            cells.Add(new CellBoundaryDescriptor(
                id: "reporting-cell",
                sourceModuleId: "cell-tests",
                displayName: "Reporting Cell",
                description: "Isolates reporting workloads from interactive requests.",
                blastRadius: "analytics-only",
                routingStrategy: "async-replica"));
        }
    }
}
