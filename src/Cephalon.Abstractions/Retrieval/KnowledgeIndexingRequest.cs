namespace Cephalon.Abstractions.Retrieval;

/// <summary>
/// Describes a managed indexing request for one knowledge collection.
/// </summary>
public sealed class KnowledgeIndexingRequest
{
    /// <summary>
    /// Creates a managed indexing request.
    /// </summary>
    /// <param name="collectionId">The collection identifier to index.</param>
    /// <param name="runId">The stable run identifier for this indexing attempt.</param>
    /// <param name="actorId">The optional actor that requested indexing.</param>
    /// <param name="correlationId">The optional correlation identifier for this indexing attempt.</param>
    /// <param name="requestedAtUtc">The optional UTC timestamp when indexing was requested.</param>
    /// <param name="metadata">Optional operator-facing metadata attached to the indexing request.</param>
    public KnowledgeIndexingRequest(
        string collectionId,
        string runId,
        string? actorId = null,
        string? correlationId = null,
        DateTimeOffset? requestedAtUtc = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(collectionId))
        {
            throw new ArgumentException("Collection id is required.", nameof(collectionId));
        }

        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("Run id is required.", nameof(runId));
        }

        CollectionId = collectionId.Trim();
        RunId = runId.Trim();
        ActorId = string.IsNullOrWhiteSpace(actorId) ? null : actorId.Trim();
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        RequestedAtUtc = requestedAtUtc.GetValueOrDefault(DateTimeOffset.UtcNow);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the collection identifier to index.
    /// </summary>
    public string CollectionId { get; }

    /// <summary>
    /// Gets the stable run identifier for this indexing attempt.
    /// </summary>
    public string RunId { get; }

    /// <summary>
    /// Gets the optional actor that requested indexing.
    /// </summary>
    public string? ActorId { get; }

    /// <summary>
    /// Gets the optional correlation identifier for this indexing attempt.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets the UTC timestamp when indexing was requested.
    /// </summary>
    public DateTimeOffset RequestedAtUtc { get; }

    /// <summary>
    /// Gets optional operator-facing metadata attached to the indexing request.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
