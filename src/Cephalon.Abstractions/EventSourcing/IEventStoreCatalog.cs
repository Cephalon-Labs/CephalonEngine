namespace Cephalon.Abstractions.EventSourcing;

/// <summary>
/// Exposes the event-stream surfaces visible to the current runtime.
/// </summary>
public interface IEventStoreCatalog
{
    /// <summary>
    /// Gets all event-stream descriptors visible to the current runtime.
    /// </summary>
    IReadOnlyList<EventStreamDescriptor> All { get; }

    /// <summary>
    /// Gets all event streams backed by the requested provider identifier.
    /// </summary>
    /// <param name="provider">The provider identifier to filter by.</param>
    /// <returns>The matching event streams, or an empty list when the provider contributes none.</returns>
    IReadOnlyList<EventStreamDescriptor> GetByProvider(string provider);

    /// <summary>
    /// Finds one event stream by its stable identifier.
    /// </summary>
    /// <param name="id">The event-stream identifier to resolve.</param>
    /// <returns>The matching event stream, or <see langword="null" /> when it is not active.</returns>
    EventStreamDescriptor? FindById(string id);
}
