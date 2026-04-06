namespace Cephalon.Abstractions.Behaviors;

/// <summary>Describes the resolved topology for a single behavior: its pattern, transports, and feature flags.</summary>
public sealed class BehaviorTopologyDescriptor
{
    /// <summary>Initializes a new instance of <see cref="BehaviorTopologyDescriptor"/>.</summary>
    public BehaviorTopologyDescriptor(
        string id,
        string pattern,
        IReadOnlyList<string> transportIds,
        bool inboxEnabled = false,
        bool outboxEnabled = false,
        bool eventSourcingEnabled = false,
        string? displayName = null,
        string? description = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Id = id;
        Pattern = pattern;
        TransportIds = transportIds;
        InboxEnabled = inboxEnabled;
        OutboxEnabled = outboxEnabled;
        EventSourcingEnabled = eventSourcingEnabled;
        DisplayName = displayName;
        Description = description;
        Metadata = metadata ?? new Dictionary<string, string>();
    }

    /// <summary>Gets the behavior identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the pattern identifier (e.g. "cqrs", "event-driven", "saga-step", "process-manager", "direct").</summary>
    public string Pattern { get; }

    /// <summary>Gets the transport identifiers configured for this behavior.</summary>
    public IReadOnlyList<string> TransportIds { get; }

    /// <summary>Gets a value indicating whether inbox deduplication is enabled.</summary>
    public bool InboxEnabled { get; }

    /// <summary>Gets a value indicating whether outbox staging is enabled.</summary>
    public bool OutboxEnabled { get; }

    /// <summary>Gets a value indicating whether event sourcing is wired into the behavior context.</summary>
    public bool EventSourcingEnabled { get; }

    /// <summary>Gets the optional display name.</summary>
    public string? DisplayName { get; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; }

    /// <summary>Gets additional metadata.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
