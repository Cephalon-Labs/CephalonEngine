namespace Cephalon.Abstractions.Features;

/// <summary>
/// Supplies contextual information for evaluating a feature flag at runtime.
/// </summary>
public sealed class FeatureFlagEvaluationContext
{
    /// <summary>
    /// Gets an empty feature-flag evaluation context.
    /// </summary>
    public static FeatureFlagEvaluationContext Empty { get; } = new();

    /// <summary>
    /// Creates a feature-flag evaluation context.
    /// </summary>
    /// <param name="environmentName">The active hosting environment name.</param>
    /// <param name="moduleId">The current module identifier when one is known.</param>
    /// <param name="behaviorId">The current behavior identifier when one is known.</param>
    /// <param name="capabilityKey">The current capability key when one is known.</param>
    /// <param name="transportId">The active transport identifier when one is known.</param>
    /// <param name="tenantId">The current tenant identifier when one is known.</param>
    /// <param name="subjectId">The current subject identifier when one is known.</param>
    /// <param name="tags">The descriptive tags associated with the current request or workload.</param>
    /// <param name="metadata">Additional evaluation metadata.</param>
    public FeatureFlagEvaluationContext(
        string? environmentName = null,
        string? moduleId = null,
        string? behaviorId = null,
        string? capabilityKey = null,
        string? transportId = null,
        string? tenantId = null,
        string? subjectId = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        EnvironmentName = NormalizeOptional(environmentName);
        ModuleId = NormalizeOptional(moduleId);
        BehaviorId = NormalizeOptional(behaviorId);
        CapabilityKey = NormalizeOptional(capabilityKey);
        TransportId = NormalizeOptional(transportId);
        TenantId = NormalizeOptional(tenantId);
        SubjectId = NormalizeOptional(subjectId);
        Tags = NormalizeList(tags);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the active hosting environment name.
    /// </summary>
    public string? EnvironmentName { get; }

    /// <summary>
    /// Gets the current module identifier when one is known.
    /// </summary>
    public string? ModuleId { get; }

    /// <summary>
    /// Gets the current behavior identifier when one is known.
    /// </summary>
    public string? BehaviorId { get; }

    /// <summary>
    /// Gets the current capability key when one is known.
    /// </summary>
    public string? CapabilityKey { get; }

    /// <summary>
    /// Gets the active transport identifier when one is known.
    /// </summary>
    public string? TransportId { get; }

    /// <summary>
    /// Gets the current tenant identifier when one is known.
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// Gets the current subject identifier when one is known.
    /// </summary>
    public string? SubjectId { get; }

    /// <summary>
    /// Gets the descriptive tags associated with the current request or workload.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets additional evaluation metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string[] NormalizeList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
