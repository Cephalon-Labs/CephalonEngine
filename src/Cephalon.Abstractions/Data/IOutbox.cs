namespace Cephalon.Abstractions.Data;

/// <summary>
/// Stages messages for durable delivery after the current write-side operation completes.
/// </summary>
public interface IOutbox
{
    /// <summary>
    /// Gets the stable outbox identifier owned by this implementation.
    /// </summary>
    string OutboxId { get; }

    /// <summary>
    /// Enqueues one message for later delivery.
    /// </summary>
    /// <param name="message">The message to stage for later delivery.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes when the message has been persisted to the outbox.</returns>
    ValueTask EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
