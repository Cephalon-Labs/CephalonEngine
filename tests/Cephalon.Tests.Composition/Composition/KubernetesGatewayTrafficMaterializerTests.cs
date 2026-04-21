using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Edge.KubernetesGateway.Configuration;
using Cephalon.Edge.KubernetesGateway.Registration;
using Cephalon.Edge.KubernetesGateway.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Composition;

public sealed class KubernetesGatewayTrafficMaterializerTests
{
    [Fact]
    public async Task HostedServiceSelectsKubernetesGatewayMaterializerOverGenericFallbackAndProjectsTruthfulConfiguredIntentSurface()
    {
        var services = CreateServiceCollection(
            includeAdminProjection: false,
            configureServices: collection =>
            {
                collection.AddSingleton<ICellTrafficAutomationProviderMaterializer>(
                    new TestFallbackProviderMaterializer(
                        materializerId: "fallback-kubernetes-provider",
                        providerId: KubernetesGatewayTrafficMaterializerOptions.DefaultProviderId,
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
        Assert.Equal("kubernetes-gateway-materializer", publicAutomation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Pending, publicAutomation.ProviderMaterializationState);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Pending, publicAutomation.MaterializationState);
        Assert.NotNull(publicAutomation.ProviderMaterializationObservedAtUtc);
        Assert.Equal("2", publicAutomation.RuntimeMetadata["providerSelection.matchingCandidateCount"]);
        Assert.Equal(
            "kubernetes-gateway-materializer,fallback-kubernetes-provider",
            publicAutomation.RuntimeMetadata["providerSelection.matchingCandidateIds"]);
        Assert.Equal("100", publicAutomation.RuntimeMetadata["providerSelection.selectedPriority"]);
        Assert.Equal("projected-intent", publicAutomation.RuntimeMetadata["providerMaterialization.providerAction"]);
        Assert.Equal("configured-intent", publicAutomation.RuntimeMetadata["providerMaterialization.observationMode"]);
        Assert.Equal("httproute/edge-system/orders-public-ingress", publicAutomation.RuntimeMetadata["providerMaterialization.providerRouteId"]);
        Assert.Equal("edge-system", publicAutomation.RuntimeMetadata["providerMaterialization.gatewayNamespace"]);
        Assert.Equal("public-gateway", publicAutomation.RuntimeMetadata["providerMaterialization.gatewayName"]);
        Assert.Equal("cephalon.io/gateway-controller", publicAutomation.RuntimeMetadata["providerMaterialization.controllerName"]);
        Assert.Equal("configured-intent", publicAutomation.RuntimeMetadata["providerMaterialization.statusSource"]);
        Assert.Equal("projection-only", publicAutomation.RuntimeMetadata["providerMaterialization.resourceState"]);
        Assert.Equal(CellTrafficAutomationOwnershipStates.Requested, publicAutomation.RuntimeMetadata["providerMaterialization.ownershipState"]);
        Assert.Equal(CellTrafficAutomationDependencyStates.Unknown, publicAutomation.RuntimeMetadata["providerMaterialization.dependencyState"]);
        Assert.Equal(CellTrafficAutomationDriftStates.Unknown, publicAutomation.RuntimeMetadata["providerMaterialization.driftState"]);
        Assert.Equal(CellTrafficAutomationLifecycleActions.Project, publicAutomation.RuntimeMetadata["providerMaterialization.lifecycleAction"]);
        Assert.Equal(CellTrafficAutomationOwnershipStates.Requested, publicAutomation.RuntimeMetadata["materialization.ownershipState"]);
        Assert.Equal(CellTrafficAutomationDependencyStates.Unknown, publicAutomation.RuntimeMetadata["materialization.dependencyState"]);
        Assert.Equal(CellTrafficAutomationDriftStates.Unknown, publicAutomation.RuntimeMetadata["materialization.driftState"]);
        Assert.Equal("none", publicAutomation.RuntimeMetadata["providerMaterialization.gatewayWriteAction"]);
        Assert.Equal("none", publicAutomation.RuntimeMetadata["providerMaterialization.httpRouteWriteAction"]);
        Assert.Equal("unknown", publicAutomation.RuntimeMetadata["providerMaterialization.httpRouteProgrammedCondition"]);

        var cellSurface = Assert.Single(
            technologyCatalog.GetByTechnology("cell-based-architecture"),
            static surface => surface.SurfaceId == "cell-traffic-automations");
        Assert.Contains(cellSurface.Entries, entry =>
            entry.Id == publicAutomation.Id &&
            entry.Metadata["providerMaterializerId"] == "kubernetes-gateway-materializer" &&
            entry.Metadata["providerMaterialization.providerRouteId"] == "httproute/edge-system/orders-public-ingress");

        var gatewaySurface = Assert.Single(
            technologyCatalog.GetByTechnology("cell-based-architecture"),
            static surface => surface.SurfaceId == "kubernetes-gateway-traffic-materializations");
        Assert.Contains(gatewaySurface.Entries, entry =>
            entry.Id == publicAutomation.Id &&
            entry.Metadata["routeId"] == "orders-to-public-ingress" &&
            entry.Metadata["providerRouteId"] == "httproute/edge-system/orders-public-ingress" &&
            entry.Metadata["gatewayName"] == "public-gateway" &&
            entry.Metadata["httpRouteBackendRefs"] == "service/orders-runtime/orders-api:8443@weight/100" &&
            entry.Metadata["statusSource"] == "configured-intent");

        Assert.Contains(snapshot.CellTrafficAutomations, automation =>
            automation.RouteId == "orders-to-public-ingress" &&
            automation.ProviderMaterializerId == "kubernetes-gateway-materializer" &&
            automation.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Pending &&
            automation.MaterializationState == CellTrafficAutomationMaterializationStates.Pending);
    }

    [Fact]
    public async Task HostedServiceLeavesUnmappedKubernetesGatewayAutomationUnavailableWithoutFallbackMaterializer()
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
        Assert.Equal("kubernetes-gateway-materializer", publicAutomation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Pending, publicAutomation.ProviderMaterializationState);

        var adminAutomation = catalog.GetByRouteId("orders-to-admin-ingress");
        Assert.NotNull(adminAutomation);
        Assert.Null(adminAutomation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Unavailable, adminAutomation.ProviderMaterializationState);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Unavailable, adminAutomation.MaterializationState);
        Assert.Equal("0", adminAutomation.RuntimeMetadata["providerSelection.matchingCandidateCount"]);
        Assert.Equal(string.Empty, adminAutomation.RuntimeMetadata["providerSelection.matchingCandidateIds"]);
        Assert.Equal("provider:unavailable", adminAutomation.RuntimeMetadata["materialization.stateBreakdown"]);

        var gatewaySurface = Assert.Single(
            technologyCatalog.GetByTechnology("cell-based-architecture"),
            static surface => surface.SurfaceId == "kubernetes-gateway-traffic-materializations");
        Assert.Contains(gatewaySurface.Entries, entry => entry.Metadata["routeId"] == "orders-to-public-ingress");
        Assert.DoesNotContain(gatewaySurface.Entries, entry => entry.Metadata["routeId"] == "orders-to-admin-ingress");
    }

    [Fact]
    public async Task HostedServiceProjectsObservedGatewayApiStatusWhenObserveOnlyModeIsEnabled()
    {
        var services = CreateServiceCollection(
            includeAdminProjection: false,
            controlPlaneMode: KubernetesGatewayTrafficObservationModes.ObserveOnly,
            configureServices: collection =>
            {
                collection.AddSingleton<IKubernetesGatewayTrafficObservationSource>(
                    new StaticObservationSource(
                        static (automation, projection) => CreateObservedAppliedResult(automation, projection)));
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
        Assert.Equal("kubernetes-gateway-materializer", automation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Applied, automation.ProviderMaterializationState);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Applied, automation.MaterializationState);
        Assert.Equal("observe-only", automation.RuntimeMetadata["providerMaterialization.providerAction"]);
        Assert.Equal("observe-only", automation.RuntimeMetadata["providerMaterialization.observationMode"]);
        Assert.Equal("gateway-api-status", automation.RuntimeMetadata["providerMaterialization.statusSource"]);
        Assert.Equal("true", automation.RuntimeMetadata["providerMaterialization.gatewayAcceptedCondition"]);
        Assert.Equal("true", automation.RuntimeMetadata["providerMaterialization.gatewayProgrammedCondition"]);
        Assert.Equal("true", automation.RuntimeMetadata["providerMaterialization.httpRouteAcceptedCondition"]);
        Assert.Equal("true", automation.RuntimeMetadata["providerMaterialization.httpRouteResolvedRefsCondition"]);
        Assert.Equal("in-sync", automation.RuntimeMetadata["providerMaterialization.driftState"]);
        Assert.Equal(CellTrafficAutomationOwnershipStates.Owned, automation.RuntimeMetadata["providerMaterialization.ownershipState"]);
        Assert.Equal(CellTrafficAutomationDependencyStates.Satisfied, automation.RuntimeMetadata["providerMaterialization.dependencyState"]);
        Assert.Equal(CellTrafficAutomationLifecycleActions.Observe, automation.RuntimeMetadata["providerMaterialization.lifecycleAction"]);

        var gatewaySurface = Assert.Single(
            technologyCatalog.GetByTechnology("cell-based-architecture"),
            static surface => surface.SurfaceId == "kubernetes-gateway-traffic-materializations");
        Assert.Contains(gatewaySurface.Entries, entry =>
            entry.Id == automation.Id &&
            entry.Metadata["statusSource"] == "gateway-api-status" &&
            entry.Metadata["providerAction"] == "observe-only" &&
            entry.Metadata["gatewayAcceptedCondition"] == "true");
    }

    [Fact]
    public async Task HostedServiceAppliesOwnedHttpRoutesAndProjectsObservedTruthWhenApplyAndReconcileModeIsEnabled()
    {
        var services = CreateServiceCollection(
            includeAdminProjection: false,
            controlPlaneMode: KubernetesGatewayTrafficObservationModes.ApplyAndReconcile,
            configureServices: collection =>
            {
                collection.AddSingleton<IKubernetesGatewayTrafficApplyService>(
                    new StaticApplyService(
                        static (automation, projection) => CreateApplyPendingResult(automation.RouteId, "created")));
                collection.AddSingleton<IKubernetesGatewayTrafficObservationSource>(
                    new StaticObservationSource(
                        static (automation, projection) => CreateObservedAppliedResult(automation, projection)));
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
        Assert.Equal("kubernetes-gateway-materializer", automation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Applied, automation.ProviderMaterializationState);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Applied, automation.MaterializationState);
        Assert.Equal("apply-and-reconcile", automation.RuntimeMetadata["providerMaterialization.providerAction"]);
        Assert.Equal("apply-and-reconcile", automation.RuntimeMetadata["providerMaterialization.observationMode"]);
        Assert.Equal("gateway-api-status", automation.RuntimeMetadata["providerMaterialization.statusSource"]);
        Assert.Equal("created", automation.RuntimeMetadata["providerMaterialization.httpRouteWriteAction"]);
        Assert.Equal("none", automation.RuntimeMetadata["providerMaterialization.gatewayWriteAction"]);
        Assert.Equal("owned", automation.RuntimeMetadata["providerMaterialization.ownershipState"]);
        Assert.Equal(CellTrafficAutomationDependencyStates.Satisfied, automation.RuntimeMetadata["providerMaterialization.dependencyState"]);
        Assert.Equal(CellTrafficAutomationDriftStates.InSync, automation.RuntimeMetadata["materialization.driftState"]);
        Assert.Equal(CellTrafficAutomationLifecycleActions.Create, automation.RuntimeMetadata["providerMaterialization.lifecycleAction"]);
        Assert.Equal("true", automation.RuntimeMetadata["providerMaterialization.httpRouteAcceptedCondition"]);
        Assert.Equal("in-sync", automation.RuntimeMetadata["providerMaterialization.driftState"]);

        var gatewaySurface = Assert.Single(
            technologyCatalog.GetByTechnology("cell-based-architecture"),
            static surface => surface.SurfaceId == "kubernetes-gateway-traffic-materializations");
        Assert.Contains(gatewaySurface.Entries, entry =>
            entry.Id == automation.Id &&
            entry.Metadata["providerAction"] == "apply-and-reconcile" &&
            entry.Metadata["httpRouteWriteAction"] == "created" &&
            entry.Metadata["lifecycleAction"] == CellTrafficAutomationLifecycleActions.Create &&
            entry.Metadata["statusSource"] == "gateway-api-status");
    }

    [Fact]
    public void OwnershipEvaluationTreatsExternalHttpRoutesAsConflicts()
    {
        var automation = CreateAutomationDescriptor(
            automationId: "orders-public-automation",
            routeId: "orders-to-public-ingress",
            sourceModuleId: "kubernetes-gateway-traffic-tests");
        using var source = new KubernetesGatewayTrafficObservationSource(
            new KubernetesGatewayTrafficMaterializerOptions(),
            TimeProvider.System,
            runtimeCatalogAccessor: () => new StaticCellTrafficAutomationRuntimeCatalog([automation]));

        var ownership = source.EvaluateOwnership(
            new KubernetesGatewayHttpRouteResource
            {
                Metadata = new KubernetesGatewayObjectMetadata
                {
                    Name = "orders-public-ingress"
                }
            },
            automation);

        Assert.Equal(CellTrafficAutomationOwnershipStates.OwnershipConflict, ownership.State);
        Assert.False(ownership.IsOwned);
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
            sourceModuleId: "kubernetes-gateway-traffic-tests");
        using var source = new KubernetesGatewayTrafficObservationSource(
            new KubernetesGatewayTrafficMaterializerOptions(),
            TimeProvider.System,
            runtimeCatalogAccessor: () => new StaticCellTrafficAutomationRuntimeCatalog([automation]));

        var ownership = source.EvaluateOwnership(
            new KubernetesGatewayHttpRouteResource
            {
                Metadata = new KubernetesGatewayObjectMetadata
                {
                    Name = "orders-public-ingress",
                    Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [KubernetesGatewayOwnership.ManagedByLabel] = KubernetesGatewayOwnership.ManagedByValue
                    },
                    Annotations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [KubernetesGatewayOwnership.RouteIdAnnotation] = automation.RouteId
                    }
                }
            },
            automation);

        Assert.Equal(CellTrafficAutomationOwnershipStates.Orphaned, ownership.State);
        Assert.False(ownership.IsOwned);
        Assert.False(ownership.IsConflict);
        Assert.True(ownership.CanTransfer);
        Assert.Equal("incomplete-current-owner", ownership.Reason);
        Assert.Equal("orphaned-httproute", ownership.ResourceState);
        Assert.Equal(automation.Id, ownership.ActiveOwnerId);
    }

    [Fact]
    public async Task ObservationHostedServiceRefreshesLiveGatewayStatusOnPollingInterval()
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
            controlPlaneMode: KubernetesGatewayTrafficObservationModes.ObserveOnly,
            pollingIntervalSeconds: 1,
            configureServices: collection =>
            {
                collection.AddSingleton<IKubernetesGatewayTrafficObservationSource>(source);
            });

        using var provider = services.BuildServiceProvider();
        foreach (var hostedService in provider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        var catalog = provider.GetRequiredService<ICellTrafficAutomationRuntimeCatalog>();

        var initial = catalog.GetByRouteId("orders-to-public-ingress");
        Assert.NotNull(initial);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Pending, initial.ProviderMaterializationState);
        Assert.Equal("missing-httproute", initial.RuntimeMetadata["providerMaterialization.resourceState"]);

        await Task.Delay(TimeSpan.FromMilliseconds(1400));

        var refreshed = catalog.GetByRouteId("orders-to-public-ingress");
        Assert.NotNull(refreshed);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Applied, refreshed.ProviderMaterializationState);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Applied, refreshed.MaterializationState);
        Assert.Equal("available", refreshed.RuntimeMetadata["providerMaterialization.resourceState"]);
        Assert.Equal("in-sync", refreshed.RuntimeMetadata["providerMaterialization.driftState"]);
        Assert.Equal("gateway-api-status", refreshed.RuntimeMetadata["providerMaterialization.statusSource"]);
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
            controlPlaneMode: KubernetesGatewayTrafficObservationModes.ApplyAndReconcile,
            pollingIntervalSeconds: 1,
            configureServices: collection =>
            {
                collection.AddSingleton<IKubernetesGatewayTrafficApplyService>(applyService);
                collection.AddSingleton<IKubernetesGatewayTrafficObservationSource>(observationSource);
            });

        using var provider = services.BuildServiceProvider();
        foreach (var hostedService in provider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        var catalog = provider.GetRequiredService<ICellTrafficAutomationRuntimeCatalog>();

        var initial = catalog.GetByRouteId("orders-to-public-ingress");
        Assert.NotNull(initial);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Pending, initial.ProviderMaterializationState);
        Assert.Equal("apply-and-reconcile", initial.RuntimeMetadata["providerMaterialization.providerAction"]);
        Assert.Equal("created", initial.RuntimeMetadata["providerMaterialization.httpRouteWriteAction"]);

        await Task.Delay(TimeSpan.FromMilliseconds(1400));

        var refreshed = catalog.GetByRouteId("orders-to-public-ingress");
        Assert.NotNull(refreshed);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Applied, refreshed.ProviderMaterializationState);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Applied, refreshed.MaterializationState);
        Assert.Equal("apply-and-reconcile", refreshed.RuntimeMetadata["providerMaterialization.providerAction"]);
        Assert.Equal("replaced", refreshed.RuntimeMetadata["providerMaterialization.httpRouteWriteAction"]);
        Assert.Equal("gateway-api-status", refreshed.RuntimeMetadata["providerMaterialization.statusSource"]);
        Assert.Equal("in-sync", refreshed.RuntimeMetadata["providerMaterialization.driftState"]);
    }

    private static ServiceCollection CreateServiceCollection(
        bool includeAdminProjection,
        string controlPlaneMode = KubernetesGatewayTrafficObservationModes.ConfiguredIntent,
        int pollingIntervalSeconds = 30,
        Action<ServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        configureServices?.Invoke(services);
        services.AddCephalon(engine => ConfigureEngine(engine, includeAdminProjection, controlPlaneMode, pollingIntervalSeconds));
        return services;
    }

    private static void ConfigureEngine(
        EngineBuilder engine,
        bool includeAdminProjection,
        string controlPlaneMode = KubernetesGatewayTrafficObservationModes.ConfiguredIntent,
        int pollingIntervalSeconds = 30)
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
                            providerId: KubernetesGatewayTrafficMaterializerOptions.DefaultProviderId),
                        new CellTrafficAutomationRouteSettings(
                            routeId: "orders-to-admin-ingress",
                            automationMode: "automatic",
                            triggerMode: "source-health",
                            actionMode: "prefer-local-route",
                            materializationMode: "provider-managed",
                            notes: null,
                            metadata: null,
                            providerId: KubernetesGatewayTrafficMaterializerOptions.DefaultProviderId)
                    ]))));
        engine.AddModule(new KubernetesGatewayTrafficTestModule());
        engine.AddKubernetesGatewayTrafficMaterializer(options =>
        {
            options.ControllerName = "cephalon.io/gateway-controller";
            options.GatewayClassName = "cephalon-public";
            options.GatewayNamespace = "edge-system";
            options.GatewayName = "public-gateway";
            options.ListenerName = "https";
            options.RouteNamespace = "edge-system";
            options.Observation.Mode = controlPlaneMode;
            if (!string.Equals(
                    controlPlaneMode,
                    KubernetesGatewayTrafficObservationModes.ConfiguredIntent,
                    StringComparison.OrdinalIgnoreCase))
            {
                options.Observation.PollingIntervalSeconds = pollingIntervalSeconds;
                options.Observation.StaleAfterSeconds = Math.Max(5, pollingIntervalSeconds * 3);
            }

            options.Routes.Add(new KubernetesGatewayTrafficRouteOptions
            {
                RouteId = "orders-to-public-ingress",
                HttpRouteName = "orders-public-ingress",
                BackendNamespace = "orders-runtime",
                BackendServiceName = "orders-api",
                BackendPort = 8443,
                BackendWeight = 100
            });

            if (includeAdminProjection)
            {
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
            }
        });
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
            routingStrategy: "gateway-managed",
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

    private static CellTrafficAutomationProviderMaterializationResult CreateApplyPendingResult(string routeId, string writeAction)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["providerAction"] = KubernetesGatewayTrafficObservationModes.ApplyAndReconcile,
            ["observationMode"] = KubernetesGatewayTrafficObservationModes.ApplyAndReconcile,
            ["statusSource"] = "control-plane-apply",
            ["resourceState"] = "write-succeeded",
            ["driftState"] = "reconciling",
            ["driftReasons"] = string.Empty,
            ["dependencyState"] = CellTrafficAutomationDependencyStates.Satisfied,
            ["lifecycleAction"] = string.Equals(writeAction, "created", StringComparison.OrdinalIgnoreCase)
                ? CellTrafficAutomationLifecycleActions.Create
                : CellTrafficAutomationLifecycleActions.Replace,
            ["gatewayWriteAction"] = "none",
            ["gatewayWriteReason"] = "preprovisioned-dependency",
            ["httpRouteWriteAction"] = writeAction,
            ["ownershipState"] = CellTrafficAutomationOwnershipStates.Owned,
            ["httpRouteAppliedGeneration"] = writeAction == "created" ? "1" : "2",
            ["providerRouteId"] = "httproute/edge-system/orders-public-ingress",
            ["gatewayNamespace"] = "edge-system",
            ["gatewayName"] = "public-gateway",
            ["controllerName"] = "cephalon.io/gateway-controller"
        };

        return new CellTrafficAutomationProviderMaterializationResult(
            state: CellTrafficAutomationProviderMaterializationStates.Pending,
            observedAtUtc: DateTimeOffset.UtcNow,
            metadata: metadata);
    }

    private static CellTrafficAutomationProviderMaterializationResult CreateObservedPendingResult(string routeId)
    {
        var metadata = CreateObservedMetadata(routeId, resourceState: "missing-httproute");
        metadata["gatewayExists"] = "true";
        metadata["httpRouteExists"] = "false";
        metadata["driftState"] = "unknown";
        metadata["driftReasons"] = string.Empty;
        metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Requested;
        metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Satisfied;
        metadata["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Observe;

        return new CellTrafficAutomationProviderMaterializationResult(
            state: CellTrafficAutomationProviderMaterializationStates.Pending,
            observedAtUtc: DateTimeOffset.UtcNow,
            metadata: metadata);
    }

    private static CellTrafficAutomationProviderMaterializationResult CreateObservedAppliedResult(
        CellTrafficAutomationRuntimeDescriptor automation,
        KubernetesGatewayTrafficRouteProjection projection)
    {
        return CreateObservedAppliedResult(automation.RouteId);
    }

    private static CellTrafficAutomationProviderMaterializationResult CreateObservedAppliedResult(string routeId)
    {
        var metadata = CreateObservedMetadata(routeId, resourceState: "available");
        metadata["gatewayExists"] = "true";
        metadata["httpRouteExists"] = "true";
        metadata["gatewayGeneration"] = "4";
        metadata["gatewayAcceptedCondition"] = "true";
        metadata["gatewayAcceptedObservedGeneration"] = "4";
        metadata["gatewayAcceptedReason"] = "Accepted";
        metadata["gatewayProgrammedCondition"] = "true";
        metadata["gatewayProgrammedObservedGeneration"] = "4";
        metadata["gatewayProgrammedReason"] = "Programmed";
        metadata["httpRouteGeneration"] = "7";
        metadata["httpRouteAcceptedCondition"] = "true";
        metadata["httpRouteAcceptedObservedGeneration"] = "7";
        metadata["httpRouteAcceptedReason"] = "Accepted";
        metadata["httpRouteResolvedRefsCondition"] = "true";
        metadata["httpRouteResolvedRefsObservedGeneration"] = "7";
        metadata["httpRouteResolvedRefsReason"] = "ResolvedRefs";
        metadata["driftState"] = "in-sync";
        metadata["driftReasons"] = string.Empty;
        metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Owned;
        metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Satisfied;
        metadata["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Observe;
        metadata["managedBy"] = "edge-kubernetes-gateway";
        metadata["observedAutomationId"] = "orders-to-public-ingress";
        metadata["observedRouteId"] = "orders-to-public-ingress";
        metadata["observedSourceModuleId"] = "kubernetes-gateway-traffic-tests";

        return new CellTrafficAutomationProviderMaterializationResult(
            state: CellTrafficAutomationProviderMaterializationStates.Applied,
            observedAtUtc: DateTimeOffset.UtcNow,
            metadata: metadata);
    }

    private static Dictionary<string, string> CreateObservedMetadata(string routeId, string resourceState)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["providerAction"] = "observe-only",
            ["observationMode"] = KubernetesGatewayTrafficObservationModes.ObserveOnly,
            ["statusSource"] = "gateway-api-status",
            ["resourceState"] = resourceState,
            ["ownershipState"] = CellTrafficAutomationOwnershipStates.Requested,
            ["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown,
            ["driftState"] = CellTrafficAutomationDriftStates.Unknown,
            ["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Observe,
            ["observationFreshUntilUtc"] = DateTimeOffset.UtcNow.AddMinutes(2).ToString("O"),
            ["providerRouteId"] = $"httproute/edge-system/{routeId.Replace("orders-to-", "orders-", StringComparison.OrdinalIgnoreCase)}"
        };

        if (string.Equals(routeId, "orders-to-public-ingress", StringComparison.OrdinalIgnoreCase))
        {
            metadata["gatewayNamespace"] = "edge-system";
            metadata["gatewayName"] = "public-gateway";
            metadata["controllerName"] = "cephalon.io/gateway-controller";
            metadata["providerRouteId"] = "httproute/edge-system/orders-public-ingress";
        }

        return metadata;
    }

    private sealed class StaticApplyService(
        Func<CellTrafficAutomationRuntimeDescriptor, KubernetesGatewayTrafficRouteProjection, CellTrafficAutomationProviderMaterializationResult> factory)
        : IKubernetesGatewayTrafficApplyService
    {
        public ValueTask<CellTrafficAutomationProviderMaterializationResult> ApplyAsync(
            CellTrafficAutomationRuntimeDescriptor automation,
            KubernetesGatewayTrafficRouteProjection projection,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(factory(automation, projection));
        }
    }

    private sealed class SequencedApplyService(
        Dictionary<string, Queue<CellTrafficAutomationProviderMaterializationResult>> resultsByRouteId)
        : IKubernetesGatewayTrafficApplyService
    {
        private readonly Lock gate = new();

        public ValueTask<CellTrafficAutomationProviderMaterializationResult> ApplyAsync(
            CellTrafficAutomationRuntimeDescriptor automation,
            KubernetesGatewayTrafficRouteProjection projection,
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

    private sealed class StaticObservationSource(
        Func<CellTrafficAutomationRuntimeDescriptor, KubernetesGatewayTrafficRouteProjection, CellTrafficAutomationProviderMaterializationResult> factory)
        : IKubernetesGatewayTrafficObservationSource
    {
        public ValueTask<CellTrafficAutomationProviderMaterializationResult> ObserveAsync(
            CellTrafficAutomationRuntimeDescriptor automation,
            KubernetesGatewayTrafficRouteProjection projection,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(factory(automation, projection));
        }
    }

    private sealed class SequencedObservationSource(
        Dictionary<string, Queue<CellTrafficAutomationProviderMaterializationResult>> resultsByRouteId)
        : IKubernetesGatewayTrafficObservationSource
    {
        private readonly Lock gate = new();

        public ValueTask<CellTrafficAutomationProviderMaterializationResult> ObserveAsync(
            CellTrafficAutomationRuntimeDescriptor automation,
            KubernetesGatewayTrafficRouteProjection projection,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(automation);
            cancellationToken.ThrowIfCancellationRequested();

            lock (gate)
            {
                if (!resultsByRouteId.TryGetValue(automation.RouteId, out var queue) || queue.Count == 0)
                {
                    return ValueTask.FromResult(CreateObservedAppliedResult(automation.RouteId));
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

    private sealed class KubernetesGatewayTrafficTestModule :
        ModuleBase,
        ICellBoundaryContributor,
        ICellRouteContributor,
        ICellHealthIsolationContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "kubernetes-gateway-traffic-tests",
            displayName: "Kubernetes Gateway Traffic Tests",
            description: "Provides cell topology and health isolation for Kubernetes Gateway materializer tests.");

        public void RegisterCellBoundaries(ICellBoundaryRegistry cells)
        {
            cells.Add(new CellBoundaryDescriptor(
                id: "orders-cell",
                sourceModuleId: "kubernetes-gateway-traffic-tests",
                displayName: "Orders Cell",
                description: "Keeps order-serving workloads together.",
                blastRadius: "regional",
                routingStrategy: "local-first",
                moduleIds: ["kubernetes-gateway-traffic-tests"]));
            cells.Add(new CellBoundaryDescriptor(
                id: "public-edge-cell",
                sourceModuleId: "kubernetes-gateway-traffic-tests",
                displayName: "Public Edge Cell",
                description: "Fronts public ingress traffic.",
                blastRadius: "public-edge",
                routingStrategy: "gateway-fanout"));
            cells.Add(new CellBoundaryDescriptor(
                id: "admin-edge-cell",
                sourceModuleId: "kubernetes-gateway-traffic-tests",
                displayName: "Admin Edge Cell",
                description: "Fronts administrative ingress traffic.",
                blastRadius: "admin-edge",
                routingStrategy: "gateway-fanout"));
        }

        public void RegisterCellRoutes(ICellRouteRegistry routes)
        {
            routes.Add(new CellRouteDescriptor(
                id: "orders-to-public-ingress",
                sourceModuleId: "kubernetes-gateway-traffic-tests",
                sourceCellId: "orders-cell",
                targetCellId: "public-edge-cell",
                displayName: "Orders To Public Ingress",
                description: "Projects order-serving traffic to the public ingress boundary.",
                routingStrategy: "gateway-managed",
                governanceMode: "policy-guarded",
                transportIds: ["rest-api"]));
            routes.Add(new CellRouteDescriptor(
                id: "orders-to-admin-ingress",
                sourceModuleId: "kubernetes-gateway-traffic-tests",
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
                sourceModuleId: "kubernetes-gateway-traffic-tests",
                cellId: "orders-cell",
                displayName: "Orders Cell Health",
                description: "Contains order-serving failures inside the orders cell.",
                failureIsolationMode: "cell-quarantine",
                readinessScope: "dependency-aware",
                restartScope: "cell-only",
                dependencyIds: ["orders-db"]));
            healthIsolations.Add(new CellHealthIsolationDescriptor(
                id: "public-edge-health",
                sourceModuleId: "kubernetes-gateway-traffic-tests",
                cellId: "public-edge-cell",
                displayName: "Public Edge Health",
                description: "Contains public ingress failures.",
                failureIsolationMode: "degraded-serving",
                readinessScope: "best-effort",
                restartScope: "manual",
                dependencyIds: ["public-gateway"]));
            healthIsolations.Add(new CellHealthIsolationDescriptor(
                id: "admin-edge-health",
                sourceModuleId: "kubernetes-gateway-traffic-tests",
                cellId: "admin-edge-cell",
                displayName: "Admin Edge Health",
                description: "Contains admin ingress failures.",
                failureIsolationMode: "degraded-serving",
                readinessScope: "dependency-aware",
                restartScope: "manual",
                dependencyIds: ["admin-gateway"]));
        }
    }
}
