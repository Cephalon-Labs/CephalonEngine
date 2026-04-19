namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Describes the effective runtime migration policy for one strangler-fig route.
/// </summary>
public sealed class StranglerFigMigrationRuntimeDescriptor
{
    /// <summary>
    /// Creates a strangler-fig runtime migration descriptor.
    /// </summary>
    /// <param name="routeId">The stable route identifier.</param>
    /// <param name="sourceModuleId">The Cephalon module that owns the modern boundary for this route.</param>
    /// <param name="displayName">The operator-facing route name.</param>
    /// <param name="description">The human-readable description of the migration boundary.</param>
    /// <param name="pathPrefix">The rooted path prefix that this route matches.</param>
    /// <param name="authoredTarget">The target preferred by the authored route descriptor.</param>
    /// <param name="requestedTarget">The target requested after applying migration-policy overlays.</param>
    /// <param name="effectiveTarget">The target that will actually receive traffic after endpoint fallback is considered.</param>
    /// <param name="requestedTargetSource">The source of the requested target, such as <c>authored-route</c> or <c>migration-route</c>.</param>
    /// <param name="selectionMode">The runtime selection result, such as <c>requested-target</c> or <c>fallback-target</c>.</param>
    /// <param name="selectedEndpoint">The concrete endpoint or boundary identifier that will receive traffic.</param>
    /// <param name="legacyEndpoint">The legacy boundary identifier or endpoint when one is configured.</param>
    /// <param name="modernEndpoint">The modern Cephalon boundary identifier or endpoint when one is configured.</param>
    /// <param name="methods">Optional request methods that this route matches.</param>
    /// <param name="progressState">The normalized migration-progress state for the route.</param>
    /// <param name="progressPercent">The normalized migration-progress percentage for the route.</param>
    /// <param name="metadata">The original authored route metadata.</param>
    /// <param name="runtimeMetadata">Additional runtime-only metadata such as notes or overlay provenance.</param>
    public StranglerFigMigrationRuntimeDescriptor(
        string routeId,
        string sourceModuleId,
        string displayName,
        string description,
        string pathPrefix,
        StranglerFigTarget authoredTarget,
        StranglerFigTarget requestedTarget,
        StranglerFigTarget effectiveTarget,
        string requestedTargetSource,
        string selectionMode,
        string selectedEndpoint,
        string? legacyEndpoint = null,
        string? modernEndpoint = null,
        IReadOnlyList<string>? methods = null,
        string? progressState = null,
        int progressPercent = 0,
        IReadOnlyDictionary<string, string>? metadata = null,
        IReadOnlyDictionary<string, string>? runtimeMetadata = null)
    {
        if (string.IsNullOrWhiteSpace(routeId))
        {
            throw new ArgumentException("Route id is required.", nameof(routeId));
        }

        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            throw new ArgumentException("Source module id is required.", nameof(sourceModuleId));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Route display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Route description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(pathPrefix))
        {
            throw new ArgumentException("Route path prefix is required.", nameof(pathPrefix));
        }

        if (string.IsNullOrWhiteSpace(requestedTargetSource))
        {
            throw new ArgumentException("Requested target source is required.", nameof(requestedTargetSource));
        }

        if (string.IsNullOrWhiteSpace(selectionMode))
        {
            throw new ArgumentException("Selection mode is required.", nameof(selectionMode));
        }

        if (string.IsNullOrWhiteSpace(selectedEndpoint))
        {
            throw new ArgumentException("Selected endpoint is required.", nameof(selectedEndpoint));
        }

        if (progressPercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(progressPercent), progressPercent, "Progress percent must be between 0 and 100.");
        }

        RouteId = routeId.Trim();
        SourceModuleId = sourceModuleId.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        PathPrefix = pathPrefix.Trim();
        AuthoredTarget = authoredTarget;
        RequestedTarget = requestedTarget;
        EffectiveTarget = effectiveTarget;
        RequestedTargetSource = requestedTargetSource.Trim();
        SelectionMode = selectionMode.Trim();
        SelectedEndpoint = selectedEndpoint.Trim();
        LegacyEndpoint = NormalizeEndpoint(legacyEndpoint);
        ModernEndpoint = NormalizeEndpoint(modernEndpoint);
        Methods = NormalizeMethods(methods);
        ProgressState = string.IsNullOrWhiteSpace(progressState)
            ? "not-started"
            : progressState.Trim();
        ProgressPercent = progressPercent;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        RuntimeMetadata = runtimeMetadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(runtimeMetadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable route identifier.
    /// </summary>
    public string RouteId { get; }

    /// <summary>
    /// Gets the module that owns the modern Cephalon boundary for this route.
    /// </summary>
    public string SourceModuleId { get; }

    /// <summary>
    /// Gets the operator-facing route name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the migration boundary.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the rooted path prefix that matches this route.
    /// </summary>
    public string PathPrefix { get; }

    /// <summary>
    /// Gets the target preferred by the authored route descriptor.
    /// </summary>
    public StranglerFigTarget AuthoredTarget { get; }

    /// <summary>
    /// Gets the target requested after applying migration-policy overlays.
    /// </summary>
    public StranglerFigTarget RequestedTarget { get; }

    /// <summary>
    /// Gets the target that will actually receive traffic after endpoint fallback is considered.
    /// </summary>
    public StranglerFigTarget EffectiveTarget { get; }

    /// <summary>
    /// Gets the source of the requested target, such as <c>authored-route</c>, <c>migration-default</c>, or <c>migration-route</c>.
    /// </summary>
    public string RequestedTargetSource { get; }

    /// <summary>
    /// Gets the runtime selection result, such as <c>requested-target</c> or <c>fallback-target</c>.
    /// </summary>
    public string SelectionMode { get; }

    /// <summary>
    /// Gets the concrete endpoint or boundary identifier that will receive traffic.
    /// </summary>
    public string SelectedEndpoint { get; }

    /// <summary>
    /// Gets the legacy boundary identifier or endpoint when one is configured.
    /// </summary>
    public string? LegacyEndpoint { get; }

    /// <summary>
    /// Gets the modern Cephalon boundary identifier or endpoint when one is configured.
    /// </summary>
    public string? ModernEndpoint { get; }

    /// <summary>
    /// Gets the normalized request methods that this route matches.
    /// </summary>
    public IReadOnlyList<string> Methods { get; }

    /// <summary>
    /// Gets the normalized migration-progress state for the route.
    /// </summary>
    public string ProgressState { get; }

    /// <summary>
    /// Gets the normalized migration-progress percentage for the route.
    /// </summary>
    public int ProgressPercent { get; }

    /// <summary>
    /// Gets the original authored route metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets runtime-only metadata such as notes or overlay provenance.
    /// </summary>
    public IReadOnlyDictionary<string, string> RuntimeMetadata { get; }

    private static string? NormalizeEndpoint(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string[] NormalizeMethods(IReadOnlyList<string>? methods)
    {
        return methods?
            .Where(static method => !string.IsNullOrWhiteSpace(method))
            .Select(static method => method.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static method => method, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
