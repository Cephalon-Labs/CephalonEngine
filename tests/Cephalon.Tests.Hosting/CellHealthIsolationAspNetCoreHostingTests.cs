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

public sealed class CellHealthIsolationAspNetCoreHostingTests
{
    [Fact]
    public async Task MapCephalonExposesCellHealthIsolationsAndTechnologySurfaces()
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
            engine.AddModule(new CellHealthIsolationHostingTestModule());
            engine.AddCellBoundary(new CellBoundaryDescriptor(
                id: "platform-control",
                sourceModuleId: "platform",
                displayName: "Platform Control Cell",
                description: "Keeps shared control-plane workflows in one cell.",
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
                dependencyIds: ["consul-control", "postgres-control"]));
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var healthIsolations = await client.GetFromJsonAsync<CellHealthIsolationDescriptor[]>("/engine/cell-health-isolations");
        var healthIsolation = await client.GetFromJsonAsync<CellHealthIsolationDescriptor>("/engine/cell-health-isolations/orders-cell-health");
        var moduleHealthIsolations = await client.GetFromJsonAsync<CellHealthIsolationDescriptor[]>("/engine/cell-health-isolations/modules/cell-health-hosting-tests");
        var cellHealthIsolations = await client.GetFromJsonAsync<CellHealthIsolationDescriptor[]>("/engine/cell-health-isolations/cells/orders-cell");
        var dependencyHealthIsolations = await client.GetFromJsonAsync<CellHealthIsolationDescriptor[]>("/engine/cell-health-isolations/dependencies/orders-db");
        var surfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/cell-based-architecture");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(healthIsolations);
        Assert.Equal(3, healthIsolations.Length);
        Assert.Contains(healthIsolations, candidate =>
            candidate.Id == "platform-control-health" &&
            candidate.FailureIsolationMode == "fail-closed");

        Assert.NotNull(healthIsolation);
        Assert.Equal("orders-cell", healthIsolation.CellId);
        Assert.Equal("cell-quarantine", healthIsolation.FailureIsolationMode);
        Assert.Equal("cell-only", healthIsolation.RestartScope);

        Assert.NotNull(moduleHealthIsolations);
        Assert.Equal(2, moduleHealthIsolations.Length);
        Assert.Contains(moduleHealthIsolations, candidate => candidate.Id == "reporting-cell-health");

        Assert.NotNull(cellHealthIsolations);
        var ordersIsolation = Assert.Single(cellHealthIsolations);
        Assert.Equal("orders-cell-health", ordersIsolation.Id);

        Assert.NotNull(dependencyHealthIsolations);
        var dependencyIsolation = Assert.Single(dependencyHealthIsolations);
        Assert.Equal("orders-cell-health", dependencyIsolation.Id);

        Assert.NotNull(surfaces);
        var healthIsolationSurface = Assert.Single(
            surfaces,
            static surface => surface.SurfaceId == "cell-health-isolations");
        Assert.Contains(healthIsolationSurface.Entries, entry =>
            entry.Id == "platform-control-health" &&
            entry.Metadata["cellId"] == "platform-control");
        Assert.Contains(healthIsolationSurface.Entries, entry =>
            entry.Id == "orders-cell-health" &&
            entry.Metadata["dependencyIds"] == "orders-cache,orders-db" &&
            entry.Metadata["restartScope"] == "cell-only");

        Assert.NotNull(snapshot);
        Assert.Equal(3, snapshot.CellHealthIsolations.Count);
        Assert.Contains(snapshot.CellHealthIsolations, candidate =>
            candidate.Id == "reporting-cell-health" &&
            candidate.ReadinessScope == "best-effort");
    }

    private sealed class CellHealthIsolationHostingTestModule :
        ModuleBase,
        ICellBoundaryContributor,
        ICellHealthIsolationContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "cell-health-hosting-tests",
            displayName: "Cell Health Hosting Tests",
            description: "Provides cell boundaries and health isolation answers for ASP.NET Core hosting tests.",
            dependsOn: [typeof(PlatformTestModule), typeof(DiscoveryTestModule)]);

        public void RegisterCellBoundaries(ICellBoundaryRegistry cells)
        {
            cells.Add(new CellBoundaryDescriptor(
                id: "orders-cell",
                sourceModuleId: "cell-health-hosting-tests",
                displayName: "Orders Cell",
                description: "Keeps order-facing workloads together.",
                blastRadius: "regional",
                routingStrategy: "local-first",
                moduleIds: ["cell-health-hosting-tests", "discovery"]));
            cells.Add(new CellBoundaryDescriptor(
                id: "reporting-cell",
                sourceModuleId: "cell-health-hosting-tests",
                displayName: "Reporting Cell",
                description: "Keeps reporting traffic separate from live request flows.",
                blastRadius: "analytics-only",
                routingStrategy: "async-replica"));
        }

        public void RegisterCellHealthIsolations(ICellHealthIsolationRegistry healthIsolations)
        {
            healthIsolations.Add(new CellHealthIsolationDescriptor(
                id: "orders-cell-health",
                sourceModuleId: "cell-health-hosting-tests",
                cellId: "orders-cell",
                displayName: "Orders Cell Health Isolation",
                description: "Quarantines order-serving failures to the orders cell.",
                failureIsolationMode: "cell-quarantine",
                readinessScope: "dependency-aware",
                restartScope: "cell-only",
                dependencyIds: ["orders-cache", "orders-db"]));
            healthIsolations.Add(new CellHealthIsolationDescriptor(
                id: "reporting-cell-health",
                sourceModuleId: "cell-health-hosting-tests",
                cellId: "reporting-cell",
                displayName: "Reporting Cell Health Isolation",
                description: "Lets reporting workloads degrade independently from interactive cells.",
                failureIsolationMode: "degraded-serving",
                readinessScope: "best-effort",
                restartScope: "manual"));
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
