namespace Cephalon.Eventing.Services;

internal sealed class EventSerializerRegistry : IEventSerializerRegistry
{
    private readonly List<EventSerializerDescriptor> serializers = [];

    public void Add(EventSerializerDescriptor serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        serializers.Add(serializer);
    }

    public IReadOnlyList<EventSerializerDescriptor> Build()
    {
        return serializers
            .GroupBy(static serializer => serializer.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.Last())
            .OrderBy(static serializer => serializer.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
