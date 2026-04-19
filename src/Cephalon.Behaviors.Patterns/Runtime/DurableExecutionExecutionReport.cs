namespace Cephalon.Behaviors.Patterns.Runtime;

/// <summary>
/// Describes one reported durable-execution observation for an active stream.
/// </summary>
internal sealed class DurableExecutionExecutionReport
{
    public DurableExecutionExecutionReport(
        string behaviorId,
        string streamId,
        string outcome,
        string stage,
        DateTimeOffset observedAtUtc,
        long? replayedVersion = null,
        long? knownVersion = null,
        int? httpStatusCode = null,
        int appendedEventCount = 0,
        bool producedOutput = false,
        bool isCompleted = false,
        string? error = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            throw new ArgumentException("Behavior id is required.", nameof(behaviorId));
        }

        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        if (string.IsNullOrWhiteSpace(stage))
        {
            throw new ArgumentException("Stage is required.", nameof(stage));
        }

        if (appendedEventCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(appendedEventCount),
                appendedEventCount,
                "Appended event count must be greater than or equal to 0.");
        }

        if (httpStatusCode is < 100 or > 999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(httpStatusCode),
                httpStatusCode,
                "HTTP status codes must stay in the HTTP status-code range.");
        }

        BehaviorId = behaviorId.Trim();
        StreamId = streamId.Trim();
        Outcome = outcome.Trim();
        Stage = stage.Trim();
        ObservedAtUtc = observedAtUtc;
        ReplayedVersion = replayedVersion;
        KnownVersion = knownVersion;
        HttpStatusCode = httpStatusCode;
        AppendedEventCount = appendedEventCount;
        ProducedOutput = producedOutput;
        IsCompleted = isCompleted;
        Error = string.IsNullOrWhiteSpace(error) ? null : error.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    public string BehaviorId { get; }

    public string StreamId { get; }

    public string Outcome { get; }

    public string Stage { get; }

    public DateTimeOffset ObservedAtUtc { get; }

    public long? ReplayedVersion { get; }

    public long? KnownVersion { get; }

    public int? HttpStatusCode { get; }

    public int AppendedEventCount { get; }

    public bool ProducedOutput { get; }

    public bool IsCompleted { get; }

    public string? Error { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }
}
