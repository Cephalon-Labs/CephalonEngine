namespace Cephalon.Eventing.Services;

/// <summary>
/// Allows a module to contribute event schema registry metadata into the active eventing runtime pack.
/// </summary>
public interface IEventSchemaRegistryContributor
{
    /// <summary>
    /// Registers one or more event schema registry descriptors with the supplied registry.
    /// </summary>
    /// <param name="registries">The registry that collects contributed event schema registry descriptors.</param>
    void RegisterEventSchemaRegistries(IEventSchemaRegistryRegistry registries);
}
