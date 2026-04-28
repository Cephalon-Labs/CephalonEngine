namespace Cephalon.Retrieval.Services;

/// <summary>
/// Provides request context to a knowledge document provider during managed indexing.
/// </summary>
public sealed class KnowledgeDocumentProviderContext
{
    /// <summary>
    /// Creates provider context for a managed indexing request.
    /// </summary>
    /// <param name="collection">The collection being indexed.</param>
    /// <param name="runId">The stable indexing run identifier.</param>
    /// <param name="requestedAtUtc">The UTC timestamp when indexing was requested.</param>
    /// <param name="actorId">The optional actor that requested indexing.</param>
    /// <param name="correlationId">The optional correlation identifier for the indexing run.</param>
    /// <param name="metadata">Optional operator-facing metadata attached to the indexing request.</param>
    public KnowledgeDocumentProviderContext(
        KnowledgeCollectionDescriptor collection,
        string runId,
        DateTimeOffset requestedAtUtc,
        string? actorId = null,
        string? correlationId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(collection);

        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("Run id is required.", nameof(runId));
        }

        Collection = collection;
        RunId = runId.Trim();
        RequestedAtUtc = requestedAtUtc == default ? DateTimeOffset.UtcNow : requestedAtUtc;
        ActorId = string.IsNullOrWhiteSpace(actorId) ? null : actorId.Trim();
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the collection being indexed.
    /// </summary>
    public KnowledgeCollectionDescriptor Collection { get; }

    /// <summary>
    /// Gets the stable indexing run identifier.
    /// </summary>
    public string RunId { get; }

    /// <summary>
    /// Gets the UTC timestamp when indexing was requested.
    /// </summary>
    public DateTimeOffset RequestedAtUtc { get; }

    /// <summary>
    /// Gets the optional actor that requested indexing.
    /// </summary>
    public string? ActorId { get; }

    /// <summary>
    /// Gets the optional correlation identifier for the indexing run.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets optional operator-facing metadata attached to the indexing request.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
