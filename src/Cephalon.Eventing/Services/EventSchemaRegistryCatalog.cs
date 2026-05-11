using Cephalon.Eventing.Configuration;

namespace Cephalon.Eventing.Services;

internal sealed class EventSchemaRegistryCatalog : IEventSchemaRegistryCatalog
{
    private readonly Dictionary<string, EventSchemaRegistryDescriptor> byId;
    private readonly Dictionary<string, IReadOnlyList<EventSchemaRegistryDescriptor>> byProvider;
    private readonly Dictionary<string, IReadOnlyList<EventSchemaRegistryDescriptor>> byFormat;

    public EventSchemaRegistryCatalog(
        EventingOptions options,
        IEnumerable<IEventSchemaRegistryContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contributors);

        var registry = new EventSchemaRegistryRegistry();
        foreach (var descriptor in options.SchemaRegistries)
        {
            registry.Add(descriptor);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterEventSchemaRegistries(registry);
        }

        Registries = registry.Build();
        byId = Registries.ToDictionary(static descriptor => descriptor.Id, StringComparer.OrdinalIgnoreCase);
        byProvider = Registries
            .GroupBy(static descriptor => descriptor.Provider, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<EventSchemaRegistryDescriptor>)group
                    .OrderBy(static descriptor => descriptor.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
        byFormat = Registries
            .SelectMany(static descriptor => descriptor.SupportedFormats.Select(format => new { Format = format, Descriptor = descriptor }))
            .GroupBy(static entry => entry.Format, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<EventSchemaRegistryDescriptor>)group
                    .Select(static entry => entry.Descriptor)
                    .OrderBy(static descriptor => descriptor.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<EventSchemaRegistryDescriptor> Registries { get; }

    public IReadOnlyList<EventSchemaRegistryDescriptor> GetByProvider(string provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        return byProvider.TryGetValue(provider.Trim(), out var registries)
            ? registries
            : [];
    }

    public IReadOnlyList<EventSchemaRegistryDescriptor> GetByFormat(string format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        return byFormat.TryGetValue(format.Trim(), out var registries)
            ? registries
            : [];
    }

    public bool TryGet(string schemaRegistryId, out EventSchemaRegistryDescriptor registry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaRegistryId);

        return byId.TryGetValue(schemaRegistryId.Trim(), out registry!);
    }

    public bool TryGetForSerializer(EventSerializerDescriptor serializer, out EventSchemaRegistryDescriptor registry)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        if (string.IsNullOrWhiteSpace(serializer.SchemaRegistryId))
        {
            registry = null!;
            return false;
        }

        return TryGet(serializer.SchemaRegistryId, out registry);
    }
}
