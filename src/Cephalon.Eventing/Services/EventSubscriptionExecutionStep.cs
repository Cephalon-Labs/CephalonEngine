namespace Cephalon.Eventing.Services;

/// <summary>
/// Represents the next step in the code-owned event subscription execution pipeline.
/// </summary>
/// <param name="context">The managed subscription execution context for the current attempt.</param>
/// <param name="cancellationToken">The token that cancels the execution attempt.</param>
/// <returns>A task that completes when the next subscription execution step finishes.</returns>
public delegate ValueTask EventSubscriptionExecutionStep(
    EventSubscriptionExecutionContext context,
    CancellationToken cancellationToken = default);
