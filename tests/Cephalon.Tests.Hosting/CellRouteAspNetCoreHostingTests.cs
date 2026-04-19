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

public sealed class CellRouteAspNetCoreHostingTests
{
    [Fact]
    public async Task MapCephalonExposesCellRoutesAndTechnologySurfaces()
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
            engine.AddModule(new CellRouteHostingTestModule());
            engine.AddCellBoundary(new CellBoundaryDescriptor(
                id: "platform-control",
                sourceModuleId: "platform",
                displayName: "Platform Control Cell",
                description: "Keeps control-plane workflows in one shared cell.",
                blastRadius: "shared-control",
                routingStrategy: "local-preferred",
                moduleIds: ["platform"]));
            engine.AddCellRoute(new CellRouteDescriptor(
                id: "platform-to-orders",
                sourceModuleId: "platform",
                sourceCellId: "platform-control",
                targetCellId: "orders-cell",
                displayName: "Platform to Orders",
                description: "Lets platform control-plane flows call the orders cell.",
                routingStrategy: "sync-request",
                governanceMode: "capability-gated",
                transportIds: ["rest-api"],
                requiredCapabilityKey: "platform.orders.read"));
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var routes = await client.GetFromJsonAsync<CellRouteDescriptor[]>("/engine/cell-routes");
        var route = await client.GetFromJsonAsync<CellRouteDescriptor>("/engine/cell-routes/orders-to-reporting");
        var moduleRoutes = await client.GetFromJsonAsync<CellRouteDescriptor[]>("/engine/cell-routes/modules/cell-route-hosting-tests");
        var sourceCellRoutes = await client.GetFromJsonAsync<CellRouteDescriptor[]>("/engine/cell-routes/source-cells/orders-cell");
        var targetCellRoutes = await client.GetFromJsonAsync<CellRouteDescriptor[]>("/engine/cell-routes/target-cells/platform-control");
        var surfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/cell-based-architecture");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(routes);
        Assert.Equal(3, routes.Length);
        Assert.Contains(routes, candidate =>
            candidate.Id == "platform-to-orders" &&
            candidate.RequiredCapabilityKey == "platform.orders.read");

        Assert.NotNull(route);
        Assert.Equal("orders-cell", route.SourceCellId);
        Assert.Equal("reporting-cell", route.TargetCellId);
        Assert.Equal("async-governed", route.GovernanceMode);

        Assert.NotNull(moduleRoutes);
        Assert.Equal(2, moduleRoutes.Length);
        Assert.Contains(moduleRoutes, candidate => candidate.Id == "orders-to-platform");

        Assert.NotNull(sourceCellRoutes);
        Assert.Equal(2, sourceCellRoutes.Length);
        Assert.Contains(sourceCellRoutes, candidate => candidate.Id == "orders-to-reporting");

        Assert.NotNull(targetCellRoutes);
        Assert.Single(targetCellRoutes);
        Assert.Equal("orders-to-platform", targetCellRoutes[0].Id);

        Assert.NotNull(surfaces);
        Assert.Contains(surfaces, surface => surface.SurfaceId == "cell-routes");
        var cellRouteSurface = Assert.Single(
            surfaces,
            static surface => surface.SurfaceId == "cell-routes");
        Assert.Contains(cellRouteSurface.Entries, entry =>
            entry.Id == "platform-to-orders" &&
            entry.Metadata["sourceCellId"] == "platform-control" &&
            entry.Metadata["targetCellId"] == "orders-cell" &&
            entry.Metadata["requiredCapabilityKey"] == "platform.orders.read");
        Assert.Contains(cellRouteSurface.Entries, entry =>
            entry.Id == "orders-to-reporting" &&
            entry.Metadata["governanceMode"] == "async-governed");

        Assert.NotNull(snapshot);
        Assert.Equal(3, snapshot.CellRoutes.Count);
        Assert.Contains(snapshot.CellRoutes, candidate =>
            candidate.Id == "orders-to-platform" &&
            candidate.RoutingStrategy == "governed-egress");
    }

    private sealed class CellRouteHostingTestModule :
        ModuleBase,
        ICellBoundaryContributor,
        ICellRouteContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "cell-route-hosting-tests",
            displayName: "Cell Route Hosting Tests",
            description: "Provides cell boundaries and routes for ASP.NET Core hosting tests.",
            dependsOn: [typeof(PlatformTestModule), typeof(DiscoveryTestModule)]);

        public void RegisterCellBoundaries(ICellBoundaryRegistry cells)
        {
            cells.Add(new CellBoundaryDescriptor(
                id: "orders-cell",
                sourceModuleId: "cell-route-hosting-tests",
                displayName: "Orders Cell",
                description: "Keeps order-facing workloads together.",
                blastRadius: "regional",
                routingStrategy: "local-first",
                moduleIds: ["cell-route-hosting-tests", "discovery"]));
            cells.Add(new CellBoundaryDescriptor(
                id: "reporting-cell",
                sourceModuleId: "cell-route-hosting-tests",
                displayName: "Reporting Cell",
                description: "Keeps reporting traffic separate from live request flows.",
                blastRadius: "analytics-only",
                routingStrategy: "async-replica"));
        }

        public void RegisterCellRoutes(ICellRouteRegistry routes)
        {
            routes.Add(new CellRouteDescriptor(
                id: "orders-to-reporting",
                sourceModuleId: "cell-route-hosting-tests",
                sourceCellId: "orders-cell",
                targetCellId: "reporting-cell",
                displayName: "Orders to Reporting",
                description: "Projects order events into reporting workflows.",
                routingStrategy: "async-event",
                governanceMode: "async-governed",
                transportIds: ["server-sent-events", "websocket"]));
            routes.Add(new CellRouteDescriptor(
                id: "orders-to-platform",
                sourceModuleId: "cell-route-hosting-tests",
                sourceCellId: "orders-cell",
                targetCellId: "platform-control",
                displayName: "Orders to Platform",
                description: "Publishes platform governance events from the orders cell.",
                routingStrategy: "governed-egress",
                governanceMode: "internal-only",
                transportIds: ["json-rpc"]));
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
