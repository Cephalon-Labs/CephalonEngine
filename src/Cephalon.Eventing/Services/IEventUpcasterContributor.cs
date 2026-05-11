namespace Cephalon.Eventing.Services;

/// <summary>
/// Allows a module to contribute event upcaster metadata into the active eventing runtime pack.
/// </summary>
public interface IEventUpcasterContributor
{
    /// <summary>
    /// Registers one or more event upcaster descriptors with the supplied registry.
    /// </summary>
    /// <param name="upcasters">The registry that collects contributed event upcaster descriptors.</param>
    void RegisterEventUpcasters(IEventUpcasterRegistry upcasters);
}
