using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.ClickHouse.Configuration;

namespace Cephalon.Data.ClickHouse.Services;

/// <summary>
/// Contributes the ClickHouse outbox producer surface to the active technology runtime catalog.
/// </summary>
internal sealed class ClickHouseOutboxRuntimeSurfaceContributor(IOutboxCatalog catalog) : ITechnologyRuntimeContributor
{
    /// <inheritdoc />
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "outbox-producers",
            displayName: "Outbox Producers",
            description: "Durable outbox producers that stage integration events for later delivery through the active runtime.",
            entries: catalog.GetByProvider(ClickHouseDataOptions.ProviderId)
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
