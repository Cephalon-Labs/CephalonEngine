namespace Cephalon.Eventing.Services;

/// <summary>
/// Provides the merged event schema registry descriptors visible to the active eventing runtime.
/// </summary>
public interface IEventSchemaRegistryCatalog
{
    /// <summary>
    /// Gets all registered event schema registry descriptors.
    /// </summary>
    IReadOnlyList<EventSchemaRegistryDescriptor> Registries { get; }

    /// <summary>
    /// Gets all schema registries registered for the supplied provider.
    /// </summary>
    /// <param name="provider">The provider or product family.</param>
    /// <returns>The matching schema registries ordered by identifier.</returns>
    IReadOnlyList<EventSchemaRegistryDescriptor> GetByProvider(string provider);

    /// <summary>
    /// Gets all schema registries registered for the supplied serialization format.
    /// </summary>
    /// <param name="format">The serialization format.</param>
    /// <returns>The matching schema registries ordered by identifier.</returns>
    IReadOnlyList<EventSchemaRegistryDescriptor> GetByFormat(string format);

    /// <summary>
    /// Attempts to resolve a schema registry by its stable identifier.
    /// </summary>
    /// <param name="schemaRegistryId">The stable schema registry identifier.</param>
    /// <param name="registry">When found, the matching schema registry descriptor.</param>
    /// <returns><see langword="true" /> when a registry with the identifier exists.</returns>
    bool TryGet(string schemaRegistryId, out EventSchemaRegistryDescriptor registry);

    /// <summary>
    /// Attempts to resolve the schema registry selected by an event serializer.
    /// </summary>
    /// <param name="serializer">The event serializer descriptor.</param>
    /// <param name="registry">When found, the matching schema registry descriptor.</param>
    /// <returns><see langword="true" /> when the serializer's schema registry identifier is available.</returns>
    bool TryGetForSerializer(EventSerializerDescriptor serializer, out EventSchemaRegistryDescriptor registry);
}
