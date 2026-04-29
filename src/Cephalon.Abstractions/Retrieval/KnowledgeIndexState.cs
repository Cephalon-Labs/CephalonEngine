namespace Cephalon.Abstractions.Retrieval;

/// <summary>
/// Describes the latest managed index and query execution state for one knowledge collection.
/// </summary>
/// <param name="CollectionId">The collection identifier represented by this state.</param>
/// <param name="LastRunId">The latest indexing run identifier when one has been observed.</param>
/// <param name="LastOutcome">The latest indexing outcome when one has been observed.</param>
/// <param name="LastObservedAtUtc">The UTC timestamp when the latest indexing observation was recorded.</param>
/// <param name="LastIndexedAtUtc">The UTC timestamp when the latest replacement index was published.</param>
/// <param name="SourceFreshnessUtc">The newest source-document timestamp observed during the latest successful indexing run.</param>
/// <param name="DocumentCount">The number of documents currently stored in the managed index.</param>
/// <param name="FreshnessState">The operator-facing freshness state captured for the latest indexing observation.</param>
/// <param name="StartedCount">The number of indexing runs that have started.</param>
/// <param name="SucceededCount">The number of indexing runs that have completed successfully.</param>
/// <param name="FailedCount">The number of indexing runs that have failed.</param>
/// <param name="SkippedCount">The number of indexing runs that have been skipped.</param>
/// <param name="QueryCount">The number of managed queries executed against this collection.</param>
/// <param name="LastQueriedAtUtc">The UTC timestamp when the latest query was executed.</param>
/// <param name="LastQueryFingerprint">A non-reversible SHA-256 fingerprint of the latest query text.</param>
/// <param name="LastQueryLength">The character length of the latest query text.</param>
/// <param name="LastQueryMatchedCount">The number of matches returned by the latest query.</param>
/// <param name="LastActorId">The latest actor identifier when one was supplied by an indexing request.</param>
/// <param name="LastCorrelationId">The latest correlation identifier when one was supplied by an indexing request.</param>
/// <param name="LastError">The latest operator-facing error summary when indexing failed.</param>
/// <param name="Metadata">Optional operator-facing metadata captured with the latest indexing observation.</param>
public sealed record KnowledgeIndexState(
    string CollectionId,
    string? LastRunId,
    string? LastOutcome,
    DateTimeOffset? LastObservedAtUtc,
    DateTimeOffset? LastIndexedAtUtc,
    DateTimeOffset? SourceFreshnessUtc,
    int DocumentCount,
    string FreshnessState,
    int StartedCount,
    int SucceededCount,
    int FailedCount,
    int SkippedCount,
    int QueryCount,
    DateTimeOffset? LastQueriedAtUtc,
    string? LastQueryFingerprint,
    int LastQueryLength,
    int LastQueryMatchedCount,
    string? LastActorId,
    string? LastCorrelationId,
    string? LastError,
    IReadOnlyDictionary<string, string> Metadata)
{
    /// <summary>
    /// Gets the total number of indexing observations recorded for this collection.
    /// </summary>
    public int TotalIndexRuns => StartedCount + SucceededCount + FailedCount + SkippedCount;
}
