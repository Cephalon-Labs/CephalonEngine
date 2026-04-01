using Cephalon.Edge.Configuration;

namespace Cephalon.Edge.Services;

internal sealed class EdgeNodeCatalog : IEdgeNodeCatalog
{
    private readonly Dictionary<string, EdgeNodeDescriptor> index;

    public EdgeNodeCatalog(
        EdgeRuntimeOptions options,
        IEnumerable<IEdgeNodeContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contributors);

        var registry = new EdgeNodeRegistry();
        foreach (var node in options.Nodes)
        {
            registry.Add(node);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterNodes(registry);
        }

        Nodes = registry.Build();
        index = Nodes.ToDictionary(static node => node.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<EdgeNodeDescriptor> Nodes { get; }

    public bool TryGet(string nodeId, out EdgeNodeDescriptor node)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);

        return index.TryGetValue(nodeId.Trim(), out node!);
    }
}
