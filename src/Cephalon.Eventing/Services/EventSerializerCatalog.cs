using Cephalon.Eventing.Configuration;

namespace Cephalon.Eventing.Services;

internal sealed class EventSerializerCatalog : IEventSerializerCatalog
{
    private readonly Dictionary<string, EventSerializerDescriptor> byId;
    private readonly Dictionary<string, IReadOnlyList<EventSerializerDescriptor>> byContentType;

    public EventSerializerCatalog(
        EventingOptions options,
        IEnumerable<IEventSerializerContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contributors);

        var registry = new EventSerializerRegistry();
        foreach (var serializer in options.Serializers)
        {
            registry.Add(serializer);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterEventSerializers(registry);
        }

        Serializers = registry.Build();
        byId = Serializers.ToDictionary(static serializer => serializer.Id, StringComparer.OrdinalIgnoreCase);
        byContentType = Serializers
            .GroupBy(static serializer => serializer.ContentType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<EventSerializerDescriptor>)group
                    .OrderBy(static serializer => serializer.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<EventSerializerDescriptor> Serializers { get; }

    public IReadOnlyList<EventSerializerDescriptor> GetByContentType(string contentType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        return byContentType.TryGetValue(contentType.Trim(), out var serializers)
            ? serializers
            : [];
    }

    public bool TryGet(string serializerId, out EventSerializerDescriptor serializer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serializerId);

        return byId.TryGetValue(serializerId.Trim(), out serializer!);
    }

    public bool TryGetForContract(EventContractDescriptor contract, out EventSerializerDescriptor serializer)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return TryGet(contract.SerializerId, out serializer);
    }
}
