using Cephalon.Abstractions.Technologies;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventingSchemaRegistryRuntimeSurfaceContributor(
    IEventSchemaRegistryCatalog schemaRegistryCatalog,
    IEventSerializerCatalog serializerCatalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "event-schema-registries",
            displayName: "Event Schema Registries",
            description: "Provider-neutral event schema registry descriptors available to the active eventing runtime.",
            entries: schemaRegistryCatalog.Registries
                .Select(registry => new TechnologyRuntimeEntry(
                    id: registry.Id,
                    displayName: registry.DisplayName,
                    description: registry.Description,
                    metadata: BuildMetadata(registry, serializerCatalog.Serializers)))
                .ToArray());
    }

    private static Dictionary<string, string> BuildMetadata(
        EventSchemaRegistryDescriptor registry,
        IReadOnlyList<EventSerializerDescriptor> serializers)
    {
        var matchingSerializerCount = serializers.Count(serializer =>
            string.Equals(serializer.SchemaRegistryId, registry.Id, StringComparison.OrdinalIgnoreCase));

        var metadata = new Dictionary<string, string>(registry.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["provider"] = registry.Provider,
            ["endpointKind"] = registry.EndpointKind,
            ["runtimeKind"] = registry.RuntimeKind,
            ["canReadSchemas"] = registry.CanReadSchemas.ToString().ToLowerInvariant(),
            ["canWriteSchemas"] = registry.CanWriteSchemas.ToString().ToLowerInvariant(),
            ["validatesCompatibility"] = registry.ValidatesCompatibility.ToString().ToLowerInvariant(),
            ["supportedFormats"] = string.Join(",", registry.SupportedFormats),
            ["matchingSerializerCount"] = matchingSerializerCount.ToString(CultureInfo.InvariantCulture),
            ["wolverineRequired"] = "false",
            ["tags"] = string.Join(",", registry.Tags)
        };

        return metadata;
    }
}
