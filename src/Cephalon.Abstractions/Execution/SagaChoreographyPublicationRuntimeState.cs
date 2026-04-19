namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Describes the latest operator-facing runtime state reported for one live saga-choreography
/// publication path.
/// </summary>
/// <param name="Id">
/// The stable runtime-state identifier for this observed choreography publication path.
/// </param>
/// <param name="BehaviorId">The stable choreography behavior identifier that produced the publication.</param>
/// <param name="PublicationId">The stable publication identifier declared by the choreography step.</param>
/// <param name="ChannelId">The logical channel or destination identifier used by the publication.</param>
/// <param name="EventType">The logical event type identifier used by the publication.</param>
/// <param name="OccurredAtUtc">The UTC timestamp carried by the observed publication itself.</param>
/// <param name="SourceModuleId">The owning module identifier when one is known at runtime.</param>
/// <param name="TransportIds">The transport identifiers that expose the owning choreography behavior.</param>
/// <param name="CorrelationId">The correlation identifier associated with the publication when one exists.</param>
/// <param name="TenantId">The tenant identifier associated with the publication when one exists.</param>
/// <param name="ContentType">The payload content type when one is known.</param>
/// <param name="IsCompensation">Indicates whether the publication represents compensation work.</param>
/// <param name="LastOutcome">The last reported publication outcome identifier when one exists.</param>
/// <param name="LastObservedAtUtc">The UTC timestamp when the latest publication observation was reported.</param>
/// <param name="LastPublisherType">
/// The last concrete publisher implementation type that accepted or rejected the publication when one
/// was reported.
/// </param>
/// <param name="AcceptedCount">The number of <c>accepted</c> observations reported so far.</param>
/// <param name="FailedCount">The number of <c>failed</c> observations reported so far.</param>
/// <param name="LastError">
/// The latest operator-facing error summary when the publication handoff reported a failure.
/// </param>
/// <param name="Metadata">The operator-facing metadata captured by the latest observation.</param>
public sealed record SagaChoreographyPublicationRuntimeState(
    string Id,
    string BehaviorId,
    string PublicationId,
    string ChannelId,
    string EventType,
    DateTimeOffset OccurredAtUtc,
    string? SourceModuleId,
    IReadOnlyList<string> TransportIds,
    string? CorrelationId,
    string? TenantId,
    string? ContentType,
    bool IsCompensation,
    string? LastOutcome,
    DateTimeOffset? LastObservedAtUtc,
    string? LastPublisherType,
    int AcceptedCount,
    int FailedCount,
    string? LastError,
    IReadOnlyDictionary<string, string> Metadata)
{
    /// <summary>
    /// Gets the total number of publication observations reported for this runtime-state entry.
    /// </summary>
    public int TotalReports => AcceptedCount + FailedCount;

    /// <summary>
    /// Gets a value indicating whether the latest report says the publication handoff succeeded.
    /// </summary>
    public bool IsAccepted => string.Equals(LastOutcome, "accepted", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the latest report says the publication handoff failed.
    /// </summary>
    public bool IsFailed => string.Equals(LastOutcome, "failed", StringComparison.OrdinalIgnoreCase);
}
