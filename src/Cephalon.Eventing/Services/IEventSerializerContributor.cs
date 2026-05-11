namespace Cephalon.Eventing.Services;

/// <summary>
/// Allows a module to contribute event serializer metadata into the active eventing runtime pack.
/// </summary>
public interface IEventSerializerContributor
{
    /// <summary>
    /// Registers one or more event serializer descriptors with the supplied registry.
    /// </summary>
    /// <param name="serializers">The registry that collects contributed event serializer descriptors.</param>
    void RegisterEventSerializers(IEventSerializerRegistry serializers);
}
