using Cephalon.Abstractions.Technologies;

namespace Cephalon.Eventing.Services;

internal sealed class EventingUpcasterRuntimeSurfaceContributor(
    IEventUpcasterCatalog upcasterCatalog,
    IEventContractCatalog contractCatalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "event-upcasters",
            displayName: "Event Upcasters",
            description: "Provider-neutral event upcaster descriptors available to the active eventing runtime.",
            entries: upcasterCatalog.Upcasters
                .Select(upcaster => new TechnologyRuntimeEntry(
                    id: upcaster.Id,
                    displayName: upcaster.DisplayName,
                    description: upcaster.Description,
                    metadata: BuildMetadata(upcaster, contractCatalog)))
                .ToArray());
    }

    private static Dictionary<string, string> BuildMetadata(
        EventUpcasterDescriptor upcaster,
        IEventContractCatalog contractCatalog)
    {
        var sourceContractResolved = contractCatalog.TryGetVersion(upcaster.EventType, upcaster.FromVersion, out _);
        var targetContractResolved = contractCatalog.TryGetVersion(upcaster.EventType, upcaster.ToVersion, out _);
        var metadata = new Dictionary<string, string>(upcaster.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["eventType"] = upcaster.EventType,
            ["fromVersion"] = upcaster.FromVersion,
            ["toVersion"] = upcaster.ToVersion,
            ["runtimeKind"] = upcaster.RuntimeKind,
            ["canUpcast"] = upcaster.CanUpcast.ToString().ToLowerInvariant(),
            ["sourceContractResolved"] = sourceContractResolved.ToString().ToLowerInvariant(),
            ["targetContractResolved"] = targetContractResolved.ToString().ToLowerInvariant(),
            ["wolverineRequired"] = "false",
            ["tags"] = string.Join(",", upcaster.Tags)
        };

        return metadata;
    }
}
