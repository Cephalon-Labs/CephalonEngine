namespace Cephalon.Abstractions.Data;

/// <summary>
/// Exposes operator-facing runtime state reported by event-publication paths.
/// </summary>
public interface IEventPublicationRuntimeCatalog
{
    /// <summary>
    /// Gets the reported publication-state entries visible to the current runtime.
    /// </summary>
    IReadOnlyList<EventPublicationRuntimeState> States { get; }

    /// <summary>
    /// Gets the latest reported publication state for one publication id.
    /// </summary>
    /// <param name="publicationId">The stable publication identifier to resolve.</param>
    /// <returns>The latest reported state, or <see langword="null" /> when that publication has not reported runtime state.</returns>
    EventPublicationRuntimeState? GetByPublicationId(string publicationId);

    /// <summary>
    /// Gets the reported publication states for one channel identifier.
    /// </summary>
    /// <param name="channelId">The stable channel identifier to resolve.</param>
    /// <returns>The reported states for the channel, ordered by publication identifier.</returns>
    IReadOnlyList<EventPublicationRuntimeState> GetByChannelId(string channelId);

    /// <summary>
    /// Tries to get the latest reported publication state for one publication id.
    /// </summary>
    /// <param name="publicationId">The stable publication identifier to resolve.</param>
    /// <param name="state">Receives the latest reported state when one exists.</param>
    /// <returns><see langword="true" /> when a reported state exists; otherwise, <see langword="false" />.</returns>
    bool TryGet(string publicationId, out EventPublicationRuntimeState? state);
}
