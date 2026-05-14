namespace Cephalon.EventSourcing.Services;

/// <summary>
/// Describes one on-demand event-stream replay operation.
/// </summary>
public sealed class EventStreamReplayRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventStreamReplayRequest" /> class.
    /// </summary>
    /// <param name="streamId">The stable event-stream identifier to replay.</param>
    /// <param name="fromVersion">The first stream version to replay when no newer snapshot is available.</param>
    /// <param name="useSnapshots">Whether the replay should start from a saved snapshot when one is available.</param>
    /// <param name="saveSnapshot">Whether the replay should save the final aggregate state as a new snapshot.</param>
    /// <param name="rebuildProjections">Whether registered <c>IProjection&lt;IDomainEvent&gt;</c> services should receive replayed events.</param>
    public EventStreamReplayRequest(
        string streamId,
        long fromVersion = 0,
        bool useSnapshots = true,
        bool saveSnapshot = true,
        bool rebuildProjections = true)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        if (fromVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fromVersion), fromVersion, "Replay start version cannot be negative.");
        }

        StreamId = streamId.Trim();
        FromVersion = fromVersion;
        UseSnapshots = useSnapshots;
        SaveSnapshot = saveSnapshot;
        RebuildProjections = rebuildProjections;
    }

    /// <summary>
    /// Gets the stable event-stream identifier to replay.
    /// </summary>
    public string StreamId { get; }

    /// <summary>
    /// Gets the first stream version to replay when no newer snapshot is available.
    /// </summary>
    public long FromVersion { get; }

    /// <summary>
    /// Gets a value indicating whether the replay should start from a saved snapshot when one is available.
    /// </summary>
    public bool UseSnapshots { get; }

    /// <summary>
    /// Gets a value indicating whether the replay should save the final aggregate state as a new snapshot.
    /// </summary>
    public bool SaveSnapshot { get; }

    /// <summary>
    /// Gets a value indicating whether registered <c>IProjection&lt;IDomainEvent&gt;</c> services should receive replayed events.
    /// </summary>
    public bool RebuildProjections { get; }
}
