namespace Cephalon.Abstractions.Retrieval;

/// <summary>
/// Describes the result of a managed retrieval query.
/// </summary>
/// <param name="CollectionId">The collection identifier that was queried.</param>
/// <param name="QueryText">The query text supplied by the caller.</param>
/// <param name="QueriedAtUtc">The UTC timestamp when the query executed.</param>
/// <param name="Matches">The ranked matches returned by the managed query engine.</param>
/// <param name="TotalIndexedDocuments">The number of indexed documents available at query time.</param>
/// <param name="Metadata">Optional operator-facing metadata captured with the query result.</param>
public sealed record KnowledgeQueryResult(
    string CollectionId,
    string QueryText,
    DateTimeOffset QueriedAtUtc,
    IReadOnlyList<KnowledgeQueryMatch> Matches,
    int TotalIndexedDocuments,
    IReadOnlyDictionary<string, string> Metadata)
{
    /// <summary>
    /// Gets a value indicating whether the query returned at least one match.
    /// </summary>
    public bool HasMatches => Matches.Count > 0;
}
