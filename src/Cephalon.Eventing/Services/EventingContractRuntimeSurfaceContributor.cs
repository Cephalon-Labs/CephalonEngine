using Cephalon.Abstractions.Technologies;

namespace Cephalon.Eventing.Services;

internal sealed class EventingContractRuntimeSurfaceContributor(IEventContractCatalog catalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "event-contracts",
            displayName: "Event Contracts",
            description: "Provider-neutral event contract descriptors available to the active eventing runtime.",
            entries: catalog.Contracts
                .Select(contract => new TechnologyRuntimeEntry(
                    id: contract.Id,
                    displayName: contract.DisplayName,
                    description: contract.Description,
                    metadata: BuildMetadata(contract)))
                .ToArray());
    }

    private static Dictionary<string, string> BuildMetadata(EventContractDescriptor contract)
    {
        var metadata = new Dictionary<string, string>(contract.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["eventType"] = contract.EventType,
            ["version"] = contract.Version,
            ["contentType"] = contract.ContentType,
            ["serializerId"] = contract.SerializerId,
            ["envelopeSchema"] = contract.EnvelopeSchema,
            ["compatibilityPolicy"] = contract.CompatibilityPolicy,
            ["wolverineRequired"] = "false",
            ["tags"] = string.Join(",", contract.Tags)
        };

        return metadata;
    }
}
