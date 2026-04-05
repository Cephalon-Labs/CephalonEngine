namespace Cephalon.Abstractions.Data;

/// <summary>
/// Applies one message, event, or record to a projection target.
/// </summary>
/// <typeparam name="TMessage">The message type consumed by the projection.</typeparam>
public interface IProjection<in TMessage>
{
    /// <summary>
    /// Projects the supplied message into the target read model or data view.
    /// </summary>
    /// <param name="message">The message to project.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes when the projection has finished applying the message.</returns>
    ValueTask ProjectAsync(TMessage message, CancellationToken cancellationToken = default);
}
