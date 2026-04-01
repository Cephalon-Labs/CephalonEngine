using Cephalon.Abstractions.Technologies;

namespace Cephalon.Retrieval.Services;

internal sealed class RetrievalRuntimeSurfaceContributor(IKnowledgeCatalog catalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "knowledge-retrieval",
            surfaceId: "knowledge-collections",
            displayName: "Knowledge Collections",
            description: "Registered knowledge collections available to the active retrieval runtime.",
            entries: catalog.Collections
                .Select(collection => new TechnologyRuntimeEntry(
                    id: collection.Id,
                    displayName: collection.DisplayName,
                    description: collection.Description,
                    metadata: new Dictionary<string, string>
                    {
                        ["tags"] = string.Join(",", collection.Tags)
                    }))
                .ToArray());
    }
}
