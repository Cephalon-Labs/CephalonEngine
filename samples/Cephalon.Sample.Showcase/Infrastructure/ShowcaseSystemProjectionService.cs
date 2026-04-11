using System.Reflection;
using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Capabilities;
using Cephalon.AspNetCore.Documentation;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Engine.Trust;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Sample.Showcase.Infrastructure;

/// <summary>
/// Shapes runtime and business projections into UI-friendly responses for the showcase console.
/// </summary>
internal sealed class ShowcaseSystemProjectionService(
    IConfiguration configuration,
    IHostEnvironment environment,
    IRuntimeIntrospectionSnapshotProvider snapshotProvider,
    RuntimeHealthEvaluator runtimeHealthEvaluator,
    IBehaviorCatalog behaviorCatalog,
    IEnumerable<EndpointDataSource> endpointDataSources,
    ShowcaseActivityFeed activityFeed,
    ShowcaseInMemoryEventStore eventStore,
    PackagePolicy packagePolicy,
    CapabilityPolicyEvaluator capabilityPolicyEvaluator,
    ShowcaseReadDbContext? readDb,
    ShowcaseWriteDbContext? writeDb,
    IAuditHistoryReader? auditHistoryReader)
{
    private readonly ApiRoutesOptions apiRoutes = ApiRoutesOptions.FromConfiguration(configuration);
    private readonly OpenApiEndpointOptions openApiOptions = OpenApiEndpointOptions.FromConfiguration(configuration);
    private readonly string defaultOpenApiDocumentName = ResolveDefaultOpenApiDocumentName(configuration);

    public async Task<ShowcaseSystemSummaryResponse> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var runtimeSnapshot = snapshotProvider.CreateSnapshot();
        var readiness = runtimeHealthEvaluator.EvaluateReadiness();
        var liveness = runtimeHealthEvaluator.EvaluateLiveness();
        var commerce = await LoadCommerceSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var runtimeSummary = BuildRuntimeSummary(runtimeSnapshot, readiness, liveness);
        var businessSummary = BuildBusinessSummary(commerce);
        var dependencies = readiness.Dependencies.Select(ToDependencySummary).ToArray();
        var recentAuditEntries = await LoadRecentAuditEntriesAsync(cancellationToken).ConfigureAwait(false);

        return new ShowcaseSystemSummaryResponse(
            Runtime: runtimeSummary,
            Business: businessSummary,
            Documentation: BuildDocumentationLinks(),
            Dependencies: dependencies,
            RecentAuditEntries: recentAuditEntries,
            SuggestedJourneys: BuildSuggestedJourneys(runtimeSummary, businessSummary));
    }

    public async Task<ShowcaseBusinessResponse> GetBusinessAsync(
        CancellationToken cancellationToken = default)
    {
        var commerce = await LoadCommerceSnapshotAsync(cancellationToken).ConfigureAwait(false);
        return new ShowcaseBusinessResponse(
            Summary: BuildBusinessSummary(commerce),
            Products: commerce.Products,
            Carts: commerce.Carts,
            Orders: commerce.Orders,
            Inventory: commerce.Inventory,
            Shipments: commerce.Shipments);
    }

    public Task<ShowcaseRuntimeResponse> GetRuntimeAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var runtimeSnapshot = snapshotProvider.CreateSnapshot();
        var readiness = runtimeHealthEvaluator.EvaluateReadiness();
        var liveness = runtimeHealthEvaluator.EvaluateLiveness();
        var manifest = runtimeSnapshot.Manifest;

        var response = new ShowcaseRuntimeResponse(
            Summary: BuildRuntimeSummary(runtimeSnapshot, readiness, liveness),
            Documentation: BuildDocumentationLinks(),
            Facets:
            [
                new ShowcaseFacetMetric("Modules", manifest.Modules.Count, "Loaded modules in the runtime manifest."),
                new ShowcaseFacetMetric("Capabilities", manifest.Capabilities.Count, "Published capability contracts."),
                new ShowcaseFacetMetric("Behaviors", behaviorCatalog.All.Count, "Resolved behavior topology descriptors."),
                new ShowcaseFacetMetric("Patterns", manifest.AppProfile.Patterns.Count, "Active architecture and behavior patterns."),
                new ShowcaseFacetMetric("Transports", manifest.AppProfile.Transports.Count, "Selected host transports."),
                new ShowcaseFacetMetric("Technologies", manifest.AppProfile.Technologies.Count, "Enabled technology profiles."),
                new ShowcaseFacetMetric("Execution Graphs", runtimeSnapshot.ExecutionGraphs.Count, "Execution graphs contributed by active modules."),
                new ShowcaseFacetMetric("Hosted Executions", runtimeSnapshot.HostedExecutions.Count, "Background and hosted execution surfaces."),
                new ShowcaseFacetMetric("Projections", runtimeSnapshot.Projections.Count, "Projection surfaces visible to the runtime."),
                new ShowcaseFacetMetric("Outboxes", runtimeSnapshot.Outboxes.Count, "Reliable dispatch outboxes."),
                new ShowcaseFacetMetric("Dispatch Runtimes", runtimeSnapshot.EventDispatchRuntimes.Count, "Event dispatch runtimes."),
                new ShowcaseFacetMetric("Diagnostics", runtimeSnapshot.DiagnosticsConventions.Count, "Published diagnostics conventions.")
            ],
            Modules: manifest.Modules
                .OrderBy(static module => module.Id, StringComparer.OrdinalIgnoreCase)
                .Select(static module => new ShowcaseModuleRow(
                    module.Id,
                    module.DisplayName,
                    module.Version,
                    module.IsTrusted,
                    module.DependsOn.Count,
                    module.PackageId,
                    module.Tags))
                .ToArray(),
            Capabilities: manifest.Capabilities
                .OrderBy(static capability => capability.SourceModuleId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static capability => capability.Key, StringComparer.OrdinalIgnoreCase)
                .Select(static capability => new ShowcaseCapabilityRow(
                    capability.Key,
                    capability.DisplayName,
                    capability.Description,
                    capability.SourceModuleId))
                .ToArray(),
            Patterns: manifest.AppProfile.Patterns
                .Select(static pattern => new ShowcaseNamedDescriptor(
                    pattern.Id,
                    pattern.DisplayName,
                    pattern.Description))
                .ToArray(),
            Transports: manifest.AppProfile.Transports
                .Select(static transport => new ShowcaseNamedDescriptor(
                    transport.Id,
                    transport.DisplayName,
                    transport.Description))
                .ToArray(),
            Dependencies: readiness.Dependencies.Select(ToDependencySummary).ToArray());

        return Task.FromResult(response);
    }

    public Task<ShowcaseGovernanceResponse> GetGovernanceAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var runtimeSnapshot = snapshotProvider.CreateSnapshot();
        var trustSnapshot = capabilityPolicyEvaluator.Snapshot;
        var packagePolicyRules = BuildPackagePolicyRules(packagePolicy);
        var authorizationPolicies = runtimeSnapshot.AuthorizationPolicies
            .OrderBy(static policy => policy.Id, StringComparer.OrdinalIgnoreCase)
            .Select(policy => new ShowcaseAuthorizationPolicyRow(
                policy.Id,
                policy.DisplayName,
                policy.Description,
                policy.Modes.Select(static mode => mode.ToString()).ToArray(),
                policy.Tags,
                CreateMetadataPreview(
                    policy.Metadata,
                    maxEntries: 5,
                    preferredKeys: ["roles", "allowedBehaviors", "sourceModuleId"])))
            .ToArray();
        var technologySurfaces = runtimeSnapshot.TechnologySurfaces
            .OrderBy(static surface => surface.TechnologyId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static surface => surface.SurfaceId, StringComparer.OrdinalIgnoreCase)
            .Select(surface => new ShowcaseTechnologySurfaceRow(
                surface.TechnologyId,
                surface.SurfaceId,
                surface.DisplayName,
                surface.Description,
                surface.Entries.Count,
                surface.Entries
                    .OrderBy(static entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .Select(entry => new ShowcaseTechnologyRuntimeEntryRow(
                        entry.Id,
                        entry.DisplayName,
                        entry.Description,
                        CreateMetadataPreview(
                            entry.Metadata,
                            maxEntries: 6,
                            preferredKeys:
                            [
                                "provider",
                                "connectionMode",
                                "dispatchRuntime",
                                "runtimeState",
                                "selectedAuthorizationModes",
                                "policyIds",
                                "configuredTenantIds",
                                "defaultTenantId",
                                "outboxIds",
                                "topologySource"
                            ])))
                    .ToArray()))
            .ToArray();
        var packages = runtimeSnapshot.Manifest.Packages
            .OrderBy(static package => package.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static package => new ShowcasePackageGovernanceRow(
                package.Id,
                package.Kind,
                package.AssemblyName,
                package.Version,
                package.PublisherId,
                package.IsTrusted,
                package.IsSignatureVerified,
                package.TrustReason,
                package.SignatureVerificationReason,
                package.Modules))
            .ToArray();
        var operationalStory = runtimeSnapshot.OperationalStory;
        var capabilityDecisions = BuildCapabilityDecisionMetrics(trustSnapshot);

        var response = new ShowcaseGovernanceResponse(
            Summary: new ShowcaseGovernanceSummary(
                LoadedPackageCount: runtimeSnapshot.Manifest.Packages.Count,
                TrustedPackageCount: trustSnapshot.Packages.Count(static package => package.IsTrusted),
                PackagePolicyRequirementCount: packagePolicyRules.Count(static rule =>
                    rule.Enabled &&
                    !string.Equals(rule.Key, "allow-assembly-path-packages", StringComparison.OrdinalIgnoreCase)),
                AllowAssemblyPathPackages: packagePolicy.AllowAssemblyPathPackages,
                RequireTrustedPackages: trustSnapshot.Policy.RequireTrustedPackages,
                DefaultCapabilityAccess: trustSnapshot.Policy.DefaultCapabilityAccess.ToString(),
                CapabilityAllowedCount: trustSnapshot.Capabilities.Count(static decision => decision.IsAllowed),
                CapabilityBlockedCount: trustSnapshot.Capabilities.Count(static decision => !decision.IsAllowed),
                AuthorizationPolicyCount: authorizationPolicies.Length,
                TechnologySurfaceCount: technologySurfaces.Length,
                TechnologyEntryCount: technologySurfaces.Sum(static surface => surface.EntryCount),
                TimelineEventCount: operationalStory.Timeline.Count),
            PackagePolicy: packagePolicyRules,
            Trust: new ShowcaseTrustSummary(
                RequireTrustedPackages: trustSnapshot.Policy.RequireTrustedPackages,
                DefaultCapabilityAccess: trustSnapshot.Policy.DefaultCapabilityAccess.ToString(),
                TrustedAssemblyCount: trustSnapshot.Policy.TrustedAssemblies.Count,
                TrustedPackageAllowListCount: trustSnapshot.Policy.TrustedPackages.Count,
                TrustedPublisherCount: trustSnapshot.Policy.TrustedPublishers.Count,
                TrustedSignerCount: trustSnapshot.Policy.TrustedSignerFingerprints.Count,
                TrustedPublicKeyCount: trustSnapshot.Policy.TrustedSignaturePublicKeys.Count,
                TrustedCertificateCount: trustSnapshot.Policy.TrustedSignatureCertificates.Count,
                TrustedCertificateAuthorityCount: trustSnapshot.Policy.TrustedSignatureCertificateAuthorities.Count,
                AllowedChecksumRuleCount: trustSnapshot.Policy.AllowedPackageChecksums.Count,
                CapabilityOverrideCount: trustSnapshot.Policy.Capabilities.Count),
            CapabilityDecisions: capabilityDecisions,
            Packages: packages,
            AuthorizationPolicies: authorizationPolicies,
            TechnologySurfaces: technologySurfaces,
            RuntimeStory: new ShowcaseRuntimeStorySummary(
                GeneratedAtUtc: operationalStory.GeneratedAtUtc,
                Status: operationalStory.Status.Status.ToString(),
                StartedAtUtc: operationalStory.Status.StartedAtUtc,
                LoadedPackageCount: operationalStory.LoadedPackages.Count,
                ModuleCount: operationalStory.Modules.Count,
                StartedModuleCount: operationalStory.Modules.Count(static module => module.IsStarted),
                ExecutionGraphCount: operationalStory.ExecutionGraphs.Count,
                ActiveExecutionGraphCount: operationalStory.ExecutionGraphs.Count(static graph => graph.IsActive),
                HostedExecutionCount: operationalStory.HostedExecutions.Count,
                ActiveHostedExecutionCount: operationalStory.HostedExecutions.Count(static execution => execution.IsActive),
                TimelineEventCount: operationalStory.Timeline.Count),
            RecentTimeline: operationalStory.Timeline
                .OrderByDescending(static item => item.OccurredAtUtc)
                .Take(10)
                .Select(static item => new ShowcaseRuntimeStoryEventRow(
                    item.OccurredAtUtc,
                    item.Scope.ToString(),
                    item.Phase,
                    item.Outcome.ToString(),
                    item.SubjectId,
                    item.Message))
                .ToArray());

        return Task.FromResult(response);
    }

    public Task<ShowcaseTransportCatalogResponse> GetTransportsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var restOperations = ResolveRestOperations();
        var behaviorRestRoutes = restOperations
            .Where(static operation => !string.IsNullOrWhiteSpace(operation.BehaviorId))
            .GroupBy(static operation => operation.BehaviorId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<ShowcaseTransportRouteRow>)group
                    .Select(static operation => new ShowcaseTransportRouteRow(
                        TransportId: "http.rest",
                        Method: operation.Method,
                        Route: operation.Route,
                        Canonical: true,
                        IsBehaviorOwned: true))
                    .OrderBy(static route => route.Method, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static route => route.Route, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var descriptors = behaviorCatalog.All
            .OrderBy(static descriptor => descriptor.Pattern, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static descriptor => descriptor.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var behaviors = descriptors
            .Select(descriptor =>
            {
                var transportIds = new HashSet<string>(descriptor.TransportIds, StringComparer.OrdinalIgnoreCase);
                if (behaviorRestRoutes.ContainsKey(descriptor.Id))
                {
                    transportIds.Add("http.rest");
                }

                var routes = new List<ShowcaseTransportRouteRow>();
                if (behaviorRestRoutes.TryGetValue(descriptor.Id, out var restRoutes))
                {
                    routes.AddRange(restRoutes);
                }

                foreach (var transportId in descriptor.TransportIds)
                {
                    var route = TryResolveTransportRoute(transportId, descriptor);
                    if (route is not null)
                    {
                        routes.Add(route);
                    }
                }

                return new ShowcaseBehaviorTransportRow(
                    BehaviorId: descriptor.Id,
                    Pattern: descriptor.Pattern,
                    DisplayName: descriptor.DisplayName,
                    Description: descriptor.Description,
                    InboxEnabled: descriptor.InboxEnabled,
                    OutboxEnabled: descriptor.OutboxEnabled,
                    EventSourcingEnabled: descriptor.EventSourcingEnabled,
                    TransportIds: transportIds.OrderBy(static item => item, StringComparer.OrdinalIgnoreCase).ToArray(),
                    Routes: routes
                        .DistinctBy(static route => $"{route.TransportId}|{route.Method}|{route.Route}", StringComparer.OrdinalIgnoreCase)
                        .OrderBy(static route => route.TransportId, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(static route => route.Route, StringComparer.OrdinalIgnoreCase)
                        .ToArray());
            })
            .ToArray();

        var response = new ShowcaseTransportCatalogResponse(
            Summary: new ShowcaseTransportCatalogSummary(
                BehaviorCount: behaviors.Length,
                RestOperationCount: restOperations.Length,
                TransportCount: behaviors
                    .SelectMany(static behavior => behavior.TransportIds)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count()),
            Behaviors: behaviors,
            RestOperations: restOperations);

        return Task.FromResult(response);
    }

    public ShowcaseActivityResponse GetActivity(int limit = 50)
    {
        return new ShowcaseActivityResponse(
            Entries: activityFeed.GetRecent(limit),
            TotalRecorded: activityFeed.TotalRecorded);
    }

    private async Task<ShowcaseCommerceSnapshot> LoadCommerceSnapshotAsync(CancellationToken cancellationToken)
    {
        ShowcaseCommerceDbContextBase? db = readDb is not null ? readDb : writeDb;
        if (db is null)
        {
            return new ShowcaseCommerceSnapshot(
                Products: ShowcaseDataStore.Products.Values
                    .OrderBy(static product => product.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(product =>
                    {
                        ShowcaseDataStore.Inventory.TryGetValue(product.Id, out var inventory);
                        return new ShowcaseCatalogProductRow(
                            product.Id,
                            product.Name,
                            product.Category,
                            product.PriceInCents,
                            product.IsActive,
                            inventory?.QuantityOnHand ?? 0,
                            inventory?.QuantityReserved ?? 0,
                            (inventory?.QuantityOnHand ?? 0) - (inventory?.QuantityReserved ?? 0));
                    })
                    .ToArray(),
                Carts: BuildCartRows(),
                Orders: ShowcaseDataStore.Orders.Values
                    .OrderByDescending(static order => order.PlacedAtUtc)
                    .Select(static order => new ShowcaseOrderRow(
                        order.OrderId,
                        order.CustomerId,
                        order.Status.ToString(),
                        order.Items.Count,
                        order.TotalInCents,
                        new DateTimeOffset(order.PlacedAtUtc, TimeSpan.Zero),
                        order.UpdatedAtUtc is { } updatedAtUtc
                            ? new DateTimeOffset(updatedAtUtc, TimeSpan.Zero)
                            : null))
                    .ToArray(),
                Inventory: ShowcaseDataStore.Inventory.Values
                    .OrderBy(static item => item.ProductId, StringComparer.OrdinalIgnoreCase)
                    .Select(item => new ShowcaseInventoryRow(
                        item.ProductId,
                        ShowcaseDataStore.Products.TryGetValue(item.ProductId, out var product) ? product.Name : item.ProductId,
                        item.QuantityOnHand,
                        item.QuantityReserved,
                        item.QuantityAvailable,
                        item.WarehouseCode,
                        new DateTimeOffset(item.LastUpdatedAtUtc, TimeSpan.Zero)))
                    .ToArray(),
                Shipments: ShowcaseDataStore.Shipments.Values
                    .OrderByDescending(static shipment => shipment.CreatedAtUtc)
                    .Select(static shipment => new ShowcaseShipmentRow(
                        shipment.ShipmentId,
                        shipment.OrderId,
                        shipment.Status.ToString(),
                        shipment.Carrier,
                        shipment.TrackingNumber,
                        new DateTimeOffset(shipment.CreatedAtUtc, TimeSpan.Zero),
                        shipment.EstimatedDeliveryUtc is { } estimatedDeliveryUtc
                            ? new DateTimeOffset(estimatedDeliveryUtc, TimeSpan.Zero)
                            : null,
                        shipment.DeliveredAtUtc is { } deliveredAtUtc
                            ? new DateTimeOffset(deliveredAtUtc, TimeSpan.Zero)
                            : null))
                    .ToArray());
        }

        var products = await db.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var inventory = await db.InventoryItems
            .AsNoTracking()
            .OrderBy(item => item.ProductId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var orders = await db.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .OrderByDescending(order => order.PlacedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var shipments = await db.Shipments
            .AsNoTracking()
            .OrderByDescending(shipment => shipment.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var inventoryByProductId = inventory.ToDictionary(item => item.ProductId, StringComparer.OrdinalIgnoreCase);

        return new ShowcaseCommerceSnapshot(
            Products: products
                .Select(product =>
                {
                    inventoryByProductId.TryGetValue(product.Id, out var inventoryItem);
                    return new ShowcaseCatalogProductRow(
                        product.Id,
                        product.Name,
                        product.Category,
                        product.PriceInCents,
                        product.IsActive,
                        inventoryItem?.QuantityOnHand ?? 0,
                        inventoryItem?.QuantityReserved ?? 0,
                        (inventoryItem?.QuantityOnHand ?? 0) - (inventoryItem?.QuantityReserved ?? 0));
                })
                .ToArray(),
            Carts: BuildCartRows(),
            Orders: orders
                .Select(static order => new ShowcaseOrderRow(
                    order.OrderId,
                    order.CustomerId,
                    order.Status,
                    order.Items.Count,
                    order.TotalInCents,
                    new DateTimeOffset(order.PlacedAtUtc, TimeSpan.Zero),
                    order.UpdatedAtUtc is { } updatedAtUtc
                        ? new DateTimeOffset(updatedAtUtc, TimeSpan.Zero)
                        : null))
                .ToArray(),
            Inventory: inventory
                .Select(item => new ShowcaseInventoryRow(
                    item.ProductId,
                    products.FirstOrDefault(product => string.Equals(product.Id, item.ProductId, StringComparison.OrdinalIgnoreCase))?.Name ?? item.ProductId,
                    item.QuantityOnHand,
                    item.QuantityReserved,
                    item.QuantityOnHand - item.QuantityReserved,
                    item.WarehouseCode,
                    new DateTimeOffset(item.LastUpdatedAtUtc, TimeSpan.Zero)))
                .ToArray(),
            Shipments: shipments
                .Select(static shipment => new ShowcaseShipmentRow(
                    shipment.ShipmentId,
                    shipment.OrderId,
                    shipment.Status,
                    shipment.Carrier,
                    shipment.TrackingNumber,
                    new DateTimeOffset(shipment.CreatedAtUtc, TimeSpan.Zero),
                    shipment.EstimatedDeliveryUtc is { } estimatedDeliveryUtc
                        ? new DateTimeOffset(estimatedDeliveryUtc, TimeSpan.Zero)
                        : null,
                    shipment.DeliveredAtUtc is { } deliveredAtUtc
                        ? new DateTimeOffset(deliveredAtUtc, TimeSpan.Zero)
                        : null))
                .ToArray());
    }

    private ShowcaseCartRow[] BuildCartRows()
    {
        var aggregate = new Domain.Cart.Models.ShoppingCartAggregate();

        return eventStore.GetSnapshots()
            .Where(static snapshot => snapshot.StreamId.StartsWith("cart-", StringComparison.OrdinalIgnoreCase))
            .Select(snapshot =>
            {
                var cart = new Domain.Cart.Models.ShoppingCart
                {
                    CartId = snapshot.StreamId["cart-".Length..],
                    CustomerId = snapshot.StreamId
                };

                foreach (var @event in snapshot.Events)
                {
                    cart = aggregate.Apply(cart, @event);
                }

                return new ShowcaseCartRow(
                    CartId: cart.CartId,
                    ItemCount: cart.Items.Count,
                    TotalInCents: cart.TotalInCents,
                    IsCheckedOut: cart.IsCheckedOut,
                    Version: cart.Version,
                    LastOccurredAtUtc: snapshot.LastOccurredAtUtc);
            })
            .OrderByDescending(static cart => cart.LastOccurredAtUtc)
            .ToArray();
    }

    private async Task<IReadOnlyList<ShowcaseRecentAuditEntry>> LoadRecentAuditEntriesAsync(CancellationToken cancellationToken)
    {
        if (auditHistoryReader is null)
        {
            return [];
        }

        var result = await auditHistoryReader.QueryAsync(
            new AuditHistoryQuery(limit: 8),
            cancellationToken).ConfigureAwait(false);

        return result.Entries
            .OrderByDescending(static entry => entry.OccurredAtUtc)
            .Take(8)
            .Select(static entry => new ShowcaseRecentAuditEntry(
                entry.Id,
                entry.Category,
                entry.Action,
                entry.Summary,
                entry.Outcome.ToString(),
                entry.OccurredAtUtc,
                entry.SubjectId,
                entry.CorrelationId))
            .ToArray();
    }

    private ShowcaseRuntimeSummary BuildRuntimeSummary(
        RuntimeIntrospectionSnapshot runtimeSnapshot,
        RuntimeHealthReport readiness,
        RuntimeHealthReport liveness)
    {
        var manifest = runtimeSnapshot.Manifest;
        return new ShowcaseRuntimeSummary(
            Environment: environment.EnvironmentName,
            BlueprintId: manifest.AppProfile.BlueprintId,
            BlueprintDisplayName: manifest.AppProfile.BlueprintDisplayName,
            Status: runtimeSnapshot.Status.Status.ToString(),
            Readiness: readiness.State.ToString(),
            Liveness: liveness.State.ToString(),
            WriteProvider: manifest.AppProfile.Databases.Write.Provider ?? "Unknown",
            ReadProvider: manifest.AppProfile.Databases.Read.Provider ?? "Unknown",
            HistoryProvider: manifest.AppProfile.Databases.History.Provider ?? "Unknown",
            ModuleCount: manifest.Modules.Count,
            CapabilityCount: manifest.Capabilities.Count,
            BehaviorCount: behaviorCatalog.All.Count,
            TransportCount: manifest.AppProfile.Transports.Count,
            TechnologyCount: manifest.AppProfile.Technologies.Count,
            DependencyCount: readiness.Dependencies.Count,
            PackageCount: manifest.Packages.Count,
            DiagnosticsConventionCount: runtimeSnapshot.DiagnosticsConventions.Count,
            AuthorizationPolicyCount: runtimeSnapshot.AuthorizationPolicies.Count,
            OutboxCount: runtimeSnapshot.Outboxes.Count,
            EventDispatchRuntimeCount: runtimeSnapshot.EventDispatchRuntimes.Count,
            StartedAtUtc: runtimeSnapshot.Status.StartedAtUtc,
            RestartCount: runtimeSnapshot.Status.RestartCount,
            ActiveWindow: readiness.ActiveWindow,
            ActiveWindowEndsAtUtc: readiness.ActiveWindowEndsAtUtc);
    }

    private static ShowcaseBusinessSummary BuildBusinessSummary(ShowcaseCommerceSnapshot commerce)
    {
        return new ShowcaseBusinessSummary(
            ActiveProducts: commerce.Products.Count(static product => product.IsActive),
            Orders: commerce.Orders.Count,
            PendingOrders: commerce.Orders.Count(static order => string.Equals(order.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
            DeliveredOrders: commerce.Orders.Count(static order => string.Equals(order.Status, "Delivered", StringComparison.OrdinalIgnoreCase)),
            RevenueInCents: commerce.Orders.Sum(static order => order.TotalInCents),
            Shipments: commerce.Shipments.Count,
            DeliveredShipments: commerce.Shipments.Count(static shipment => string.Equals(shipment.Status, "Delivered", StringComparison.OrdinalIgnoreCase)),
            ReservedUnits: commerce.Inventory.Sum(static item => item.QuantityReserved),
            OpenCarts: commerce.Carts.Count(static cart => !cart.IsCheckedOut),
            CheckedOutCarts: commerce.Carts.Count(static cart => cart.IsCheckedOut),
            GeneratedAtUtc: DateTimeOffset.UtcNow);
    }

    private ShowcaseDocumentationLinks BuildDocumentationLinks()
    {
        var openApiJsonPath = openApiOptions.RoutePattern.Replace(
            "{documentName}",
            defaultOpenApiDocumentName,
            StringComparison.OrdinalIgnoreCase);
        var scalarPath = $"{openApiOptions.ScalarRoutePrefix.TrimEnd('/')}/{defaultOpenApiDocumentName}";

        return new ShowcaseDocumentationLinks(
            OpenApiJsonPath: openApiJsonPath,
            ScalarPath: scalarPath,
            RuntimeSnapshotPath: "/engine/snapshot",
            RuntimeStoryPath: "/engine/runtime-story",
            DiagnosticsPath: "/engine/diagnostics",
            ModulesPath: "/engine/modules",
            CapabilitiesPath: "/engine/capabilities",
            AuditHistoryPath: $"{apiRoutes.RestPrefix}/v1/showcase/audit/history");
    }

    private List<string> BuildSuggestedJourneys(
        ShowcaseRuntimeSummary runtimeSummary,
        ShowcaseBusinessSummary businessSummary)
    {
        var journeys = new List<string>();

        if (!string.Equals(runtimeSummary.Readiness, "Healthy", StringComparison.OrdinalIgnoreCase))
        {
            journeys.Add("Inspect dependency health and runtime story before exercising domain scenarios.");
        }

        if (businessSummary.Orders == 0)
        {
            journeys.Add("Create a cart, place an order, then watch inventory and shipping move through the workflow.");
        }
        else if (businessSummary.PendingOrders > 0)
        {
            journeys.Add("Take a pending order through reserve, ship, and deliver to verify the full multi-module flow.");
        }

        if (activityFeed.TotalRecorded == 0)
        {
            journeys.Add("Open the activity stream, run a scenario, and confirm the server-backed request feed updates in real time.");
        }

        journeys.Add("Use the transport explorer to compare the same behavior topology across REST, GraphQL, JSON-RPC, SSE, and WebSocket surfaces.");
        return journeys;
    }

    private static ShowcasePackagePolicyRule[] BuildPackagePolicyRules(PackagePolicy packagePolicy)
    {
        return
        [
            new ShowcasePackagePolicyRule(
                "allow-assembly-path-packages",
                "Allow Assembly Path Packages",
                packagePolicy.AllowAssemblyPathPackages,
                "Permits raw assembly-path package loading when manifest-driven packaging is not required."),
            new ShowcasePackagePolicyRule(
                "require-version",
                "Require Version",
                packagePolicy.RequireVersion,
                "Requires cephalon.package.json to declare a stable package version."),
            new ShowcasePackagePolicyRule(
                "require-minimum-engine-version",
                "Require Minimum Engine Version",
                packagePolicy.RequireMinimumEngineVersion,
                "Requires packages to declare the minimum compatible Cephalon engine version."),
            new ShowcasePackagePolicyRule(
                "require-maximum-engine-version",
                "Require Maximum Engine Version",
                packagePolicy.RequireMaximumEngineVersion,
                "Requires packages to declare the maximum compatible Cephalon engine version."),
            new ShowcasePackagePolicyRule(
                "require-supported-target-frameworks",
                "Require Supported Target Frameworks",
                packagePolicy.RequireSupportedTargetFrameworks,
                "Requires packages to declare supported target frameworks."),
            new ShowcasePackagePolicyRule(
                "require-publisher-id",
                "Require Publisher Id",
                packagePolicy.RequirePublisherId,
                "Requires packages to declare publisher provenance metadata."),
            new ShowcasePackagePolicyRule(
                "require-signature-fingerprint",
                "Require Signature Fingerprint",
                packagePolicy.RequireSignatureFingerprint,
                "Requires at least one declared signature to include a signer fingerprint."),
            new ShowcasePackagePolicyRule(
                "require-signature-key-id",
                "Require Signature Key Id",
                packagePolicy.RequireSignatureKeyId,
                "Requires at least one declared signature to include a trusted-key identifier."),
            new ShowcasePackagePolicyRule(
                "require-signature-value",
                "Require Signature Value",
                packagePolicy.RequireSignatureValue,
                "Requires at least one declared detached-signature payload."),
            new ShowcasePackagePolicyRule(
                "require-signature-verification",
                "Require Signature Verification",
                packagePolicy.RequireSignatureVerification,
                "Requires package signatures to verify against trusted keys or certificates."),
            new ShowcasePackagePolicyRule(
                "require-integrity-sha256",
                "Require Integrity SHA-256",
                packagePolicy.RequireIntegritySha256,
                "Requires packages to declare integrity.sha256 so the engine can validate assembly integrity.")
        ];
    }

    private static ShowcaseFacetMetric[] BuildCapabilityDecisionMetrics(TrustSnapshot trustSnapshot)
    {
        return
        [
            new ShowcaseFacetMetric(
                "Allowed Now",
                trustSnapshot.Capabilities.Count(static decision => decision.IsAllowed),
                "Capabilities currently permitted by the effective trust policy."),
            new ShowcaseFacetMetric(
                "Blocked Now",
                trustSnapshot.Capabilities.Count(static decision => !decision.IsAllowed),
                "Capabilities currently rejected by the effective trust policy."),
            new ShowcaseFacetMetric(
                "Trusted Only",
                trustSnapshot.Capabilities.Count(static decision => decision.Access == CapabilityAccess.TrustedOnly),
                "Capabilities that require trusted module or package sources."),
            new ShowcaseFacetMetric(
                "Denied",
                trustSnapshot.Capabilities.Count(static decision => decision.Access == CapabilityAccess.Denied),
                "Capabilities explicitly denied by policy."),
            new ShowcaseFacetMetric(
                "Trusted Sources",
                trustSnapshot.Capabilities.Count(static decision => decision.SourceTrusted),
                "Capabilities currently contributed by trusted sources.")
        ];
    }

    private static Dictionary<string, string> CreateMetadataPreview(
        IReadOnlyDictionary<string, string> metadata,
        int maxEntries,
        IReadOnlyList<string> preferredKeys)
    {
        if (metadata.Count == 0 || maxEntries <= 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var selectedKeys = new List<string>(maxEntries);
        foreach (var preferredKey in preferredKeys)
        {
            if (metadata.ContainsKey(preferredKey) &&
                !selectedKeys.Contains(preferredKey, StringComparer.OrdinalIgnoreCase))
            {
                selectedKeys.Add(preferredKey);
            }

            if (selectedKeys.Count == maxEntries)
            {
                break;
            }
        }

        if (selectedKeys.Count < maxEntries)
        {
            foreach (var key in metadata.Keys.OrderBy(static key => key, StringComparer.OrdinalIgnoreCase))
            {
                if (selectedKeys.Contains(key, StringComparer.OrdinalIgnoreCase) ||
                    !IsSafePreviewMetadataKey(key))
                {
                    continue;
                }

                selectedKeys.Add(key);
                if (selectedKeys.Count == maxEntries)
                {
                    break;
                }
            }
        }

        return selectedKeys.ToDictionary(
            static key => key,
            key => metadata[key],
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsSafePreviewMetadataKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var normalized = key.Trim();
        return normalized.StartsWith("pattern.", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("transport.", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("dispatchPolicy.", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("dispatchRuntime.", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("roleRuntime.", StringComparison.OrdinalIgnoreCase) ||
               SafePreviewMetadataKeys.Contains(normalized);
    }

    private ShowcaseRestOperationRow[] ResolveRestOperations()
    {
        var operations = new List<ShowcaseRestOperationRow>();

        foreach (var endpoint in endpointDataSources.SelectMany(static source => source.Endpoints).OfType<RouteEndpoint>())
        {
            var rawPattern = endpoint.RoutePattern.RawText;
            if (string.IsNullOrWhiteSpace(rawPattern))
            {
                continue;
            }

            var route = rawPattern.StartsWith('/')
                ? rawPattern
                : $"/{rawPattern}";
            if (!route.Contains("/showcase/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var httpMethods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["GET"];
            var behaviorMetadata = endpoint.Metadata.FirstOrDefault(static metadata =>
                string.Equals(metadata.GetType().Name, "BehaviorRestEndpointMetadata", StringComparison.Ordinal));
            var groupMetadata = endpoint.Metadata.FirstOrDefault(static metadata =>
                string.Equals(metadata.GetType().Name, "BehaviorRestGroupMetadata", StringComparison.Ordinal));
            var behaviorId = TryReadMetadataProperty(behaviorMetadata, "BehaviorId");
            var moduleId = TryReadMetadataProperty(behaviorMetadata, "ModuleId")
                ?? TryReadMetadataProperty(groupMetadata, "ModuleId");
            var displayName = endpoint.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName
                ?? endpoint.DisplayName;

            foreach (var method in httpMethods)
            {
                operations.Add(new ShowcaseRestOperationRow(
                    Method: method,
                    Route: route,
                    DisplayName: displayName,
                    ModuleId: moduleId,
                    BehaviorId: behaviorId));
            }
        }

        return operations
            .DistinctBy(static operation => $"{operation.Method}|{operation.Route}", StringComparer.OrdinalIgnoreCase)
            .OrderBy(static operation => operation.Route, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static operation => operation.Method, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private ShowcaseTransportRouteRow? TryResolveTransportRoute(
        string transportId,
        BehaviorTopologyDescriptor descriptor)
    {
        return NormalizeBehaviorTransportId(transportId) switch
        {
            "http.jsonrpc" => new ShowcaseTransportRouteRow(
                "http.jsonrpc",
                "POST",
                JoinSegments(apiRoutes.JsonRpcPrefix, defaultOpenApiDocumentName, descriptor.ApiSurface.GroupPath, descriptor.ApiSurface.OperationPath),
                Canonical: true,
                IsBehaviorOwned: true),
            "http.sse" => new ShowcaseTransportRouteRow(
                "http.sse",
                "GET",
                JoinSegments(apiRoutes.SsePrefix, defaultOpenApiDocumentName, descriptor.ApiSurface.GroupPath, descriptor.ApiSurface.OperationPath),
                Canonical: true,
                IsBehaviorOwned: true),
            "http.ws" => new ShowcaseTransportRouteRow(
                "http.ws",
                "WS",
                JoinSegments(apiRoutes.WsPrefix, defaultOpenApiDocumentName, descriptor.ApiSurface.GroupPath, descriptor.ApiSurface.OperationPath),
                Canonical: true,
                IsBehaviorOwned: true),
            "http.graphql" => new ShowcaseTransportRouteRow(
                "http.graphql",
                "POST",
                JoinSegments(apiRoutes.GraphQLPrefix, defaultOpenApiDocumentName, descriptor.ApiSurface.GroupPath, descriptor.ApiSurface.OperationPath),
                Canonical: true,
                IsBehaviorOwned: true),
            "http.graphql-sse" => new ShowcaseTransportRouteRow(
                "http.graphql-sse",
                "POST",
                JoinSegments(apiRoutes.GraphQLSsePrefix, defaultOpenApiDocumentName, descriptor.ApiSurface.GroupPath, descriptor.ApiSurface.OperationPath),
                Canonical: true,
                IsBehaviorOwned: true),
            "http.graphql-ws" => new ShowcaseTransportRouteRow(
                "http.graphql-ws",
                "WS",
                JoinSegments(apiRoutes.GraphQLWsPrefix, defaultOpenApiDocumentName, descriptor.ApiSurface.GroupPath, descriptor.ApiSurface.OperationPath),
                Canonical: true,
                IsBehaviorOwned: true),
            "grpc" => new ShowcaseTransportRouteRow(
                "grpc",
                "gRPC",
                apiRoutes.GrpcPrefix,
                Canonical: false,
                IsBehaviorOwned: false),
            _ => null
        };
    }

    private static ShowcaseDependencySummary ToDependencySummary(
        Cephalon.Abstractions.Health.DependencyHealthReport report)
    {
        return new ShowcaseDependencySummary(
            report.Id,
            report.DisplayName,
            report.State.ToString(),
            report.Required,
            report.Description,
            report.Source);
    }

    private static string NormalizeBehaviorTransportId(string transportId)
    {
        if (string.Equals(transportId, "http.grpc", StringComparison.OrdinalIgnoreCase))
        {
            return "grpc";
        }

        return transportId.Trim();
    }

    private static string JoinSegments(params string[] segments)
    {
        var normalizedSegments = segments
            .Where(static segment => !string.IsNullOrWhiteSpace(segment))
            .Select(static segment => segment.Trim('/'))
            .Where(static segment => segment.Length > 0)
            .ToArray();

        return normalizedSegments.Length == 0
            ? "/"
            : "/" + string.Join("/", normalizedSegments);
    }

    private static string? TryReadMetadataProperty(object? metadata, string propertyName)
    {
        if (metadata is null)
        {
            return null;
        }

        var property = metadata.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return property?.GetValue(metadata) as string;
    }

    private static string ResolveDefaultOpenApiDocumentName(IConfiguration configuration)
    {
        var defaultVersion = NormalizeVersionDocumentName(configuration["OpenApi:DefaultVersion"]);
        if (defaultVersion is not null)
        {
            return defaultVersion;
        }

        var defaultDocument = configuration["OpenApi:DefaultDocument"]?.Trim();
        if (!string.IsNullOrWhiteSpace(defaultDocument))
        {
            return defaultDocument;
        }

        var enabledVersions = configuration.GetSection("OpenApi:EnabledVersions").Get<string[]>()
            ?? configuration.GetSection("OpenApi:EnableVersions").Get<string[]>();
        var firstVersion = enabledVersions?
            .Select(NormalizeVersionDocumentName)
            .FirstOrDefault(static candidate => !string.IsNullOrWhiteSpace(candidate));

        return firstVersion ?? "v1";
    }

    private static string? NormalizeVersionDocumentName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[1..];
        }

        return int.TryParse(normalized, out var major) && major > 0
            ? $"v{major}"
            : null;
    }

    private static readonly HashSet<string> SafePreviewMetadataKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "behaviorCount",
        "policyCount",
        "policyIds",
        "provider",
        "connectionMode",
        "topologySource",
        "dispatchRuntime",
        "dispatchStore",
        "runtimeState",
        "selectedAuthorizationModes",
        "configuredTenantIds",
        "configuredTenantCount",
        "configuredDomainCount",
        "defaultTenantId",
        "outboxIds",
        "adapter",
        "dispatchBridge",
        "dispatchMode",
        "deliveryMode",
        "dispatchBatchSize",
        "channelCount",
        "idempotency",
        "executionMode",
        "configuredTargets",
        "applyOnStartup",
        "resolutionMode",
        "requestedRole",
        "resolvedRole",
        "dbContext",
        "roles",
        "allowedBehaviors",
        "sourceModuleId",
        "authorizationEvaluatorCount",
        "authorizationEvaluatorTypes",
        "defaultEvaluatorType",
        "ambientContextAccessorCount",
        "ambientContextAccessorTypes",
        "executionGraphId",
        "hostedExecutionId"
    };

    private sealed record ShowcaseCommerceSnapshot(
        IReadOnlyList<ShowcaseCatalogProductRow> Products,
        IReadOnlyList<ShowcaseCartRow> Carts,
        IReadOnlyList<ShowcaseOrderRow> Orders,
        IReadOnlyList<ShowcaseInventoryRow> Inventory,
        IReadOnlyList<ShowcaseShipmentRow> Shipments);
}
