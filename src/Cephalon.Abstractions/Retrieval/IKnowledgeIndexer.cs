namespace Cephalon.Abstractions.Retrieval;

/// <summary>
/// Builds and replaces managed indexes for registered knowledge collections.
/// </summary>
public interface IKnowledgeIndexer
{
    /// <summary>
    /// Indexes the documents supplied by providers for the requested collection.
    /// </summary>
    /// <param name="request">The indexing request to execute.</param>
    /// <param name="cancellationToken">A token that can cancel the indexing run.</param>
    /// <returns>The final indexing result recorded for operator introspection.</returns>
    ValueTask<KnowledgeIndexingResult> IndexAsync(
        KnowledgeIndexingRequest request,
        CancellationToken cancellationToken = default);
}
