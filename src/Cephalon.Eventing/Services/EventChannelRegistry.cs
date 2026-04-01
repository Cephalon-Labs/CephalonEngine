namespace Cephalon.Eventing.Services;

internal sealed class EventChannelRegistry : IEventChannelRegistry
{
    private readonly List<EventChannelDescriptor> channels = [];

    public void Add(EventChannelDescriptor channel)
    {
        ArgumentNullException.ThrowIfNull(channel);

        channels.Add(channel);
    }

    public IReadOnlyList<EventChannelDescriptor> Build()
    {
        return channels
            .GroupBy(static channel => channel.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(static channel => channel.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
