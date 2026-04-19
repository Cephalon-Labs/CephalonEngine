namespace Cephalon.Abstractions.Features;

/// <summary>
/// Describes the result of evaluating a feature flag for a specific runtime context.
/// </summary>
/// <param name="FeatureId">The evaluated feature-flag identifier.</param>
/// <param name="IsDefined">Indicates whether the feature flag exists in the active runtime.</param>
/// <param name="IsEnabled">Indicates whether the feature flag resolved to enabled.</param>
/// <param name="Matched">
/// Indicates whether the supplied evaluation context matched the targeting constraints for the
/// feature flag.
/// </param>
/// <param name="Reason">The operator-facing explanation for the evaluation result.</param>
/// <param name="SourceKind">The ownership kind for the resolved feature flag when one exists.</param>
/// <param name="SourceModuleId">The owning module identifier when the feature flag is module-owned.</param>
public sealed record FeatureFlagEvaluationResult(
    string FeatureId,
    bool IsDefined,
    bool IsEnabled,
    bool Matched,
    string Reason,
    FeatureFlagSourceKind? SourceKind = null,
    string? SourceModuleId = null);
