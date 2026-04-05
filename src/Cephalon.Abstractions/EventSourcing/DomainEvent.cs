namespace Cephalon.Abstractions.EventSourcing;

/// <summary>
/// Provides a minimal record base for immutable domain events.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent" /> record.
    /// </summary>
    /// <param name="streamId">The stable stream identifier that owns the event.</param>
    /// <param name="streamVersion">The optimistic stream version assigned to the event.</param>
    /// <param name="occurredAtUtc">The time at which the event occurred in UTC.</param>
    protected DomainEvent(
        string streamId,
        long streamVersion,
        DateTime occurredAtUtc)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        StreamId = streamId.Trim();
        StreamVersion = streamVersion;
        OccurredAtUtc = occurredAtUtc;
    }

    /// <summary>
    /// Gets the stable stream identifier that owns the event.
    /// </summary>
    public string StreamId { get; init; }

    /// <summary>
    /// Gets the optimistic stream version assigned to the event.
    /// </summary>
    public long StreamVersion { get; init; }

    /// <summary>
    /// Gets the time at which the event occurred in UTC.
    /// </summary>
    public DateTime OccurredAtUtc { get; init; }
}
