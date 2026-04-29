namespace Cephalon.Abstractions.Data;

/// <summary>
/// Publishes integration events through the active eventing runtime without exposing package-specific implementation types to host adapters.
/// </summary>
public interface IEventPublicationDispatcher
{
    /// <summary>
    /// Publishes one integration event through the active eventing runtime.
    /// </summary>
    /// <param name="request">The publication request to dispatch.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>The operator-facing publication result when the active runtime accepts the event.</returns>
    ValueTask<EventPublicationResult> PublishAsync(EventPublicationRequest request, CancellationToken cancellationToken = default);
}
