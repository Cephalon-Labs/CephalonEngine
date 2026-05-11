using Cephalon.Abstractions.Technologies;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventingSerializerRuntimeSurfaceContributor(
    IEventSerializerCatalog serializerCatalog,
    IEventContractCatalog contractCatalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "event-serializers",
            displayName: "Event Serializers",
            description: "Provider-neutral event serializer descriptors available to the active eventing runtime.",
            entries: serializerCatalog.Serializers
                .Select(serializer => new TechnologyRuntimeEntry(
                    id: serializer.Id,
                    displayName: serializer.DisplayName,
                    description: serializer.Description,
                    metadata: BuildMetadata(serializer, contractCatalog.Contracts)))
                .ToArray());
    }

    private static Dictionary<string, string> BuildMetadata(
        EventSerializerDescriptor serializer,
        IReadOnlyList<EventContractDescriptor> contracts)
    {
        var matchingContractCount = contracts.Count(contract =>
            string.Equals(contract.SerializerId, serializer.Id, StringComparison.OrdinalIgnoreCase));

        var metadata = new Dictionary<string, string>(serializer.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["contentType"] = serializer.ContentType,
            ["format"] = serializer.Format,
            ["runtimeKind"] = serializer.RuntimeKind,
            ["canRead"] = serializer.CanRead.ToString().ToLowerInvariant(),
            ["canWrite"] = serializer.CanWrite.ToString().ToLowerInvariant(),
            ["requiresSchemaRegistry"] = serializer.RequiresSchemaRegistry.ToString().ToLowerInvariant(),
            ["matchingContractCount"] = matchingContractCount.ToString(CultureInfo.InvariantCulture),
            ["wolverineRequired"] = "false",
            ["tags"] = string.Join(",", serializer.Tags)
        };

        return metadata;
    }
}
