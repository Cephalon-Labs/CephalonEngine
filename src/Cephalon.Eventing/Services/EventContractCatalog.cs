using Cephalon.Eventing.Configuration;

namespace Cephalon.Eventing.Services;

internal sealed class EventContractCatalog : IEventContractCatalog
{
    private readonly Dictionary<string, EventContractDescriptor> byId;
    private readonly Dictionary<string, IReadOnlyList<EventContractDescriptor>> byEventType;
    private readonly Dictionary<string, EventContractDescriptor> byEventTypeAndVersion;

    public EventContractCatalog(
        EventingOptions options,
        IEnumerable<IEventContractContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contributors);

        var registry = new EventContractRegistry();
        foreach (var contract in options.Contracts)
        {
            registry.Add(contract);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterEventContracts(registry);
        }

        Contracts = registry.Build();
        byId = Contracts.ToDictionary(static contract => contract.Id, StringComparer.OrdinalIgnoreCase);
        byEventType = Contracts
            .GroupBy(static contract => contract.EventType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<EventContractDescriptor>)group
                    .OrderBy(static contract => contract.Version, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
        byEventTypeAndVersion = Contracts
            .GroupBy(static contract => CreateEventVersionKey(contract.EventType, contract.Version), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.Last(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<EventContractDescriptor> Contracts { get; }

    public IReadOnlyList<EventContractDescriptor> GetByEventType(string eventType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        return byEventType.TryGetValue(eventType.Trim(), out var contracts)
            ? contracts
            : [];
    }

    public bool TryGet(string contractId, out EventContractDescriptor contract)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contractId);

        return byId.TryGetValue(contractId.Trim(), out contract!);
    }

    public bool TryGetVersion(string eventType, string version, out EventContractDescriptor contract)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        return byEventTypeAndVersion.TryGetValue(CreateEventVersionKey(eventType, version), out contract!);
    }

    private static string CreateEventVersionKey(string eventType, string version)
    {
        return $"{eventType.Trim()}\n{version.Trim()}";
    }
}
