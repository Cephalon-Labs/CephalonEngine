namespace Cephalon.Eventing.Services;

/// <summary>
/// Allows a module to contribute event contract metadata into the active eventing runtime pack.
/// </summary>
public interface IEventContractContributor
{
    /// <summary>
    /// Registers one or more event contract descriptors with the supplied registry.
    /// </summary>
    /// <param name="contracts">The registry that collects contributed event contract descriptors.</param>
    void RegisterEventContracts(IEventContractRegistry contracts);
}
