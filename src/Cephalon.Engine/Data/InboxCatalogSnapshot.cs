using Cephalon.Abstractions.Data;

namespace Cephalon.Engine.Data;

internal sealed class InboxCatalogSnapshot : IInboxCatalog
{
    private readonly IReadOnlyList<InboxDescriptor> inboxes;
    private readonly Dictionary<string, InboxDescriptor> inboxesById;
    private readonly Dictionary<string, IReadOnlyList<InboxDescriptor>> inboxesBySourceModule;
    private readonly Dictionary<string, IReadOnlyList<InboxDescriptor>> inboxesByProvider;
    private readonly Dictionary<string, IReadOnlyList<InboxDescriptor>> inboxesByChannelId;

    public InboxCatalogSnapshot(IEnumerable<InboxDescriptor> inboxes)
    {
        ArgumentNullException.ThrowIfNull(inboxes);

        this.inboxes = inboxes.ToArray();
        inboxesById = this.inboxes.ToDictionary(static inbox => inbox.Id, StringComparer.OrdinalIgnoreCase);
        inboxesBySourceModule = this.inboxes
            .GroupBy(static inbox => inbox.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<InboxDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        inboxesByProvider = this.inboxes
            .GroupBy(static inbox => inbox.Provider, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<InboxDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        inboxesByChannelId = this.inboxes
            .SelectMany(static inbox => inbox.ChannelIds.Select(channelId => new KeyValuePair<string, InboxDescriptor>(channelId, inbox)))
            .GroupBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<InboxDescriptor>)group.Select(static pair => pair.Value).ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<InboxDescriptor> Inboxes => inboxes;

    public InboxDescriptor? GetById(string inboxId)
    {
        if (string.IsNullOrWhiteSpace(inboxId))
        {
            return null;
        }

        return inboxesById.TryGetValue(inboxId.Trim(), out var inbox)
            ? inbox
            : null;
    }

    public IReadOnlyList<InboxDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return inboxesBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<InboxDescriptor> GetByProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return [];
        }

        return inboxesByProvider.TryGetValue(provider.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<InboxDescriptor> GetByChannelId(string channelId)
    {
        if (string.IsNullOrWhiteSpace(channelId))
        {
            return [];
        }

        return inboxesByChannelId.TryGetValue(channelId.Trim(), out var matches)
            ? matches
            : [];
    }
}
