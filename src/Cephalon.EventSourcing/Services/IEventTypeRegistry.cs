using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.EventSourcing.Services;

/// <summary>
/// Resolves, serializes, and deserializes the domain-event types known to a Cephalon runtime.
/// </summary>
/// <remarks>
/// Provider event stores use this registry instead of resolving persisted CLR type names with
/// <c>Type.GetType</c>. The registry is intentionally closed and host-owned so trimming and
/// Native AOT lanes can replace runtime type-name discovery with generated descriptors.
/// </remarks>
public interface IEventTypeRegistry
{
    /// <summary>
    /// Gets all event-type descriptors known to this registry.
    /// </summary>
    IReadOnlyList<EventTypeDescriptor> All { get; }

    /// <summary>
    /// Gets the persisted event-type name for a domain-event instance.
    /// </summary>
    /// <param name="evt">The domain event being appended.</param>
    /// <returns>The stable event-type name that should be persisted with the payload.</returns>
    string GetName(IDomainEvent evt);

    /// <summary>
    /// Serializes a domain-event payload using the descriptor registered for its concrete event type.
    /// </summary>
    /// <param name="evt">The domain event to serialize.</param>
    /// <returns>The serialized event payload.</returns>
    string Serialize(IDomainEvent evt);

    /// <summary>
    /// Deserializes a persisted payload using the descriptor registered for the event-type name.
    /// </summary>
    /// <param name="eventTypeName">The event-type name read from the event store.</param>
    /// <param name="payload">The serialized event payload.</param>
    /// <returns>The rehydrated domain event.</returns>
    IDomainEvent Deserialize(string eventTypeName, string payload);

    /// <summary>
    /// Attempts to find a descriptor by its persisted event-type name or one of its aliases.
    /// </summary>
    /// <param name="eventTypeName">The persisted event-type name to resolve.</param>
    /// <param name="descriptor">The resolved descriptor, when one exists.</param>
    /// <returns><see langword="true" /> when the registry contains a matching descriptor.</returns>
    bool TryFindByName(string eventTypeName, out EventTypeDescriptor descriptor);

    /// <summary>
    /// Attempts to find a descriptor by the concrete event CLR type.
    /// </summary>
    /// <param name="eventType">The concrete event type to resolve.</param>
    /// <param name="descriptor">The resolved descriptor, when one exists.</param>
    /// <returns><see langword="true" /> when the registry contains a matching descriptor.</returns>
    bool TryFindByType(Type eventType, out EventTypeDescriptor descriptor);
}
