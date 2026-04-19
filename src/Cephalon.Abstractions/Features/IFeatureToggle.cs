namespace Cephalon.Abstractions.Features;

/// <summary>
/// Evaluates runtime feature flags against an optional evaluation context.
/// </summary>
public interface IFeatureToggle
{
    /// <summary>
    /// Evaluates whether the requested feature flag is enabled for the supplied context.
    /// </summary>
    /// <param name="featureFlagId">The stable feature-flag identifier to evaluate.</param>
    /// <param name="context">The optional runtime context used for targeting evaluation.</param>
    /// <returns><see langword="true" /> when the feature flag resolves to enabled; otherwise <see langword="false" />.</returns>
    bool IsEnabled(string featureFlagId, FeatureFlagEvaluationContext? context = null);

    /// <summary>
    /// Evaluates the requested feature flag and returns a richer operator-facing result.
    /// </summary>
    /// <param name="featureFlagId">The stable feature-flag identifier to evaluate.</param>
    /// <param name="context">The optional runtime context used for targeting evaluation.</param>
    /// <returns>The full evaluation result.</returns>
    FeatureFlagEvaluationResult Evaluate(string featureFlagId, FeatureFlagEvaluationContext? context = null);
}
