using System.Net.Http.Json;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Resilience;
using Cephalon.Abstractions.Technologies;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Edge.KubernetesGateway.Configuration;
using Cephalon.Edge.KubernetesGateway.Registration;
using Cephalon.Edge.KubernetesGateway.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

public sealed class KubernetesGatewayTrafficMaterializerAspNetCoreHostingTests
{
    [Fact]
    public async Task MapCephalonExposesKubernetesGatewayTrafficMaterializationSurface()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks()
            .AddCheck("cephalon.liveness", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live", "engine"])
            .AddCheck("cephalon.readiness", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["ready", "engine"]);
        builder.Services.AddSingleton<IRateLimitingRuntimeCatalog>(EmptyRateLimitingRuntimeCatalog.Instance);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:RouteId"] = "orders-to-public-ingress";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:AutomationMode"] = "automatic";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:TriggerMode"] = "source-or-target-health";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:ActionMode"] = "shed-load";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:MaterializationMode"] = "provider-managed";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:ProviderId"] =
            KubernetesGatewayTrafficMaterializerOptions.DefaultProviderId;
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:1:RouteId"] = "orders-to-admin-ingress";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:1:AutomationMode"] = "automatic";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:1:TriggerMode"] = "source-health";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:1:ActionMode"] = "prefer-local-route";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:1:MaterializationMode"] = "provider-managed";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:1:ProviderId"] =
            KubernetesGatewayTrafficMaterializerOptions.DefaultProviderId;
        builder.Services.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddModule(new KubernetesGatewayTrafficHostingTestModule());
            engine.AddKubernetesGatewayTrafficMaterializer(options =>
            {
                options.ControllerName = "cephalon.io/gateway-controller";
                options.GatewayClassName = "cephalon-public";
                options.GatewayNamespace = "edge-system";
                options.GatewayName = "public-gateway";
                options.ListenerName = "https";
                options.RouteNamespace = "edge-system";
                options.Routes.Add(new KubernetesGatewayTrafficRouteOptions
                {
                    RouteId = "orders-to-public-ingress",
                    HttpRouteName = "orders-public-ingress",
                    BackendNamespace = "orders-runtime",
                    BackendServiceName = "orders-api",
                    BackendPort = 8443,
                    BackendWeight = 100
                });
                options.Routes.Add(new KubernetesGatewayTrafficRouteOptions
                {
                    RouteId = "orders-to-admin-ingress",
                    HttpRouteName = "orders-admin-ingress",
                    GatewayName = "admin-gateway",
                    ListenerName = "admin-https",
                    BackendNamespace = "orders-admin",
                    BackendServiceName = "orders-admin-api",
                    BackendPort = 9443
                });
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var providerAutomations =
            await client.GetFromJsonAsync<CellTrafficAutomationRuntimeDescriptor[]>("/engine/cell-traffic-automations/providers/kubernetes-gateway");
        var surfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/cell-based-architecture");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(providerAutomations);
        Assert.Equal(2, providerAutomations.Length);
        Assert.Contains(providerAutomations, automation =>
            automation.RouteId == "orders-to-public-ingress" &&
            automation.ProviderMaterializerId == "kubernetes-gateway-materializer" &&
            automation.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Applied &&
            automation.MaterializationState == CellTrafficAutomationMaterializationStates.Applied &&
            automation.RuntimeMetadata["providerMaterialization.providerRouteId"] == "httproute/edge-system/orders-public-ingress");
        Assert.Contains(providerAutomations, automation =>
            automation.RouteId == "orders-to-admin-ingress" &&
            automation.ProviderMaterializerId == "kubernetes-gateway-materializer" &&
            automation.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Applied &&
            automation.RuntimeMetadata["providerMaterialization.gatewayName"] == "admin-gateway" &&
            automation.RuntimeMetadata["providerMaterialization.listenerName"] == "admin-https");

        Assert.NotNull(surfaces);
        var gatewaySurface = Assert.Single(
            surfaces,
            static surface => surface.SurfaceId == "kubernetes-gateway-traffic-materializations");
        Assert.Contains(gatewaySurface.Entries, entry =>
            entry.Metadata["routeId"] == "orders-to-public-ingress" &&
            entry.Metadata["gatewayName"] == "public-gateway" &&
            entry.Metadata["providerRouteId"] == "httproute/edge-system/orders-public-ingress" &&
            entry.Metadata["httpRouteBackendRefs"] == "service/orders-runtime/orders-api:8443@weight/100");
        Assert.Contains(gatewaySurface.Entries, entry =>
            entry.Metadata["routeId"] == "orders-to-admin-ingress" &&
            entry.Metadata["gatewayName"] == "admin-gateway" &&
            entry.Metadata["listenerName"] == "admin-https" &&
            entry.Metadata["providerRouteId"] == "httproute/edge-system/orders-admin-ingress");

        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.CellTrafficAutomations, automation =>
            automation.RouteId == "orders-to-public-ingress" &&
            automation.ProviderMaterializerId == "kubernetes-gateway-materializer" &&
            automation.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Applied);
        Assert.Contains(snapshot.CellTrafficAutomations, automation =>
            automation.RouteId == "orders-to-admin-ingress" &&
            automation.ProviderMaterializerId == "kubernetes-gateway-materializer" &&
            automation.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Applied);
    }

    [Fact]
    public async Task MapCephalonExposesObservedKubernetesGatewayTrafficMaterializationSurface()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks()
            .AddCheck("cephalon.liveness", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live", "engine"])
            .AddCheck("cephalon.readiness", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["ready", "engine"]);
        builder.Services.AddSingleton<IRateLimitingRuntimeCatalog>(EmptyRateLimitingRuntimeCatalog.Instance);
        builder.Services.AddSingleton<IKubernetesGatewayTrafficObservationSource>(
            new StaticObservationSource(static () => CreateObservedAppliedResult()));
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:RouteId"] = "orders-to-public-ingress";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:AutomationMode"] = "automatic";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:TriggerMode"] = "source-or-target-health";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:ActionMode"] = "shed-load";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:MaterializationMode"] = "provider-managed";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:0:ProviderId"] =
            KubernetesGatewayTrafficMaterializerOptions.DefaultProviderId;
        builder.Services.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddModule(new KubernetesGatewayTrafficHostingTestModule());
            engine.AddKubernetesGatewayTrafficMaterializer(options =>
            {
                options.ControllerName = "cephalon.io/gateway-controller";
                options.GatewayClassName = "cephalon-public";
                options.GatewayNamespace = "edge-system";
                options.GatewayName = "public-gateway";
                options.ListenerName = "https";
                options.RouteNamespace = "edge-system";
                options.Observation.Mode = KubernetesGatewayTrafficObservationModes.ObserveOnly;
                options.Observation.PollingIntervalSeconds = 60;
                options.Observation.StaleAfterSeconds = 180;
                options.Routes.Add(new KubernetesGatewayTrafficRouteOptions
                {
                    RouteId = "orders-to-public-ingress",
                    HttpRouteName = "orders-public-ingress",
                    BackendNamespace = "orders-runtime",
                    BackendServiceName = "orders-api",
                    BackendPort = 8443,
                    BackendWeight = 100
                });
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var providerAutomations =
            await client.GetFromJsonAsync<CellTrafficAutomationRuntimeDescriptor[]>("/engine/cell-traffic-automations/providers/kubernetes-gateway");
        var surfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/cell-based-architecture");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(providerAutomations);
        var automation = Assert.Single(providerAutomations);
        Assert.Equal("orders-to-public-ingress", automation.RouteId);
        Assert.Equal("kubernetes-gateway-materializer", automation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Applied, automation.ProviderMaterializationState);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Applied, automation.MaterializationState);
        Assert.Equal("observe-only", automation.RuntimeMetadata["providerMaterialization.providerAction"]);
        Assert.Equal("observe-only", automation.RuntimeMetadata["providerMaterialization.observationMode"]);
        Assert.Equal("gateway-api-status", automation.RuntimeMetadata["providerMaterialization.statusSource"]);
        Assert.Equal("true", automation.RuntimeMetadata["providerMaterialization.gatewayAcceptedCondition"]);
        Assert.Equal("true", automation.RuntimeMetadata["providerMaterialization.httpRouteResolvedRefsCondition"]);
        Assert.Equal("in-sync", automation.RuntimeMetadata["providerMaterialization.driftState"]);

        Assert.NotNull(surfaces);
        var gatewaySurface = Assert.Single(
            surfaces,
            static surface => surface.SurfaceId == "kubernetes-gateway-traffic-materializations");
        var observedEntry = Assert.Single(gatewaySurface.Entries, entry => entry.Metadata["routeId"] == "orders-to-public-ingress");
        Assert.Equal("observe-only", observedEntry.Metadata["providerAction"]);
        Assert.Equal("gateway-api-status", observedEntry.Metadata["statusSource"]);
        Assert.Equal("true", observedEntry.Metadata["gatewayAcceptedCondition"]);
        Assert.Equal("true", observedEntry.Metadata["httpRouteResolvedRefsCondition"]);
        Assert.Equal("in-sync", observedEntry.Metadata["driftState"]);

        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.CellTrafficAutomations, entry =>
            entry.RouteId == "orders-to-public-ingress" &&
            entry.ProviderMaterializerId == "kubernetes-gateway-materializer" &&
            entry.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Applied &&
            entry.RuntimeMetadata["providerMaterialization.statusSource"] == "gateway-api-status");
    }

    private static CellTrafficAutomationProviderMaterializationResult CreateObservedAppliedResult()
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["providerAction"] = "observe-only",
            ["observationMode"] = KubernetesGatewayTrafficObservationModes.ObserveOnly,
            ["statusSource"] = "gateway-api-status",
            ["gatewayNamespace"] = "edge-system",
            ["gatewayName"] = "public-gateway",
            ["providerRouteId"] = "httproute/edge-system/orders-public-ingress",
            ["controllerName"] = "cephalon.io/gateway-controller",
            ["resourceState"] = "available",
            ["gatewayExists"] = "true",
            ["httpRouteExists"] = "true",
            ["gatewayAcceptedCondition"] = "true",
            ["gatewayProgrammedCondition"] = "true",
            ["httpRouteAcceptedCondition"] = "true",
            ["httpRouteResolvedRefsCondition"] = "true",
            ["driftState"] = "in-sync",
            ["driftReasons"] = string.Empty,
            ["observationFreshUntilUtc"] = DateTimeOffset.UtcNow.AddMinutes(3).ToString("O")
        };

        return new CellTrafficAutomationProviderMaterializationResult(
            state: CellTrafficAutomationProviderMaterializationStates.Applied,
            observedAtUtc: DateTimeOffset.UtcNow,
            metadata: metadata);
    }

    private sealed class StaticObservationSource(
        Func<CellTrafficAutomationProviderMaterializationResult> factory)
        : IKubernetesGatewayTrafficObservationSource
    {
        public ValueTask<CellTrafficAutomationProviderMaterializationResult> ObserveAsync(
            CellTrafficAutomationRuntimeDescriptor automation,
            KubernetesGatewayTrafficRouteProjection projection,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(factory());
        }
    }

    private sealed class KubernetesGatewayTrafficHostingTestModule :
        ModuleBase,
        ICellBoundaryContributor,
        ICellRouteContributor,
        ICellHealthIsolationContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "kubernetes-gateway-traffic-hosting-tests",
            displayName: "Kubernetes Gateway Traffic Hosting Tests",
            description: "Provides cell topology and health isolation for Kubernetes Gateway ASP.NET Core hosting tests.");

        public void RegisterCellBoundaries(ICellBoundaryRegistry cells)
        {
            cells.Add(new CellBoundaryDescriptor(
                id: "orders-cell",
                sourceModuleId: "kubernetes-gateway-traffic-hosting-tests",
                displayName: "Orders Cell",
                description: "Keeps order-serving workloads together.",
                blastRadius: "regional",
                routingStrategy: "local-first",
                moduleIds: ["kubernetes-gateway-traffic-hosting-tests"]));
            cells.Add(new CellBoundaryDescriptor(
                id: "public-edge-cell",
                sourceModuleId: "kubernetes-gateway-traffic-hosting-tests",
                displayName: "Public Edge Cell",
                description: "Fronts public ingress traffic.",
                blastRadius: "public-edge",
                routingStrategy: "gateway-fanout"));
            cells.Add(new CellBoundaryDescriptor(
                id: "admin-edge-cell",
                sourceModuleId: "kubernetes-gateway-traffic-hosting-tests",
                displayName: "Admin Edge Cell",
                description: "Fronts administrative ingress traffic.",
                blastRadius: "admin-edge",
                routingStrategy: "gateway-fanout"));
        }

        public void RegisterCellRoutes(ICellRouteRegistry routes)
        {
            routes.Add(new CellRouteDescriptor(
                id: "orders-to-public-ingress",
                sourceModuleId: "kubernetes-gateway-traffic-hosting-tests",
                sourceCellId: "orders-cell",
                targetCellId: "public-edge-cell",
                displayName: "Orders To Public Ingress",
                description: "Projects order-serving traffic to the public ingress boundary.",
                routingStrategy: "gateway-managed",
                governanceMode: "policy-guarded",
                transportIds: ["rest-api"]));
            routes.Add(new CellRouteDescriptor(
                id: "orders-to-admin-ingress",
                sourceModuleId: "kubernetes-gateway-traffic-hosting-tests",
                sourceCellId: "orders-cell",
                targetCellId: "admin-edge-cell",
                displayName: "Orders To Admin Ingress",
                description: "Projects administrative order traffic to the admin ingress boundary.",
                routingStrategy: "gateway-managed",
                governanceMode: "policy-guarded",
                transportIds: ["rest-api"]));
        }

        public void RegisterCellHealthIsolations(ICellHealthIsolationRegistry healthIsolations)
        {
            healthIsolations.Add(new CellHealthIsolationDescriptor(
                id: "orders-cell-health",
                sourceModuleId: "kubernetes-gateway-traffic-hosting-tests",
                cellId: "orders-cell",
                displayName: "Orders Cell Health",
                description: "Contains order-serving failures inside the orders cell.",
                failureIsolationMode: "cell-quarantine",
                readinessScope: "dependency-aware",
                restartScope: "cell-only",
                dependencyIds: ["orders-db"]));
            healthIsolations.Add(new CellHealthIsolationDescriptor(
                id: "public-edge-health",
                sourceModuleId: "kubernetes-gateway-traffic-hosting-tests",
                cellId: "public-edge-cell",
                displayName: "Public Edge Health",
                description: "Contains public ingress failures.",
                failureIsolationMode: "degraded-serving",
                readinessScope: "best-effort",
                restartScope: "manual",
                dependencyIds: ["public-gateway"]));
            healthIsolations.Add(new CellHealthIsolationDescriptor(
                id: "admin-edge-health",
                sourceModuleId: "kubernetes-gateway-traffic-hosting-tests",
                cellId: "admin-edge-cell",
                displayName: "Admin Edge Health",
                description: "Contains admin ingress failures.",
                failureIsolationMode: "degraded-serving",
                readinessScope: "dependency-aware",
                restartScope: "manual",
                dependencyIds: ["admin-gateway"]));
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
