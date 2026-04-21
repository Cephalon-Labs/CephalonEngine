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
        Assert.Null(defaultAutomation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Unavailable, defaultAutomation.ProviderMaterializationState);
        Assert.Null(defaultAutomation.ProviderMaterializationObservedAtUtc);
        Assert.Null(defaultAutomation.ProviderMaterializationError);
        Assert.Equal(["orders-cell-health"], defaultAutomation.SourceHealthIsolationIds);
        Assert.Equal(["reporting-cell-health"], defaultAutomation.TargetHealthIsolationIds);
        Assert.Equal(["orders-db", "reporting-replica"], defaultAutomation.DependencyIds);

        var routedAutomation = catalog.GetById("orders-to-platform-control");
        Assert.NotNull(routedAutomation);
        Assert.Equal("advisory", routedAutomation.AutomationMode);
        Assert.Equal("source-health", routedAutomation.TriggerMode);
        Assert.Equal("prefer-local-route", routedAutomation.ActionMode);
        Assert.Equal("provider-managed", routedAutomation.MaterializationMode);
        Assert.Equal("cell-route", routedAutomation.PolicySource);
        Assert.Equal("control-plane-gateway", routedAutomation.ProviderId);
        Assert.Equal(["platform-edge"], routedAutomation.EdgeNodeIds);
        Assert.Null(routedAutomation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Unavailable, routedAutomation.ProviderMaterializationState);
        Assert.Null(routedAutomation.ProviderMaterializationObservedAtUtc);
        Assert.Null(routedAutomation.ProviderMaterializationError);
        Assert.Equal(["orders-cell-health"], routedAutomation.SourceHealthIsolationIds);
        Assert.Equal(["platform-control-health"], routedAutomation.TargetHealthIsolationIds);
        Assert.Equal("Keep provider handoff explicit for control-plane traffic.", routedAutomation.RuntimeMetadata["note"]);
        Assert.Equal("ingress-provider", routedAutomation.RuntimeMetadata["handoff"]);

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
            automation.RouteId == "orders-to-platform-control" &&
            automation.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Unavailable &&
            automation.ProviderId == "control-plane-gateway" &&
            automation.EdgeNodeIds.SequenceEqual(["platform-edge"]));

        var surface = Assert.Single(
            technologyCatalog.GetByTechnology("cell-based-architecture"),
            static candidate => candidate.SurfaceId == "cell-traffic-automations");
        Assert.Contains(surface.Entries, entry =>
            entry.Id == "orders-to-reporting" &&
            entry.Metadata["providerId"] == "regional-traffic-mesh" &&
            entry.Metadata["edgeNodeIds"] == "storefront-edge" &&
            entry.Metadata["providerMaterializationState"] == CellTrafficAutomationProviderMaterializationStates.Unavailable &&
            entry.Metadata["policySource"] == "cell-default" &&
            entry.Metadata["sourceHealthIsolationIds"] == "orders-cell-health" &&
            entry.Metadata["targetHealthIsolationIds"] == "reporting-cell-health");
        Assert.Contains(surface.Entries, entry =>
            entry.Id == "orders-to-platform-control" &&
            entry.Metadata["providerId"] == "control-plane-gateway" &&
            entry.Metadata["edgeNodeIds"] == "platform-edge" &&
            entry.Metadata["providerMaterializationState"] == CellTrafficAutomationProviderMaterializationStates.Unavailable &&
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
                    providerId: "regional-traffic-mesh"));
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
        Assert.Equal("regional-traffic-materializer", defaultAutomation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Applied, defaultAutomation.ProviderMaterializationState);
        Assert.NotNull(defaultAutomation.ProviderMaterializationObservedAtUtc);
        Assert.Null(defaultAutomation.ProviderMaterializationError);
        Assert.Equal("regional-route-orders-to-reporting", defaultAutomation.RuntimeMetadata["providerMaterialization.providerRouteId"]);
        Assert.Equal("reconciled", defaultAutomation.RuntimeMetadata["providerMaterialization.providerAction"]);

        var routedAutomation = catalog.GetByRouteId("orders-to-platform-control");
        Assert.NotNull(routedAutomation);
        Assert.Null(routedAutomation.ProviderMaterializerId);
        Assert.Equal(CellTrafficAutomationProviderMaterializationStates.Unavailable, routedAutomation.ProviderMaterializationState);
        Assert.Null(routedAutomation.ProviderMaterializationObservedAtUtc);

        Assert.Contains(snapshot.CellTrafficAutomations, automation =>
            automation.RouteId == "orders-to-reporting" &&
            automation.ProviderMaterializerId == "regional-traffic-materializer" &&
            automation.ProviderMaterializationState == CellTrafficAutomationProviderMaterializationStates.Applied);

        var surface = Assert.Single(
            technologyCatalog.GetByTechnology("cell-based-architecture"),
            static candidate => candidate.SurfaceId == "cell-traffic-automations");
        Assert.Contains(surface.Entries, entry =>
            entry.Id == "orders-to-reporting" &&
            entry.Metadata["providerMaterializerId"] == "regional-traffic-materializer" &&
            entry.Metadata["providerMaterializationState"] == CellTrafficAutomationProviderMaterializationStates.Applied &&
            entry.Metadata["providerMaterialization.providerRouteId"] == "regional-route-orders-to-reporting");
    }

    [Fact]
    public void BuildFailsWhenMultipleProviderMaterializersClaimSameProvider()
    {
        var services = CreateServiceCollection(collection =>
        {
            collection.AddSingleton<ICellTrafficAutomationProviderMaterializer>(
                new TestCellTrafficAutomationProviderMaterializer(
                    materializerId: "regional-traffic-materializer-a",
                    providerId: "regional-traffic-mesh"));
            collection.AddSingleton<ICellTrafficAutomationProviderMaterializer>(
                new TestCellTrafficAutomationProviderMaterializer(
                    materializerId: "regional-traffic-materializer-b",
                    providerId: "regional-traffic-mesh"));
        });

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ICellTrafficAutomationRuntimeCatalog>());

        Assert.Contains("regional-traffic-mesh", exception.Message, StringComparison.Ordinal);
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
        string providerId) : ICellTrafficAutomationProviderMaterializer
    {
        public string MaterializerId { get; } = materializerId;

        public string ProviderId { get; } = providerId;

        public ValueTask<CellTrafficAutomationProviderMaterializationResult> MaterializeAsync(
            CellTrafficAutomationRuntimeDescriptor automation,
            CancellationToken cancellationToken = default)
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["providerRouteId"] = $"regional-route-{automation.RouteId}",
                ["providerAction"] = "reconciled"
            };

            return ValueTask.FromResult(new CellTrafficAutomationProviderMaterializationResult(
                state: CellTrafficAutomationProviderMaterializationStates.Applied,
                observedAtUtc: DateTimeOffset.UtcNow,
                metadata: metadata));
        }
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
