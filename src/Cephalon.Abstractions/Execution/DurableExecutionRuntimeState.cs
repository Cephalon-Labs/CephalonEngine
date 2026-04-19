namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Describes the latest operator-facing runtime state reported for one durable-execution stream.
/// </summary>
/// <param name="BehaviorId">The stable durable behavior identifier that owns the stream.</param>
/// <param name="StreamId">The stable event-stream identifier reported by the durable workflow.</param>
/// <param name="SourceModuleId">The owning module identifier when one is known at runtime.</param>
/// <param name="TransportIds">The transport identifiers that expose the durable workflow.</param>
/// <param name="LastOutcome">The last reported durable-execution outcome identifier when one exists.</param>
/// <param name="LastStage">The last reported durable-execution stage identifier when one exists.</param>
/// <param name="LastObservedAtUtc">The UTC timestamp when the latest observation was reported.</param>
/// <param name="LastReplayedVersion">
/// The latest stream version that was fully replayed before the durable step executed.
/// </param>
/// <param name="LastKnownVersion">
/// The latest stream version known after the reported durable step finished or failed.
/// </param>
/// <param name="LastHttpStatusCode">
/// The latest HTTP success status code returned by the durable execution strategy when one was reported.
/// </param>
/// <param name="LastAppendedEventCount">
/// The number of domain events appended by the latest successful durable step.
/// </param>
/// <param name="LastStepProducedOutput">
/// Indicates whether the latest successful durable step produced local output.
/// </param>
/// <param name="LastStepCompleted">
/// Indicates whether the latest reported durable step declared the workflow completed.
/// </param>
/// <param name="StartedCount">The number of <c>started</c> observations reported so far.</param>
/// <param name="SucceededCount">The number of <c>succeeded</c> observations reported so far.</param>
/// <param name="ContinuationCount">
/// The number of <c>continuation-staged</c> observations reported so far.
/// </param>
/// <param name="CompletedCount">The number of <c>completed</c> observations reported so far.</param>
/// <param name="FailedCount">The number of <c>failed</c> observations reported so far.</param>
/// <param name="LastError">
/// The latest operator-facing error summary when the durable step reported a failure.
/// </param>
/// <param name="Metadata">The operator-facing metadata captured by the latest report.</param>
public sealed record DurableExecutionRuntimeState(
    string BehaviorId,
    string StreamId,
    string? SourceModuleId,
    IReadOnlyList<string> TransportIds,
    string? LastOutcome,
    string? LastStage,
    DateTimeOffset? LastObservedAtUtc,
    long? LastReplayedVersion,
    long? LastKnownVersion,
    int? LastHttpStatusCode,
    int LastAppendedEventCount,
    bool LastStepProducedOutput,
    bool LastStepCompleted,
    int StartedCount,
    int SucceededCount,
    int ContinuationCount,
    int CompletedCount,
    int FailedCount,
    string? LastError,
    IReadOnlyDictionary<string, string> Metadata)
{
    /// <summary>
    /// Gets the total number of observations reported for this durable-execution stream.
    /// </summary>
    public int TotalReports => StartedCount + SucceededCount + ContinuationCount + CompletedCount + FailedCount;

    /// <summary>
    /// Gets a value indicating whether the latest report says the workflow still has continuation work pending.
    /// </summary>
    public bool ContinuationPending => string.Equals(LastOutcome, "continuation-staged", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the latest report says the durable stream is currently in a failed posture.
    /// </summary>
    public bool IsFailed => string.Equals(LastOutcome, "failed", StringComparison.OrdinalIgnoreCase);
}
