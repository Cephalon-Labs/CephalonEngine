namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one bounded CDC capture batch and the outbox publications it produced.
/// </summary>
public sealed class CdcCaptureExecutionResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CdcCaptureExecutionResult" /> class.
    /// </summary>
    /// <param name="messages">The outbox publications produced by the capture batch.</param>
    /// <param name="capturedChangeCount">
    /// The number of source changes observed by the capture batch. When omitted, Cephalon uses
    /// the produced-message count as the default captured-change answer for the batch.
    /// </param>
    /// <param name="changeId">The latest provider-facing change identifier when one is available.</param>
    /// <param name="checkpoint">The latest provider-facing checkpoint or cursor when one is available.</param>
    /// <param name="freshness">The optional typed freshness answer reported by the capture implementation.</param>
    /// <param name="lag">The optional typed lag answer reported by the capture implementation.</param>
    /// <param name="publication">
    /// The optional typed publication-posture answer reported by the capture implementation before
    /// any linked outbox-dispatch overlay is applied.
    /// </param>
    /// <param name="metadata">Optional operator-facing metadata captured alongside the batch.</param>
    public CdcCaptureExecutionResult(
        IReadOnlyList<OutboxMessage>? messages = null,
        int? capturedChangeCount = null,
        string? changeId = null,
        string? checkpoint = null,
        CdcCaptureFreshnessStatus? freshness = null,
        CdcCaptureLagStatus? lag = null,
        CdcCapturePublicationStatus? publication = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (capturedChangeCount is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capturedChangeCount),
                capturedChangeCount,
                "Captured change count must be greater than or equal to 0.");
        }

        Messages = messages?.ToArray() ?? [];
        ProducedMessageCount = Messages.Count;
        CapturedChangeCount = capturedChangeCount ?? ProducedMessageCount;
        ChangeId = string.IsNullOrWhiteSpace(changeId) ? null : changeId.Trim();
        Checkpoint = string.IsNullOrWhiteSpace(checkpoint) ? null : checkpoint.Trim();
        Freshness = freshness;
        Lag = lag;
        Publication = publication;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the outbox publications produced by the capture batch.
    /// </summary>
    public IReadOnlyList<OutboxMessage> Messages { get; }

    /// <summary>
    /// Gets the number of source changes observed by the capture batch.
    /// </summary>
    public int CapturedChangeCount { get; }

    /// <summary>
    /// Gets the number of outbox publications produced by the capture batch.
    /// </summary>
    public int ProducedMessageCount { get; }

    /// <summary>
    /// Gets the latest provider-facing change identifier when one was reported.
    /// </summary>
    public string? ChangeId { get; }

    /// <summary>
    /// Gets the latest provider-facing checkpoint or cursor when one was reported.
    /// </summary>
    public string? Checkpoint { get; }

    /// <summary>
    /// Gets the typed freshness answer reported by the capture implementation when one was supplied.
    /// </summary>
    public CdcCaptureFreshnessStatus? Freshness { get; }

    /// <summary>
    /// Gets the typed lag answer reported by the capture implementation when one was supplied.
    /// </summary>
    public CdcCaptureLagStatus? Lag { get; }

    /// <summary>
    /// Gets the typed publication-posture answer reported by the capture implementation when one was supplied.
    /// </summary>
    public CdcCapturePublicationStatus? Publication { get; }

    /// <summary>
    /// Gets optional operator-facing metadata captured alongside the batch.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
