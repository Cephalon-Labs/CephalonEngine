namespace Cephalon.Edge.Services;

/// <summary>
/// Exposes the merged set of edge nodes available to the active edge runtime.
/// </summary>
public interface IEdgeNodeCatalog
{
    /// <summary>
    /// Gets the effective node set after host options and module contributors have both been applied.
    /// </summary>
    IReadOnlyList<EdgeNodeDescriptor> Nodes { get; }

    /// <summary>
    /// Attempts to resolve an edge node descriptor by identifier.
    /// </summary>
    /// <param name="nodeId">The node identifier to resolve.</param>
    /// <param name="node">When this method returns, contains the resolved node if found.</param>
    /// <returns><see langword="true" /> when the node exists; otherwise <see langword="false" />.</returns>
    bool TryGet(string nodeId, out EdgeNodeDescriptor node);
}
