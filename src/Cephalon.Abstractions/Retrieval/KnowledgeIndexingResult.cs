namespace Cephalon.Abstractions.Retrieval;

/// <summary>
/// Describes the result of a managed indexing attempt.
/// </summary>
/// <param name="CollectionId">The collection identifier that was indexed.</param>
/// <param name="RunId">The stable indexing run identifier.</param>
/// <param name="Outcome">The stable indexing outcome identifier.</param>
/// <param name="ObservedAtUtc">The UTC timestamp when the outcome was observed.</param>
/// <param name="IndexedAtUtc">The UTC timestamp when the replacement index was published.</param>
/// <param name="SourceFreshnessUtc">The newest source-document timestamp observed during indexing.</param>
/// <param name="DocumentCount">The number of documents stored in the replacement index.</param>
/// <param name="Error">The operator-facing error summary when indexing failed.</param>
/// <param name="Metadata">Optional operator-facing metadata captured with the result.</param>
public sealed record KnowledgeIndexingResult(
    string CollectionId,
    string RunId,
    string Outcome,
    DateTimeOffset ObservedAtUtc,
    DateTimeOffset? IndexedAtUtc,
    DateTimeOffset? SourceFreshnessUtc,
    int DocumentCount,
    string? Error,
    IReadOnlyDictionary<string, string> Metadata);
