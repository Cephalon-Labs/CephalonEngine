using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Resilience;
using Cephalon.Abstractions.Technologies;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace Cephalon.Tests.Hosting;

public sealed class CellBoundaryAspNetCoreHostingTests
{
    [Fact]
    public async Task MapCephalonExposesCellBoundariesAndTechnologySurfaces()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks()
            .AddCheck("cephalon.liveness", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live", "engine"])
            .AddCheck("cephalon.readiness", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["ready", "engine"]);
        builder.Services.AddSingleton<IRateLimitingRuntimeCatalog>(EmptyRateLimitingRuntimeCatalog.Instance);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Services.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
            engine.AddModule(new CellBoundaryHostingTestModule());
            engine.AddCellBoundary(new CellBoundaryDescriptor(
                id: "platform-control",
                sourceModuleId: "platform",
                displayName: "Platform Control Cell",
                description: "Keeps shared control-plane routes in one isolated boundary.",
                blastRadius: "shared-control",
                routingStrategy: "local-preferred",
                moduleIds: ["platform"],
                metadata: new Dictionary<string, string>
                {
                    ["lane"] = "control"
                }));
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var cells = await client.GetFromJsonAsync<CellBoundaryDescriptor[]>("/engine/cells");
        var cell = await client.GetFromJsonAsync<CellBoundaryDescriptor>("/engine/cells/orders-cell");
        var moduleCells = await client.GetFromJsonAsync<CellBoundaryDescriptor[]>("/engine/cells/modules/cell-hosting-tests");
        var surfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/cell-based-architecture");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(cells);
        Assert.Equal(3, cells.Length);
        Assert.Contains(cells, candidate =>
            candidate.Id == "platform-control" &&
            candidate.BlastRadius == "shared-control");

        Assert.NotNull(cell);
        Assert.Equal("cell-hosting-tests", cell.SourceModuleId);
        Assert.Equal("regional", cell.BlastRadius);
        Assert.Equal(["cell-hosting-tests", "discovery"], cell.ModuleIds);

        Assert.NotNull(moduleCells);
        Assert.Equal(2, moduleCells.Length);
        Assert.Contains(moduleCells, candidate => candidate.Id == "reporting-cell");

        Assert.NotNull(surfaces);
        var surface = Assert.Single(surfaces);
        Assert.Equal("cell-boundaries", surface.SurfaceId);
        Assert.Contains(surface.Entries, entry =>
            entry.Id == "platform-control" &&
            entry.Metadata["lane"] == "control");
        Assert.Contains(surface.Entries, entry =>
            entry.Id == "orders-cell" &&
            entry.Metadata["moduleIds"] == "cell-hosting-tests,discovery");

        Assert.NotNull(snapshot);
        Assert.Equal(3, snapshot.CellBoundaries.Count);
        Assert.Contains(snapshot.CellBoundaries, candidate =>
            candidate.Id == "reporting-cell" &&
            candidate.RoutingStrategy == "async-replica");
    }

    private sealed class CellBoundaryHostingTestModule : ModuleBase, ICellBoundaryContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "cell-hosting-tests",
            displayName: "Cell Hosting Tests",
            description: "Provides cell boundaries for ASP.NET Core hosting tests.",
            dependsOn: [typeof(PlatformTestModule), typeof(DiscoveryTestModule)]);

        public void RegisterCellBoundaries(ICellBoundaryRegistry cells)
        {
            cells.Add(new CellBoundaryDescriptor(
                id: "orders-cell",
                sourceModuleId: "cell-hosting-tests",
                displayName: "Orders Cell",
                description: "Keeps order workflows and API ownership in one cell.",
                blastRadius: "regional",
                routingStrategy: "local-first",
                moduleIds: ["cell-hosting-tests", "discovery"]));
            cells.Add(new CellBoundaryDescriptor(
                id: "reporting-cell",
                sourceModuleId: "cell-hosting-tests",
                displayName: "Reporting Cell",
                description: "Separates reporting workloads from live request flows.",
                blastRadius: "analytics-only",
                routingStrategy: "async-replica"));
        }
    }

    private sealed class EmptyRateLimitingRuntimeCatalog : IRateLimitingRuntimeCatalog
    {
        public static EmptyRateLimitingRuntimeCatalog Instance { get; } = new();

        public IReadOnlyList<RateLimitingRuntimeDescriptor> Policies => [];

        public RateLimitingRuntimeDescriptor? GetById(string policyId) => null;

        public IReadOnlyList<RateLimitingRuntimeDescriptor> GetByTransportId(string transportId) => [];
    }
}
