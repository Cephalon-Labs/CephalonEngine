using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.Cassandra.Configuration;

namespace Cephalon.Data.Cassandra.Services;

/// <summary>
/// Contributes the Cassandra inbox store surface to the active technology runtime catalog.
/// </summary>
internal sealed class CassandraInboxRuntimeSurfaceContributor(IInboxCatalog catalog) : ITechnologyRuntimeContributor
{
    /// <inheritdoc />
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "inbox-stores",
            displayName: "Inbox Stores",
            description: "Processed-message stores that record handled inbound messages for idempotent follow-through.",
            entries: catalog.GetByProvider(CassandraDataOptions.ProviderId)
                .Select(CreateEntry)
                .ToArray());
    }

    private static TechnologyRuntimeEntry CreateEntry(InboxDescriptor inbox)
    {
        var metadata = new Dictionary<string, string>(inbox.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["sourceModuleId"] = inbox.SourceModuleId,
            ["provider"] = inbox.Provider,
            ["mode"] = inbox.Mode
        };

        if (inbox.ChannelIds.Count > 0)
        {
            metadata["channelIds"] = string.Join(",", inbox.ChannelIds);
        }

        if (inbox.Tags.Count > 0)
        {
            metadata["tags"] = string.Join(",", inbox.Tags);
        }

        return new TechnologyRuntimeEntry(
            id: inbox.Id,
            displayName: inbox.DisplayName,
            description: inbox.Description,
            metadata: metadata);
    }
}
