namespace Cephalon.Retrieval.Services;

internal sealed class KnowledgeCollectionRegistry : IKnowledgeCollectionRegistry
{
    private readonly List<KnowledgeCollectionDescriptor> collections = [];

    public void Add(KnowledgeCollectionDescriptor collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        collections.Add(collection);
    }

    public IReadOnlyList<KnowledgeCollectionDescriptor> Build()
    {
        return collections
            .GroupBy(static collection => collection.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(static collection => collection.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
