namespace Cephalon.Abstractions.EventSourcing;

/// <summary>
/// Represents one immutable domain event stored in an append-only stream.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the stable stream identifier that owns the event.
    /// </summary>
    string StreamId { get; }

    /// <summary>
    /// Gets the optimistic stream version assigned to the event.
    /// </summary>
    long StreamVersion { get; }

    /// <summary>
    /// Gets the time at which the event occurred in UTC.
    /// </summary>
    DateTime OccurredAtUtc { get; }
}
