namespace Cephalon.Abstractions.Data;

/// <summary>
/// Tracks inbound messages so consumer pipelines can enforce idempotent handling.
/// </summary>
public interface IInbox
{
    /// <summary>
    /// Determines whether the requested message identifier has already been recorded as processed.
    /// </summary>
    /// <param name="messageId">The stable inbound message identifier.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns><see langword="true" /> when the message has already been processed; otherwise, <see langword="false" />.</returns>
    ValueTask<bool> HasProcessedAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records one inbound message as processed.
    /// </summary>
    /// <param name="message">The inbound message that completed processing.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes when the inbox has persisted the processed-message record.</returns>
    ValueTask MarkProcessedAsync(InboxMessage message, CancellationToken cancellationToken = default);
}
