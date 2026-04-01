namespace Cephalon.Edge.Services;

/// <summary>
/// Collects edge node descriptors contributed to the active edge runtime pack.
/// </summary>
public interface IEdgeNodeRegistry
{
    /// <summary>
    /// Adds an edge node descriptor to the registry.
    /// </summary>
    /// <param name="node">The node descriptor to contribute.</param>
    void Add(EdgeNodeDescriptor node);
}
