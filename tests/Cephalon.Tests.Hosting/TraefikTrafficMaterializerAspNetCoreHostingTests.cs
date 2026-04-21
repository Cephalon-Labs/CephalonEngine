using System.Globalization;
using System.Net.Http.Json;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Resilience;
using Cephalon.Abstractions.Technologies;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Edge.Traefik.Configuration;
using Cephalon.Edge.Traefik.Registration;
using Cephalon.Edge.Traefik.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

public sealed class TraefikTrafficMaterializerAspNetCoreHostingTests
{
    [Fact]
    public async Task MapCephalonExposesTraefikTrafficMaterializationSurface()
    {
        var builder = CreateBuilder();
        builder.Services.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddModule(new TraefikTrafficHostingTestModule());
            ConfigureTraefik(engine, controlPlaneMode: TraefikTrafficObservationModes.ConfiguredIntent);
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var providerAutomations =
            await client.GetFromJsonAsync<CellTrafficAutomationRuntimeDescriptor[]>("/engine/cell-traffic-automations/providers/traefik");
        var surfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/cell-based-architecture");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(providerAutomations);
        Assert.Equal(2, providerAutomations.Length);
        Assert.Contains(providerAutomations, automation =>
            automation.RouteId == "orders-to-public-ingress" &&
            automation.ProviderMaterializerId == "traefik-materializer" &&
            automation.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Pending &&
            automation.MaterializationState == CellTrafficAutomationMaterializationStates.Pending &&
            automation.MaterializationConditions.Any(condition =>
                condition.Dimension == CellTrafficAutomationMaterializationConditionDimensions.Provider &&
                condition.ConditionId == "runtime-observable") &&
            automation.RuntimeMetadata["providerMaterialization.providerRouteId"] == "ingressroute/edge-traefik/orders-public-ingress" &&
            automation.RuntimeMetadata["providerMaterialization.serviceRefs"] == "service/orders-runtime/orders-api:8443@weight/100" &&
            automation.RuntimeMetadata["providerMaterialization.middlewareRefs"] == "middleware/edge-traefik/secure-headers,middleware/edge-security/orders-rate-limit" &&
            automation.RuntimeMetadata["providerMaterialization.ownershipState"] == CellTrafficAutomationOwnershipStates.Requested &&
            automation.RuntimeMetadata["providerMaterialization.lifecycleAction"] == CellTrafficAutomationLifecycleActions.Project &&
            automation.RuntimeMetadata["providerMaterialization.ingressRouteWriteAction"] == "none");
        Assert.Contains(providerAutomations, automation =>
            automation.RouteId == "orders-to-admin-ingress" &&
            automation.ProviderMaterializerId == "traefik-materializer" &&
            automation.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Pending &&
            automation.RuntimeMetadata["providerMaterialization.entryPoints"] == "admin-websecure" &&
            automation.RuntimeMetadata["providerMaterialization.providerRouteId"] == "ingressroute/edge-traefik/orders-admin-ingress");

        Assert.NotNull(surfaces);
        var traefikSurface = Assert.Single(
            surfaces,
            static surface => surface.SurfaceId == "traefik-ingressroute-traffic-materializations");
        Assert.Contains(traefikSurface.Entries, entry =>
            entry.Metadata["routeId"] == "orders-to-public-ingress" &&
            entry.Metadata["providerRouteId"] == "ingressroute/edge-traefik/orders-public-ingress" &&
            entry.Metadata["entryPoints"] == "websecure" &&
            entry.Metadata["matchRule"] == "Host(`orders.example.com`) && PathPrefix(`/orders`)" &&
            entry.Metadata["middlewareRefs"] == "middleware/edge-traefik/secure-headers,middleware/edge-security/orders-rate-limit" &&
            entry.Metadata["tlsOptionsRef"] == "tlsoption/edge-security/strict-mtls");
        Assert.Contains(traefikSurface.Entries, entry =>
            entry.Metadata["routeId"] == "orders-to-admin-ingress" &&
            entry.Metadata["providerRouteId"] == "ingressroute/edge-traefik/orders-admin-ingress" &&
            entry.Metadata["entryPoints"] == "admin-websecure" &&
            entry.Metadata["tlsSecretName"] == "orders-admin-tls");

        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.CellTrafficAutomations, automation =>
            automation.RouteId == "orders-to-public-ingress" &&
            automation.ProviderMaterializerId == "traefik-materializer" &&
            automation.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Pending);
        Assert.Contains(snapshot.CellTrafficAutomations, automation =>
            automation.RouteId == "orders-to-admin-ingress" &&
            automation.ProviderMaterializerId == "traefik-materializer" &&
            automation.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Pending);
    }

    [Fact]
    public async Task MapCephalonExposesObservedTraefikTrafficMaterializationSurface()
    {
        var builder = CreateBuilder();
        builder.Services.AddSingleton<ITraefikTrafficObservationSource>(
            new StaticObservationSource(() => CreateObservedAppliedResult()));
        builder.Services.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddModule(new TraefikTrafficHostingTestModule());
            ConfigureTraefik(engine, controlPlaneMode: TraefikTrafficObservationModes.ObserveOnly);
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var providerAutomations =
            await client.GetFromJsonAsync<CellTrafficAutomationRuntimeDescriptor[]>("/engine/cell-traffic-automations/providers/traefik");
        var surfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/cell-based-architecture");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(providerAutomations);
        Assert.Equal(2, providerAutomations.Length);
        Assert.Contains(providerAutomations, automation =>
            automation.RouteId == "orders-to-public-ingress" &&
            automation.ProviderMaterializerId == "traefik-materializer" &&
            automation.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Applied &&
            automation.MaterializationState == CellTrafficAutomationMaterializationStates.Applied &&
            automation.MaterializationConditions.Any(condition =>
                condition.Dimension == CellTrafficAutomationMaterializationConditionDimensions.Provider &&
                condition.ConditionId == "ingress-route-present" &&
                condition.State == CellTrafficAutomationMaterializationConditionStates.Met) &&
            automation.RuntimeMetadata["providerMaterialization.providerAction"] == "observe-only" &&
            automation.RuntimeMetadata["providerMaterialization.observationMode"] == TraefikTrafficObservationModes.ObserveOnly &&
            automation.RuntimeMetadata["providerMaterialization.statusSource"] == "traefik-ingressroute-observation" &&
            automation.RuntimeMetadata["providerMaterialization.resourceState"] == "available" &&
            automation.RuntimeMetadata["providerMaterialization.ingressRouteExists"] == "true" &&
            automation.RuntimeMetadata["providerMaterialization.observedServiceRefs"] == "service/orders-runtime/orders-api:8443@weight/100" &&
            automation.RuntimeMetadata["providerMaterialization.ownershipState"] == CellTrafficAutomationOwnershipStates.Owned &&
            automation.RuntimeMetadata["providerMaterialization.dependencyState"] == CellTrafficAutomationDependencyStates.Satisfied &&
            automation.RuntimeMetadata["providerMaterialization.driftState"] == CellTrafficAutomationDriftStates.InSync);

        Assert.NotNull(surfaces);
        var traefikSurface = Assert.Single(
            surfaces,
            static surface => surface.SurfaceId == "traefik-ingressroute-traffic-materializations");
        Assert.Contains(traefikSurface.Entries, entry =>
            entry.Metadata["routeId"] == "orders-to-public-ingress" &&
            entry.Metadata["providerAction"] == "observe-only" &&
            entry.Metadata["statusSource"] == "traefik-ingressroute-observation" &&
            entry.Metadata["observedServiceRefs"] == "service/orders-runtime/orders-api:8443@weight/100" &&
            entry.Metadata["observedTlsOptionsRef"] == "tlsoption/edge-security/strict-mtls");

        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.CellTrafficAutomations, automation =>
            automation.RouteId == "orders-to-public-ingress" &&
            automation.ProviderMaterializerId == "traefik-materializer" &&
            automation.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Applied &&
            automation.RuntimeMetadata["providerMaterialization.statusSource"] == "traefik-ingressroute-observation");
    }

    [Fact]
    public async Task MapCephalonExposesApplyAndReconcileTraefikTrafficMaterializationSurface()
    {
        var builder = CreateBuilder();
        builder.Services.AddSingleton<ITraefikTrafficApplyService>(
            new StaticApplyService(() => CreateApplyPendingResult("created")));
        builder.Services.AddSingleton<ITraefikTrafficObservationSource>(
            new StaticObservationSource(() => CreateObservedAppliedResult()));
        builder.Services.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddModule(new TraefikTrafficHostingTestModule());
            ConfigureTraefik(engine, controlPlaneMode: TraefikTrafficObservationModes.ApplyAndReconcile);
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var providerAutomations =
            await client.GetFromJsonAsync<CellTrafficAutomationRuntimeDescriptor[]>("/engine/cell-traffic-automations/providers/traefik");
        var surfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/cell-based-architecture");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(providerAutomations);
        Assert.Contains(providerAutomations, automation =>
            automation.RouteId == "orders-to-public-ingress" &&
            automation.ProviderMaterializerId == "traefik-materializer" &&
            automation.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Applied &&
            automation.MaterializationState == CellTrafficAutomationMaterializationStates.Applied &&
            automation.MaterializationConditions.Any(condition =>
                condition.Dimension == CellTrafficAutomationMaterializationConditionDimensions.Provider &&
                condition.ConditionId == "ingress-route-present" &&
                condition.State == CellTrafficAutomationMaterializationConditionStates.Met) &&
            automation.RuntimeMetadata["providerMaterialization.providerAction"] == "apply-and-reconcile" &&
            automation.RuntimeMetadata["providerMaterialization.observationMode"] == TraefikTrafficObservationModes.ApplyAndReconcile &&
            automation.RuntimeMetadata["providerMaterialization.statusSource"] == "traefik-ingressroute-observation" &&
            automation.RuntimeMetadata["providerMaterialization.ingressRouteWriteAction"] == "created" &&
            automation.RuntimeMetadata["providerMaterialization.resourceState"] == "available" &&
            automation.RuntimeMetadata["providerMaterialization.ownershipState"] == CellTrafficAutomationOwnershipStates.Owned &&
            automation.RuntimeMetadata["providerMaterialization.dependencyState"] == CellTrafficAutomationDependencyStates.Satisfied &&
            automation.RuntimeMetadata["providerMaterialization.driftState"] == CellTrafficAutomationDriftStates.InSync &&
            automation.RuntimeMetadata["providerMaterialization.lifecycleAction"] == CellTrafficAutomationLifecycleActions.Create);

        Assert.NotNull(surfaces);
        var traefikSurface = Assert.Single(
            surfaces,
            static surface => surface.SurfaceId == "traefik-ingressroute-traffic-materializations");
        Assert.Contains(traefikSurface.Entries, entry =>
            entry.Metadata["routeId"] == "orders-to-public-ingress" &&
            entry.Metadata["providerAction"] == "apply-and-reconcile" &&
            entry.Metadata["statusSource"] == "traefik-ingressroute-observation" &&
            entry.Metadata["ingressRouteWriteAction"] == "created" &&
            entry.Metadata["lifecycleAction"] == CellTrafficAutomationLifecycleActions.Create &&
            entry.Metadata["observedServiceRefs"] == "service/orders-runtime/orders-api:8443@weight/100");

        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.CellTrafficAutomations, automation =>
            automation.RouteId == "orders-to-public-ingress" &&
            automation.ProviderMaterializerId == "traefik-materializer" &&
            automation.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Applied &&
            automation.RuntimeMetadata["providerMaterialization.ingressRouteWriteAction"] == "created");
    }

    [Fact]
    public async Task MapCephalonExposesTraefikCleanupSweepMetadataOnExistingSurfaces()
    {
        var builder = CreateBuilder();
        builder.Services.AddSingleton<ITraefikTrafficApplyService>(
            new StaticApplyService(() => CreateApplyPendingResult("created")));
        builder.Services.AddSingleton<ITraefikTrafficObservationSource>(
            new StaticObservationSource(() => CreateObservedAppliedResult(), static () => CreateCleanupSweepResult()));
        builder.Services.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddModule(new TraefikTrafficHostingTestModule());
            engine.AddTraefikTrafficMaterializer(options =>
            {
                options.RouteNamespace = "edge-traefik";
                options.EntryPoints.Add("websecure");
                options.Observation.Mode = TraefikTrafficObservationModes.ApplyAndReconcile;
                options.Observation.PollingIntervalSeconds = 1;
                options.Observation.StaleAfterSeconds = 180;
                options.Observation.EnableCleanupSweep = true;
                options.Routes.Add(new TraefikIngressRouteOptions
                {
                    RouteId = "orders-to-public-ingress",
                    IngressRouteName = "orders-public-ingress",
                    MatchRule = "Host(`orders.example.com`) && PathPrefix(`/orders`)",
                    BackendNamespace = "orders-runtime",
                    BackendServiceName = "orders-api",
                    BackendPort = 8443,
                    BackendWeight = 100,
                    BackendScheme = "https",
                    PassHostHeader = true,
                    TlsSecretName = "orders-public-tls",
                    TlsOptionsName = "strict-mtls",
                    TlsOptionsNamespace = "edge-security"
                });
                options.Routes[0].Middlewares.Add(new TraefikMiddlewareReferenceOptions
                {
                    Name = "secure-headers"
                });
                options.Routes[0].Middlewares.Add(new TraefikMiddlewareReferenceOptions
                {
                    Name = "orders-rate-limit",
                    Namespace = "edge-security"
                });
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        await Task.Delay(TimeSpan.FromMilliseconds(1400));

        var client = app.GetTestClient();
        var providerAutomations =
            await client.GetFromJsonAsync<CellTrafficAutomationRuntimeDescriptor[]>("/engine/cell-traffic-automations/providers/traefik");
        var surfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/cell-based-architecture");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(providerAutomations);
        Assert.Contains(providerAutomations, automation =>
            automation.RouteId == "orders-to-public-ingress" &&
            automation.RuntimeMetadata["providerMaterialization.cleanup.cleanupStrategy"] == "primary-and-owned-dependencies" &&
            automation.RuntimeMetadata["providerMaterialization.cleanupSweepEnabled"] == "true" &&
            automation.RuntimeMetadata["providerMaterialization.cleanupState"] == "applied" &&
            automation.RuntimeMetadata["providerMaterialization.cleanup.candidateCount"] == "4" &&
            automation.RuntimeMetadata["providerMaterialization.cleanup.deletedTransferredResourceCount"] == "2" &&
            automation.RuntimeMetadata["providerMaterialization.cleanup.prunedOrphanResourceCount"] == "2" &&
            automation.RuntimeMetadata["providerMaterialization.cleanup.dependencyCandidateCount"] == "2" &&
            automation.RuntimeMetadata["providerMaterialization.cleanup.removedDependencyResourceCount"] == "2");

        Assert.NotNull(surfaces);
        var traefikSurface = Assert.Single(
            surfaces,
            static surface => surface.SurfaceId == "traefik-ingressroute-traffic-materializations");
        Assert.Contains(traefikSurface.Entries, entry =>
            entry.Metadata["routeId"] == "orders-to-public-ingress" &&
            entry.Metadata["cleanupState"] == "applied" &&
            entry.Metadata["cleanup.cleanupStrategy"] == "primary-and-owned-dependencies" &&
            entry.Metadata["cleanup.candidateCount"] == "4" &&
            entry.Metadata["cleanup.dependencyCandidateCount"] == "2" &&
            entry.Metadata["cleanup.lifecycleActions"] == "delete,prune");

        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.CellTrafficAutomations, automation =>
            automation.RouteId == "orders-to-public-ingress" &&
            automation.RuntimeMetadata["providerMaterialization.cleanupState"] == "applied");
    }

    private static WebApplicationBuilder CreateBuilder()
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
            TraefikTrafficMaterializerOptions.DefaultProviderId;
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:1:RouteId"] = "orders-to-admin-ingress";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:1:AutomationMode"] = "automatic";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:1:TriggerMode"] = "source-health";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:1:ActionMode"] = "prefer-local-route";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:1:MaterializationMode"] = "provider-managed";
        builder.Configuration[$"{EngineSettings.SectionName}:Cells:TrafficAutomation:Routes:1:ProviderId"] =
            TraefikTrafficMaterializerOptions.DefaultProviderId;
        return builder;
    }

    private static void ConfigureTraefik(EngineBuilder engine, string controlPlaneMode)
    {
        engine.AddTraefikTrafficMaterializer(options =>
        {
            options.RouteNamespace = "edge-traefik";
            options.EntryPoints.Add("websecure");
            options.Observation.Mode = controlPlaneMode;
            options.Observation.PollingIntervalSeconds = 60;
            options.Observation.StaleAfterSeconds = 180;
            options.Routes.Add(new TraefikIngressRouteOptions
            {
                RouteId = "orders-to-public-ingress",
                IngressRouteName = "orders-public-ingress",
                MatchRule = "Host(`orders.example.com`) && PathPrefix(`/orders`)",
                BackendNamespace = "orders-runtime",
                BackendServiceName = "orders-api",
                BackendPort = 8443,
                BackendWeight = 100,
                BackendScheme = "https",
                PassHostHeader = true,
                TlsSecretName = "orders-public-tls",
                TlsOptionsName = "strict-mtls",
                TlsOptionsNamespace = "edge-security"
            });
            options.Routes[0].Middlewares.Add(new TraefikMiddlewareReferenceOptions
            {
                Name = "secure-headers"
            });
            options.Routes[0].Middlewares.Add(new TraefikMiddlewareReferenceOptions
            {
                Name = "orders-rate-limit",
                Namespace = "edge-security"
            });
            options.Routes.Add(new TraefikIngressRouteOptions
            {
                RouteId = "orders-to-admin-ingress",
                IngressRouteName = "orders-admin-ingress",
                MatchRule = "Host(`admin.example.com`) && PathPrefix(`/orders`)",
                BackendNamespace = "orders-admin",
                BackendServiceName = "orders-admin-api",
                BackendPort = 9443,
                TlsSecretName = "orders-admin-tls"
            });
            options.Routes[1].EntryPoints.Add("admin-websecure");
            options.Routes[1].Middlewares.Add(new TraefikMiddlewareReferenceOptions
            {
                Name = "admin-authn",
                Namespace = "edge-security"
            });
        });
    }

    private static CellTrafficAutomationProviderMaterializationResult CreateObservedAppliedResult()
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["providerAction"] = "observe-only",
            ["observationMode"] = TraefikTrafficObservationModes.ObserveOnly,
            ["statusSource"] = "traefik-ingressroute-observation",
            ["resourceState"] = "available",
            ["providerRouteId"] = "ingressroute/edge-traefik/orders-public-ingress",
            ["ingressRouteResourceId"] = "ingressroute/edge-traefik/orders-public-ingress",
            ["ingressRouteExists"] = "true",
            ["observedIngressRouteName"] = "orders-public-ingress",
            ["observedIngressRouteNamespace"] = "edge-traefik",
            ["observedIngressRouteGeneration"] = "3",
            ["observedEntryPoints"] = "websecure",
            ["observedEntryPointCount"] = "1",
            ["observedRouteCount"] = "1",
            ["observedMatchRule"] = "Host(`orders.example.com`) && PathPrefix(`/orders`)",
            ["observedRouteKind"] = "Rule",
            ["observedMiddlewareRefs"] = "middleware/edge-traefik/secure-headers,middleware/edge-security/orders-rate-limit",
            ["observedServiceRefs"] = "service/orders-runtime/orders-api:8443@weight/100",
            ["observedBackendScheme"] = "https",
            ["observedBackendPassHostHeader"] = "true",
            ["observedTlsSecretName"] = "orders-public-tls",
            ["observedTlsOptionsRef"] = "tlsoption/edge-security/strict-mtls",
            ["ownershipState"] = CellTrafficAutomationOwnershipStates.Owned,
            ["managedBy"] = "edge-traefik",
            ["observedAutomationId"] = "orders-to-public-ingress",
            ["observedRouteId"] = "orders-to-public-ingress",
            ["observedSourceModuleId"] = "traefik-traffic-hosting-tests",
            ["dependencyState"] = CellTrafficAutomationDependencyStates.Satisfied,
            ["backendServiceExists"] = "true",
            ["tlsOptionsExists"] = "true",
            ["tlsSecretExists"] = "true",
            ["missingMiddlewareRefs"] = string.Empty,
            ["dependencyMissingRefs"] = string.Empty,
            ["driftState"] = CellTrafficAutomationDriftStates.InSync,
            ["driftReasons"] = string.Empty,
            ["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Observe,
            ["observationFreshUntilUtc"] = DateTimeOffset.UtcNow.AddMinutes(3).ToString("O", CultureInfo.InvariantCulture)
        };

        return new CellTrafficAutomationProviderMaterializationResult(
            CellTrafficAutomationProviderMaterializationStates.Applied,
            DateTimeOffset.UtcNow,
            metadata: metadata);
    }

    private static CellTrafficAutomationProviderMaterializationResult CreateApplyPendingResult(string writeAction)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["providerAction"] = TraefikTrafficObservationModes.ApplyAndReconcile,
            ["observationMode"] = TraefikTrafficObservationModes.ApplyAndReconcile,
            ["statusSource"] = "control-plane-apply",
            ["resourceState"] = "write-succeeded",
            ["ingressRouteExists"] = "true",
            ["providerRouteId"] = "ingressroute/edge-traefik/orders-public-ingress",
            ["ingressRouteResourceId"] = "ingressroute/edge-traefik/orders-public-ingress",
            ["ingressRouteNamespace"] = "edge-traefik",
            ["ingressRouteName"] = "orders-public-ingress",
            ["ownershipState"] = CellTrafficAutomationOwnershipStates.Owned,
            ["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown,
            ["driftState"] = CellTrafficAutomationDriftStates.Reconciling,
            ["driftReasons"] = string.Empty,
            ["missingMiddlewareRefs"] = string.Empty,
            ["dependencyMissingRefs"] = string.Empty,
            ["lifecycleAction"] = string.Equals(writeAction, "created", StringComparison.OrdinalIgnoreCase)
                ? CellTrafficAutomationLifecycleActions.Create
                : CellTrafficAutomationLifecycleActions.Replace,
            ["ingressRouteWriteAction"] = writeAction,
            ["ingressRouteAppliedGeneration"] = writeAction == "created" ? "1" : "2"
        };

        return new CellTrafficAutomationProviderMaterializationResult(
            CellTrafficAutomationProviderMaterializationStates.Pending,
            DateTimeOffset.UtcNow,
            metadata: metadata);
    }

    private static TraefikTrafficCleanupSweepResult CreateCleanupSweepResult()
    {
        return new TraefikTrafficCleanupSweepResult(
            DateTimeOffset.UtcNow,
            "applied",
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["activeAutomationCount"] = "1",
                ["cleanupStrategy"] = "primary-and-owned-dependencies",
                ["candidateCount"] = "4",
                ["removedResourceCount"] = "4",
                ["deletedTransferredResourceCount"] = "2",
                ["prunedOrphanResourceCount"] = "2",
                ["primaryCandidateCount"] = "2",
                ["removedPrimaryResourceCount"] = "2",
                ["deletedTransferredPrimaryResourceCount"] = "1",
                ["prunedOrphanPrimaryResourceCount"] = "1",
                ["primaryResourceIds"] = "ingressroute/edge-traefik/orders-public-ingress-legacy,ingressroute/edge-traefik/orders-public-ingress-stale",
                ["dependencyCandidateCount"] = "2",
                ["removedDependencyResourceCount"] = "2",
                ["deletedTransferredDependencyResourceCount"] = "1",
                ["prunedOrphanDependencyResourceCount"] = "1",
                ["dependencyResourceIds"] = "middleware/edge-security/orders-rate-limit-legacy,tlsoption/edge-security/strict-mtls-stale",
                ["dependencyKinds"] = "middleware,tlsoption",
                ["dependencyNamespaces"] = "edge-security",
                ["resourceIds"] = "ingressroute/edge-traefik/orders-public-ingress-legacy,ingressroute/edge-traefik/orders-public-ingress-stale,middleware/edge-security/orders-rate-limit-legacy,tlsoption/edge-security/strict-mtls-stale",
                ["lifecycleActions"] = "delete,prune",
                ["namespaces"] = "edge-traefik"
            });
    }

    private sealed class StaticObservationSource(
        Func<CellTrafficAutomationProviderMaterializationResult> factory,
        Func<TraefikTrafficCleanupSweepResult>? cleanupFactory = null)
        : ITraefikTrafficObservationSource
    {
        public ValueTask<CellTrafficAutomationProviderMaterializationResult> ObserveAsync(
            CellTrafficAutomationRuntimeDescriptor automation,
            TraefikIngressRouteProjection projection,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(factory());
        }

        public ValueTask<TraefikTrafficCleanupSweepResult> SweepCleanupAsync(
            IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> activeAutomations,
            IReadOnlyCollection<TraefikIngressRouteProjection> activeProjections,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(
                cleanupFactory?.Invoke() ??
                new TraefikTrafficCleanupSweepResult(DateTimeOffset.UtcNow, "idle"));
        }
    }

    private sealed class StaticApplyService(
        Func<CellTrafficAutomationProviderMaterializationResult> factory)
        : ITraefikTrafficApplyService
    {
        public ValueTask<CellTrafficAutomationProviderMaterializationResult> ApplyAsync(
            CellTrafficAutomationRuntimeDescriptor automation,
            TraefikIngressRouteProjection projection,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(factory());
        }
    }

    private sealed class TraefikTrafficHostingTestModule :
        ModuleBase,
        ICellBoundaryContributor,
        ICellRouteContributor,
        ICellHealthIsolationContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "traefik-traffic-hosting-tests",
            displayName: "Traefik Traffic Hosting Tests",
            description: "Provides cell topology and health isolation for Traefik ASP.NET Core hosting tests.");

        public void RegisterCellBoundaries(ICellBoundaryRegistry cells)
        {
            cells.Add(new CellBoundaryDescriptor(
                id: "orders-cell",
                sourceModuleId: "traefik-traffic-hosting-tests",
                displayName: "Orders Cell",
                description: "Keeps order-serving workloads together.",
                blastRadius: "regional",
                routingStrategy: "local-first",
                moduleIds: ["traefik-traffic-hosting-tests"]));
            cells.Add(new CellBoundaryDescriptor(
                id: "public-edge-cell",
                sourceModuleId: "traefik-traffic-hosting-tests",
                displayName: "Public Edge Cell",
                description: "Fronts public ingress traffic.",
                blastRadius: "public-edge",
                routingStrategy: "edge-fanout"));
            cells.Add(new CellBoundaryDescriptor(
                id: "admin-edge-cell",
                sourceModuleId: "traefik-traffic-hosting-tests",
                displayName: "Admin Edge Cell",
                description: "Fronts administrative ingress traffic.",
                blastRadius: "admin-edge",
                routingStrategy: "edge-fanout"));
        }

        public void RegisterCellRoutes(ICellRouteRegistry routes)
        {
            routes.Add(new CellRouteDescriptor(
                id: "orders-to-public-ingress",
                sourceModuleId: "traefik-traffic-hosting-tests",
                sourceCellId: "orders-cell",
                targetCellId: "public-edge-cell",
                displayName: "Orders To Public Ingress",
                description: "Projects order-serving traffic to the public edge boundary.",
                routingStrategy: "ingressroute-managed",
                governanceMode: "policy-guarded",
                transportIds: ["rest-api"]));
            routes.Add(new CellRouteDescriptor(
                id: "orders-to-admin-ingress",
                sourceModuleId: "traefik-traffic-hosting-tests",
                sourceCellId: "orders-cell",
                targetCellId: "admin-edge-cell",
                displayName: "Orders To Admin Ingress",
                description: "Projects administrative order traffic to the admin edge boundary.",
                routingStrategy: "ingressroute-managed",
                governanceMode: "policy-guarded",
                transportIds: ["rest-api"]));
        }

        public void RegisterCellHealthIsolations(ICellHealthIsolationRegistry healthIsolations)
        {
            healthIsolations.Add(new CellHealthIsolationDescriptor(
                id: "orders-cell-health",
                sourceModuleId: "traefik-traffic-hosting-tests",
                cellId: "orders-cell",
                displayName: "Orders Cell Health",
                description: "Contains order-serving failures inside the orders cell.",
                failureIsolationMode: "cell-quarantine",
                readinessScope: "dependency-aware",
                restartScope: "cell-only",
                dependencyIds: ["orders-db"]));
            healthIsolations.Add(new CellHealthIsolationDescriptor(
                id: "public-edge-health",
                sourceModuleId: "traefik-traffic-hosting-tests",
                cellId: "public-edge-cell",
                displayName: "Public Edge Health",
                description: "Contains public ingress failures.",
                failureIsolationMode: "degraded-serving",
                readinessScope: "best-effort",
                restartScope: "manual",
                dependencyIds: ["public-ingress"]));
            healthIsolations.Add(new CellHealthIsolationDescriptor(
                id: "admin-edge-health",
                sourceModuleId: "traefik-traffic-hosting-tests",
                cellId: "admin-edge-cell",
                displayName: "Admin Edge Health",
                description: "Contains admin ingress failures.",
                failureIsolationMode: "degraded-serving",
                readinessScope: "dependency-aware",
                restartScope: "manual",
                dependencyIds: ["admin-ingress"]));
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
