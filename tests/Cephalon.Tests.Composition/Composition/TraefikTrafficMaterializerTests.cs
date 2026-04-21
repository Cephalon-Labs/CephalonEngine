using System.Globalization;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Edge.Traefik.Configuration;
using Cephalon.Edge.Traefik.Registration;
using Cephalon.Edge.Traefik.Services;
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
        Assert.Equal(TraefikTrafficObservationModes.ConfiguredIntent, publicAutomation.RuntimeMetadata["providerMaterialization.observationMode"]);
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
        Assert.Equal(CellTrafficAutomationOwnershipStates.Requested, publicAutomation.RuntimeMetadata["providerMaterialization.ownershipState"]);
        Assert.Equal(CellTrafficAutomationDependencyStates.Unknown, publicAutomation.RuntimeMetadata["providerMaterialization.dependencyState"]);
        Assert.Equal(CellTrafficAutomationDriftStates.Unknown, publicAutomation.RuntimeMetadata["providerMaterialization.driftState"]);
        Assert.Equal(CellTrafficAutomationLifecycleActions.Project, publicAutomation.RuntimeMetadata["providerMaterialization.lifecycleAction"]);
        Assert.Equal("none", publicAutomation.RuntimeMetadata["providerMaterialization.ingressRouteWriteAction"]);

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

    [Fact]
    public async Task HostedServiceProjectsObservedTraefikStatusWhenObserveOnlyModeIsEnabled()
    {
        var services = CreateServiceCollection(
            includeAdminProjection: false,
            controlPlaneMode: TraefikTrafficObservationModes.ObserveOnly,
            configureServices: collection =>
            {
                collection.AddSingleton<ITraefikTrafficObservationSource>(
                    new StaticObservationSource(CreateObservedAppliedResult));
            });

        using var provider = services.BuildServiceProvider();
        foreach (var hostedService in provider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        var catalog = provider.GetRequiredService<ICellTrafficAutomationRuntimeCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        var automation = catalog.GetByRouteId("orders-to-public-ingress");
        Assert.NotNull(automation);
        Assert.Equal("traefik-materializer", automation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Applied, automation.ProviderMaterializationState);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Applied, automation.MaterializationState);
        Assert.Equal("observe-only", automation.RuntimeMetadata["providerMaterialization.providerAction"]);
        Assert.Equal(TraefikTrafficObservationModes.ObserveOnly, automation.RuntimeMetadata["providerMaterialization.observationMode"]);
        Assert.Equal("traefik-ingressroute-observation", automation.RuntimeMetadata["providerMaterialization.statusSource"]);
        Assert.Equal("available", automation.RuntimeMetadata["providerMaterialization.resourceState"]);
        Assert.Equal("true", automation.RuntimeMetadata["providerMaterialization.ingressRouteExists"]);
        Assert.Equal("websecure", automation.RuntimeMetadata["providerMaterialization.observedEntryPoints"]);
        Assert.Equal("Host(`orders.example.com`) && PathPrefix(`/orders`)", automation.RuntimeMetadata["providerMaterialization.observedMatchRule"]);
        Assert.Equal("middleware/edge-traefik/secure-headers,middleware/edge-security/orders-rate-limit", automation.RuntimeMetadata["providerMaterialization.observedMiddlewareRefs"]);
        Assert.Equal("service/orders-runtime/orders-api:8443@weight/100", automation.RuntimeMetadata["providerMaterialization.observedServiceRefs"]);
        Assert.Equal("orders-public-tls", automation.RuntimeMetadata["providerMaterialization.observedTlsSecretName"]);
        Assert.Equal("tlsoption/edge-security/strict-mtls", automation.RuntimeMetadata["providerMaterialization.observedTlsOptionsRef"]);
        Assert.Equal(CellTrafficAutomationOwnershipStates.Owned, automation.RuntimeMetadata["providerMaterialization.ownershipState"]);
        Assert.Equal("edge-traefik", automation.RuntimeMetadata["providerMaterialization.managedBy"]);
        Assert.Equal("orders-to-public-ingress", automation.RuntimeMetadata["providerMaterialization.observedAutomationId"]);
        Assert.Equal("traefik-traffic-tests", automation.RuntimeMetadata["providerMaterialization.observedSourceModuleId"]);
        Assert.Equal(CellTrafficAutomationDependencyStates.Satisfied, automation.RuntimeMetadata["providerMaterialization.dependencyState"]);
        Assert.Equal("true", automation.RuntimeMetadata["providerMaterialization.backendServiceExists"]);
        Assert.Equal("true", automation.RuntimeMetadata["providerMaterialization.tlsSecretExists"]);
        Assert.Equal("true", automation.RuntimeMetadata["providerMaterialization.tlsOptionsExists"]);
        Assert.Equal(CellTrafficAutomationDriftStates.InSync, automation.RuntimeMetadata["providerMaterialization.driftState"]);
        Assert.Equal(string.Empty, automation.RuntimeMetadata["providerMaterialization.driftReasons"]);
        Assert.Equal(CellTrafficAutomationLifecycleActions.Observe, automation.RuntimeMetadata["providerMaterialization.lifecycleAction"]);
        Assert.True(automation.RuntimeMetadata.ContainsKey("providerMaterialization.observationFreshUntilUtc"));

        var traefikSurface = Assert.Single(
            technologyCatalog.GetByTechnology("cell-based-architecture"),
            static surface => surface.SurfaceId == "traefik-ingressroute-traffic-materializations");
        Assert.Contains(traefikSurface.Entries, entry =>
            entry.Id == automation.Id &&
            entry.Metadata["providerAction"] == "observe-only" &&
            entry.Metadata["statusSource"] == "traefik-ingressroute-observation" &&
            entry.Metadata["driftState"] == CellTrafficAutomationDriftStates.InSync &&
            entry.Metadata["observedServiceRefs"] == "service/orders-runtime/orders-api:8443@weight/100");
    }

    [Fact]
    public async Task HostedServiceAppliesOwnedTraefikIngressRoutesAndProjectsObservedTruthWhenApplyAndReconcileModeIsEnabled()
    {
        var services = CreateServiceCollection(
            includeAdminProjection: false,
            controlPlaneMode: TraefikTrafficObservationModes.ApplyAndReconcile,
            configureServices: collection =>
            {
                collection.AddSingleton<ITraefikTrafficApplyService>(
                    new StaticApplyService(
                        static (automation, projection) => CreateApplyPendingResult(automation.RouteId, "created")));
                collection.AddSingleton<ITraefikTrafficObservationSource>(
                    new StaticObservationSource(CreateObservedAppliedResult));
            });

        using var provider = services.BuildServiceProvider();
        foreach (var hostedService in provider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        var catalog = provider.GetRequiredService<ICellTrafficAutomationRuntimeCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        var automation = catalog.GetByRouteId("orders-to-public-ingress");
        Assert.NotNull(automation);
        Assert.Equal("traefik-materializer", automation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Applied, automation.ProviderMaterializationState);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Applied, automation.MaterializationState);
        Assert.Equal("apply-and-reconcile", automation.RuntimeMetadata["providerMaterialization.providerAction"]);
        Assert.Equal(TraefikTrafficObservationModes.ApplyAndReconcile, automation.RuntimeMetadata["providerMaterialization.observationMode"]);
        Assert.Equal("traefik-ingressroute-observation", automation.RuntimeMetadata["providerMaterialization.statusSource"]);
        Assert.Equal("created", automation.RuntimeMetadata["providerMaterialization.ingressRouteWriteAction"]);
        Assert.Equal("available", automation.RuntimeMetadata["providerMaterialization.resourceState"]);
        Assert.Equal(CellTrafficAutomationOwnershipStates.Owned, automation.RuntimeMetadata["providerMaterialization.ownershipState"]);
        Assert.Equal(CellTrafficAutomationDependencyStates.Satisfied, automation.RuntimeMetadata["providerMaterialization.dependencyState"]);
        Assert.Equal(CellTrafficAutomationDriftStates.InSync, automation.RuntimeMetadata["providerMaterialization.driftState"]);
        Assert.Equal(CellTrafficAutomationLifecycleActions.Create, automation.RuntimeMetadata["providerMaterialization.lifecycleAction"]);
        Assert.Equal("true", automation.RuntimeMetadata["providerMaterialization.ingressRouteExists"]);

        var traefikSurface = Assert.Single(
            technologyCatalog.GetByTechnology("cell-based-architecture"),
            static surface => surface.SurfaceId == "traefik-ingressroute-traffic-materializations");
        Assert.Contains(traefikSurface.Entries, entry =>
            entry.Id == automation.Id &&
            entry.Metadata["providerAction"] == "apply-and-reconcile" &&
            entry.Metadata["statusSource"] == "traefik-ingressroute-observation" &&
            entry.Metadata["ingressRouteWriteAction"] == "created" &&
            entry.Metadata["lifecycleAction"] == CellTrafficAutomationLifecycleActions.Create &&
            entry.Metadata["observedServiceRefs"] == "service/orders-runtime/orders-api:8443@weight/100");
    }

    [Fact]
    public void OwnershipEvaluationTreatsExternalIngressRoutesAsConflicts()
    {
        var automation = CreateAutomationDescriptor(
            automationId: "orders-public-automation",
            routeId: "orders-to-public-ingress",
            sourceModuleId: "traefik-traffic-tests");
        using var source = new TraefikTrafficObservationSource(
            new TraefikTrafficMaterializerOptions(),
            TimeProvider.System,
            runtimeCatalogAccessor: () => new StaticCellTrafficAutomationRuntimeCatalog([automation]));

        var ownership = source.EvaluateOwnership(
            new TraefikIngressRouteResource
            {
                Metadata = new k8s.Models.V1ObjectMeta
                {
                    Name = "orders-public-ingress"
                }
            },
            automation);

        Assert.Equal(CellTrafficAutomationOwnershipStates.OwnershipConflict, ownership.State);
        Assert.True(ownership.IsConflict);
        Assert.False(ownership.CanTransfer);
        Assert.Equal("external-unmanaged-resource", ownership.Reason);
        Assert.Equal("ownership-conflict", ownership.ResourceState);
        Assert.Null(ownership.ActiveOwnerId);
    }

    [Fact]
    public void OwnershipEvaluationTreatsIncompleteCurrentOwnershipMetadataAsTransferCandidate()
    {
        var automation = CreateAutomationDescriptor(
            automationId: "orders-public-automation",
            routeId: "orders-to-public-ingress",
            sourceModuleId: "traefik-traffic-tests");
        using var source = new TraefikTrafficObservationSource(
            new TraefikTrafficMaterializerOptions(),
            TimeProvider.System,
            runtimeCatalogAccessor: () => new StaticCellTrafficAutomationRuntimeCatalog([automation]));

        var ownership = source.EvaluateOwnership(
            new TraefikIngressRouteResource
            {
                Metadata = new k8s.Models.V1ObjectMeta
                {
                    Name = "orders-public-ingress",
                    Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [TraefikOwnership.ManagedByLabel] = TraefikOwnership.ManagedByValue
                    },
                    Annotations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [TraefikOwnership.RouteIdAnnotation] = automation.RouteId
                    }
                }
            },
            automation);

        Assert.Equal(CellTrafficAutomationOwnershipStates.Orphaned, ownership.State);
        Assert.False(ownership.IsConflict);
        Assert.True(ownership.CanTransfer);
        Assert.Equal("incomplete-current-owner", ownership.Reason);
        Assert.Equal("orphaned-ingressroute", ownership.ResourceState);
        Assert.Equal(automation.Id, ownership.ActiveOwnerId);
    }

    [Fact]
    public async Task ObservationHostedServiceRefreshesLiveTraefikStatusOnPollingInterval()
    {
        var source = new SequencedObservationSource(new Dictionary<string, Queue<CellTrafficAutomationProviderMaterializationResult>>(StringComparer.OrdinalIgnoreCase)
        {
            ["orders-to-public-ingress"] = new Queue<CellTrafficAutomationProviderMaterializationResult>(
            [
                CreateObservedPendingResult("orders-to-public-ingress"),
                CreateObservedAppliedResult("orders-to-public-ingress")
            ])
        });
        var services = CreateServiceCollection(
            includeAdminProjection: false,
            controlPlaneMode: TraefikTrafficObservationModes.ObserveOnly,
            observationPollingIntervalSeconds: 1,
            configureServices: collection =>
            {
                collection.AddSingleton<ITraefikTrafficObservationSource>(source);
            });

        using var provider = services.BuildServiceProvider();
        foreach (var hostedService in provider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        var catalog = provider.GetRequiredService<ICellTrafficAutomationRuntimeCatalog>();

        var automation = catalog.GetByRouteId("orders-to-public-ingress");
        Assert.NotNull(automation);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Pending, automation.ProviderMaterializationState);
        Assert.Equal("missing-ingressroute", automation.RuntimeMetadata["providerMaterialization.resourceState"]);
        Assert.Equal(CellTrafficAutomationOwnershipStates.Requested, automation.RuntimeMetadata["providerMaterialization.ownershipState"]);

        await WaitForConditionAsync(
            () =>
            {
                var refreshed = catalog.GetByRouteId("orders-to-public-ingress");
                return refreshed is not null &&
                    string.Equals(
                        refreshed.ProviderMaterializationState,
                        CellTrafficAutomationProviderMaterializationStates.Applied,
                        StringComparison.OrdinalIgnoreCase);
            },
            TimeSpan.FromSeconds(10));

        automation = catalog.GetByRouteId("orders-to-public-ingress");
        Assert.NotNull(automation);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Applied, automation.ProviderMaterializationState);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Applied, automation.MaterializationState);
        Assert.Equal("available", automation.RuntimeMetadata["providerMaterialization.resourceState"]);
        Assert.Equal(CellTrafficAutomationDriftStates.InSync, automation.RuntimeMetadata["providerMaterialization.driftState"]);
        Assert.Equal("true", automation.RuntimeMetadata["providerMaterialization.backendServiceExists"]);
    }

    [Fact]
    public async Task ApplyAndReconcileRefreshesControlPlaneStateOnPollingInterval()
    {
        var observationSource = new SequencedObservationSource(new Dictionary<string, Queue<CellTrafficAutomationProviderMaterializationResult>>(StringComparer.OrdinalIgnoreCase)
        {
            ["orders-to-public-ingress"] = new Queue<CellTrafficAutomationProviderMaterializationResult>(
            [
                CreateObservedPendingResult("orders-to-public-ingress"),
                CreateObservedAppliedResult("orders-to-public-ingress")
            ])
        });
        var applyService = new SequencedApplyService(new Dictionary<string, Queue<CellTrafficAutomationProviderMaterializationResult>>(StringComparer.OrdinalIgnoreCase)
        {
            ["orders-to-public-ingress"] = new Queue<CellTrafficAutomationProviderMaterializationResult>(
            [
                CreateApplyPendingResult("orders-to-public-ingress", "created"),
                CreateApplyPendingResult("orders-to-public-ingress", "replaced")
            ])
        });
        var services = CreateServiceCollection(
            includeAdminProjection: false,
            controlPlaneMode: TraefikTrafficObservationModes.ApplyAndReconcile,
            observationPollingIntervalSeconds: 1,
            configureServices: collection =>
            {
                collection.AddSingleton<ITraefikTrafficApplyService>(applyService);
                collection.AddSingleton<ITraefikTrafficObservationSource>(observationSource);
            });

        using var provider = services.BuildServiceProvider();
        foreach (var hostedService in provider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        var catalog = provider.GetRequiredService<ICellTrafficAutomationRuntimeCatalog>();

        var automation = catalog.GetByRouteId("orders-to-public-ingress");
        Assert.NotNull(automation);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Pending, automation.ProviderMaterializationState);
        Assert.Equal("missing-ingressroute", automation.RuntimeMetadata["providerMaterialization.resourceState"]);
        Assert.Equal("created", automation.RuntimeMetadata["providerMaterialization.ingressRouteWriteAction"]);
        Assert.Equal(CellTrafficAutomationOwnershipStates.Requested, automation.RuntimeMetadata["providerMaterialization.ownershipState"]);

        await WaitForConditionAsync(
            () =>
            {
                var refreshed = catalog.GetByRouteId("orders-to-public-ingress");
                return refreshed is not null &&
                    string.Equals(
                        refreshed.ProviderMaterializationState,
                        CellTrafficAutomationProviderMaterializationStates.Applied,
                        StringComparison.OrdinalIgnoreCase);
            },
            TimeSpan.FromSeconds(10));

        automation = catalog.GetByRouteId("orders-to-public-ingress");
        Assert.NotNull(automation);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Applied, automation.ProviderMaterializationState);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Applied, automation.MaterializationState);
        Assert.Equal("available", automation.RuntimeMetadata["providerMaterialization.resourceState"]);
        Assert.Equal("replaced", automation.RuntimeMetadata["providerMaterialization.ingressRouteWriteAction"]);
        Assert.Equal(CellTrafficAutomationDriftStates.InSync, automation.RuntimeMetadata["providerMaterialization.driftState"]);
        Assert.Equal("traefik-ingressroute-observation", automation.RuntimeMetadata["providerMaterialization.statusSource"]);
    }

    private static ServiceCollection CreateServiceCollection(
        bool includeAdminProjection,
        string? controlPlaneMode = null,
        int observationPollingIntervalSeconds = 30,
        int observationStaleAfterSeconds = 90,
        Action<ServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        configureServices?.Invoke(services);
        services.AddCephalon(engine => ConfigureEngine(
            engine,
            includeAdminProjection,
            controlPlaneMode,
            observationPollingIntervalSeconds,
            observationStaleAfterSeconds));
        return services;
    }

    private static void ConfigureEngine(
        EngineBuilder engine,
        bool includeAdminProjection,
        string? controlPlaneMode,
        int observationPollingIntervalSeconds,
        int observationStaleAfterSeconds)
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
            options.Observation.Mode = controlPlaneMode ?? TraefikTrafficObservationModes.ConfiguredIntent;
            options.Observation.PollingIntervalSeconds = observationPollingIntervalSeconds;
            options.Observation.StaleAfterSeconds = observationStaleAfterSeconds;
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

    private static CellTrafficAutomationProviderMaterializationResult CreateObservedAppliedResult(
        CellTrafficAutomationRuntimeDescriptor automation,
        TraefikIngressRouteProjection projection)
    {
        var metadata = projection.CreateMetadata();
        metadata["providerAction"] = TraefikTrafficObservationModes.ObserveOnly;
        metadata["observationMode"] = TraefikTrafficObservationModes.ObserveOnly;
        metadata["statusSource"] = "traefik-ingressroute-observation";
        metadata["resourceState"] = "available";
        metadata["ingressRouteExists"] = "true";
        metadata["observedIngressRouteName"] = projection.IngressRouteName;
        metadata["observedIngressRouteNamespace"] = projection.RouteNamespace;
        metadata["observedIngressRouteGeneration"] = "3";
        metadata["observedEntryPoints"] = string.Join(",", projection.EntryPoints);
        metadata["observedEntryPointCount"] = projection.EntryPoints.Count.ToString(CultureInfo.InvariantCulture);
        metadata["observedRouteCount"] = "1";
        metadata["observedMatchRule"] = projection.MatchRule;
        metadata["observedRouteKind"] = "Rule";
        metadata["observedMiddlewareRefs"] = projection.MiddlewareReferences;
        metadata["observedServiceRefs"] = projection.BackendReference;
        metadata["observedBackendScheme"] = projection.BackendScheme ?? string.Empty;
        metadata["observedBackendPassHostHeader"] = projection.PassHostHeader is true ? "true" : projection.PassHostHeader is false ? "false" : string.Empty;
        metadata["observedTlsSecretName"] = projection.TlsSecretName ?? string.Empty;
        metadata["observedTlsOptionsRef"] = projection.TlsOptionsReference ?? string.Empty;
        metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Owned;
        metadata["managedBy"] = "edge-traefik";
        metadata["observedAutomationId"] = automation.Id;
        metadata["observedRouteId"] = automation.RouteId;
        metadata["observedSourceModuleId"] = automation.SourceModuleId;
        metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Satisfied;
        metadata["backendServiceExists"] = "true";
        metadata["tlsOptionsExists"] = "true";
        metadata["tlsSecretExists"] = "true";
        metadata["missingMiddlewareRefs"] = string.Empty;
        metadata["dependencyMissingRefs"] = string.Empty;
        metadata["driftState"] = CellTrafficAutomationDriftStates.InSync;
        metadata["driftReasons"] = string.Empty;
        metadata["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Observe;
        metadata["observationFreshUntilUtc"] = DateTimeOffset.UtcNow.AddMinutes(2).ToString("O", CultureInfo.InvariantCulture);

        return new CellTrafficAutomationProviderMaterializationResult(
            CellTrafficAutomationProviderMaterializationStates.Applied,
            DateTimeOffset.UtcNow,
            metadata: metadata);
    }

    private static CellTrafficAutomationProviderMaterializationResult CreateObservedAppliedResult(string routeId)
    {
        var metadata = CreateObservedMetadata(routeId, resourceState: "available");
        metadata["ingressRouteExists"] = "true";
        metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Owned;
        metadata["managedBy"] = "edge-traefik";
        metadata["observedAutomationId"] = routeId;
        metadata["observedRouteId"] = routeId;
        metadata["observedSourceModuleId"] = "traefik-traffic-tests";
        metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Satisfied;
        metadata["backendServiceExists"] = "true";
        metadata["tlsOptionsExists"] = "true";
        metadata["tlsSecretExists"] = "true";
        metadata["missingMiddlewareRefs"] = string.Empty;
        metadata["dependencyMissingRefs"] = string.Empty;
        metadata["driftState"] = CellTrafficAutomationDriftStates.InSync;
        metadata["driftReasons"] = string.Empty;

        return new CellTrafficAutomationProviderMaterializationResult(
            CellTrafficAutomationProviderMaterializationStates.Applied,
            DateTimeOffset.UtcNow,
            metadata: metadata);
    }

    private static CellTrafficAutomationProviderMaterializationResult CreateApplyPendingResult(string routeId, string writeAction)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["providerAction"] = TraefikTrafficObservationModes.ApplyAndReconcile,
            ["observationMode"] = TraefikTrafficObservationModes.ApplyAndReconcile,
            ["statusSource"] = "control-plane-apply",
            ["resourceState"] = "write-succeeded",
            ["ingressRouteExists"] = "true",
            ["driftState"] = CellTrafficAutomationDriftStates.Reconciling,
            ["driftReasons"] = string.Empty,
            ["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown,
            ["missingMiddlewareRefs"] = string.Empty,
            ["dependencyMissingRefs"] = string.Empty,
            ["lifecycleAction"] = string.Equals(writeAction, "created", StringComparison.OrdinalIgnoreCase)
                ? CellTrafficAutomationLifecycleActions.Create
                : CellTrafficAutomationLifecycleActions.Replace,
            ["ingressRouteWriteAction"] = writeAction,
            ["ownershipState"] = CellTrafficAutomationOwnershipStates.Owned,
            ["ingressRouteAppliedGeneration"] = writeAction == "created" ? "1" : "2",
            ["providerRouteId"] = "ingressroute/edge-traefik/orders-public-ingress",
            ["ingressRouteResourceId"] = "ingressroute/edge-traefik/orders-public-ingress",
            ["ingressRouteNamespace"] = "edge-traefik",
            ["ingressRouteName"] = "orders-public-ingress"
        };

        return new CellTrafficAutomationProviderMaterializationResult(
            state: CellTrafficAutomationProviderMaterializationStates.Pending,
            observedAtUtc: DateTimeOffset.UtcNow,
            metadata: metadata);
    }

    private static CellTrafficAutomationProviderMaterializationResult CreateObservedPendingResult(string routeId)
    {
        var metadata = CreateObservedMetadata(routeId, resourceState: "missing-ingressroute");
        metadata["ingressRouteExists"] = "false";
        metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Requested;
        metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown;
        metadata["backendServiceExists"] = "false";
        metadata["tlsOptionsExists"] = "false";
        metadata["tlsSecretExists"] = "false";
        metadata["missingMiddlewareRefs"] = string.Empty;
        metadata["dependencyMissingRefs"] = string.Empty;
        metadata["driftState"] = CellTrafficAutomationDriftStates.Unknown;
        metadata["driftReasons"] = string.Empty;

        return new CellTrafficAutomationProviderMaterializationResult(
            CellTrafficAutomationProviderMaterializationStates.Pending,
            DateTimeOffset.UtcNow,
            metadata: metadata);
    }

    private static Dictionary<string, string> CreateObservedMetadata(string routeId, string resourceState)
    {
        var routeName = routeId switch
        {
            "orders-to-public-ingress" => "orders-public-ingress",
            "orders-to-admin-ingress" => "orders-admin-ingress",
            _ => routeId
        };
        var matchRule = routeId switch
        {
            "orders-to-public-ingress" => "Host(`orders.example.com`) && PathPrefix(`/orders`)",
            "orders-to-admin-ingress" => "Host(`admin.example.com`) && PathPrefix(`/orders`)",
            _ => string.Empty
        };
        var entryPoints = routeId switch
        {
            "orders-to-public-ingress" => "websecure",
            "orders-to-admin-ingress" => "admin-websecure",
            _ => string.Empty
        };
        var middlewareRefs = routeId switch
        {
            "orders-to-public-ingress" => "middleware/edge-traefik/secure-headers,middleware/edge-security/orders-rate-limit",
            "orders-to-admin-ingress" => "middleware/edge-security/admin-authn",
            _ => string.Empty
        };
        var serviceRefs = routeId switch
        {
            "orders-to-public-ingress" => "service/orders-runtime/orders-api:8443@weight/100",
            "orders-to-admin-ingress" => "service/orders-admin/orders-admin-api:9443",
            _ => string.Empty
        };
        var tlsSecret = routeId switch
        {
            "orders-to-public-ingress" => "orders-public-tls",
            "orders-to-admin-ingress" => "orders-admin-tls",
            _ => string.Empty
        };
        var tlsOptions = routeId switch
        {
            "orders-to-public-ingress" => "tlsoption/edge-security/strict-mtls",
            _ => string.Empty
        };
        var backendScheme = routeId == "orders-to-public-ingress" ? "https" : string.Empty;
        var passHostHeader = routeId == "orders-to-public-ingress" ? "true" : string.Empty;

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["providerAction"] = "observe-only",
            ["observationMode"] = TraefikTrafficObservationModes.ObserveOnly,
            ["statusSource"] = "traefik-ingressroute-observation",
            ["resourceState"] = resourceState,
            ["providerRouteId"] = $"ingressroute/edge-traefik/{routeName}",
            ["ingressRouteResourceId"] = $"ingressroute/edge-traefik/{routeName}",
            ["observedIngressRouteName"] = routeName,
            ["observedIngressRouteNamespace"] = "edge-traefik",
            ["observedIngressRouteGeneration"] = "3",
            ["observedEntryPoints"] = entryPoints,
            ["observedEntryPointCount"] = string.IsNullOrWhiteSpace(entryPoints) ? "0" : entryPoints.Split(',').Length.ToString(CultureInfo.InvariantCulture),
            ["observedRouteCount"] = resourceState == "missing-ingressroute" ? "0" : "1",
            ["observedMatchRule"] = matchRule,
            ["observedRouteKind"] = "Rule",
            ["observedMiddlewareRefs"] = middlewareRefs,
            ["observedServiceRefs"] = serviceRefs,
            ["observedBackendScheme"] = backendScheme,
            ["observedBackendPassHostHeader"] = passHostHeader,
            ["observedTlsSecretName"] = tlsSecret,
            ["observedTlsOptionsRef"] = tlsOptions,
            ["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Observe,
            ["observationFreshUntilUtc"] = DateTimeOffset.UtcNow.AddMinutes(2).ToString("O", CultureInfo.InvariantCulture)
        };
    }

    private static async Task WaitForConditionAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(200);
        }

        Assert.True(condition(), "Timed out waiting for the expected condition.");
    }

    private static CellTrafficAutomationRuntimeDescriptor CreateAutomationDescriptor(
        string automationId,
        string routeId,
        string sourceModuleId)
    {
        return new CellTrafficAutomationRuntimeDescriptor(
            id: automationId,
            routeId: routeId,
            sourceModuleId: sourceModuleId,
            sourceCellId: "orders-cell",
            targetCellId: "public-edge-cell",
            displayName: automationId,
            description: "Test automation descriptor.",
            routingStrategy: "ingressroute-managed",
            governanceMode: "policy-guarded",
            automationMode: "automatic",
            triggerMode: "source-or-target-health",
            actionMode: "shed-load",
            materializationMode: "provider-managed",
            policySource: "cell-route");
    }

    private sealed class StaticCellTrafficAutomationRuntimeCatalog(
        IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> automations) : ICellTrafficAutomationRuntimeCatalog
    {
        public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> Automations { get; } = automations;

        public CellTrafficAutomationRuntimeDescriptor? GetById(string automationId) =>
            Automations.FirstOrDefault(automation => string.Equals(automation.Id, automationId, StringComparison.OrdinalIgnoreCase));

        public CellTrafficAutomationRuntimeDescriptor? GetByRouteId(string routeId) =>
            Automations.FirstOrDefault(automation => string.Equals(automation.RouteId, routeId, StringComparison.OrdinalIgnoreCase));

        public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetBySourceModule(string sourceModuleId) =>
            Automations.Where(automation => string.Equals(automation.SourceModuleId, sourceModuleId, StringComparison.OrdinalIgnoreCase)).ToArray();

        public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetBySourceCellId(string sourceCellId) =>
            Automations.Where(automation => string.Equals(automation.SourceCellId, sourceCellId, StringComparison.OrdinalIgnoreCase)).ToArray();

        public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetByTargetCellId(string targetCellId) =>
            Automations.Where(automation => string.Equals(automation.TargetCellId, targetCellId, StringComparison.OrdinalIgnoreCase)).ToArray();

        public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetByProvider(string provider) =>
            Automations.Where(automation => string.Equals(automation.ProviderId, provider, StringComparison.OrdinalIgnoreCase)).ToArray();

        public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetByEdgeNodeId(string edgeNodeId) =>
            Automations.Where(automation => automation.EdgeNodeIds.Any(edgeNode =>
                string.Equals(edgeNode, edgeNodeId, StringComparison.OrdinalIgnoreCase))).ToArray();

        public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetByHealthIsolationId(string healthIsolationId) =>
            Automations.Where(automation =>
                automation.SourceHealthIsolationIds.Any(id => string.Equals(id, healthIsolationId, StringComparison.OrdinalIgnoreCase)) ||
                automation.TargetHealthIsolationIds.Any(id => string.Equals(id, healthIsolationId, StringComparison.OrdinalIgnoreCase))).ToArray();
    }

    private sealed class StaticObservationSource(
        Func<CellTrafficAutomationRuntimeDescriptor, TraefikIngressRouteProjection, CellTrafficAutomationProviderMaterializationResult> factory)
        : ITraefikTrafficObservationSource
    {
        public ValueTask<CellTrafficAutomationProviderMaterializationResult> ObserveAsync(
            CellTrafficAutomationRuntimeDescriptor automation,
            TraefikIngressRouteProjection projection,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(factory(automation, projection));
        }
    }

    private sealed class StaticApplyService(
        Func<CellTrafficAutomationRuntimeDescriptor, TraefikIngressRouteProjection, CellTrafficAutomationProviderMaterializationResult> factory)
        : ITraefikTrafficApplyService
    {
        public ValueTask<CellTrafficAutomationProviderMaterializationResult> ApplyAsync(
            CellTrafficAutomationRuntimeDescriptor automation,
            TraefikIngressRouteProjection projection,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(factory(automation, projection));
        }
    }

    private sealed class SequencedObservationSource(
        Dictionary<string, Queue<CellTrafficAutomationProviderMaterializationResult>> resultsByRouteId)
        : ITraefikTrafficObservationSource
    {
        private readonly Lock gate = new();

        public ValueTask<CellTrafficAutomationProviderMaterializationResult> ObserveAsync(
            CellTrafficAutomationRuntimeDescriptor automation,
            TraefikIngressRouteProjection projection,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(automation);
            cancellationToken.ThrowIfCancellationRequested();

            lock (gate)
            {
                if (!resultsByRouteId.TryGetValue(automation.RouteId, out var queue) || queue.Count == 0)
                {
                    return ValueTask.FromResult(CreateObservedAppliedResult(automation, projection));
                }

                var result = queue.Dequeue();
                if (queue.Count == 0)
                {
                    queue.Enqueue(result);
                }

                return ValueTask.FromResult(result);
            }
        }
    }

    private sealed class SequencedApplyService(
        Dictionary<string, Queue<CellTrafficAutomationProviderMaterializationResult>> resultsByRouteId)
        : ITraefikTrafficApplyService
    {
        private readonly Lock gate = new();

        public ValueTask<CellTrafficAutomationProviderMaterializationResult> ApplyAsync(
            CellTrafficAutomationRuntimeDescriptor automation,
            TraefikIngressRouteProjection projection,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(automation);
            cancellationToken.ThrowIfCancellationRequested();

            lock (gate)
            {
                if (!resultsByRouteId.TryGetValue(automation.RouteId, out var queue) || queue.Count == 0)
                {
                    return ValueTask.FromResult(CreateApplyPendingResult(automation.RouteId, "replaced"));
                }

                var result = queue.Dequeue();
                if (queue.Count == 0)
                {
                    queue.Enqueue(result);
                }

                return ValueTask.FromResult(result);
            }
        }
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
                ["providerRouteId"] = $"fallback/{automation.RouteId}",
                ["ownershipState"] = CellTrafficAutomationOwnershipStates.Owned,
                ["dependencyState"] = CellTrafficAutomationDependencyStates.Satisfied,
                ["driftState"] = CellTrafficAutomationDriftStates.InSync,
                ["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Reconcile
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
