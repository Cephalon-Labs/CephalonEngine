using Cephalon.Eventing.Configuration;

namespace Cephalon.Eventing.Services;

internal sealed class EventChannelCatalog : IEventChannelCatalog
{
    private readonly Dictionary<string, EventChannelDescriptor> index;

    public EventChannelCatalog(
        EventingOptions options,
        IEnumerable<IEventChannelContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contributors);

        var registry = new EventChannelRegistry();
        foreach (var channel in options.Channels)
        {
            registry.Add(channel);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterChannels(registry);
        }

        Channels = registry.Build();
        index = Channels.ToDictionary(static channel => channel.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<EventChannelDescriptor> Channels { get; }

    public bool TryGet(string channelId, out EventChannelDescriptor channel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);

        return index.TryGetValue(channelId.Trim(), out channel!);
    }
}
