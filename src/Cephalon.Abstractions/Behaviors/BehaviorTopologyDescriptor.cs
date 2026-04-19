namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Describes the resolved topology for a single behavior, including its pattern, transports,
/// feature flags, and shared logical API surface.
/// </summary>
public sealed class BehaviorTopologyDescriptor
{
    /// <summary>Initializes a new instance of <see cref="BehaviorTopologyDescriptor"/>.</summary>
    public BehaviorTopologyDescriptor(
        string id,
        string pattern,
        IReadOnlyList<string> transportIds,
        bool inboxEnabled = false,
        bool outboxEnabled = false,
        bool eventSourcingEnabled = false,
        BehaviorApiSurfaceDescriptor? apiSurface = null,
        string? displayName = null,
        string? description = null,
        IReadOnlyList<string>? requiredFeatureFlagIds = null,
        string? sourceModuleId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Behavior id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Behavior pattern is required.", nameof(pattern));
        }

        ArgumentNullException.ThrowIfNull(transportIds);

        Id = id;
        Pattern = pattern;
        TransportIds = transportIds;
        InboxEnabled = inboxEnabled;
        OutboxEnabled = outboxEnabled;
        EventSourcingEnabled = eventSourcingEnabled;
        ApiSurface = apiSurface ?? BehaviorApiSurfaceDescriptor.CreateDefault(id);
        DisplayName = displayName;
        Description = description;
        RequiredFeatureFlagIds = NormalizeRequiredFeatureFlagIds(requiredFeatureFlagIds);
        SourceModuleId = NormalizeOptional(sourceModuleId);
        Metadata = metadata ?? new Dictionary<string, string>();
    }

    /// <summary>Gets the behavior identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the pattern identifier (e.g. "cqrs", "event-driven", "saga-step", "process-manager", "direct").</summary>
    public string Pattern { get; }

    /// <summary>Gets the transport identifiers configured for this behavior.</summary>
    public IReadOnlyList<string> TransportIds { get; }

    /// <summary>Gets a value indicating whether inbox deduplication is enabled.</summary>
    public bool InboxEnabled { get; }

    /// <summary>Gets a value indicating whether outbox staging is enabled.</summary>
    public bool OutboxEnabled { get; }

    /// <summary>Gets a value indicating whether event sourcing is wired into the behavior context.</summary>
    public bool EventSourcingEnabled { get; }

    /// <summary>
    /// Gets the logical public API surface projected by route-shaped transport adapters.
    /// </summary>
    /// <remarks>
    /// When no explicit API surface is supplied, the descriptor derives one from the behavior
    /// identifier so route-shaped transports can project canonical paths without hard-coding the
    /// behavior id into every transport binding.
    /// </remarks>
    public BehaviorApiSurfaceDescriptor ApiSurface { get; }

    /// <summary>Gets the optional display name.</summary>
    public string? DisplayName { get; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the ordered feature-flag identifiers that must resolve to enabled before the behavior
    /// can execute.
    /// </summary>
    public IReadOnlyList<string> RequiredFeatureFlagIds { get; }

    /// <summary>
    /// Gets the module identifier that owns this behavior when ownership is known at runtime.
    /// </summary>
    public string? SourceModuleId { get; }

    /// <summary>Gets additional metadata.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string[] NormalizeRequiredFeatureFlagIds(
        IReadOnlyList<string>? requiredFeatureFlagIds)
    {
        if (requiredFeatureFlagIds is null || requiredFeatureFlagIds.Count == 0)
        {
            return [];
        }

        var normalized = new List<string>(requiredFeatureFlagIds.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var featureFlagId in requiredFeatureFlagIds)
        {
            if (string.IsNullOrWhiteSpace(featureFlagId))
            {
                continue;
            }

            var candidate = featureFlagId.Trim();
            if (seen.Add(candidate))
            {
                normalized.Add(candidate);
            }
        }

        return normalized.Count == 0
            ? []
            : normalized.ToArray();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
