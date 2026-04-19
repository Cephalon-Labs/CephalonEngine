using Cephalon.Abstractions.Features;

namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Thrown when a behavior declares one or more required feature flags and the active runtime
/// evaluation context does not satisfy one of them.
/// </summary>
public sealed class BehaviorFeatureDisabledException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="BehaviorFeatureDisabledException" />.
    /// </summary>
    /// <param name="behaviorId">The behavior identifier that was blocked.</param>
    /// <param name="featureFlagId">The specific required feature flag that evaluated to disabled.</param>
    /// <param name="requiredFeatureFlagIds">The full ordered set of required feature flags.</param>
    /// <param name="reason">The evaluation reason returned by the feature-toggle runtime.</param>
    /// <param name="sourceKind">The ownership kind of the resolved feature flag when one exists.</param>
    /// <param name="sourceModuleId">The owning module identifier when the resolved feature flag is module-owned.</param>
    public BehaviorFeatureDisabledException(
        string behaviorId,
        string featureFlagId,
        IReadOnlyList<string> requiredFeatureFlagIds,
        string reason,
        FeatureFlagSourceKind? sourceKind = null,
        string? sourceModuleId = null)
        : base(BuildMessage(behaviorId, featureFlagId, reason))
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            throw new ArgumentException("Behavior id is required.", nameof(behaviorId));
        }

        if (string.IsNullOrWhiteSpace(featureFlagId))
        {
            throw new ArgumentException("Feature flag id is required.", nameof(featureFlagId));
        }

        ArgumentNullException.ThrowIfNull(requiredFeatureFlagIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        BehaviorId = behaviorId.Trim();
        FeatureFlagId = featureFlagId.Trim();
        RequiredFeatureFlagIds = requiredFeatureFlagIds
            .Where(static featureFlag => !string.IsNullOrWhiteSpace(featureFlag))
            .Select(static featureFlag => featureFlag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Reason = reason.Trim();
        SourceKind = sourceKind;
        SourceModuleId = string.IsNullOrWhiteSpace(sourceModuleId)
            ? null
            : sourceModuleId.Trim();
    }

    /// <summary>
    /// Gets the behavior identifier that was blocked.
    /// </summary>
    public string BehaviorId { get; }

    /// <summary>
    /// Gets the required feature flag that evaluated to disabled for the active runtime context.
    /// </summary>
    public string FeatureFlagId { get; }

    /// <summary>
    /// Gets the full ordered set of required feature flags declared by the behavior.
    /// </summary>
    public IReadOnlyList<string> RequiredFeatureFlagIds { get; }

    /// <summary>
    /// Gets the operator-facing evaluation reason that explained why the feature flag was not
    /// available for the current runtime context.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets the ownership kind of the resolved feature flag when one exists.
    /// </summary>
    public FeatureFlagSourceKind? SourceKind { get; }

    /// <summary>
    /// Gets the owning module identifier when the resolved feature flag is module-owned.
    /// </summary>
    public string? SourceModuleId { get; }

    private static string BuildMessage(string behaviorId, string featureFlagId, string reason)
    {
        return $"Behavior '{behaviorId}' is not available because required feature flag '{featureFlagId}' is disabled. {reason}";
    }
}
