namespace Cephalon.Abstractions.Features;

/// <summary>
/// Describes the result of evaluating one feature-flag provider binding.
/// </summary>
/// <param name="ProviderId">The external provider identifier.</param>
/// <param name="ProviderFeatureId">The provider-side feature identifier.</param>
/// <param name="IsDefined">Indicates whether the provider recognizes the requested feature.</param>
/// <param name="IsEnabled">Indicates whether the provider resolved the feature to enabled.</param>
/// <param name="Reason">The operator-facing explanation for the provider evaluation result.</param>
public sealed record FeatureFlagProviderEvaluationResult(
    string ProviderId,
    string ProviderFeatureId,
    bool IsDefined,
    bool IsEnabled,
    string Reason);
