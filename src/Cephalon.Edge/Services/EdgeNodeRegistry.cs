namespace Cephalon.Edge.Services;

internal sealed class EdgeNodeRegistry : IEdgeNodeRegistry
{
    private readonly List<EdgeNodeDescriptor> nodes = [];

    public void Add(EdgeNodeDescriptor node)
    {
        ArgumentNullException.ThrowIfNull(node);

        nodes.Add(node);
    }

    public IReadOnlyList<EdgeNodeDescriptor> Build()
    {
        return nodes
            .GroupBy(static node => node.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(static node => node.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
