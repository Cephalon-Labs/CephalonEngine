namespace Cephalon.Abstractions.Features;

/// <summary>
/// Evaluates Cephalon feature-flag provider bindings against external or provider-owned state.
/// </summary>
/// <remarks>
/// Implementations are expected to answer from cached or in-memory provider state rather than
/// performing network I/O on the hot execution path.
/// </remarks>
public interface IFeatureFlagProvider
{
    /// <summary>
    /// Gets the stable provider identifier used by feature-flag bindings.
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Evaluates one provider binding for the supplied Cephalon feature flag and runtime context.
    /// </summary>
    /// <param name="binding">The provider binding attached to the Cephalon feature flag.</param>
    /// <param name="featureFlag">The owning Cephalon feature-flag descriptor.</param>
    /// <param name="context">The optional runtime context used for evaluation.</param>
    /// <returns>The provider evaluation result.</returns>
    FeatureFlagProviderEvaluationResult Evaluate(
        FeatureFlagProviderBindingDescriptor binding,
        FeatureFlagDescriptor featureFlag,
        FeatureFlagEvaluationContext? context = null);
}
