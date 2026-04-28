namespace Cephalon.Retrieval.Services;

/// <summary>
/// Describes a managed retrieval query request for one knowledge collection.
/// </summary>
public sealed class KnowledgeQueryRequest
{
    /// <summary>
    /// Creates a managed retrieval query request.
    /// </summary>
    /// <param name="collectionId">The collection identifier to query.</param>
    /// <param name="queryText">The text to search for.</param>
    /// <param name="maxResults">The optional maximum number of matches to return. When omitted, the runtime default is used.</param>
    /// <param name="actorId">The optional actor that requested the query.</param>
    /// <param name="correlationId">The optional correlation identifier for the query.</param>
    /// <param name="metadata">Optional request metadata used by provider-specific query engines.</param>
    public KnowledgeQueryRequest(
        string collectionId,
        string queryText,
        int? maxResults = null,
        string? actorId = null,
        string? correlationId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(collectionId))
        {
            throw new ArgumentException("Collection id is required.", nameof(collectionId));
        }

        if (string.IsNullOrWhiteSpace(queryText))
        {
            throw new ArgumentException("Query text is required.", nameof(queryText));
        }

        if (maxResults is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults), maxResults, "Max results must be greater than or equal to 1.");
        }

        CollectionId = collectionId.Trim();
        QueryText = queryText.Trim();
        MaxResults = maxResults;
        ActorId = string.IsNullOrWhiteSpace(actorId) ? null : actorId.Trim();
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the collection identifier to query.
    /// </summary>
    public string CollectionId { get; }

    /// <summary>
    /// Gets the text to search for.
    /// </summary>
    public string QueryText { get; }

    /// <summary>
    /// Gets the optional maximum number of matches to return.
    /// </summary>
    public int? MaxResults { get; }

    /// <summary>
    /// Gets the optional actor that requested the query.
    /// </summary>
    public string? ActorId { get; }

    /// <summary>
    /// Gets the optional correlation identifier for the query.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets optional request metadata used by provider-specific query engines.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
