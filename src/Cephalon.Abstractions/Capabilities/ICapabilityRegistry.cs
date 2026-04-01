namespace Cephalon.Abstractions.Capabilities;

/// <summary>
/// Registers capabilities exposed by modules and packages.
/// </summary>
public interface ICapabilityRegistry
{
    /// <summary>
    /// Adds a capability to the registry.
    /// </summary>
    /// <param name="capability">The capability to register.</param>
    void Add(Capability capability);
}
