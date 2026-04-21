using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Edge.Traefik.Configuration;
using Cephalon.Edge.Traefik.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Composition;

public sealed class TraefikTrafficMaterializerTests
{
    [Fact]
    public async Task HostedServiceSelectsTraefikMaterializerOverGenericFallbackAndProjectsTruthfulIngressRouteSurface()
    {
        var services = CreateServiceCollection(
            includeAdminProjection: false,
            configureServices: collection =>
            {
                collection.AddSingleton<ICellTrafficAutomationProviderMaterializer>(
                    new TestFallbackProviderMaterializer(
                        materializerId: "fallback-traefik-provider",
                        providerId: TraefikTrafficMaterializerOptions.DefaultProviderId,
                        priority: 10));
            });

        using var provider = services.BuildServiceProvider();
        foreach (var hostedService in provider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        var catalog = provider.GetRequiredService<ICellTrafficAutomationRuntimeCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        var publicAutomation = catalog.GetByRouteId("orders-to-public-ingress");
        Assert.NotNull(publicAutomation);
        Assert.Equal("traefik-materializer", publicAutomation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Pending, publicAutomation.ProviderMaterializationState);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Pending, publicAutomation.MaterializationState);
        Assert.NotNull(publicAutomation.ProviderMaterializationObservedAtUtc);
        Assert.Equal("2", publicAutomation.RuntimeMetadata["providerSelection.matchingCandidateCount"]);
        Assert.Equal(
            "traefik-materializer,fallback-traefik-provider",
            publicAutomation.RuntimeMetadata["providerSelection.matchingCandidateIds"]);
        Assert.Equal("100", publicAutomation.RuntimeMetadata["providerSelection.selectedPriority"]);
        Assert.Equal("projected-intent", publicAutomation.RuntimeMetadata["providerMaterialization.providerAction"]);
        Assert.Equal("configured-intent", publicAutomation.RuntimeMetadata["providerMaterialization.observationMode"]);
        Assert.Equal("ingressroute/edge-traefik/orders-public-ingress", publicAutomation.RuntimeMetadata["providerMaterialization.providerRouteId"]);
        Assert.Equal("Host(`orders.example.com`) && PathPrefix(`/orders`)", publicAutomation.RuntimeMetadata["providerMaterialization.matchRule"]);
        Assert.Equal("websecure", publicAutomation.RuntimeMetadata["providerMaterialization.entryPoints"]);
        Assert.Equal("middleware/edge-traefik/secure-headers,middleware/edge-security/orders-rate-limit", publicAutomation.RuntimeMetadata["providerMaterialization.middlewareRefs"]);
        Assert.Equal("service/orders-runtime/orders-api:8443@weight/100", publicAutomation.RuntimeMetadata["providerMaterialization.serviceRefs"]);
        Assert.Equal("https", publicAutomation.RuntimeMetadata["providerMaterialization.backendScheme"]);
        Assert.Equal("true", publicAutomation.RuntimeMetadata["providerMaterialization.backendPassHostHeader"]);
        Assert.Equal("orders-public-tls", publicAutomation.RuntimeMetadata["providerMaterialization.tlsSecretName"]);
        Assert.Equal("tlsoption/edge-security/strict-mtls", publicAutomation.RuntimeMetadata["providerMaterialization.tlsOptionsRef"]);
        Assert.Equal("configured-intent", publicAutomation.RuntimeMetadata["providerMaterialization.statusSource"]);
        Assert.Equal("projection-only", publicAutomation.RuntimeMetadata["providerMaterialization.resourceState"]);

        var cellSurface = Assert.Single(
            technologyCatalog.GetByTechnology("cell-based-architecture"),
            static surface => surface.SurfaceId == "cell-traffic-automations");
        Assert.Contains(cellSurface.Entries, entry =>
            entry.Id == publicAutomation.Id &&
            entry.Metadata["providerMaterializerId"] == "traefik-materializer" &&
            entry.Metadata["providerMaterialization.providerRouteId"] == "ingressroute/edge-traefik/orders-public-ingress");

        var traefikSurface = Assert.Single(
            technologyCatalog.GetByTechnology("cell-based-architecture"),
            static surface => surface.SurfaceId == "traefik-ingressroute-traffic-materializations");
        Assert.Contains(traefikSurface.Entries, entry =>
            entry.Id == publicAutomation.Id &&
            entry.Metadata["routeId"] == "orders-to-public-ingress" &&
            entry.Metadata["providerRouteId"] == "ingressroute/edge-traefik/orders-public-ingress" &&
            entry.Metadata["matchRule"] == "Host(`orders.example.com`) && PathPrefix(`/orders`)" &&
            entry.Metadata["middlewareRefs"] == "middleware/edge-traefik/secure-headers,middleware/edge-security/orders-rate-limit" &&
            entry.Metadata["serviceRefs"] == "service/orders-runtime/orders-api:8443@weight/100" &&
            entry.Metadata["tlsOptionsRef"] == "tlsoption/edge-security/strict-mtls");

        Assert.Contains(snapshot.CellTrafficAutomations, automation =>
            automation.RouteId == "orders-to-public-ingress" &&
            automation.ProviderMaterializerId == "traefik-materializer" &&
            automation.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Pending &&
            automation.MaterializationState == CellTrafficAutomationMaterializationStates.Pending);
    }

    [Fact]
    public async Task HostedServiceLeavesUnmappedTraefikAutomationUnavailableWithoutFallbackMaterializer()
    {
        var services = CreateServiceCollection(includeAdminProjection: false);

        using var provider = services.BuildServiceProvider();
        foreach (var hostedService in provider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        var catalog = provider.GetRequiredService<ICellTrafficAutomationRuntimeCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        var publicAutomation = catalog.GetByRouteId("orders-to-public-ingress");
        Assert.NotNull(publicAutomation);
        Assert.Equal("traefik-materializer", publicAutomation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Pending, publicAutomation.ProviderMaterializationState);

        var adminAutomation = catalog.GetByRouteId("orders-to-admin-ingress");
        Assert.NotNull(adminAutomation);
        Assert.Null(adminAutomation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Unavailable, adminAutomation.ProviderMaterializationState);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Unavailable, adminAutomation.MaterializationState);
        Assert.Equal("0", adminAutomation.RuntimeMetadata["providerSelection.matchingCandidateCount"]);
        Assert.Equal(string.Empty, adminAutomation.RuntimeMetadata["providerSelection.matchingCandidateIds"]);
        Assert.Equal("provider:unavailable", adminAutomation.RuntimeMetadata["materialization.stateBreakdown"]);

        var traefikSurface = Assert.Single(
            technologyCatalog.GetByTechnology("cell-based-architecture"),
            static surface => surface.SurfaceId == "traefik-ingressroute-traffic-materializations");
        Assert.Contains(traefikSurface.Entries, entry => entry.Metadata["routeId"] == "orders-to-public-ingress");
        Assert.DoesNotContain(traefikSurface.Entries, entry => entry.Metadata["routeId"] == "orders-to-admin-ingress");
    }

    private static ServiceCollection CreateServiceCollection(
        bool includeAdminProjection,
        Action<ServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        configureServices?.Invoke(services);
        services.AddCephalon(engine => ConfigureEngine(engine, includeAdminProjection));
        return services;
    }

    private static void ConfigureEngine(
        EngineBuilder engine,
        bool includeAdminProjection)
    {
        engine.UseSettings(new EngineSettings(
            blueprint: "Microservice",
            cells: new CellSettings(
                new CellTrafficAutomationSettings(
                    routes:
                    [
                        new CellTrafficAutomationRouteSettings(
                            routeId: "orders-to-public-ingress",
                            automationMode: "automatic",
                            triggerMode: "source-or-target-health",
                            actionMode: "shed-load",
                            materializationMode: "provider-managed",
                            notes: null,
                            metadata: null,
                            providerId: TraefikTrafficMaterializerOptions.DefaultProviderId),
                        new CellTrafficAutomationRouteSettings(
                            routeId: "orders-to-admin-ingress",
                            automationMode: "automatic",
                            triggerMode: "source-health",
                            actionMode: "prefer-local-route",
                            materializationMode: "provider-managed",
                            notes: null,
                            metadata: null,
                            providerId: TraefikTrafficMaterializerOptions.DefaultProviderId)
                    ]))));
        engine.AddModule(new TraefikTrafficTestModule());
        engine.AddTraefikTrafficMaterializer(options =>
        {
            options.RouteNamespace = "edge-traefik";
            options.EntryPoints.Add("websecure");
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

            if (includeAdminProjection)
            {
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
            }
        });
    }

    private sealed class TestFallbackProviderMaterializer(
        string materializerId,
        string providerId,
        int priority) : ICellTrafficAutomationProviderMaterializer
    {
        public string MaterializerId { get; } = materializerId;

        public string ProviderId { get; } = providerId;

        public int Priority { get; } = priority;

        public bool CanMaterialize(CellTrafficAutomationRuntimeDescriptor automation) =>
            string.Equals(automation.ProviderId, ProviderId, StringComparison.OrdinalIgnoreCase);

        public ValueTask<CellTrafficAutomationProviderMaterializationResult> MaterializeAsync(
            CellTrafficAutomationRuntimeDescriptor automation,
            CancellationToken cancellationToken = default)
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["providerAction"] = "fallback-reconciled",
                ["providerRouteId"] = $"fallback/{automation.RouteId}"
            };

            return ValueTask.FromResult(new CellTrafficAutomationProviderMaterializationResult(
                state: CellTrafficAutomationProviderMaterializationStates.Applied,
                observedAtUtc: DateTimeOffset.UtcNow,
                metadata: metadata));
        }
    }

    private sealed class TraefikTrafficTestModule :
        ModuleBase,
        ICellBoundaryContributor,
        ICellRouteContributor,
        ICellHealthIsolationContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "traefik-traffic-tests",
            displayName: "Traefik Traffic Tests",
            description: "Provides cell topology and health isolation for Traefik traffic materializer tests.");

        public void RegisterCellBoundaries(ICellBoundaryRegistry cells)
        {
            cells.Add(new CellBoundaryDescriptor(
                id: "orders-cell",
                sourceModuleId: "traefik-traffic-tests",
                displayName: "Orders Cell",
                description: "Keeps order-serving workloads together.",
                blastRadius: "regional",
                routingStrategy: "local-first",
                moduleIds: ["traefik-traffic-tests"]));
            cells.Add(new CellBoundaryDescriptor(
                id: "public-edge-cell",
                sourceModuleId: "traefik-traffic-tests",
                displayName: "Public Edge Cell",
                description: "Fronts public ingress traffic.",
                blastRadius: "public-edge",
                routingStrategy: "edge-fanout"));
            cells.Add(new CellBoundaryDescriptor(
                id: "admin-edge-cell",
                sourceModuleId: "traefik-traffic-tests",
                displayName: "Admin Edge Cell",
                description: "Fronts administrative ingress traffic.",
                blastRadius: "admin-edge",
                routingStrategy: "edge-fanout"));
        }

        public void RegisterCellRoutes(ICellRouteRegistry routes)
        {
            routes.Add(new CellRouteDescriptor(
                id: "orders-to-public-ingress",
                sourceModuleId: "traefik-traffic-tests",
                sourceCellId: "orders-cell",
                targetCellId: "public-edge-cell",
                displayName: "Orders To Public Ingress",
                description: "Projects order-serving traffic to the public edge boundary.",
                routingStrategy: "ingressroute-managed",
                governanceMode: "policy-guarded",
                transportIds: ["rest-api"]));
            routes.Add(new CellRouteDescriptor(
                id: "orders-to-admin-ingress",
                sourceModuleId: "traefik-traffic-tests",
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
                sourceModuleId: "traefik-traffic-tests",
                cellId: "orders-cell",
                displayName: "Orders Cell Health",
                description: "Contains order-serving failures inside the orders cell.",
                failureIsolationMode: "cell-quarantine",
                readinessScope: "dependency-aware",
                restartScope: "cell-only",
                dependencyIds: ["orders-db"]));
            healthIsolations.Add(new CellHealthIsolationDescriptor(
                id: "public-edge-health",
                sourceModuleId: "traefik-traffic-tests",
                cellId: "public-edge-cell",
                displayName: "Public Edge Health",
                description: "Contains public ingress failures.",
                failureIsolationMode: "degraded-serving",
                readinessScope: "best-effort",
                restartScope: "manual",
                dependencyIds: ["public-ingress"]));
            healthIsolations.Add(new CellHealthIsolationDescriptor(
                id: "admin-edge-health",
                sourceModuleId: "traefik-traffic-tests",
                cellId: "admin-edge-cell",
                displayName: "Admin Edge Health",
                description: "Contains admin ingress failures.",
                failureIsolationMode: "degraded-serving",
                readinessScope: "dependency-aware",
                restartScope: "manual",
                dependencyIds: ["admin-ingress"]));
        }
    }
}
