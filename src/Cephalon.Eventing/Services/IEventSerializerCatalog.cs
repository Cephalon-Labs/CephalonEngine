namespace Cephalon.Eventing.Services;

/// <summary>
/// Provides the merged event serializer descriptors visible to the active eventing runtime.
/// </summary>
public interface IEventSerializerCatalog
{
    /// <summary>
    /// Gets all registered event serializer descriptors.
    /// </summary>
    IReadOnlyList<EventSerializerDescriptor> Serializers { get; }

    /// <summary>
    /// Gets all serializer descriptors registered for the supplied content type.
    /// </summary>
    /// <param name="contentType">The wire content type.</param>
    /// <returns>The matching serializer descriptors ordered by identifier.</returns>
    IReadOnlyList<EventSerializerDescriptor> GetByContentType(string contentType);

    /// <summary>
    /// Attempts to resolve a serializer by its stable identifier.
    /// </summary>
    /// <param name="serializerId">The stable serializer identifier.</param>
    /// <param name="serializer">When found, the matching serializer descriptor.</param>
    /// <returns><see langword="true" /> when a serializer with the identifier exists.</returns>
    bool TryGet(string serializerId, out EventSerializerDescriptor serializer);

    /// <summary>
    /// Attempts to resolve the serializer selected by an event contract.
    /// </summary>
    /// <param name="contract">The event contract descriptor.</param>
    /// <param name="serializer">When found, the matching serializer descriptor.</param>
    /// <returns><see langword="true" /> when the contract's serializer identifier is available.</returns>
    bool TryGetForContract(EventContractDescriptor contract, out EventSerializerDescriptor serializer);
}
