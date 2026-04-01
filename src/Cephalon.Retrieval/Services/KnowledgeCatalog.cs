using Cephalon.Retrieval.Configuration;

namespace Cephalon.Retrieval.Services;

internal sealed class KnowledgeCatalog : IKnowledgeCatalog
{
    private readonly Dictionary<string, KnowledgeCollectionDescriptor> index;

    public KnowledgeCatalog(
        RetrievalOptions options,
        IEnumerable<IKnowledgeCollectionContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contributors);

        var registry = new KnowledgeCollectionRegistry();
        foreach (var collection in options.Collections)
        {
            registry.Add(collection);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterCollections(registry);
        }

        Collections = registry.Build();
        index = Collections.ToDictionary(static collection => collection.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<KnowledgeCollectionDescriptor> Collections { get; }

    public bool TryGet(string collectionId, out KnowledgeCollectionDescriptor collection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionId);

        return index.TryGetValue(collectionId.Trim(), out collection!);
    }
}
