namespace Cephalon.Eventing.Services;

/// <summary>
/// Adds a code-owned middleware step around direct in-process event subscription execution.
/// </summary>
/// <remarks>
/// Register implementations through dependency injection when a host or module needs a
/// type-safe subscription execution pipeline. This contract is deliberately not configuration
/// driven so publish/subscribe hot paths stay code-owned and avoid string-based handler binding.
/// </remarks>
public interface IEventSubscriptionExecutionMiddleware
{
    /// <summary>
    /// Invokes this middleware step and optionally forwards execution to the next step.
    /// </summary>
    /// <param name="context">The managed subscription execution context for the current attempt.</param>
    /// <param name="nextStep">The next middleware or subscription executor in the pipeline.</param>
    /// <param name="cancellationToken">The token that cancels the execution attempt.</param>
    /// <returns>A task that completes when this middleware step finishes.</returns>
    ValueTask InvokeAsync(
        EventSubscriptionExecutionContext context,
        EventSubscriptionExecutionStep nextStep,
        CancellationToken cancellationToken = default);
}
