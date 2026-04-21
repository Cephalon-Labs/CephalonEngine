using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Edge.Registration;
using Cephalon.Edge.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Composition;

public sealed class CellTrafficAutomationRuntimeCatalogTests
{
    [Fact]
    public void BuildCollectsCellTrafficAutomationsAndProjectsTechnologySurface()
    {
        var services = CreateServiceCollection();

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var catalog = provider.GetRequiredService<ICellTrafficAutomationRuntimeCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        Assert.Equal(2, catalog.Automations.Count);
        Assert.Contains(runtime.Manifest.AppProfile.Technologies, technology => technology.Id == "cell-based-architecture");

        var defaultAutomation = catalog.GetByRouteId("orders-to-reporting");
        Assert.NotNull(defaultAutomation);
        Assert.Equal("automatic", defaultAutomation.AutomationMode);
        Assert.Equal("source-or-target-health", defaultAutomation.TriggerMode);
        Assert.Equal("shed-load", defaultAutomation.ActionMode);
        Assert.Equal("provider-and-edge-managed", defaultAutomation.MaterializationMode);
        Assert.Equal("cell-default", defaultAutomation.PolicySource);
        Assert.Equal("regional-traffic-mesh", defaultAutomation.ProviderId);
        Assert.Equal(["storefront-edge"], defaultAutomation.EdgeNodeIds);
        Assert.Equal("edge-runtime-materializer", defaultAutomation.EdgeMaterializerId);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Pending, defaultAutomation.EdgeMaterializationState);
        Assert.Null(defaultAutomation.EdgeMaterializationObservedAtUtc);
        Assert.Null(defaultAutomation.EdgeMaterializationError);
        Assert.Null(defaultAutomation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Unavailable, defaultAutomation.ProviderMaterializationState);
        Assert.Null(defaultAutomation.ProviderMaterializationObservedAtUtc);
        Assert.Null(defaultAutomation.ProviderMaterializationError);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Partial, defaultAutomation.MaterializationState);
        Assert.Null(defaultAutomation.MaterializationObservedAtUtc);
        Assert.Null(defaultAutomation.MaterializationError);
        Assert.Equal(["orders-cell-health"], defaultAutomation.SourceHealthIsolationIds);
        Assert.Equal(["reporting-cell-health"], defaultAutomation.TargetHealthIsolationIds);
        Assert.Equal(["orders-db", "reporting-replica"], defaultAutomation.DependencyIds);
        Assert.Equal("0", defaultAutomation.RuntimeMetadata["providerSelection.matchingCandidateCount"]);
        Assert.Equal(string.Empty, defaultAutomation.RuntimeMetadata["providerSelection.matchingCandidateIds"]);
        Assert.Equal("1", defaultAutomation.RuntimeMetadata["edgeSelection.matchingCandidateCount"]);
        Assert.Equal("edge-runtime-materializer", defaultAutomation.RuntimeMetadata["edgeSelection.matchingCandidateIds"]);
        Assert.Equal("0", defaultAutomation.RuntimeMetadata["edgeSelection.selectedPriority"]);
        Assert.Equal("provider,edge", defaultAutomation.RuntimeMetadata["materialization.requiredDimensions"]);
        Assert.Equal("1", defaultAutomation.RuntimeMetadata["materialization.selectedDimensionCount"]);
        Assert.Equal("edge", defaultAutomation.RuntimeMetadata["materialization.selectedDimensions"]);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Partial, defaultAutomation.RuntimeMetadata["materialization.state"]);
        Assert.Equal("provider:unavailable,edge:pending", defaultAutomation.RuntimeMetadata["materialization.stateBreakdown"]);
        Assert.Equal(CellTrafficAutomationOwnershipStates.Requested, defaultAutomation.RuntimeMetadata["providerMaterialization.ownershipState"]);
        Assert.Equal(CellTrafficAutomationOwnershipStates.Requested, defaultAutomation.RuntimeMetadata["edgeMaterialization.ownershipState"]);
        Assert.Equal(CellTrafficAutomationOwnershipStates.Requested, defaultAutomation.RuntimeMetadata["materialization.ownershipState"]);
        Assert.Equal("provider:requested,edge:requested", defaultAutomation.RuntimeMetadata["materialization.ownershipBreakdown"]);
        Assert.Equal(CellTrafficAutomationDependencyStates.Unknown, defaultAutomation.RuntimeMetadata["materialization.dependencyState"]);
        Assert.Equal("provider:unknown,edge:unknown", defaultAutomation.RuntimeMetadata["materialization.dependencyBreakdown"]);
        Assert.Equal(CellTrafficAutomationDriftStates.Unknown, defaultAutomation.RuntimeMetadata["materialization.driftState"]);
        Assert.Equal("provider:unknown,edge:unknown", defaultAutomation.RuntimeMetadata["materialization.driftBreakdown"]);
        Assert.Equal("0", defaultAutomation.RuntimeMetadata["materialization.lifecycleActionCount"]);
        Assert.Equal(string.Empty, defaultAutomation.RuntimeMetadata["materialization.lifecycleActions"]);
        Assert.Equal(string.Empty, defaultAutomation.RuntimeMetadata["materialization.lifecycleActionBreakdown"]);

        var routedAutomation = catalog.GetById("orders-to-platform-control");
        Assert.NotNull(routedAutomation);
        Assert.Equal("advisory", routedAutomation.AutomationMode);
        Assert.Equal("source-health", routedAutomation.TriggerMode);
        Assert.Equal("prefer-local-route", routedAutomation.ActionMode);
        Assert.Equal("provider-managed", routedAutomation.MaterializationMode);
        Assert.Equal("cell-route", routedAutomation.PolicySource);
        Assert.Equal("control-plane-gateway", routedAutomation.ProviderId);
        Assert.Equal(["platform-edge"], routedAutomation.EdgeNodeIds);
        Assert.Null(routedAutomation.EdgeMaterializerId);
        Assert.Null(routedAutomation.EdgeMaterializationState);
        Assert.Null(routedAutomation.EdgeMaterializationObservedAtUtc);
        Assert.Null(routedAutomation.EdgeMaterializationError);
        Assert.Null(routedAutomation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Unavailable, routedAutomation.ProviderMaterializationState);
        Assert.Null(routedAutomation.ProviderMaterializationObservedAtUtc);
        Assert.Null(routedAutomation.ProviderMaterializationError);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Unavailable, routedAutomation.MaterializationState);
        Assert.Null(routedAutomation.MaterializationObservedAtUtc);
        Assert.Null(routedAutomation.MaterializationError);
        Assert.Equal(["orders-cell-health"], routedAutomation.SourceHealthIsolationIds);
        Assert.Equal(["platform-control-health"], routedAutomation.TargetHealthIsolationIds);
        Assert.Equal("Keep provider handoff explicit for control-plane traffic.", routedAutomation.RuntimeMetadata["note"]);
        Assert.Equal("ingress-provider", routedAutomation.RuntimeMetadata["handoff"]);
        Assert.Equal("0", routedAutomation.RuntimeMetadata["providerSelection.matchingCandidateCount"]);
        Assert.Equal("provider", routedAutomation.RuntimeMetadata["materialization.requiredDimensions"]);
        Assert.Equal("0", routedAutomation.RuntimeMetadata["materialization.selectedDimensionCount"]);
        Assert.Equal(string.Empty, routedAutomation.RuntimeMetadata["materialization.selectedDimensions"]);
        Assert.Equal("provider:unavailable", routedAutomation.RuntimeMetadata["materialization.stateBreakdown"]);
        Assert.Equal(CellTrafficAutomationOwnershipStates.Requested, routedAutomation.RuntimeMetadata["providerMaterialization.ownershipState"]);
        Assert.Equal(CellTrafficAutomationOwnershipStates.Requested, routedAutomation.RuntimeMetadata["materialization.ownershipState"]);
        Assert.Equal("provider:requested", routedAutomation.RuntimeMetadata["materialization.ownershipBreakdown"]);
        Assert.Equal(CellTrafficAutomationDependencyStates.Unknown, routedAutomation.RuntimeMetadata["materialization.dependencyState"]);
        Assert.Equal("provider:unknown", routedAutomation.RuntimeMetadata["materialization.dependencyBreakdown"]);
        Assert.Equal(CellTrafficAutomationDriftStates.Unknown, routedAutomation.RuntimeMetadata["materialization.driftState"]);
        Assert.Equal("provider:unknown", routedAutomation.RuntimeMetadata["materialization.driftBreakdown"]);

        var sourceModuleAutomations = catalog.GetBySourceModule("cell-traffic-tests");
        Assert.Equal(2, sourceModuleAutomations.Count);

        var targetCellAutomations = catalog.GetByTargetCellId("reporting-cell");
        var reportingAutomation = Assert.Single(targetCellAutomations);
        Assert.Equal("orders-to-reporting", reportingAutomation.RouteId);

        var meshAutomations = catalog.GetByProvider("regional-traffic-mesh");
        var meshAutomation = Assert.Single(meshAutomations);
        Assert.Equal("orders-to-reporting", meshAutomation.RouteId);

        var storefrontEdgeAutomations = catalog.GetByEdgeNodeId("storefront-edge");
        var storefrontAutomation = Assert.Single(storefrontEdgeAutomations);
        Assert.Equal("orders-to-reporting", storefrontAutomation.RouteId);

        var healthIsolationAutomations = catalog.GetByHealthIsolationId("orders-cell-health");
        Assert.Equal(2, healthIsolationAutomations.Count);

        Assert.Equal(2, snapshot.CellTrafficAutomations.Count);
        Assert.Contains(snapshot.CellTrafficAutomations, automation =>
            automation.RouteId == "orders-to-reporting" &&
            automation.EdgeMaterializerId == "edge-runtime-materializer" &&
            automation.EdgeMaterializationState == CellTrafficAutomationMaterializationStates.Pending &&
            automation.MaterializationState == CellTrafficAutomationMaterializationStates.Partial);
        Assert.Contains(snapshot.CellTrafficAutomations, automation =>
            automation.RouteId == "orders-to-platform-control" &&
            automation.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Unavailable &&
            automation.MaterializationState == CellTrafficAutomationMaterializationStates.Unavailable &&
            automation.ProviderId == "control-plane-gateway" &&
            automation.EdgeNodeIds.SequenceEqual(["platform-edge"]));

        var surface = Assert.Single(
            technologyCatalog.GetByTechnology("cell-based-architecture"),
            static candidate => candidate.SurfaceId == "cell-traffic-automations");
        Assert.Contains(surface.Entries, entry =>
            entry.Id == "orders-to-reporting" &&
            entry.Metadata["providerId"] == "regional-traffic-mesh" &&
            entry.Metadata["edgeNodeIds"] == "storefront-edge" &&
            entry.Metadata["edgeMaterializerId"] == "edge-runtime-materializer" &&
            entry.Metadata["edgeMaterializationState"] == CellTrafficAutomationMaterializationStates.Pending &&
            entry.Metadata["providerMaterializationState"] == CellTrafficAutomationProviderMaterializationStates.Unavailable &&
            entry.Metadata["materializationState"] == CellTrafficAutomationMaterializationStates.Partial &&
            entry.Metadata["policySource"] == "cell-default" &&
            entry.Metadata["sourceHealthIsolationIds"] == "orders-cell-health" &&
            entry.Metadata["targetHealthIsolationIds"] == "reporting-cell-health");
        Assert.Contains(surface.Entries, entry =>
            entry.Id == "orders-to-platform-control" &&
            entry.Metadata["providerId"] == "control-plane-gateway" &&
            entry.Metadata["edgeNodeIds"] == "platform-edge" &&
            entry.Metadata["providerMaterializationState"] == CellTrafficAutomationProviderMaterializationStates.Unavailable &&
            entry.Metadata["materializationState"] == CellTrafficAutomationMaterializationStates.Unavailable &&
            entry.Metadata["materializationMode"] == "provider-managed" &&
            entry.Metadata["handoff"] == "ingress-provider");
    }

    [Fact]
    public async Task HostedServiceMaterializesProviderManagedTrafficAutomationWhenProviderMaterializerIsRegistered()
    {
        var services = CreateServiceCollection(collection =>
        {
            collection.AddSingleton<ICellTrafficAutomationProviderMaterializer>(
                new TestCellTrafficAutomationProviderMaterializer(
                    materializerId: "regional-traffic-materializer",
                    providerId: "regional-traffic-mesh",
                    priority: 100));
            collection.AddSingleton<ICellTrafficAutomationProviderMaterializer>(
                new TestCellTrafficAutomationProviderMaterializer(
                    materializerId: "regional-traffic-fallback",
                    providerId: "regional-traffic-mesh",
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

        var defaultAutomation = catalog.GetByRouteId("orders-to-reporting");
        Assert.NotNull(defaultAutomation);
        Assert.Equal("edge-runtime-materializer", defaultAutomation.EdgeMaterializerId);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Applied, defaultAutomation.EdgeMaterializationState);
        Assert.NotNull(defaultAutomation.EdgeMaterializationObservedAtUtc);
        Assert.Null(defaultAutomation.EdgeMaterializationError);
        Assert.Equal("reconciled", defaultAutomation.RuntimeMetadata["edgeMaterialization.edgeAction"]);
        Assert.Equal("storefront-edge", defaultAutomation.RuntimeMetadata["edgeMaterialization.materializedEdgeNodeIds"]);
        Assert.Equal("regional-traffic-materializer", defaultAutomation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Applied, defaultAutomation.ProviderMaterializationState);
        Assert.NotNull(defaultAutomation.ProviderMaterializationObservedAtUtc);
        Assert.Null(defaultAutomation.ProviderMaterializationError);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Applied, defaultAutomation.MaterializationState);
        Assert.NotNull(defaultAutomation.MaterializationObservedAtUtc);
        Assert.Null(defaultAutomation.MaterializationError);
        Assert.Equal("2", defaultAutomation.RuntimeMetadata["providerSelection.matchingCandidateCount"]);
        Assert.Equal("regional-traffic-materializer,regional-traffic-fallback", defaultAutomation.RuntimeMetadata["providerSelection.matchingCandidateIds"]);
        Assert.Equal("100", defaultAutomation.RuntimeMetadata["providerSelection.selectedPriority"]);
        Assert.Equal("regional-route-orders-to-reporting", defaultAutomation.RuntimeMetadata["providerMaterialization.providerRouteId"]);
        Assert.Equal("reconciled", defaultAutomation.RuntimeMetadata["providerMaterialization.providerAction"]);
        Assert.Equal(CellTrafficAutomationOwnershipStates.Owned, defaultAutomation.RuntimeMetadata["providerMaterialization.ownershipState"]);
        Assert.Equal(CellTrafficAutomationDependencyStates.Satisfied, defaultAutomation.RuntimeMetadata["providerMaterialization.dependencyState"]);
        Assert.Equal(CellTrafficAutomationDriftStates.InSync, defaultAutomation.RuntimeMetadata["providerMaterialization.driftState"]);
        Assert.Equal(CellTrafficAutomationLifecycleActions.Reconcile, defaultAutomation.RuntimeMetadata["providerMaterialization.lifecycleAction"]);
        Assert.Equal(CellTrafficAutomationOwnershipStates.Owned, defaultAutomation.RuntimeMetadata["edgeMaterialization.ownershipState"]);
        Assert.Equal(CellTrafficAutomationDependencyStates.Satisfied, defaultAutomation.RuntimeMetadata["edgeMaterialization.dependencyState"]);
        Assert.Equal(CellTrafficAutomationDriftStates.InSync, defaultAutomation.RuntimeMetadata["edgeMaterialization.driftState"]);
        Assert.Equal(CellTrafficAutomationLifecycleActions.Reconcile, defaultAutomation.RuntimeMetadata["edgeMaterialization.lifecycleAction"]);
        Assert.Equal(CellTrafficAutomationOwnershipStates.Owned, defaultAutomation.RuntimeMetadata["materialization.ownershipState"]);
        Assert.Equal("provider:owned,edge:owned", defaultAutomation.RuntimeMetadata["materialization.ownershipBreakdown"]);
        Assert.Equal(CellTrafficAutomationDependencyStates.Satisfied, defaultAutomation.RuntimeMetadata["materialization.dependencyState"]);
        Assert.Equal("provider:satisfied,edge:satisfied", defaultAutomation.RuntimeMetadata["materialization.dependencyBreakdown"]);
        Assert.Equal(CellTrafficAutomationDriftStates.InSync, defaultAutomation.RuntimeMetadata["materialization.driftState"]);
        Assert.Equal("provider:in-sync,edge:in-sync", defaultAutomation.RuntimeMetadata["materialization.driftBreakdown"]);
        Assert.Equal("1", defaultAutomation.RuntimeMetadata["materialization.lifecycleActionCount"]);
        Assert.Equal(CellTrafficAutomationLifecycleActions.Reconcile, defaultAutomation.RuntimeMetadata["materialization.lifecycleActions"]);
        Assert.Equal("provider:reconcile,edge:reconcile", defaultAutomation.RuntimeMetadata["materialization.lifecycleActionBreakdown"]);
        Assert.Equal("10", defaultAutomation.RuntimeMetadata["materialization.conditionCount"]);
        Assert.Equal("5", defaultAutomation.RuntimeMetadata["providerMaterialization.conditionCount"]);
        Assert.Equal("5", defaultAutomation.RuntimeMetadata["edgeMaterialization.conditionCount"]);
        Assert.Equal(CellTrafficAutomationMaterializationConditionSeverities.Info, defaultAutomation.RuntimeMetadata["materialization.highestConditionSeverity"]);
        Assert.Contains(defaultAutomation.MaterializationConditions, condition =>
            condition.Dimension == CellTrafficAutomationMaterializationConditionDimensions.Provider &&
            condition.ConditionId == "runtime-observable" &&
            condition.Category == CellTrafficAutomationMaterializationConditionCategories.Observation);
        Assert.Contains(defaultAutomation.MaterializationConditions, condition =>
            condition.Dimension == CellTrafficAutomationMaterializationConditionDimensions.Edge &&
            condition.ConditionId == "runtime-observable" &&
            condition.Category == CellTrafficAutomationMaterializationConditionCategories.Observation);

        var routedAutomation = catalog.GetByRouteId("orders-to-platform-control");
        Assert.NotNull(routedAutomation);
        Assert.Null(routedAutomation.EdgeMaterializerId);
        Assert.Null(routedAutomation.EdgeMaterializationState);
        Assert.Null(routedAutomation.EdgeMaterializationObservedAtUtc);
        Assert.Null(routedAutomation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Unavailable, routedAutomation.ProviderMaterializationState);
        Assert.Null(routedAutomation.ProviderMaterializationObservedAtUtc);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Unavailable, routedAutomation.MaterializationState);

        Assert.Contains(snapshot.CellTrafficAutomations, automation =>
            automation.RouteId == "orders-to-reporting" &&
            automation.EdgeMaterializerId == "edge-runtime-materializer" &&
            automation.EdgeMaterializationState == CellTrafficAutomationMaterializationStates.Applied &&
            automation.ProviderMaterializerId == "regional-traffic-materializer" &&
            automation.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Applied &&
            automation.MaterializationState == CellTrafficAutomationMaterializationStates.Applied &&
            automation.MaterializationConditions.Count == 10);

        var surface = Assert.Single(
            technologyCatalog.GetByTechnology("cell-based-architecture"),
            static candidate => candidate.SurfaceId == "cell-traffic-automations");
        Assert.Contains(surface.Entries, entry =>
            entry.Id == "orders-to-reporting" &&
            entry.Metadata["edgeMaterializerId"] == "edge-runtime-materializer" &&
            entry.Metadata["edgeMaterializationState"] == CellTrafficAutomationMaterializationStates.Applied &&
            entry.Metadata["edgeMaterialization.materializedEdgeNodeIds"] == "storefront-edge" &&
            entry.Metadata["providerMaterializerId"] == "regional-traffic-materializer" &&
            entry.Metadata["providerMaterializationState"] == CellTrafficAutomationProviderMaterializationStates.Applied &&
            entry.Metadata["materializationState"] == CellTrafficAutomationMaterializationStates.Applied &&
            entry.Metadata["providerMaterialization.providerRouteId"] == "regional-route-orders-to-reporting" &&
            entry.Metadata["materialization.conditionCount"] == "10" &&
            entry.Metadata["materialization.highestConditionSeverity"] == CellTrafficAutomationMaterializationConditionSeverities.Info &&
            entry.Metadata["materialization.ownershipState"] == CellTrafficAutomationOwnershipStates.Owned &&
            entry.Metadata["materialization.dependencyState"] == CellTrafficAutomationDependencyStates.Satisfied &&
            entry.Metadata["materialization.driftState"] == CellTrafficAutomationDriftStates.InSync);
    }

    [Fact]
    public async Task HostedServiceSummarizesOwnershipLifecycleWhenProviderReportsConflict()
    {
        var services = CreateServiceCollection(collection =>
        {
            collection.AddSingleton<ICellTrafficAutomationProviderMaterializer>(
                new ConflictCellTrafficAutomationProviderMaterializer(
                    materializerId: "regional-traffic-materializer",
                    providerId: "regional-traffic-mesh",
                    priority: 100));
        });

        using var provider = services.BuildServiceProvider();
        foreach (var hostedService in provider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        var catalog = provider.GetRequiredService<ICellTrafficAutomationRuntimeCatalog>();

        var automation = catalog.GetByRouteId("orders-to-reporting");
        Assert.NotNull(automation);
        Assert.Equal("regional-traffic-materializer", automation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Failed, automation.ProviderMaterializationState);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Applied, automation.EdgeMaterializationState);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Partial, automation.MaterializationState);
        Assert.Contains("provider:", automation.MaterializationError, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(CellTrafficAutomationOwnershipStates.OwnershipConflict, automation.RuntimeMetadata["providerMaterialization.ownershipState"]);
        Assert.Equal(CellTrafficAutomationDependencyStates.Missing, automation.RuntimeMetadata["providerMaterialization.dependencyState"]);
        Assert.Equal(CellTrafficAutomationDriftStates.Drifted, automation.RuntimeMetadata["providerMaterialization.driftState"]);
        Assert.Equal(CellTrafficAutomationLifecycleActions.Transfer, automation.RuntimeMetadata["providerMaterialization.lifecycleAction"]);
        Assert.Equal(CellTrafficAutomationOwnershipStates.OwnershipConflict, automation.RuntimeMetadata["materialization.ownershipState"]);
        Assert.Equal(CellTrafficAutomationDependencyStates.Missing, automation.RuntimeMetadata["materialization.dependencyState"]);
        Assert.Equal(CellTrafficAutomationDriftStates.Drifted, automation.RuntimeMetadata["materialization.driftState"]);
        Assert.Equal("2", automation.RuntimeMetadata["materialization.lifecycleActionCount"]);
        Assert.Equal("reconcile,transfer", automation.RuntimeMetadata["materialization.lifecycleActions"]);
        Assert.Equal("provider:transfer,edge:reconcile", automation.RuntimeMetadata["materialization.lifecycleActionBreakdown"]);
    }

    [Fact]
    public void BuildFailsWhenMultipleProviderMaterializersMatchSameAutomationAtSamePriority()
    {
        var services = CreateServiceCollection(collection =>
        {
            collection.AddSingleton<ICellTrafficAutomationProviderMaterializer>(
                new TestCellTrafficAutomationProviderMaterializer(
                    materializerId: "regional-traffic-materializer-a",
                    providerId: "regional-traffic-mesh",
                    priority: 100));
            collection.AddSingleton<ICellTrafficAutomationProviderMaterializer>(
                new TestCellTrafficAutomationProviderMaterializer(
                    materializerId: "regional-traffic-materializer-b",
                    providerId: "regional-traffic-mesh",
                    priority: 100));
        });

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ICellTrafficAutomationRuntimeCatalog>());

        Assert.Contains("orders-to-reporting", exception.Message, StringComparison.Ordinal);
        Assert.Contains("regional-traffic-mesh", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSelectsHighestPriorityEdgeMaterializerWhenMultipleMatchAutomation()
    {
        var services = CreateServiceCollection(collection =>
        {
            collection.AddSingleton<ICellTrafficAutomationEdgeMaterializer>(
                new TestEdgeTrafficAutomationMaterializer(
                    materializerId: "priority-edge-materializer",
                    priority: 100));
            collection.AddSingleton<ICellTrafficAutomationEdgeMaterializer>(
                new TestEdgeTrafficAutomationMaterializer(
                    materializerId: "fallback-edge-materializer",
                    priority: 10));
        });

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<ICellTrafficAutomationRuntimeCatalog>();

        var defaultAutomation = catalog.GetByRouteId("orders-to-reporting");
        Assert.NotNull(defaultAutomation);
        Assert.Equal("priority-edge-materializer", defaultAutomation.EdgeMaterializerId);
        Assert.Equal(CellTrafficAutomationMaterializationStates.Pending, defaultAutomation.EdgeMaterializationState);
        Assert.Equal("2", defaultAutomation.RuntimeMetadata["edgeSelection.matchingCandidateCount"]);
        Assert.Equal("priority-edge-materializer,fallback-edge-materializer", defaultAutomation.RuntimeMetadata["edgeSelection.matchingCandidateIds"]);
        Assert.Equal("100", defaultAutomation.RuntimeMetadata["edgeSelection.selectedPriority"]);
    }

    [Fact]
    public void BuildFailsWhenMultipleEdgeMaterializersMatchAutomationAtSamePriority()
    {
        var services = CreateServiceCollection(collection =>
        {
            collection.AddSingleton<ICellTrafficAutomationEdgeMaterializer>(
                new TestEdgeTrafficAutomationMaterializer(
                    materializerId: "edge-materializer-a",
                    priority: 100));
            collection.AddSingleton<ICellTrafficAutomationEdgeMaterializer>(
                new TestEdgeTrafficAutomationMaterializer(
                    materializerId: "edge-materializer-b",
                    priority: 100));
        });

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ICellTrafficAutomationRuntimeCatalog>());

        Assert.Contains("orders-to-reporting", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildFailsWhenTrafficAutomationReferencesUnknownRoute()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "Microservice",
            cells: new CellSettings(
                new CellTrafficAutomationSettings(
                    routes:
                    [
                        new CellTrafficAutomationRouteSettings(
                            routeId: "missing-route",
                            automationMode: "automatic")
                    ]))));
        builder.AddModule(new PlatformTestModule());
        builder.AddModule(new DiscoveryTestModule());
        builder.AddModule(new CellTrafficAutomationCatalogTestModule());
        builder.AddCellBoundary(new CellBoundaryDescriptor(
            id: "platform-control",
            sourceModuleId: "platform",
            displayName: "Platform Control Cell",
            description: "Keeps shared control-plane workflows in one boundary.",
            blastRadius: "shared-control",
            routingStrategy: "local-preferred",
            moduleIds: ["platform"]));
        builder.AddCellHealthIsolation(new CellHealthIsolationDescriptor(
            id: "platform-control-health",
            sourceModuleId: "platform",
            cellId: "platform-control",
            displayName: "Platform Control Health Isolation",
            description: "Contains control-plane failures without leaking them into product cells.",
            failureIsolationMode: "fail-closed",
            readinessScope: "dependency-aware",
            restartScope: "host-coordinated",
            dependencyIds: ["consul-control"]));

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("missing-route", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildFailsWhenTrafficAutomationTargetsEdgeNodesWithoutEdgeTechnology()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "Microservice",
            cells: new CellSettings(
                new CellTrafficAutomationSettings(
                    defaultAutomationMode: null,
                    defaultTriggerMode: null,
                    defaultActionMode: null,
                    defaultMaterializationMode: null,
                    defaultProviderId: null,
                    defaultEdgeNodeIds: ["storefront-edge"],
                    routes:
                    [
                        new CellTrafficAutomationRouteSettings(
                            routeId: "orders-to-reporting",
                            automationMode: "automatic")
                    ]))));
        builder.AddModule(new PlatformTestModule());
        builder.AddModule(new DiscoveryTestModule());
        builder.AddModule(new CellTrafficAutomationCatalogTestModule());
        builder.AddCellBoundary(new CellBoundaryDescriptor(
            id: "platform-control",
            sourceModuleId: "platform",
            displayName: "Platform Control Cell",
            description: "Keeps shared control-plane workflows in one boundary.",
            blastRadius: "shared-control",
            routingStrategy: "local-preferred",
            moduleIds: ["platform"]));
        builder.AddCellHealthIsolation(new CellHealthIsolationDescriptor(
            id: "platform-control-health",
            sourceModuleId: "platform",
            cellId: "platform-control",
            displayName: "Platform Control Health Isolation",
            description: "Contains control-plane failures without leaking them into product cells.",
            failureIsolationMode: "fail-closed",
            readinessScope: "dependency-aware",
            restartScope: "host-coordinated",
            dependencyIds: ["consul-control"]));

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("edge-native-delivery", exception.Message, StringComparison.Ordinal);
    }

    private static ServiceCollection CreateServiceCollection(Action<ServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        configureServices?.Invoke(services);
        services.AddCephalon(ConfigureEngine);
        return services;
    }

    private static void ConfigureEngine(EngineBuilder engine)
    {
        engine.UseSettings(new EngineSettings(
            blueprint: "Microservice",
            technologies: ["EdgeNativeDelivery"],
            cells: new CellSettings(
                new CellTrafficAutomationSettings(
                    defaultAutomationMode: "automatic",
                    defaultTriggerMode: null,
                    defaultActionMode: "shed-load",
                    defaultMaterializationMode: null,
                    defaultProviderId: "regional-traffic-mesh",
                    defaultEdgeNodeIds: ["storefront-edge"],
                    routes:
                    [
                        new CellTrafficAutomationRouteSettings(
                            routeId: "orders-to-platform-control",
                            automationMode: "advisory",
                            triggerMode: "source-health",
                            actionMode: "prefer-local-route",
                            materializationMode: "provider-managed",
                            notes: "Keep provider handoff explicit for control-plane traffic.",
                            metadata: new Dictionary<string, string>
                            {
                                ["handoff"] = "ingress-provider"
                            },
                            providerId: "control-plane-gateway",
                            edgeNodeIds: ["platform-edge"])
                    ]))));
        engine.AddModule(new PlatformTestModule());
        engine.AddModule(new DiscoveryTestModule());
        engine.AddModule(new CellTrafficAutomationCatalogTestModule());
        engine.AddEdge(options =>
        {
            options.Nodes.Add(new EdgeNodeDescriptor(
                id: "storefront-edge",
                displayName: "Storefront Edge",
                description: "Regional node that fronts storefront traffic.",
                tags: ["storefront", "regional"]));
            options.Nodes.Add(new EdgeNodeDescriptor(
                id: "platform-edge",
                displayName: "Platform Edge",
                description: "Control-plane edge that fronts platform traffic.",
                tags: ["platform", "control-plane"]));
        });
        engine.AddCellBoundary(new CellBoundaryDescriptor(
            id: "platform-control",
            sourceModuleId: "platform",
            displayName: "Platform Control Cell",
            description: "Keeps shared control-plane workflows in one boundary.",
            blastRadius: "shared-control",
            routingStrategy: "local-preferred",
            moduleIds: ["platform"]));
        engine.AddCellHealthIsolation(new CellHealthIsolationDescriptor(
            id: "platform-control-health",
            sourceModuleId: "platform",
            cellId: "platform-control",
            displayName: "Platform Control Health Isolation",
            description: "Contains control-plane failures without leaking them into product cells.",
            failureIsolationMode: "fail-closed",
            readinessScope: "dependency-aware",
            restartScope: "host-coordinated",
            dependencyIds: ["consul-control", "postgres-control"]));
    }

    private sealed class TestCellTrafficAutomationProviderMaterializer(
        string materializerId,
        string providerId,
        int priority = 0) : ICellTrafficAutomationProviderMaterializer
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
                ["providerRouteId"] = $"regional-route-{automation.RouteId}",
                ["providerAction"] = "reconciled",
                ["ownershipState"] = CellTrafficAutomationOwnershipStates.Owned,
                ["dependencyState"] = CellTrafficAutomationDependencyStates.Satisfied,
                ["driftState"] = CellTrafficAutomationDriftStates.InSync,
                ["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Reconcile
            };

            return ValueTask.FromResult(new CellTrafficAutomationProviderMaterializationResult(
                state: CellTrafficAutomationProviderMaterializationStates.Applied,
                observedAtUtc: DateTimeOffset.UtcNow,
                metadata: metadata,
                conditions: CreateProviderConditions()));
        }
    }

    private sealed class TestEdgeTrafficAutomationMaterializer(
        string materializerId,
        int priority = 0) : ICellTrafficAutomationEdgeMaterializer
    {
        public string MaterializerId { get; } = materializerId;

        public int Priority { get; } = priority;

        public bool CanMaterialize(CellTrafficAutomationRuntimeDescriptor automation) =>
            automation.EdgeNodeIds.Count > 0;

        public ValueTask<CellTrafficAutomationMaterializationResult> MaterializeAsync(
            CellTrafficAutomationRuntimeDescriptor automation,
            CancellationToken cancellationToken = default)
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["materializedEdgeNodeIds"] = string.Join(",", automation.EdgeNodeIds),
                ["edgeAction"] = "reconciled",
                ["ownershipState"] = CellTrafficAutomationOwnershipStates.Owned,
                ["dependencyState"] = CellTrafficAutomationDependencyStates.Satisfied,
                ["driftState"] = CellTrafficAutomationDriftStates.InSync,
                ["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Reconcile
            };

            return ValueTask.FromResult(new CellTrafficAutomationMaterializationResult(
                state: CellTrafficAutomationMaterializationStates.Applied,
                observedAtUtc: DateTimeOffset.UtcNow,
                metadata: metadata,
                conditions: CreateEdgeConditions()));
        }
    }

    private sealed class ConflictCellTrafficAutomationProviderMaterializer(
        string materializerId,
        string providerId,
        int priority = 0) : ICellTrafficAutomationProviderMaterializer
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
                ["providerRouteId"] = $"regional-route-{automation.RouteId}",
                ["providerAction"] = "ownership-conflict",
                ["ownershipState"] = CellTrafficAutomationOwnershipStates.OwnershipConflict,
                ["dependencyState"] = CellTrafficAutomationDependencyStates.Missing,
                ["driftState"] = CellTrafficAutomationDriftStates.Drifted,
                ["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Transfer
            };

            return ValueTask.FromResult(new CellTrafficAutomationProviderMaterializationResult(
                state: CellTrafficAutomationProviderMaterializationStates.Failed,
                observedAtUtc: DateTimeOffset.UtcNow,
                error: "Provider route is still managed by another controller.",
                metadata: metadata,
                conditions: CreateConflictProviderConditions()));
        }
    }

    private static CellTrafficAutomationMaterializationConditionDescriptor[] CreateProviderConditions()
    {
        return
        [
            new(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Observation,
                "runtime-observable",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "test-runtime",
                description: "The test provider reports live materialization truth."),
            new(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Ownership,
                "ownership",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "owned",
                description: "The test provider owns the materialized route."),
            new(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Dependency,
                "dependencies",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "satisfied",
                description: "The test provider dependency posture is satisfied."),
            new(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Drift,
                "intent-alignment",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "in-sync",
                description: "The test provider matches the authored Cephalon intent."),
            new(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Lifecycle,
                "reconcile-action",
                CellTrafficAutomationMaterializationConditionStates.Pending,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: CellTrafficAutomationLifecycleActions.Reconcile,
                description: "The test provider is reconciling the materialized route.")
        ];
    }

    private static CellTrafficAutomationMaterializationConditionDescriptor[] CreateEdgeConditions()
    {
        return
        [
            new(
                CellTrafficAutomationMaterializationConditionDimensions.Edge,
                CellTrafficAutomationMaterializationConditionCategories.Observation,
                "runtime-observable",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "test-runtime",
                description: "The test edge runtime reports live materialization truth."),
            new(
                CellTrafficAutomationMaterializationConditionDimensions.Edge,
                CellTrafficAutomationMaterializationConditionCategories.Ownership,
                "ownership",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "owned",
                description: "The test edge runtime owns the materialized route."),
            new(
                CellTrafficAutomationMaterializationConditionDimensions.Edge,
                CellTrafficAutomationMaterializationConditionCategories.Dependency,
                "dependencies",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "satisfied",
                description: "The test edge runtime dependency posture is satisfied."),
            new(
                CellTrafficAutomationMaterializationConditionDimensions.Edge,
                CellTrafficAutomationMaterializationConditionCategories.Drift,
                "intent-alignment",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "in-sync",
                description: "The test edge runtime matches the authored Cephalon intent."),
            new(
                CellTrafficAutomationMaterializationConditionDimensions.Edge,
                CellTrafficAutomationMaterializationConditionCategories.Lifecycle,
                "reconcile-action",
                CellTrafficAutomationMaterializationConditionStates.Pending,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: CellTrafficAutomationLifecycleActions.Reconcile,
                description: "The test edge runtime is reconciling the materialized route.")
        ];
    }

    private static CellTrafficAutomationMaterializationConditionDescriptor[] CreateConflictProviderConditions()
    {
        return
        [
            new(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Observation,
                "runtime-observable",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "test-runtime",
                description: "The test provider reports live materialization truth."),
            new(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Ownership,
                "ownership",
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Error,
                reason: "ownership-conflict",
                description: "The test provider reports an ownership conflict."),
            new(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Dependency,
                "dependencies",
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Error,
                reason: "missing",
                description: "The test provider reports missing dependencies."),
            new(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Drift,
                "intent-alignment",
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Warning,
                reason: "drifted",
                description: "The test provider reports drift from authored intent."),
            new(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Lifecycle,
                "reconcile-action",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: CellTrafficAutomationLifecycleActions.Transfer,
                description: "The test provider is transferring ownership.")
        ];
    }

    private sealed class CellTrafficAutomationCatalogTestModule :
        ModuleBase,
        ICellBoundaryContributor,
        ICellRouteContributor,
        ICellHealthIsolationContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "cell-traffic-tests",
            displayName: "Cell Traffic Tests",
            description: "Provides cell topology, health isolation, and route governance for traffic automation tests.",
            dependsOn: [typeof(PlatformTestModule), typeof(DiscoveryTestModule)]);

        public void RegisterCellBoundaries(ICellBoundaryRegistry cells)
        {
            cells.Add(new CellBoundaryDescriptor(
                id: "orders-cell",
                sourceModuleId: "cell-traffic-tests",
                displayName: "Orders Cell",
                description: "Keeps order-serving workflows inside one cell.",
                blastRadius: "regional",
                routingStrategy: "local-first",
                moduleIds: ["cell-traffic-tests", "discovery"]));
            cells.Add(new CellBoundaryDescriptor(
                id: "reporting-cell",
                sourceModuleId: "cell-traffic-tests",
                displayName: "Reporting Cell",
                description: "Keeps reporting workloads away from interactive traffic.",
                blastRadius: "analytics-only",
                routingStrategy: "async-replica"));
        }

        public void RegisterCellRoutes(ICellRouteRegistry routes)
        {
            routes.Add(new CellRouteDescriptor(
                id: "orders-to-reporting",
                sourceModuleId: "cell-traffic-tests",
                sourceCellId: "orders-cell",
                targetCellId: "reporting-cell",
                displayName: "Orders To Reporting",
                description: "Moves order-serving traffic toward reporting projections when analytics paths stay healthy.",
                routingStrategy: "replica-fanout",
                governanceMode: "policy-guarded",
                transportIds: ["rest-api"]));
            routes.Add(new CellRouteDescriptor(
                id: "orders-to-platform-control",
                sourceModuleId: "cell-traffic-tests",
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
                sourceModuleId: "cell-traffic-tests",
                cellId: "orders-cell",
                displayName: "Orders Cell Health Isolation",
                description: "Contains order-serving failures to the orders cell.",
                failureIsolationMode: "cell-quarantine",
                readinessScope: "dependency-aware",
                restartScope: "cell-only",
                dependencyIds: ["orders-db"]));
            healthIsolations.Add(new CellHealthIsolationDescriptor(
                id: "reporting-cell-health",
                sourceModuleId: "cell-traffic-tests",
                cellId: "reporting-cell",
                displayName: "Reporting Cell Health Isolation",
                description: "Lets reporting workloads degrade without leaking into interactive cells.",
                failureIsolationMode: "degraded-serving",
                readinessScope: "best-effort",
                restartScope: "manual",
                dependencyIds: ["reporting-replica"]));
        }
    }
}
