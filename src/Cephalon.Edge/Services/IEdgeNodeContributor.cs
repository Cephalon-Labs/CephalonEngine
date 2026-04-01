namespace Cephalon.Edge.Services;

/// <summary>
/// Allows a module to contribute edge nodes into the active edge runtime pack.
/// </summary>
public interface IEdgeNodeContributor
{
    /// <summary>
    /// Registers one or more edge node descriptors with the supplied registry.
    /// </summary>
    /// <param name="nodes">The registry that collects contributed node descriptors.</param>
    void RegisterNodes(IEdgeNodeRegistry nodes);
}
