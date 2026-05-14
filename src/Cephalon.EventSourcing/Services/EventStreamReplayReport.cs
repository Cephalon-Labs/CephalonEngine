namespace Cephalon.EventSourcing.Services;

/// <summary>
/// Summarizes one completed or failed event-stream replay operation.
/// </summary>
public sealed class EventStreamReplayReport
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventStreamReplayReport" /> class.
    /// </summary>
    /// <param name="streamId">The event stream that was replayed.</param>
    /// <param name="status">The replay status.</param>
    /// <param name="startedAtUtc">The UTC timestamp when replay started.</param>
    /// <param name="completedAtUtc">The UTC timestamp when replay completed or failed.</param>
    /// <param name="usedSnapshot">Whether replay started from a saved snapshot.</param>
    /// <param name="snapshotVersion">The snapshot version used, or <c>-1</c> when no snapshot was used.</param>
    /// <param name="replayFromVersion">The first event-stream version replayed from the event store.</param>
    /// <param name="replayedEventCount">The number of domain events applied to the aggregate.</param>
    /// <param name="projectionCount">The number of projection services that participated in replay.</param>
    /// <param name="projectedEventCount">The number of projection applications completed during replay.</param>
    /// <param name="lastReplayedVersion">The latest stream version applied, or the snapshot/start version when no events were replayed.</param>
    /// <param name="snapshotSaved">Whether the final aggregate state was saved as a snapshot.</param>
    /// <param name="error">The failure message when replay failed.</param>
    public EventStreamReplayReport(
        string streamId,
        string status,
        DateTimeOffset startedAtUtc,
        DateTimeOffset completedAtUtc,
        bool usedSnapshot,
        long snapshotVersion,
        long replayFromVersion,
        int replayedEventCount,
        int projectionCount,
        int projectedEventCount,
        long lastReplayedVersion,
        bool snapshotSaved,
        string? error = null)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Replay status is required.", nameof(status));
        }

        if (replayFromVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(replayFromVersion), replayFromVersion, "Replay start version cannot be negative.");
        }

        if (replayedEventCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(replayedEventCount), replayedEventCount, "Replayed event count cannot be negative.");
        }

        if (projectionCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(projectionCount), projectionCount, "Projection count cannot be negative.");
        }

        if (projectedEventCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(projectedEventCount), projectedEventCount, "Projected event count cannot be negative.");
        }

        StreamId = streamId.Trim();
        Status = status.Trim();
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
        UsedSnapshot = usedSnapshot;
        SnapshotVersion = snapshotVersion;
        ReplayFromVersion = replayFromVersion;
        ReplayedEventCount = replayedEventCount;
        ProjectionCount = projectionCount;
        ProjectedEventCount = projectedEventCount;
        LastReplayedVersion = lastReplayedVersion;
        SnapshotSaved = snapshotSaved;
        Error = string.IsNullOrWhiteSpace(error) ? null : error.Trim();
    }

    /// <summary>
    /// Gets the event stream that was replayed.
    /// </summary>
    public string StreamId { get; }

    /// <summary>
    /// Gets the replay status.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the UTC timestamp when replay started.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp when replay completed or failed.
    /// </summary>
    public DateTimeOffset CompletedAtUtc { get; }

    /// <summary>
    /// Gets the replay duration in milliseconds.
    /// </summary>
    public double DurationMilliseconds => (CompletedAtUtc - StartedAtUtc).TotalMilliseconds;

    /// <summary>
    /// Gets a value indicating whether replay started from a saved snapshot.
    /// </summary>
    public bool UsedSnapshot { get; }

    /// <summary>
    /// Gets the snapshot version used, or <c>-1</c> when no snapshot was used.
    /// </summary>
    public long SnapshotVersion { get; }

    /// <summary>
    /// Gets the first event-stream version replayed from the event store.
    /// </summary>
    public long ReplayFromVersion { get; }

    /// <summary>
    /// Gets the number of domain events applied to the aggregate.
    /// </summary>
    public int ReplayedEventCount { get; }

    /// <summary>
    /// Gets the number of projection services that participated in replay.
    /// </summary>
    public int ProjectionCount { get; }

    /// <summary>
    /// Gets the number of projection applications completed during replay.
    /// </summary>
    public int ProjectedEventCount { get; }

    /// <summary>
    /// Gets the latest stream version applied, or the snapshot/start version when no events were replayed.
    /// </summary>
    public long LastReplayedVersion { get; }

    /// <summary>
    /// Gets a value indicating whether the final aggregate state was saved as a snapshot.
    /// </summary>
    public bool SnapshotSaved { get; }

    /// <summary>
    /// Gets the failure message when replay failed.
    /// </summary>
    public string? Error { get; }
}
