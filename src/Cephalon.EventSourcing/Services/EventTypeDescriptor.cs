using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.EventSourcing.Services;

/// <summary>
/// Describes one domain-event payload type known to the Cephalon event-type registry.
/// </summary>
/// <remarks>
/// The descriptor carries both the persisted event-type name and the serialization delegates
/// for that type. This keeps provider event stores away from string-to-type reflection and
/// gives future source generators a closed descriptor shape to emit.
/// </remarks>
public sealed class EventTypeDescriptor
{
    private readonly Func<IDomainEvent, string> _serialize;
    private readonly Func<string, IDomainEvent?> _deserialize;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypeDescriptor" /> class.
    /// </summary>
    /// <param name="eventType">The concrete domain-event CLR type.</param>
    /// <param name="name">The stable persisted event-type name.</param>
    /// <param name="serialize">The payload serializer for this event type.</param>
    /// <param name="deserialize">The payload deserializer for this event type.</param>
    /// <param name="aliases">Optional legacy names that should resolve to this event type.</param>
    public EventTypeDescriptor(
        Type eventType,
        string name,
        Func<IDomainEvent, string> serialize,
        Func<string, IDomainEvent?> deserialize,
        IEnumerable<string>? aliases = null)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(serialize);
        ArgumentNullException.ThrowIfNull(deserialize);

        if (!typeof(IDomainEvent).IsAssignableFrom(eventType))
        {
            throw new ArgumentException(
                $"The event type '{eventType.FullName}' must implement {nameof(IDomainEvent)}.",
                nameof(eventType));
        }

        EventType = eventType;
        Name = name.Trim();
        Aliases = NormalizeAliases(Name, aliases).AsReadOnly();
        _serialize = serialize;
        _deserialize = deserialize;
    }

    /// <summary>
    /// Gets the concrete domain-event CLR type.
    /// </summary>
    public Type EventType { get; }

    /// <summary>
    /// Gets the stable event-type name persisted by event stores.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the legacy or alternate names that resolve to this event type.
    /// </summary>
    public IReadOnlyList<string> Aliases { get; }

    /// <summary>
    /// Creates a descriptor for an event type using the default <c>System.Text.Json</c> generic serializer.
    /// </summary>
    /// <typeparam name="TEvent">The concrete domain-event type.</typeparam>
    /// <param name="name">An optional stable persisted name. Defaults to the event type's full name.</param>
    /// <param name="aliases">Optional legacy names that should resolve to this event type.</param>
    /// <returns>A descriptor for <typeparamref name="TEvent" />.</returns>
    public static EventTypeDescriptor Create<TEvent>(
        string? name = null,
        IEnumerable<string>? aliases = null)
        where TEvent : IDomainEvent
    {
        var eventType = typeof(TEvent);
        var eventName = NormalizeName(eventType, name);
        var allAliases = BuildDefaultAliases(eventType, eventName, aliases);

        return new EventTypeDescriptor(
            eventType,
            eventName,
            static evt => JsonSerializer.Serialize((TEvent)evt),
            static payload => JsonSerializer.Deserialize<TEvent>(payload),
            allAliases);
    }

    /// <summary>
    /// Creates a descriptor for an event type using source-generated <see cref="JsonTypeInfo{T}" /> metadata.
    /// </summary>
    /// <typeparam name="TEvent">The concrete domain-event type.</typeparam>
    /// <param name="jsonTypeInfo">The source-generated JSON type information for the event payload.</param>
    /// <param name="name">An optional stable persisted name. Defaults to the event type's full name.</param>
    /// <param name="aliases">Optional legacy names that should resolve to this event type.</param>
    /// <returns>A descriptor for <typeparamref name="TEvent" />.</returns>
    public static EventTypeDescriptor CreateWithJsonTypeInfo<TEvent>(
        JsonTypeInfo<TEvent> jsonTypeInfo,
        string? name = null,
        IEnumerable<string>? aliases = null)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);

        var eventType = typeof(TEvent);
        var eventName = NormalizeName(eventType, name);
        var allAliases = BuildDefaultAliases(eventType, eventName, aliases);

        return new EventTypeDescriptor(
            eventType,
            eventName,
            evt => JsonSerializer.Serialize((TEvent)evt, jsonTypeInfo),
            payload => JsonSerializer.Deserialize(payload, jsonTypeInfo),
            allAliases);
    }

    /// <summary>
    /// Serializes a domain-event instance using this descriptor.
    /// </summary>
    /// <param name="evt">The event instance to serialize.</param>
    /// <returns>The serialized payload.</returns>
    public string Serialize(IDomainEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        if (evt.GetType() != EventType)
        {
            throw new ArgumentException(
                $"The event instance type '{evt.GetType().FullName}' does not match descriptor type '{EventType.FullName}'.",
                nameof(evt));
        }

        return _serialize(evt);
    }

    /// <summary>
    /// Deserializes a payload using this descriptor.
    /// </summary>
    /// <param name="payload">The serialized payload.</param>
    /// <returns>The deserialized domain event, or <see langword="null" /> when the payload cannot be deserialized.</returns>
    public IDomainEvent? Deserialize(string payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return _deserialize(payload);
    }

    private static string NormalizeName(Type eventType, string? name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name.Trim();
        }

        return eventType.FullName
            ?? throw new InvalidOperationException($"The event type '{eventType.Name}' must expose a full name.");
    }

    private static List<string> BuildDefaultAliases(
        Type eventType,
        string name,
        IEnumerable<string>? aliases)
    {
        var values = new List<string>();
        if (!string.IsNullOrWhiteSpace(eventType.FullName) &&
            !string.Equals(eventType.FullName, name, StringComparison.Ordinal))
        {
            values.Add(eventType.FullName);
        }

        if (!string.IsNullOrWhiteSpace(eventType.AssemblyQualifiedName) &&
            !string.Equals(eventType.AssemblyQualifiedName, name, StringComparison.Ordinal))
        {
            values.Add(eventType.AssemblyQualifiedName);
        }

        if (aliases is not null)
        {
            values.AddRange(aliases);
        }

        return values;
    }

    private static List<string> NormalizeAliases(string name, IEnumerable<string>? aliases)
    {
        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal) { name };

        if (aliases is null)
        {
            return normalized;
        }

        foreach (var alias in aliases)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                continue;
            }

            var value = alias.Trim();
            if (seen.Add(value))
            {
                normalized.Add(value);
            }
        }

        return normalized;
    }
}
