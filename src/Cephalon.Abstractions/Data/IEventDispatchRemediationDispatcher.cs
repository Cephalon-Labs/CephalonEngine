namespace Cephalon.Abstractions.Data;

/// <summary>
/// Dispatches bounded remediation commands for staged event-dispatch paths without exposing package-specific implementation types to host adapters.
/// </summary>
public interface IEventDispatchRemediationDispatcher
{
    /// <summary>
    /// Dispatches one event-dispatch remediation command through the active eventing runtime.
    /// </summary>
    /// <param name="request">The remediation request to dispatch.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>The operator-facing command result.</returns>
    ValueTask<EventDispatchRemediationResult> DispatchAsync(EventDispatchRemediationRequest request, CancellationToken cancellationToken = default);
}
