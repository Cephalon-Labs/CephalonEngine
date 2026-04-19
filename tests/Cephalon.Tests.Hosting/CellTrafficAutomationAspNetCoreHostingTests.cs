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

public sealed class CellTrafficAutomationAspNetCoreHostingTests
{
    [Fact]
    public async Task MapCephalonExposesCellTrafficAutomationsAndTechnologySurfaces()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks()
            .AddCheck("cephalon.liveness", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live", "engine"])
            .AddCheck("cephalon.readiness", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["ready", "engine"]);
        builder.Services.AddSingleton<IRateLimitingRuntimeCatalog>(EmptyRateLimitingRuntimeCatalog.Instance);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:DefaultAutomationMode"] = "automatic";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:DefaultActionMode"] = "shed-load";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:RouteId"] = "orders-to-platform-control";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:AutomationMode"] = "advisory";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:TriggerMode"] = "source-health";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:ActionMode"] = "prefer-local-route";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:MaterializationMode"] = "provider-managed";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:Notes"] = "Keep provider handoff explicit for control-plane traffic.";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:Metadata:handoff"] = "ingress-provider";
        builder.Services.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
            engine.AddModule(new CellTrafficAutomationHostingTestModule());
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

        var automations = await client.GetFromJsonAsync<CellTrafficAutomationRuntimeDescriptor[]>("/engine/cell-traffic-automations");
        var moduleAutomations = await client.GetFromJsonAsync<CellTrafficAutomationRuntimeDescriptor[]>("/engine/cell-traffic-automations/modules/cell-traffic-hosting-tests");
        var routeAutomation = await client.GetFromJsonAsync<CellTrafficAutomationRuntimeDescriptor>("/engine/cell-traffic-automations/routes/orders-to-platform-control");
        var sourceCellAutomations = await client.GetFromJsonAsync<CellTrafficAutomationRuntimeDescriptor[]>("/engine/cell-traffic-automations/source-cells/orders-cell");
        var targetCellAutomations = await client.GetFromJsonAsync<CellTrafficAutomationRuntimeDescriptor[]>("/engine/cell-traffic-automations/target-cells/reporting-cell");
        var healthIsolationAutomations = await client.GetFromJsonAsync<CellTrafficAutomationRuntimeDescriptor[]>("/engine/cell-traffic-automations/health-isolations/orders-cell-health");
        var surfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/cell-based-architecture");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(automations);
        Assert.Equal(2, automations.Length);
        Assert.Contains(automations, candidate =>
            candidate.RouteId == "orders-to-reporting" &&
            candidate.AutomationMode == "automatic");

        Assert.NotNull(moduleAutomations);
        Assert.Equal(2, moduleAutomations.Length);

        Assert.NotNull(routeAutomation);
        Assert.Equal("advisory", routeAutomation.AutomationMode);
        Assert.Equal("source-health", routeAutomation.TriggerMode);
        Assert.Equal("prefer-local-route", routeAutomation.ActionMode);
        Assert.Equal("provider-managed", routeAutomation.MaterializationMode);
        Assert.Equal("cell-route", routeAutomation.PolicySource);
        Assert.Equal("Keep provider handoff explicit for control-plane traffic.", routeAutomation.RuntimeMetadata["note"]);

        Assert.NotNull(sourceCellAutomations);
        Assert.Equal(2, sourceCellAutomations.Length);

        Assert.NotNull(targetCellAutomations);
        var reportingAutomation = Assert.Single(targetCellAutomations);
        Assert.Equal("orders-to-reporting", reportingAutomation.RouteId);

        Assert.NotNull(healthIsolationAutomations);
        Assert.Equal(2, healthIsolationAutomations.Length);

        Assert.NotNull(surfaces);
        var trafficAutomationSurface = Assert.Single(
            surfaces,
            static surface => surface.SurfaceId == "cell-traffic-automations");
        Assert.Contains(trafficAutomationSurface.Entries, entry =>
            entry.Id == "orders-to-platform-control" &&
            entry.Metadata["materializationMode"] == "provider-managed" &&
            entry.Metadata["handoff"] == "ingress-provider");

        Assert.NotNull(snapshot);
        Assert.Equal(2, snapshot.CellTrafficAutomations.Count);
        Assert.Contains(snapshot.CellTrafficAutomations, candidate =>
            candidate.RouteId == "orders-to-reporting" &&
            candidate.TriggerMode == "source-or-target-health");
    }

    private sealed class CellTrafficAutomationHostingTestModule :
        ModuleBase,
        ICellBoundaryContributor,
        ICellRouteContributor,
        ICellHealthIsolationContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "cell-traffic-hosting-tests",
            displayName: "Cell Traffic Hosting Tests",
            description: "Provides cell topology, health isolation, and route governance for ASP.NET Core traffic automation tests.",
            dependsOn: [typeof(PlatformTestModule), typeof(DiscoveryTestModule)]);

        public void RegisterCellBoundaries(ICellBoundaryRegistry cells)
        {
            cells.Add(new CellBoundaryDescriptor(
                id: "orders-cell",
                sourceModuleId: "cell-traffic-hosting-tests",
                displayName: "Orders Cell",
                description: "Keeps order-facing workloads together.",
                blastRadius: "regional",
                routingStrategy: "local-first",
                moduleIds: ["cell-traffic-hosting-tests", "discovery"]));
            cells.Add(new CellBoundaryDescriptor(
                id: "reporting-cell",
                sourceModuleId: "cell-traffic-hosting-tests",
                displayName: "Reporting Cell",
                description: "Keeps reporting workloads away from interactive traffic.",
                blastRadius: "analytics-only",
                routingStrategy: "async-replica"));
        }

        public void RegisterCellRoutes(ICellRouteRegistry routes)
        {
            routes.Add(new CellRouteDescriptor(
                id: "orders-to-reporting",
                sourceModuleId: "cell-traffic-hosting-tests",
                sourceCellId: "orders-cell",
                targetCellId: "reporting-cell",
                displayName: "Orders To Reporting",
                description: "Moves order-serving traffic toward reporting projections when analytics paths stay healthy.",
                routingStrategy: "replica-fanout",
                governanceMode: "policy-guarded",
                transportIds: ["rest-api"]));
            routes.Add(new CellRouteDescriptor(
                id: "orders-to-platform-control",
                sourceModuleId: "cell-traffic-hosting-tests",
                sourceCellId: "orders-cell",
                targetCellId: "platform-control",
                displayName: "Orders To Platform Control",
                description: "Keeps orders cell coordination with the shared platform control cell explicit.",
                routingStrategy: "control-plane",
                governanceMode: "capability-gated",
                transportIds: ["rest-api"],
                requiredCapabilityKey: "platform.control-plane"));
        }

        public void RegisterCellHealthIsolations(ICellHealthIsolationRegistry healthIsolations)
        {
            healthIsolations.Add(new CellHealthIsolationDescriptor(
                id: "orders-cell-health",
                sourceModuleId: "cell-traffic-hosting-tests",
                cellId: "orders-cell",
                displayName: "Orders Cell Health Isolation",
                description: "Contains order-serving failures to the orders cell.",
                failureIsolationMode: "cell-quarantine",
                readinessScope: "dependency-aware",
                restartScope: "cell-only",
                dependencyIds: ["orders-db"]));
            healthIsolations.Add(new CellHealthIsolationDescriptor(
                id: "reporting-cell-health",
                sourceModuleId: "cell-traffic-hosting-tests",
                cellId: "reporting-cell",
                displayName: "Reporting Cell Health Isolation",
                description: "Lets reporting workloads degrade independently from interactive cells.",
                failureIsolationMode: "degraded-serving",
                readinessScope: "best-effort",
                restartScope: "manual",
                dependencyIds: ["reporting-replica"]));
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
