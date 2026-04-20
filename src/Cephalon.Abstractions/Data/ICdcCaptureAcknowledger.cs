namespace Cephalon.Abstractions.Data;

/// <summary>
/// Allows an active <see cref="ICdcCapture" /> implementation to acknowledge durable progress only
/// after the shared runtime stages the linked outbox publications successfully.
/// </summary>
public interface ICdcCaptureAcknowledger
{
    /// <summary>
    /// Commits or acknowledges provider-facing progress for one staged CDC batch.
    /// </summary>
    /// <param name="acknowledgement">The staged batch that is now safe to acknowledge durably.</param>
    /// <param name="cancellationToken">The token that cancels the acknowledgement operation.</param>
    /// <returns>A task that completes when the provider-facing acknowledgement has finished.</returns>
    ValueTask AcknowledgeAsync(
        CdcCaptureExecutionAcknowledgement acknowledgement,
        CancellationToken cancellationToken = default);
}
