using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

/// <summary>
/// Describes one reported runtime observation for a CDC capture.
/// </summary>
public sealed class CdcCaptureExecutionReport
{
    /// <summary>
    /// Creates a new CDC capture runtime observation.
    /// </summary>
    /// <param name="cdcCaptureId">The stable CDC capture identifier that produced the observation.</param>
    /// <param name="outcome">
    /// The stable outcome identifier, such as <c>started</c>, <c>captured</c>, <c>idle</c>, or <c>failed</c>.
    /// </param>
    /// <param name="observedAtUtc">The UTC timestamp when the observation occurred.</param>
    /// <param name="reportId">The optional stable report identifier used to make repeated submissions idempotent.</param>
    /// <param name="capturedChangeCount">The number of source changes observed by this report.</param>
    /// <param name="producedMessageCount">The number of outbox messages produced by this report.</param>
    /// <param name="changeId">The latest provider-facing change identifier when available.</param>
    /// <param name="checkpoint">The latest provider-facing checkpoint or cursor when available.</param>
    /// <param name="error">The operator-facing error summary when the observation represents a failure.</param>
    /// <param name="freshness">An optional typed freshness answer reported by the active provider/runtime.</param>
    /// <param name="observationFreshness">An optional report-freshness answer describing whether the observation itself is still current.</param>
    /// <param name="lag">An optional typed lag answer reported by the active provider/runtime.</param>
    /// <param name="publication">An optional typed publication-posture answer reported by the active provider/runtime.</param>
    /// <param name="metadata">Optional operator-facing metadata captured alongside the observation.</param>
    /// <param name="reporterId">The optional stable reporter identity that submitted the observation.</param>
    /// <param name="edgeNodeId">The optional stable edge-node identifier that originated the observation.</param>
    public CdcCaptureExecutionReport(
        string cdcCaptureId,
        string outcome,
        DateTimeOffset observedAtUtc,
        string? reportId = null,
        int capturedChangeCount = 0,
        int producedMessageCount = 0,
        string? changeId = null,
        string? checkpoint = null,
        string? error = null,
        CdcCaptureFreshnessStatus? freshness = null,
        CdcCaptureFreshnessStatus? observationFreshness = null,
        CdcCaptureLagStatus? lag = null,
        CdcCapturePublicationStatus? publication = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        string? reporterId = null,
        string? edgeNodeId = null)
    {
        if (string.IsNullOrWhiteSpace(cdcCaptureId))
        {
            throw new ArgumentException("CDC capture id is required.", nameof(cdcCaptureId));
        }

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        if (capturedChangeCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capturedChangeCount), capturedChangeCount, "Captured change count must be greater than or equal to 0.");
        }

        if (producedMessageCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(producedMessageCount), producedMessageCount, "Produced message count must be greater than or equal to 0.");
        }

        CdcCaptureId = cdcCaptureId.Trim();
        Outcome = outcome.Trim();
        ObservedAtUtc = observedAtUtc;
        ReportId = string.IsNullOrWhiteSpace(reportId) ? null : reportId.Trim();
        CapturedChangeCount = capturedChangeCount;
        ProducedMessageCount = producedMessageCount;
        ChangeId = string.IsNullOrWhiteSpace(changeId) ? null : changeId.Trim();
        Checkpoint = string.IsNullOrWhiteSpace(checkpoint) ? null : checkpoint.Trim();
        Error = string.IsNullOrWhiteSpace(error) ? null : error.Trim();
        Freshness = freshness;
        ObservationFreshness = observationFreshness;
        Lag = lag;
        Publication = publication;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        ReporterId = string.IsNullOrWhiteSpace(reporterId) ? null : reporterId.Trim();
        EdgeNodeId = string.IsNullOrWhiteSpace(edgeNodeId) ? null : edgeNodeId.Trim();
    }

    /// <summary>
    /// Gets the stable CDC capture identifier that produced the observation.
    /// </summary>
    public string CdcCaptureId { get; }

    /// <summary>
    /// Gets the stable outcome identifier for the observed CDC activity.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets the UTC timestamp when the observation occurred.
    /// </summary>
    public DateTimeOffset ObservedAtUtc { get; }

    /// <summary>
    /// Gets the optional stable report identifier used to make repeated submissions idempotent.
    /// </summary>
    public string? ReportId { get; }

    /// <summary>
    /// Gets the number of source changes observed by this report.
    /// </summary>
    public int CapturedChangeCount { get; }

    /// <summary>
    /// Gets the number of outbox messages produced by this report.
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
    /// Gets the operator-facing error summary when the observation represents a failure.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets the typed freshness answer reported by the active provider/runtime when one was supplied.
    /// </summary>
    public CdcCaptureFreshnessStatus? Freshness { get; }

    /// <summary>
    /// Gets the typed report-freshness answer reported for the observation itself when one was supplied.
    /// </summary>
    public CdcCaptureFreshnessStatus? ObservationFreshness { get; }

    /// <summary>
    /// Gets the typed lag answer reported by the active provider/runtime when one was supplied.
    /// </summary>
    public CdcCaptureLagStatus? Lag { get; }

    /// <summary>
    /// Gets the typed publication-posture answer reported by the active provider/runtime when one was supplied.
    /// </summary>
    public CdcCapturePublicationStatus? Publication { get; }

    /// <summary>
    /// Gets optional operator-facing metadata captured alongside the observation.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets the stable reporter identity that submitted the observation when one was supplied.
    /// </summary>
    public string? ReporterId { get; }

    /// <summary>
    /// Gets the stable edge-node identifier that originated the observation when one was supplied.
    /// </summary>
    public string? EdgeNodeId { get; }
}
