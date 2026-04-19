namespace Cephalon.Abstractions.Features;

/// <summary>
/// Describes one feature flag visible to the active Cephalon runtime.
/// </summary>
public sealed class FeatureFlagDescriptor
{
    /// <summary>
    /// Creates a feature-flag descriptor.
    /// </summary>
    /// <param name="id">The stable feature-flag identifier.</param>
    /// <param name="displayName">The operator-facing feature-flag name.</param>
    /// <param name="description">The human-readable description of the gated behavior.</param>
    /// <param name="enabled">
    /// Indicates whether the feature flag is enabled before any targeting constraints are applied.
    /// </param>
    /// <param name="sourceKind">Identifies whether the feature flag is host-owned or module-owned.</param>
    /// <param name="sourceModuleId">
    /// The module identifier that owns this feature flag when <paramref name="sourceKind" /> is
    /// <see cref="FeatureFlagSourceKind.Module" />.
    /// </param>
    /// <param name="targeting">The optional targeting constraints attached to the feature flag.</param>
    /// <param name="metadata">Optional operator-facing metadata.</param>
    public FeatureFlagDescriptor(
        string id,
        string displayName,
        string description,
        bool enabled = false,
        FeatureFlagSourceKind sourceKind = FeatureFlagSourceKind.Host,
        string? sourceModuleId = null,
        FeatureFlagTargetingDescriptor? targeting = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Feature flag id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Feature flag display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Feature flag description is required.", nameof(description));
        }

        var normalizedSourceModuleId = NormalizeOptional(sourceModuleId);
        if (sourceKind == FeatureFlagSourceKind.Module &&
            string.IsNullOrWhiteSpace(normalizedSourceModuleId))
        {
            throw new ArgumentException(
                "Module-owned feature flags must declare a source module id.",
                nameof(sourceModuleId));
        }

        if (sourceKind == FeatureFlagSourceKind.Host &&
            normalizedSourceModuleId is not null)
        {
            throw new ArgumentException(
                "Host-owned feature flags cannot declare a source module id.",
                nameof(sourceModuleId));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Enabled = enabled;
        SourceKind = sourceKind;
        SourceModuleId = normalizedSourceModuleId;
        Targeting = targeting ?? FeatureFlagTargetingDescriptor.Empty;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable feature-flag identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing feature-flag name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the gated behavior.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets a value indicating whether the feature flag is enabled before targeting is applied.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Gets the ownership kind for this feature flag.
    /// </summary>
    public FeatureFlagSourceKind SourceKind { get; }

    /// <summary>
    /// Gets the owning module identifier when the feature flag is module-owned.
    /// </summary>
    public string? SourceModuleId { get; }

    /// <summary>
    /// Gets the optional targeting constraints attached to the feature flag.
    /// </summary>
    public FeatureFlagTargetingDescriptor Targeting { get; }

    /// <summary>
    /// Gets operator-facing metadata for the feature flag.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
