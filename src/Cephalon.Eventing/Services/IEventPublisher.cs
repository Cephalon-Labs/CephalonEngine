namespace Cephalon.Eventing.Services;

/// <summary>
/// Accepts integration events for the active eventing runtime.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes one integration event through the active eventing runtime.
    /// </summary>
    /// <param name="publication">The publication request to handle.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes when the publication has been accepted by the runtime.</returns>
    ValueTask PublishAsync(EventPublication publication, CancellationToken cancellationToken = default);
}
