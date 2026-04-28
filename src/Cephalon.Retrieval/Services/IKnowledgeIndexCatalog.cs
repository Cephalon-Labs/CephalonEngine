namespace Cephalon.Retrieval.Services;

/// <summary>
/// Exposes operator-facing index and query execution state for the active retrieval runtime.
/// </summary>
public interface IKnowledgeIndexCatalog
{
    /// <summary>
    /// Gets the latest index state reported for registered knowledge collections.
    /// </summary>
    IReadOnlyList<KnowledgeIndexState> States { get; }

    /// <summary>
    /// Gets the latest index state for a collection when one exists.
    /// </summary>
    /// <param name="collectionId">The collection identifier to resolve.</param>
    /// <returns>The latest state for the collection, or <see langword="null" /> when no activity has been recorded.</returns>
    KnowledgeIndexState? GetByCollectionId(string collectionId);

    /// <summary>
    /// Attempts to resolve the latest index state for a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier to resolve.</param>
    /// <param name="state">When this method returns, contains the state if one was recorded.</param>
    /// <returns><see langword="true" /> when state exists; otherwise <see langword="false" />.</returns>
    bool TryGet(string collectionId, out KnowledgeIndexState? state);
}
