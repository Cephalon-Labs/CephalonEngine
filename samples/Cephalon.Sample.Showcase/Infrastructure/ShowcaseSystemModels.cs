namespace Cephalon.Sample.Showcase.Infrastructure;

internal sealed record ShowcaseDocumentationLinks(
    string OpenApiJsonPath,
    string ScalarPath,
    string RuntimeSnapshotPath,
    string RuntimeStoryPath,
    string DiagnosticsPath,
    string ModulesPath,
    string CapabilitiesPath,
    string AuditHistoryPath,
    string DatabaseTopologyPath);

internal sealed record ShowcaseRuntimeSummary(
    string Environment,
    string BlueprintId,
    string BlueprintDisplayName,
    string Status,
    string Readiness,
    string Liveness,
    string WriteProvider,
    string ReadProvider,
    string HistoryProvider,
    int ModuleCount,
    int CapabilityCount,
    int BehaviorCount,
    int TransportCount,
    int TechnologyCount,
    int DependencyCount,
    int PackageCount,
    int DiagnosticsConventionCount,
    int AuthorizationPolicyCount,
    int OutboxCount,
    int EventDispatchRuntimeCount,
    DateTimeOffset? StartedAtUtc,
    int RestartCount,
    string? ActiveWindow,
    DateTimeOffset? ActiveWindowEndsAtUtc);

internal sealed record ShowcaseBusinessSummary(
    int ActiveProducts,
    int Orders,
    int PendingOrders,
    int DeliveredOrders,
    long RevenueInCents,
    int Shipments,
    int DeliveredShipments,
    int ReservedUnits,
    int OpenCarts,
    int CheckedOutCarts,
    DateTimeOffset GeneratedAtUtc);

internal sealed record ShowcaseDependencySummary(
    string Id,
    string DisplayName,
    string State,
    bool Required,
    string Description,
    string Source);

internal sealed record ShowcaseRecentAuditEntry(
    string Id,
    string Category,
    string Action,
    string Summary,
    string Outcome,
    DateTimeOffset OccurredAtUtc,
    string? SubjectId,
    string? CorrelationId);

internal sealed record ShowcaseSystemSummaryResponse(
    ShowcaseRuntimeSummary Runtime,
    ShowcaseBusinessSummary Business,
    ShowcaseDocumentationLinks Documentation,
    IReadOnlyList<ShowcaseDependencySummary> Dependencies,
    IReadOnlyList<ShowcaseRecentAuditEntry> RecentAuditEntries,
    IReadOnlyList<string> SuggestedJourneys);

internal sealed record ShowcaseDatabaseTopologySummary(
    int RoleCount,
    int HealthyRoleCount,
    int MigrationTargetCount,
    int SucceededMigrationTargetCount,
    bool ReadModelSyncEnabled,
    string WriteProvider,
    string ReadProvider,
    string HistoryProvider,
    DateTimeOffset GeneratedAtUtc);

internal sealed record ShowcaseDatabaseTopologyInsight(
    string Id,
    string Tone,
    string Title,
    string Detail,
    string ActionLabel,
    string ActionPath);

internal sealed record ShowcaseDatabaseTopologyRoleRow(
    string Id,
    string RequestedRoleId,
    string ResolvedRoleId,
    string Provider,
    string ResolutionMode,
    string? ConnectionMode,
    string? Schema,
    string? HealthState,
    string? MigrationState,
    IReadOnlyList<string> Consumers,
    IReadOnlyDictionary<string, string> MetadataPreview,
    IReadOnlyDictionary<string, string> RuntimeMetadataPreview);

internal sealed record ShowcaseDatabaseTopologyMigrationRow(
    string Id,
    string RequestedRoleId,
    string ResolvedRoleId,
    string Status,
    string ExecutionMode,
    bool ApplyOnStartup,
    string? Provider,
    string? DbContextType,
    IReadOnlyList<string> Commands,
    IReadOnlyDictionary<string, string> MetadataPreview);

internal sealed record ShowcaseReadModelStoreCounts(
    int Products,
    int Inventory,
    int Orders,
    int Shipments);

internal sealed record ShowcaseProjectionJobSummary(
    int TotalJobs,
    int PendingJobs,
    int FailedJobs,
    int CompletedJobs,
    int DistinctScopes,
    DateTimeOffset? NextAvailableAtUtc,
    DateTimeOffset? LastCompletedAtUtc);

internal sealed record ShowcaseProjectionJobScopeRow(
    string Scope,
    int TotalJobs,
    int PendingJobs,
    int FailedJobs,
    int CompletedJobs,
    int MaxAttemptCount,
    DateTimeOffset? NextAvailableAtUtc,
    DateTimeOffset? LastCompletedAtUtc);

internal sealed record ShowcaseReadModelSyncStatus(
    bool Enabled,
    bool IsLagging,
    ShowcaseReadModelStoreCounts WriteStore,
    ShowcaseReadModelStoreCounts ReadStore,
    int ProductDelta,
    int InventoryDelta,
    int OrderDelta,
    int ShipmentDelta,
    ShowcaseProjectionJobSummary Jobs,
    IReadOnlyList<ShowcaseProjectionJobScopeRow> Scopes);

internal sealed record ShowcaseDatabaseTopologyResponse(
    ShowcaseDatabaseTopologySummary Summary,
    IReadOnlyList<ShowcaseDatabaseTopologyInsight> Insights,
    IReadOnlyList<ShowcaseDatabaseTopologyRoleRow> Roles,
    IReadOnlyList<ShowcaseDatabaseTopologyMigrationRow> Migrations,
    ShowcaseReadModelSyncStatus ReadModelSync);

internal sealed record ShowcaseCatalogProductRow(
    string Id,
    string Name,
    string Category,
    long PriceInCents,
    bool IsActive,
    int QuantityOnHand,
    int QuantityReserved,
    int QuantityAvailable);

internal sealed record ShowcaseCartRow(
    string CartId,
    int ItemCount,
    long TotalInCents,
    bool IsCheckedOut,
    long Version,
    DateTimeOffset? LastOccurredAtUtc);

internal sealed record ShowcaseOrderRow(
    string OrderId,
    string CustomerId,
    string Status,
    int ItemCount,
    long TotalInCents,
    DateTimeOffset PlacedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

internal sealed record ShowcaseInventoryRow(
    string ProductId,
    string ProductName,
    int QuantityOnHand,
    int QuantityReserved,
    int QuantityAvailable,
    string WarehouseCode,
    DateTimeOffset LastUpdatedAtUtc);

internal sealed record ShowcaseShipmentRow(
    string ShipmentId,
    string OrderId,
    string Status,
    string Carrier,
    string? TrackingNumber,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? EstimatedDeliveryUtc,
    DateTimeOffset? DeliveredAtUtc);

internal sealed record ShowcaseBusinessResponse(
    ShowcaseBusinessSummary Summary,
    IReadOnlyList<ShowcaseCatalogProductRow> Products,
    IReadOnlyList<ShowcaseCartRow> Carts,
    IReadOnlyList<ShowcaseOrderRow> Orders,
    IReadOnlyList<ShowcaseInventoryRow> Inventory,
    IReadOnlyList<ShowcaseShipmentRow> Shipments);

internal sealed record ShowcaseFacetMetric(
    string Key,
    int Count,
    string Description);

internal sealed record ShowcaseModuleRow(
    string Id,
    string DisplayName,
    string Version,
    bool IsTrusted,
    int DependencyCount,
    string? PackageId,
    IReadOnlyList<string> Tags);

internal sealed record ShowcaseCapabilityRow(
    string Key,
    string DisplayName,
    string Description,
    string SourceModuleId);

internal sealed record ShowcaseNamedDescriptor(
    string Id,
    string DisplayName,
    string Description);

internal sealed record ShowcaseRuntimeResponse(
    ShowcaseRuntimeSummary Summary,
    ShowcaseDocumentationLinks Documentation,
    IReadOnlyList<ShowcaseFacetMetric> Facets,
    IReadOnlyList<ShowcaseModuleRow> Modules,
    IReadOnlyList<ShowcaseCapabilityRow> Capabilities,
    IReadOnlyList<ShowcaseNamedDescriptor> Patterns,
    IReadOnlyList<ShowcaseNamedDescriptor> Transports,
    IReadOnlyList<ShowcaseDependencySummary> Dependencies);

internal sealed record ShowcaseGovernanceSummary(
    int LoadedPackageCount,
    int TrustedPackageCount,
    int PackagePolicyRequirementCount,
    bool AllowAssemblyPathPackages,
    bool RequireTrustedPackages,
    string DefaultCapabilityAccess,
    int CapabilityAllowedCount,
    int CapabilityBlockedCount,
    int AuthorizationPolicyCount,
    int TechnologySurfaceCount,
    int TechnologyEntryCount,
    int TimelineEventCount);

internal sealed record ShowcasePackagePolicyRule(
    string Key,
    string DisplayName,
    bool Enabled,
    string Description);

internal sealed record ShowcaseTrustSummary(
    bool RequireTrustedPackages,
    string DefaultCapabilityAccess,
    int TrustedAssemblyCount,
    int TrustedPackageAllowListCount,
    int TrustedPublisherCount,
    int TrustedSignerCount,
    int TrustedPublicKeyCount,
    int TrustedCertificateCount,
    int TrustedCertificateAuthorityCount,
    int AllowedChecksumRuleCount,
    int CapabilityOverrideCount);

internal sealed record ShowcasePackageGovernanceRow(
    string PackageId,
    string Kind,
    string AssemblyName,
    string? Version,
    string? PublisherId,
    bool IsTrusted,
    bool IsSignatureVerified,
    string TrustReason,
    string SignatureVerificationReason,
    IReadOnlyList<string> Modules);

internal sealed record ShowcaseAuthorizationPolicyRow(
    string Id,
    string DisplayName,
    string Description,
    IReadOnlyList<string> Modes,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> MetadataPreview);

internal sealed record ShowcaseTechnologyRuntimeEntryRow(
    string Id,
    string DisplayName,
    string Description,
    IReadOnlyDictionary<string, string> MetadataPreview);

internal sealed record ShowcaseTechnologySurfaceRow(
    string TechnologyId,
    string SurfaceId,
    string DisplayName,
    string Description,
    int EntryCount,
    IReadOnlyList<ShowcaseTechnologyRuntimeEntryRow> Entries);

internal sealed record ShowcaseRuntimeStorySummary(
    DateTimeOffset GeneratedAtUtc,
    string Status,
    DateTimeOffset? StartedAtUtc,
    int LoadedPackageCount,
    int ModuleCount,
    int StartedModuleCount,
    int ExecutionGraphCount,
    int ActiveExecutionGraphCount,
    int HostedExecutionCount,
    int ActiveHostedExecutionCount,
    int TimelineEventCount);

internal sealed record ShowcaseRuntimeStoryEventRow(
    DateTimeOffset OccurredAtUtc,
    string Scope,
    string Phase,
    string Outcome,
    string? SubjectId,
    string Message);

internal sealed record ShowcaseGovernanceResponse(
    ShowcaseGovernanceSummary Summary,
    IReadOnlyList<ShowcasePackagePolicyRule> PackagePolicy,
    ShowcaseTrustSummary Trust,
    IReadOnlyList<ShowcaseFacetMetric> CapabilityDecisions,
    IReadOnlyList<ShowcasePackageGovernanceRow> Packages,
    IReadOnlyList<ShowcaseAuthorizationPolicyRow> AuthorizationPolicies,
    IReadOnlyList<ShowcaseTechnologySurfaceRow> TechnologySurfaces,
    ShowcaseRuntimeStorySummary RuntimeStory,
    IReadOnlyList<ShowcaseRuntimeStoryEventRow> RecentTimeline);

internal sealed record ShowcaseTransportCatalogSummary(
    int BehaviorCount,
    int RestOperationCount,
    int TransportCount);

internal sealed record ShowcaseTransportRouteRow(
    string TransportId,
    string Method,
    string Route,
    bool Canonical,
    bool IsBehaviorOwned);

internal sealed record ShowcaseBehaviorTransportRow(
    string BehaviorId,
    string Pattern,
    string? DisplayName,
    string? Description,
    bool InboxEnabled,
    bool OutboxEnabled,
    bool EventSourcingEnabled,
    IReadOnlyList<string> TransportIds,
    IReadOnlyList<ShowcaseTransportRouteRow> Routes);

internal sealed record ShowcaseRestOperationRow(
    string Method,
    string Route,
    string? DisplayName,
    string? ModuleId,
    string? BehaviorId);

internal sealed record ShowcaseTransportCatalogResponse(
    ShowcaseTransportCatalogSummary Summary,
    IReadOnlyList<ShowcaseBehaviorTransportRow> Behaviors,
    IReadOnlyList<ShowcaseRestOperationRow> RestOperations);

internal sealed record ShowcaseActivityEntry(
    long Sequence,
    DateTimeOffset OccurredAtUtc,
    string Area,
    string Transport,
    string Method,
    string Path,
    int StatusCode,
    double DurationMs,
    string Outcome,
    string? Title);

internal sealed record ShowcaseActivityResponse(
    IReadOnlyList<ShowcaseActivityEntry> Entries,
    long TotalRecorded);

internal sealed record ShowcaseResetResponse(
    DateTimeOffset ResetAtUtc,
    int ProductCount,
    int InventoryCount,
    int OrderCount,
    int ShipmentCount,
    int CartStreamCount,
    int AuditEntryCount);
