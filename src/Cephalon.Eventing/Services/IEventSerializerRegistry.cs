namespace Cephalon.Eventing.Services;

/// <summary>
/// Collects event serializer descriptors contributed by a host or module.
/// </summary>
public interface IEventSerializerRegistry
{
    /// <summary>
    /// Adds one event serializer descriptor to the active eventing catalog.
    /// </summary>
    /// <param name="serializer">The serializer descriptor to add.</param>
    void Add(EventSerializerDescriptor serializer);
}
