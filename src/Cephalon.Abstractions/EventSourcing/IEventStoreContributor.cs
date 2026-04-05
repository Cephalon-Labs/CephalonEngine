namespace Cephalon.Abstractions.EventSourcing;

/// <summary>
/// Contributes one or more event-stream descriptors to the active runtime.
/// </summary>
public interface IEventStoreContributor
{
    /// <summary>
    /// Returns the event-stream descriptors contributed by the current module or package.
    /// </summary>
    /// <returns>The contributed event-stream descriptors.</returns>
    IReadOnlyList<EventStreamDescriptor> Contribute();
}
