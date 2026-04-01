using Cephalon.Abstractions.Technologies;

namespace Cephalon.Edge.Services;

internal sealed class EdgeRuntimeSurfaceContributor(IEdgeNodeCatalog catalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "edge-native-delivery",
            surfaceId: "edge-nodes",
            displayName: "Edge Nodes",
            description: "Registered edge nodes available to the active edge runtime.",
            entries: catalog.Nodes
                .Select(node => new TechnologyRuntimeEntry(
                    id: node.Id,
                    displayName: node.DisplayName,
                    description: node.Description,
                    metadata: new Dictionary<string, string>
                    {
                        ["tags"] = string.Join(",", node.Tags)
                    }))
                .ToArray());
    }
}
