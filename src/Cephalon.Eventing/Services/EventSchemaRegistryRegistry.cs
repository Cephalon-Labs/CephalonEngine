namespace Cephalon.Eventing.Services;

internal sealed class EventSchemaRegistryRegistry : IEventSchemaRegistryRegistry
{
    private readonly List<EventSchemaRegistryDescriptor> registries = [];

    public void Add(EventSchemaRegistryDescriptor registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registries.Add(registry);
    }

    public IReadOnlyList<EventSchemaRegistryDescriptor> Build()
    {
        return registries
            .GroupBy(static registry => registry.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.Last())
            .OrderBy(static registry => registry.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
