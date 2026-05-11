using Cephalon.Eventing.Configuration;

namespace Cephalon.Eventing.Services;

internal sealed class EventUpcasterCatalog : IEventUpcasterCatalog
{
    private readonly Dictionary<string, EventUpcasterDescriptor> byId;
    private readonly Dictionary<string, IReadOnlyList<EventUpcasterDescriptor>> byEventType;
    private readonly Dictionary<string, EventUpcasterDescriptor> byTransition;

    public EventUpcasterCatalog(
        EventingOptions options,
        IEnumerable<IEventUpcasterContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contributors);

        var registry = new EventUpcasterRegistry();
        foreach (var upcaster in options.Upcasters)
        {
            registry.Add(upcaster);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterEventUpcasters(registry);
        }

        Upcasters = registry.Build();
        byId = Upcasters.ToDictionary(static upcaster => upcaster.Id, StringComparer.OrdinalIgnoreCase);
        byEventType = Upcasters
            .GroupBy(static upcaster => upcaster.EventType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<EventUpcasterDescriptor>)group
                    .OrderBy(static upcaster => upcaster.FromVersion, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static upcaster => upcaster.ToVersion, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static upcaster => upcaster.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
        byTransition = Upcasters
            .GroupBy(static upcaster => CreateTransitionKey(upcaster.EventType, upcaster.FromVersion, upcaster.ToVersion), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.Last(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<EventUpcasterDescriptor> Upcasters { get; }

    public IReadOnlyList<EventUpcasterDescriptor> GetByEventType(string eventType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        return byEventType.TryGetValue(eventType.Trim(), out var upcasters)
            ? upcasters
            : [];
    }

    public IReadOnlyList<EventUpcasterDescriptor> GetBySourceVersion(string eventType, string fromVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fromVersion);

        return GetByEventType(eventType)
            .Where(upcaster => string.Equals(upcaster.FromVersion, fromVersion.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public bool TryGet(string upcasterId, out EventUpcasterDescriptor upcaster)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(upcasterId);

        return byId.TryGetValue(upcasterId.Trim(), out upcaster!);
    }

    public bool TryGetTransition(
        string eventType,
        string fromVersion,
        string toVersion,
        out EventUpcasterDescriptor upcaster)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fromVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(toVersion);

        return byTransition.TryGetValue(CreateTransitionKey(eventType, fromVersion, toVersion), out upcaster!);
    }

    private static string CreateTransitionKey(string eventType, string fromVersion, string toVersion)
    {
        return $"{eventType.Trim()}\n{fromVersion.Trim()}\n{toVersion.Trim()}";
    }
}
