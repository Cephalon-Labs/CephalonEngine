namespace Cephalon.Abstractions.EventSourcing;

/// <summary>
/// Represents an optimistic concurrency failure while appending events to a stream.
/// </summary>
public sealed class EventStreamConcurrencyException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventStreamConcurrencyException" /> class.
    /// </summary>
    /// <param name="streamId">The stream identifier that failed the concurrency check.</param>
    /// <param name="expectedVersion">The version that the caller expected.</param>
    /// <param name="actualVersion">The version that currently exists in the store.</param>
    public EventStreamConcurrencyException(
        string streamId,
        long expectedVersion,
        long actualVersion)
        : base($"Append rejected for stream '{streamId}' because expected version {expectedVersion} did not match actual version {actualVersion}.")
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        StreamId = streamId.Trim();
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    /// <summary>
    /// Gets the stream identifier that failed the concurrency check.
    /// </summary>
    public string StreamId { get; }

    /// <summary>
    /// Gets the version that the caller expected.
    /// </summary>
    public long ExpectedVersion { get; }

    /// <summary>
    /// Gets the version that currently exists in the store.
    /// </summary>
    public long ActualVersion { get; }
}
