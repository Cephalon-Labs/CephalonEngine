namespace Cephalon.Abstractions.EventSourcing;

/// <summary>
/// Appends and replays immutable domain events for one logical event store.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends one or more events to the requested stream after checking the expected version.
    /// </summary>
    /// <param name="streamId">The stable stream identifier.</param>
    /// <param name="events">The events to append.</param>
    /// <param name="expectedVersion">
    /// The current stream version expected by the caller. Use <c>-1</c> to require a brand-new stream.
    /// </param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes when the append finishes.</returns>
    Task AppendAsync(
        string streamId,
        IReadOnlyCollection<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the requested stream from the supplied version onward.
    /// </summary>
    /// <param name="streamId">The stable stream identifier.</param>
    /// <param name="fromVersion">The first stream version to include. The default is <c>0</c>.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>An async sequence of domain events in ascending stream-version order.</returns>
    IAsyncEnumerable<IDomainEvent> ReadStreamAsync(
        string streamId,
        long fromVersion = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest version known for the requested stream.
    /// </summary>
    /// <param name="streamId">The stable stream identifier.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>
    /// A task that returns the current stream version, or <c>-1</c> when the stream does not exist.
    /// </returns>
    Task<long> GetVersionAsync(
        string streamId,
        CancellationToken cancellationToken = default);
}
