using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Describes the effective runtime traffic-automation answer for one governed cell route.
/// </summary>
public sealed class CellTrafficAutomationRuntimeDescriptor
{
    /// <summary>
    /// Creates a cell traffic-automation runtime descriptor.
    /// </summary>
    /// <param name="id">The stable traffic-automation identifier.</param>
    /// <param name="routeId">The stable governed cell-route identifier that this automation answer applies to.</param>
    /// <param name="sourceModuleId">The Cephalon module that owns the governed route.</param>
    /// <param name="sourceCellId">The source cell identifier.</param>
    /// <param name="targetCellId">The target cell identifier.</param>
    /// <param name="displayName">The operator-facing traffic-automation name.</param>
    /// <param name="description">The human-readable description of the traffic-automation posture.</param>
    /// <param name="routingStrategy">The operator-facing routing strategy inherited from the governed route.</param>
    /// <param name="governanceMode">The operator-facing governance posture inherited from the governed route.</param>
    /// <param name="automationMode">The normalized automation posture, such as <c>advisory</c> or <c>automatic</c>.</param>
    /// <param name="triggerMode">The normalized trigger posture, such as <c>source-health</c> or <c>source-or-target-health</c>.</param>
    /// <param name="actionMode">The normalized action posture, such as <c>quarantine-route</c> or <c>shed-load</c>.</param>
    /// <param name="materializationMode">The normalized materialization posture, such as <c>runtime-catalog-only</c> or <c>provider-managed</c>.</param>
    /// <param name="policySource">The source of the effective automation policy, such as <c>cell-default</c> or <c>cell-route</c>.</param>
    /// <param name="transportIds">Optional transport identifiers inherited from the governed route.</param>
    /// <param name="requiredCapabilityKey">An optional capability key inherited from the governed route.</param>
    /// <param name="sourceHealthIsolationIds">The normalized health-isolation identifiers attached to the source cell.</param>
    /// <param name="targetHealthIsolationIds">The normalized health-isolation identifiers attached to the target cell.</param>
    /// <param name="dependencyIds">The normalized dependency identifiers observed across the related health-isolation answers.</param>
    /// <param name="metadata">The original authored route metadata.</param>
    /// <param name="runtimeMetadata">Additional runtime-only metadata such as policy notes or overlay provenance.</param>
    public CellTrafficAutomationRuntimeDescriptor(
        string id,
        string routeId,
        string sourceModuleId,
        string sourceCellId,
        string targetCellId,
        string displayName,
        string description,
        string routingStrategy,
        string governanceMode,
        string automationMode,
        string triggerMode,
        string actionMode,
        string materializationMode,
        string policySource,
        IReadOnlyList<string>? transportIds = null,
        string? requiredCapabilityKey = null,
        IReadOnlyList<string>? sourceHealthIsolationIds = null,
        IReadOnlyList<string>? targetHealthIsolationIds = null,
        IReadOnlyList<string>? dependencyIds = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        IReadOnlyDictionary<string, string>? runtimeMetadata = null)
        : this(
            id,
            routeId,
            sourceModuleId,
            sourceCellId,
            targetCellId,
            displayName,
            description,
            routingStrategy,
            governanceMode,
            automationMode,
            triggerMode,
            actionMode,
            materializationMode,
            policySource,
            transportIds,
            requiredCapabilityKey,
            sourceHealthIsolationIds,
            targetHealthIsolationIds,
            dependencyIds,
            metadata,
            runtimeMetadata,
            providerId: null,
            edgeNodeIds: null,
            edgeMaterializerId: null,
            edgeMaterializationState: null,
            edgeMaterializationObservedAtUtc: null,
            edgeMaterializationError: null,
            providerMaterializerId: null,
            providerMaterializationState: null,
            providerMaterializationObservedAtUtc: null,
            providerMaterializationError: null,
            materializationState: null,
            materializationObservedAtUtc: null,
            materializationError: null)
    {
    }

    /// <summary>
    /// Creates a cell traffic-automation runtime descriptor with provider and edge targeting.
    /// </summary>
    /// <param name="id">The stable traffic-automation identifier.</param>
    /// <param name="routeId">The stable governed cell-route identifier that this automation answer applies to.</param>
    /// <param name="sourceModuleId">The Cephalon module that owns the governed route.</param>
    /// <param name="sourceCellId">The source cell identifier.</param>
    /// <param name="targetCellId">The target cell identifier.</param>
    /// <param name="displayName">The operator-facing traffic-automation name.</param>
    /// <param name="description">The human-readable description of the traffic-automation posture.</param>
    /// <param name="routingStrategy">The operator-facing routing strategy inherited from the governed route.</param>
    /// <param name="governanceMode">The operator-facing governance posture inherited from the governed route.</param>
    /// <param name="automationMode">The normalized automation posture, such as <c>advisory</c> or <c>automatic</c>.</param>
    /// <param name="triggerMode">The normalized trigger posture, such as <c>source-health</c> or <c>source-or-target-health</c>.</param>
    /// <param name="actionMode">The normalized action posture, such as <c>quarantine-route</c> or <c>shed-load</c>.</param>
    /// <param name="materializationMode">The normalized materialization posture, such as <c>runtime-catalog-only</c>, <c>provider-managed</c>, or <c>edge-managed</c>.</param>
    /// <param name="policySource">The source of the effective automation policy, such as <c>cell-default</c> or <c>cell-route</c>.</param>
    /// <param name="transportIds">Optional transport identifiers inherited from the governed route.</param>
    /// <param name="requiredCapabilityKey">An optional capability key inherited from the governed route.</param>
    /// <param name="sourceHealthIsolationIds">The normalized health-isolation identifiers attached to the source cell.</param>
    /// <param name="targetHealthIsolationIds">The normalized health-isolation identifiers attached to the target cell.</param>
    /// <param name="dependencyIds">The normalized dependency identifiers observed across the related health-isolation answers.</param>
    /// <param name="metadata">The original authored route metadata.</param>
    /// <param name="runtimeMetadata">Additional runtime-only metadata such as policy notes or overlay provenance.</param>
    /// <param name="providerId">The optional external provider or control-plane identifier that materializes this automation.</param>
    /// <param name="edgeNodeIds">The optional edge-node identifiers associated with this automation answer.</param>
    public CellTrafficAutomationRuntimeDescriptor(
        string id,
        string routeId,
        string sourceModuleId,
        string sourceCellId,
        string targetCellId,
        string displayName,
        string description,
        string routingStrategy,
        string governanceMode,
        string automationMode,
        string triggerMode,
        string actionMode,
        string materializationMode,
        string policySource,
        IReadOnlyList<string>? transportIds,
        string? requiredCapabilityKey,
        IReadOnlyList<string>? sourceHealthIsolationIds,
        IReadOnlyList<string>? targetHealthIsolationIds,
        IReadOnlyList<string>? dependencyIds,
        IReadOnlyDictionary<string, string>? metadata,
        IReadOnlyDictionary<string, string>? runtimeMetadata,
        string? providerId,
        IReadOnlyList<string>? edgeNodeIds = null)
        : this(
            id,
            routeId,
            sourceModuleId,
            sourceCellId,
            targetCellId,
            displayName,
            description,
            routingStrategy,
            governanceMode,
            automationMode,
            triggerMode,
            actionMode,
            materializationMode,
            policySource,
            transportIds,
            requiredCapabilityKey,
            sourceHealthIsolationIds,
            targetHealthIsolationIds,
            dependencyIds,
            metadata,
            runtimeMetadata,
            providerId,
            edgeNodeIds,
            edgeMaterializerId: null,
            edgeMaterializationState: null,
            edgeMaterializationObservedAtUtc: null,
            edgeMaterializationError: null,
            providerMaterializerId: null,
            providerMaterializationState: null,
            providerMaterializationObservedAtUtc: null,
            providerMaterializationError: null,
            materializationState: null,
            materializationObservedAtUtc: null,
            materializationError: null)
    {
    }

    /// <summary>
    /// Creates a cell traffic-automation runtime descriptor with provider, edge, and materialization state.
    /// </summary>
    /// <param name="id">The stable traffic-automation identifier.</param>
    /// <param name="routeId">The stable governed cell-route identifier that this automation answer applies to.</param>
    /// <param name="sourceModuleId">The Cephalon module that owns the governed route.</param>
    /// <param name="sourceCellId">The source cell identifier.</param>
    /// <param name="targetCellId">The target cell identifier.</param>
    /// <param name="displayName">The operator-facing traffic-automation name.</param>
    /// <param name="description">The human-readable description of the traffic-automation posture.</param>
    /// <param name="routingStrategy">The operator-facing routing strategy inherited from the governed route.</param>
    /// <param name="governanceMode">The operator-facing governance posture inherited from the governed route.</param>
    /// <param name="automationMode">The normalized automation posture, such as <c>advisory</c> or <c>automatic</c>.</param>
    /// <param name="triggerMode">The normalized trigger posture, such as <c>source-health</c> or <c>source-or-target-health</c>.</param>
    /// <param name="actionMode">The normalized action posture, such as <c>quarantine-route</c> or <c>shed-load</c>.</param>
    /// <param name="materializationMode">The normalized materialization posture, such as <c>runtime-catalog-only</c>, <c>provider-managed</c>, or <c>edge-managed</c>.</param>
    /// <param name="policySource">The source of the effective automation policy, such as <c>cell-default</c> or <c>cell-route</c>.</param>
    /// <param name="transportIds">Optional transport identifiers inherited from the governed route.</param>
    /// <param name="requiredCapabilityKey">An optional capability key inherited from the governed route.</param>
    /// <param name="sourceHealthIsolationIds">The normalized health-isolation identifiers attached to the source cell.</param>
    /// <param name="targetHealthIsolationIds">The normalized health-isolation identifiers attached to the target cell.</param>
    /// <param name="dependencyIds">The normalized dependency identifiers observed across the related health-isolation answers.</param>
    /// <param name="metadata">The original authored route metadata.</param>
    /// <param name="runtimeMetadata">Additional runtime-only metadata such as policy notes or overlay provenance.</param>
    /// <param name="providerId">The optional external provider or control-plane identifier that materializes this automation.</param>
    /// <param name="edgeNodeIds">The optional edge-node identifiers associated with this automation answer.</param>
    /// <param name="edgeMaterializerId">The optional selected edge materializer identifier.</param>
    /// <param name="edgeMaterializationState">The optional edge-materialization state for this automation.</param>
    /// <param name="edgeMaterializationObservedAtUtc">The optional UTC timestamp when the edge-materialization state was last observed.</param>
    /// <param name="edgeMaterializationError">The optional operator-facing edge-materialization error summary.</param>
    /// <param name="providerMaterializerId">The optional selected provider materializer identifier.</param>
    /// <param name="providerMaterializationState">The optional provider-materialization state for this automation.</param>
    /// <param name="providerMaterializationObservedAtUtc">The optional UTC timestamp when the provider-materialization state was last observed.</param>
    /// <param name="providerMaterializationError">The optional operator-facing provider-materialization error summary.</param>
    /// <param name="materializationState">The optional overall materialization state derived from the selected provider and edge reconciliation posture.</param>
    /// <param name="materializationObservedAtUtc">The optional UTC timestamp when the overall materialization state was last observed.</param>
    /// <param name="materializationError">The optional operator-facing overall materialization error summary.</param>
    [JsonConstructor]
    public CellTrafficAutomationRuntimeDescriptor(
        string id,
        string routeId,
        string sourceModuleId,
        string sourceCellId,
        string targetCellId,
        string displayName,
        string description,
        string routingStrategy,
        string governanceMode,
        string automationMode,
        string triggerMode,
        string actionMode,
        string materializationMode,
        string policySource,
        IReadOnlyList<string>? transportIds,
        string? requiredCapabilityKey,
        IReadOnlyList<string>? sourceHealthIsolationIds,
        IReadOnlyList<string>? targetHealthIsolationIds,
        IReadOnlyList<string>? dependencyIds,
        IReadOnlyDictionary<string, string>? metadata,
        IReadOnlyDictionary<string, string>? runtimeMetadata,
        string? providerId,
        IReadOnlyList<string>? edgeNodeIds,
        string? edgeMaterializerId,
        string? edgeMaterializationState,
        DateTimeOffset? edgeMaterializationObservedAtUtc,
        string? edgeMaterializationError,
        string? providerMaterializerId,
        string? providerMaterializationState,
        DateTimeOffset? providerMaterializationObservedAtUtc,
        string? providerMaterializationError,
        string? materializationState,
        DateTimeOffset? materializationObservedAtUtc,
        string? materializationError)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Traffic automation id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(routeId))
        {
            throw new ArgumentException("Route id is required.", nameof(routeId));
        }

        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            throw new ArgumentException("Source module id is required.", nameof(sourceModuleId));
        }

        if (string.IsNullOrWhiteSpace(sourceCellId))
        {
            throw new ArgumentException("Source cell id is required.", nameof(sourceCellId));
        }

        if (string.IsNullOrWhiteSpace(targetCellId))
        {
            throw new ArgumentException("Target cell id is required.", nameof(targetCellId));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(routingStrategy))
        {
            throw new ArgumentException("Routing strategy is required.", nameof(routingStrategy));
        }

        if (string.IsNullOrWhiteSpace(governanceMode))
        {
            throw new ArgumentException("Governance mode is required.", nameof(governanceMode));
        }

        if (string.IsNullOrWhiteSpace(automationMode))
        {
            throw new ArgumentException("Automation mode is required.", nameof(automationMode));
        }

        if (string.IsNullOrWhiteSpace(triggerMode))
        {
            throw new ArgumentException("Trigger mode is required.", nameof(triggerMode));
        }

        if (string.IsNullOrWhiteSpace(actionMode))
        {
            throw new ArgumentException("Action mode is required.", nameof(actionMode));
        }

        if (string.IsNullOrWhiteSpace(materializationMode))
        {
            throw new ArgumentException("Materialization mode is required.", nameof(materializationMode));
        }

        if (string.IsNullOrWhiteSpace(policySource))
        {
            throw new ArgumentException("Policy source is required.", nameof(policySource));
        }

        Id = id.Trim();
        RouteId = routeId.Trim();
        SourceModuleId = sourceModuleId.Trim();
        SourceCellId = sourceCellId.Trim();
        TargetCellId = targetCellId.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        RoutingStrategy = routingStrategy.Trim();
        GovernanceMode = governanceMode.Trim();
        AutomationMode = automationMode.Trim();
        TriggerMode = triggerMode.Trim();
        ActionMode = actionMode.Trim();
        MaterializationMode = materializationMode.Trim();
        PolicySource = policySource.Trim();
        ProviderId = string.IsNullOrWhiteSpace(providerId)
            ? null
            : providerId.Trim();
        EdgeNodeIds = NormalizeValues(edgeNodeIds);
        EdgeMaterializerId = string.IsNullOrWhiteSpace(edgeMaterializerId)
            ? null
            : edgeMaterializerId.Trim();
        EdgeMaterializationState = string.IsNullOrWhiteSpace(edgeMaterializationState)
            ? null
            : edgeMaterializationState.Trim().ToLowerInvariant();
        EdgeMaterializationObservedAtUtc = edgeMaterializationObservedAtUtc;
        EdgeMaterializationError = string.IsNullOrWhiteSpace(edgeMaterializationError)
            ? null
            : edgeMaterializationError.Trim();
        ProviderMaterializerId = string.IsNullOrWhiteSpace(providerMaterializerId)
            ? null
            : providerMaterializerId.Trim();
        ProviderMaterializationState = string.IsNullOrWhiteSpace(providerMaterializationState)
            ? null
            : providerMaterializationState.Trim().ToLowerInvariant();
        ProviderMaterializationObservedAtUtc = providerMaterializationObservedAtUtc;
        ProviderMaterializationError = string.IsNullOrWhiteSpace(providerMaterializationError)
            ? null
            : providerMaterializationError.Trim();
        MaterializationState = string.IsNullOrWhiteSpace(materializationState)
            ? null
            : materializationState.Trim().ToLowerInvariant();
        MaterializationObservedAtUtc = materializationObservedAtUtc;
        MaterializationError = string.IsNullOrWhiteSpace(materializationError)
            ? null
            : materializationError.Trim();
        TransportIds = NormalizeValues(transportIds);
        RequiredCapabilityKey = string.IsNullOrWhiteSpace(requiredCapabilityKey)
            ? null
            : requiredCapabilityKey.Trim();
        SourceHealthIsolationIds = NormalizeValues(sourceHealthIsolationIds);
        TargetHealthIsolationIds = NormalizeValues(targetHealthIsolationIds);
        DependencyIds = NormalizeValues(dependencyIds);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        RuntimeMetadata = runtimeMetadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(runtimeMetadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable traffic-automation identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the governed cell-route identifier that this automation answer applies to.
    /// </summary>
    public string RouteId { get; }

    /// <summary>
    /// Gets the module that owns the governed route.
    /// </summary>
    public string SourceModuleId { get; }

    /// <summary>
    /// Gets the source cell identifier.
    /// </summary>
    public string SourceCellId { get; }

    /// <summary>
    /// Gets the target cell identifier.
    /// </summary>
    public string TargetCellId { get; }

    /// <summary>
    /// Gets the operator-facing traffic-automation name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the traffic-automation posture.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the routing strategy inherited from the governed route.
    /// </summary>
    public string RoutingStrategy { get; }

    /// <summary>
    /// Gets the governance posture inherited from the governed route.
    /// </summary>
    public string GovernanceMode { get; }

    /// <summary>
    /// Gets the normalized automation posture.
    /// </summary>
    public string AutomationMode { get; }

    /// <summary>
    /// Gets the normalized trigger posture.
    /// </summary>
    public string TriggerMode { get; }

    /// <summary>
    /// Gets the normalized action posture.
    /// </summary>
    public string ActionMode { get; }

    /// <summary>
    /// Gets the normalized materialization posture.
    /// </summary>
    public string MaterializationMode { get; }

    /// <summary>
    /// Gets the optional external provider or control-plane identifier that materializes this automation.
    /// </summary>
    public string? ProviderId { get; }

    /// <summary>
    /// Gets the optional edge-node identifiers associated with this automation answer.
    /// </summary>
    public IReadOnlyList<string> EdgeNodeIds { get; }

    /// <summary>
    /// Gets the optional selected edge materializer identifier.
    /// </summary>
    public string? EdgeMaterializerId { get; }

    /// <summary>
    /// Gets the optional edge-materialization state for this automation.
    /// </summary>
    public string? EdgeMaterializationState { get; }

    /// <summary>
    /// Gets the optional UTC timestamp when the edge-materialization state was last observed.
    /// </summary>
    public DateTimeOffset? EdgeMaterializationObservedAtUtc { get; }

    /// <summary>
    /// Gets the optional operator-facing edge-materialization error summary.
    /// </summary>
    public string? EdgeMaterializationError { get; }

    /// <summary>
    /// Gets the optional selected provider materializer identifier.
    /// </summary>
    public string? ProviderMaterializerId { get; }

    /// <summary>
    /// Gets the optional provider-materialization state for this automation.
    /// </summary>
    public string? ProviderMaterializationState { get; }

    /// <summary>
    /// Gets the optional UTC timestamp when the provider-materialization state was last observed.
    /// </summary>
    public DateTimeOffset? ProviderMaterializationObservedAtUtc { get; }

    /// <summary>
    /// Gets the optional operator-facing provider-materialization error summary.
    /// </summary>
    public string? ProviderMaterializationError { get; }

    /// <summary>
    /// Gets the optional overall materialization state derived from the selected provider and edge reconciliation posture.
    /// </summary>
    public string? MaterializationState { get; }

    /// <summary>
    /// Gets the optional UTC timestamp when the overall materialization state was last observed.
    /// </summary>
    public DateTimeOffset? MaterializationObservedAtUtc { get; }

    /// <summary>
    /// Gets the optional operator-facing overall materialization error summary.
    /// </summary>
    public string? MaterializationError { get; }

    /// <summary>
    /// Gets the source of the effective automation policy.
    /// </summary>
    public string PolicySource { get; }

    /// <summary>
    /// Gets the normalized transport identifiers inherited from the governed route.
    /// </summary>
    public IReadOnlyList<string> TransportIds { get; }

    /// <summary>
    /// Gets the optional capability key inherited from the governed route.
    /// </summary>
    public string? RequiredCapabilityKey { get; }

    /// <summary>
    /// Gets the normalized source-cell health-isolation identifiers.
    /// </summary>
    public IReadOnlyList<string> SourceHealthIsolationIds { get; }

    /// <summary>
    /// Gets the normalized target-cell health-isolation identifiers.
    /// </summary>
    public IReadOnlyList<string> TargetHealthIsolationIds { get; }

    /// <summary>
    /// Gets the normalized dependency identifiers observed across the related health-isolation answers.
    /// </summary>
    public IReadOnlyList<string> DependencyIds { get; }

    /// <summary>
    /// Gets the original authored route metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets runtime-only metadata such as policy notes or overlay provenance.
    /// </summary>
    public IReadOnlyDictionary<string, string> RuntimeMetadata { get; }

    private static string[] NormalizeValues(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
