namespace Cephalon.Retrieval.Services;

/// <summary>
/// Loads documents for one knowledge collection so the retrieval runtime can build a managed index.
/// </summary>
public interface IKnowledgeDocumentProvider
{
    /// <summary>
    /// Gets the collection identifier served by this provider.
    /// </summary>
    string CollectionId { get; }

    /// <summary>
    /// Loads the current document set for the requested collection.
    /// </summary>
    /// <param name="context">The provider context for the current indexing request.</param>
    /// <param name="cancellationToken">A token that can cancel the document load.</param>
    /// <returns>The current document set that should replace the collection index.</returns>
    ValueTask<IReadOnlyList<KnowledgeDocument>> LoadDocumentsAsync(
        KnowledgeDocumentProviderContext context,
        CancellationToken cancellationToken = default);
}
