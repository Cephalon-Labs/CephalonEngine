namespace Cephalon.Abstractions.EventSourcing;

/// <summary>
/// Receives event-stream descriptors contributed by active modules or packages.
/// </summary>
public interface IEventStoreRegistry
{
    /// <summary>
    /// Registers one event stream with the current runtime composition.
    /// </summary>
    /// <param name="descriptor">The event-stream descriptor to register.</param>
    void Register(EventStreamDescriptor descriptor);
}
