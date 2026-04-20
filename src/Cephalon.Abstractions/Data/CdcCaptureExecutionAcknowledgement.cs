namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one CDC batch that the shared runtime has already staged through the linked outbox
/// and is now safe to acknowledge durably.
/// </summary>
public sealed class CdcCaptureExecutionAcknowledgement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CdcCaptureExecutionAcknowledgement" /> class.
    /// </summary>
    /// <param name="cdcCaptureId">The stable CDC capture identifier that owns the staged batch.</param>
    /// <param name="outboxId">The stable outbox identifier that already accepted the staged publications.</param>
    /// <param name="messages">The outbox publications that the shared runtime staged successfully.</param>
    /// <param name="capturedChangeCount">
    /// The number of source changes observed by the staged batch. When omitted, Cephalon uses the
    /// staged-message count as the default captured-change answer.
    /// </param>
    /// <param name="changeId">The latest provider-facing change identifier when one is available.</param>
    /// <param name="checkpoint">The latest provider-facing checkpoint or cursor when one is available.</param>
    /// <param name="metadata">Optional operator-facing metadata captured alongside the staged batch.</param>
    public CdcCaptureExecutionAcknowledgement(
        string cdcCaptureId,
        string outboxId,
        IReadOnlyList<OutboxMessage>? messages = null,
        int? capturedChangeCount = null,
        string? changeId = null,
        string? checkpoint = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(cdcCaptureId))
        {
            throw new ArgumentException("CDC capture id is required.", nameof(cdcCaptureId));
        }

        if (string.IsNullOrWhiteSpace(outboxId))
        {
            throw new ArgumentException("Outbox id is required.", nameof(outboxId));
        }

        if (capturedChangeCount is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capturedChangeCount),
                capturedChangeCount,
                "Captured change count must be greater than or equal to 0.");
        }

        CdcCaptureId = cdcCaptureId.Trim();
        OutboxId = outboxId.Trim();
        Messages = messages?.ToArray() ?? [];
        StagedMessageCount = Messages.Count;
        CapturedChangeCount = capturedChangeCount ?? StagedMessageCount;
        ChangeId = string.IsNullOrWhiteSpace(changeId) ? null : changeId.Trim();
        Checkpoint = string.IsNullOrWhiteSpace(checkpoint) ? null : checkpoint.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable CDC capture identifier that owns the staged batch.
    /// </summary>
    public string CdcCaptureId { get; }

    /// <summary>
    /// Gets the stable outbox identifier that already accepted the staged publications.
    /// </summary>
    public string OutboxId { get; }

    /// <summary>
    /// Gets the outbox publications that the shared runtime staged successfully.
    /// </summary>
    public IReadOnlyList<OutboxMessage> Messages { get; }

    /// <summary>
    /// Gets the number of source changes observed by the staged batch.
    /// </summary>
    public int CapturedChangeCount { get; }

    /// <summary>
    /// Gets the number of publications that the shared runtime staged successfully.
    /// </summary>
    public int StagedMessageCount { get; }

    /// <summary>
    /// Gets the latest provider-facing change identifier when one was reported.
    /// </summary>
    public string? ChangeId { get; }

    /// <summary>
    /// Gets the latest provider-facing checkpoint or cursor when one was reported.
    /// </summary>
    public string? Checkpoint { get; }

    /// <summary>
    /// Gets optional operator-facing metadata captured alongside the staged batch.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
