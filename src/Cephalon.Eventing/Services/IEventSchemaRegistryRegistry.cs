namespace Cephalon.Eventing.Services;

/// <summary>
/// Collects event schema registry descriptors contributed by a host or module.
/// </summary>
public interface IEventSchemaRegistryRegistry
{
    /// <summary>
    /// Adds one event schema registry descriptor to the active eventing catalog.
    /// </summary>
    /// <param name="registry">The schema registry descriptor to add.</param>
    void Add(EventSchemaRegistryDescriptor registry);
}
