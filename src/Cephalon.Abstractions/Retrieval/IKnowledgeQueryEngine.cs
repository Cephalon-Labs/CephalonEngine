namespace Cephalon.Abstractions.Retrieval;

/// <summary>
/// Executes managed retrieval queries over indexed knowledge documents.
/// </summary>
public interface IKnowledgeQueryEngine
{
    /// <summary>
    /// Queries the active managed index for a collection.
    /// </summary>
    /// <param name="request">The query request to execute.</param>
    /// <param name="cancellationToken">A token that can cancel the query execution.</param>
    /// <returns>The query result, including any ranked matches found in the current index.</returns>
    ValueTask<KnowledgeQueryResult> QueryAsync(
        KnowledgeQueryRequest request,
        CancellationToken cancellationToken = default);
}
