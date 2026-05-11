namespace Cephalon.Eventing.Services;

internal sealed class EventContractRegistry : IEventContractRegistry
{
    private readonly List<EventContractDescriptor> contracts = [];

    public void Add(EventContractDescriptor contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        contracts.Add(contract);
    }

    public IReadOnlyList<EventContractDescriptor> Build()
    {
        return contracts
            .GroupBy(static contract => contract.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.Last())
            .OrderBy(static contract => contract.EventType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static contract => contract.Version, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
