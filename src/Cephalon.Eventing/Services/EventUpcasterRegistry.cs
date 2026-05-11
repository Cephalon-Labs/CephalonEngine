namespace Cephalon.Eventing.Services;

internal sealed class EventUpcasterRegistry : IEventUpcasterRegistry
{
    private readonly List<EventUpcasterDescriptor> upcasters = [];

    public void Add(EventUpcasterDescriptor upcaster)
    {
        ArgumentNullException.ThrowIfNull(upcaster);

        upcasters.Add(upcaster);
    }

    public IReadOnlyList<EventUpcasterDescriptor> Build()
    {
        return upcasters
            .GroupBy(static upcaster => upcaster.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.Last())
            .OrderBy(static upcaster => upcaster.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
