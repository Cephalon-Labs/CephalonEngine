using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.EntityFramework.Configuration;

namespace Cephalon.Data.EntityFramework.Services;

internal sealed class EntityFrameworkOutboxRuntimeSurfaceContributor(IOutboxCatalog catalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "outbox-producers",
            displayName: "Outbox Producers",
            description: "Durable outbox producers that stage integration events for later delivery through the active runtime.",
            entries: catalog.GetByProvider(EntityFrameworkDataOptions.ProviderId)
                .Select(CreateEntry)
                .ToArray());
    }

    private static TechnologyRuntimeEntry CreateEntry(OutboxDescriptor outbox)
    {
        var metadata = new Dictionary<string, string>(outbox.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["sourceModuleId"] = outbox.SourceModuleId,
            ["provider"] = outbox.Provider,
            ["mode"] = outbox.Mode
        };

        if (outbox.ChannelIds.Count > 0)
        {
            metadata["channelIds"] = string.Join(",", outbox.ChannelIds);
        }

        if (outbox.Tags.Count > 0)
        {
            metadata["tags"] = string.Join(",", outbox.Tags);
        }

        return new TechnologyRuntimeEntry(
            id: outbox.Id,
            displayName: outbox.DisplayName,
            description: outbox.Description,
            metadata: metadata);
    }
}
