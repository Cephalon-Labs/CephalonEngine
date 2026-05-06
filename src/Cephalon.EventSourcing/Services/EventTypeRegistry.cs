using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.EventSourcing.Services;

/// <summary>
/// Default merged implementation of <see cref="IEventTypeRegistry" />.
/// </summary>
public sealed class EventTypeRegistry : IEventTypeRegistry
{
    /// <summary>
    /// Gets an empty event-type registry for direct provider construction scenarios.
    /// </summary>
    public static EventTypeRegistry Empty { get; } = new([]);

    private readonly Dictionary<string, EventTypeDescriptor> _descriptorsByName;
    private readonly Dictionary<Type, EventTypeDescriptor> _descriptorsByType;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypeRegistry" /> class.
    /// </summary>
    /// <param name="contributors">The contributors whose descriptors should be merged.</param>
    public EventTypeRegistry(IEnumerable<IEventTypeContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(contributors);

        var descriptors = new List<EventTypeDescriptor>();
        var descriptorsByName = new Dictionary<string, EventTypeDescriptor>(StringComparer.Ordinal);
        var descriptorsByType = new Dictionary<Type, EventTypeDescriptor>();

        foreach (var contributor in contributors)
        {
            ArgumentNullException.ThrowIfNull(contributor);

            foreach (var descriptor in contributor.Contribute())
            {
                ArgumentNullException.ThrowIfNull(descriptor);

                if (descriptorsByType.TryGetValue(descriptor.EventType, out var existingByType))
                {
                    if (!string.Equals(existingByType.Name, descriptor.Name, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"The event type '{descriptor.EventType.FullName}' was registered with both '{existingByType.Name}' and '{descriptor.Name}'.");
                    }

                    continue;
                }

                descriptors.Add(descriptor);
                descriptorsByType.Add(descriptor.EventType, descriptor);
                AddName(descriptorsByName, descriptor.Name, descriptor);

                foreach (var alias in descriptor.Aliases)
                {
                    AddName(descriptorsByName, alias, descriptor);
                }
            }
        }

        All = descriptors.AsReadOnly();
        _descriptorsByName = descriptorsByName;
        _descriptorsByType = descriptorsByType;
    }

    /// <summary>
    /// Gets all event-type descriptors known to this registry.
    /// </summary>
    public IReadOnlyList<EventTypeDescriptor> All { get; }

    /// <summary>
    /// Gets the persisted event-type name for a domain-event instance.
    /// </summary>
    /// <param name="evt">The domain-event instance to resolve.</param>
    /// <returns>The persisted event-type name.</returns>
    public string GetName(IDomainEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        if (!TryFindByType(evt.GetType(), out var descriptor))
        {
            throw CreateUnknownEventTypeException(evt.GetType());
        }

        return descriptor.Name;
    }

    /// <summary>
    /// Serializes a domain-event instance using its registered descriptor.
    /// </summary>
    /// <param name="evt">The domain-event instance to serialize.</param>
    /// <returns>The serialized event payload.</returns>
    public string Serialize(IDomainEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        if (!TryFindByType(evt.GetType(), out var descriptor))
        {
            throw CreateUnknownEventTypeException(evt.GetType());
        }

        return descriptor.Serialize(evt);
    }

    /// <summary>
    /// Deserializes a persisted event payload using the descriptor registered for the event-type name.
    /// </summary>
    /// <param name="eventTypeName">The persisted event-type name.</param>
    /// <param name="payload">The serialized event payload.</param>
    /// <returns>The deserialized domain-event instance.</returns>
    public IDomainEvent Deserialize(string eventTypeName, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventTypeName);
        ArgumentNullException.ThrowIfNull(payload);

        if (!TryFindByName(eventTypeName, out var descriptor))
        {
            throw new InvalidOperationException(
                $"The event type name '{eventTypeName}' is not registered with the Cephalon event-type registry. Register a descriptor with AddCephalonEventType<TEvent>(...) or an IEventTypeContributor before reading event streams.");
        }

        var evt = descriptor.Deserialize(payload);
        if (evt is null)
        {
            throw new InvalidOperationException(
                $"The payload for event type name '{eventTypeName}' could not be deserialized as an IDomainEvent.");
        }

        return evt;
    }

    /// <summary>
    /// Attempts to find a descriptor by persisted event-type name or alias.
    /// </summary>
    /// <param name="eventTypeName">The persisted event-type name or alias.</param>
    /// <param name="descriptor">The matching descriptor when the name is registered.</param>
    /// <returns><c>true</c> when the event-type name is registered; otherwise, <c>false</c>.</returns>
    public bool TryFindByName(string eventTypeName, out EventTypeDescriptor descriptor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventTypeName);
        return _descriptorsByName.TryGetValue(eventTypeName.Trim(), out descriptor!);
    }

    /// <summary>
    /// Attempts to find a descriptor by concrete domain-event type.
    /// </summary>
    /// <param name="eventType">The concrete domain-event type.</param>
    /// <param name="descriptor">The matching descriptor when the type is registered.</param>
    /// <returns><c>true</c> when the event type is registered; otherwise, <c>false</c>.</returns>
    public bool TryFindByType(Type eventType, out EventTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return _descriptorsByType.TryGetValue(eventType, out descriptor!);
    }

    private static void AddName(
        Dictionary<string, EventTypeDescriptor> descriptorsByName,
        string name,
        EventTypeDescriptor descriptor)
    {
        if (descriptorsByName.TryGetValue(name, out var existing) &&
            existing.EventType != descriptor.EventType)
        {
            throw new InvalidOperationException(
                $"The event type name '{name}' is already mapped to '{existing.EventType.FullName}' and cannot also map to '{descriptor.EventType.FullName}'.");
        }

        descriptorsByName[name] = descriptor;
    }

    private static InvalidOperationException CreateUnknownEventTypeException(Type eventType) =>
        new(
            $"The domain event type '{eventType.FullName}' is not registered with the Cephalon event-type registry. Register it with AddCephalonEventType<TEvent>(...) or an IEventTypeContributor before appending events.");
}
