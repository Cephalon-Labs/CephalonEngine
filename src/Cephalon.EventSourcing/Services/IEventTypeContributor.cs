using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.EventSourcing.Services;

/// <summary>
/// Contributes known domain-event type descriptors to the Cephalon event-type registry.
/// </summary>
/// <remarks>
/// Applications, modules, and future source generators use this contract to provide the
/// closed set of domain-event payloads that an event store can serialize and deserialize
/// without resolving CLR type names from persisted data.
/// </remarks>
public interface IEventTypeContributor
{
    /// <summary>
    /// Returns the event-type descriptors contributed by this component.
    /// </summary>
    /// <returns>The descriptors that should be merged into the runtime event-type registry.</returns>
    IReadOnlyList<EventTypeDescriptor> Contribute();
}
