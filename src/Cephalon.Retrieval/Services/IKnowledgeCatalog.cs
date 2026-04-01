namespace Cephalon.Retrieval.Services;

/// <summary>
/// Exposes the merged set of knowledge collections available to the active retrieval runtime.
/// </summary>
public interface IKnowledgeCatalog
{
    /// <summary>
    /// Gets the effective collection set after host options and module contributors have both been applied.
    /// </summary>
    IReadOnlyList<KnowledgeCollectionDescriptor> Collections { get; }

    /// <summary>
    /// Attempts to resolve a knowledge collection descriptor by identifier.
    /// </summary>
    /// <param name="collectionId">The collection identifier to resolve.</param>
    /// <param name="collection">When this method returns, contains the resolved collection if found.</param>
    /// <returns><see langword="true" /> when the collection exists; otherwise <see langword="false" />.</returns>
    bool TryGet(string collectionId, out KnowledgeCollectionDescriptor collection);
}
