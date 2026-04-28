namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stable metadata keys emitted by tenant-domain ownership proof polling.
/// </summary>
public static class TenantDomainOwnershipProofPollingMetadataKeys
{
    /// <summary>
    /// Metadata key containing the latest proof polling outcome.
    /// </summary>
    public const string LastProofPollingOutcome = "lastProofPollingOutcome";

    /// <summary>
    /// Metadata key containing the latest proof polling timestamp.
    /// </summary>
    public const string LastProofPollingRanAtUtc = "lastProofPollingRanAtUtc";

    /// <summary>
    /// Metadata key containing the latest proof polling source.
    /// </summary>
    public const string LastProofPollingSource = "lastProofPollingSource";

    /// <summary>
    /// Metadata key containing the latest proof polling actor.
    /// </summary>
    public const string LastProofPollingActor = "lastProofPollingActor";

    /// <summary>
    /// Metadata key containing the latest proof polling correlation identifier.
    /// </summary>
    public const string LastProofPollingCorrelationId = "lastProofPollingCorrelationId";

    /// <summary>
    /// Metadata key containing the proof polling runner ownership mode.
    /// </summary>
    public const string ProofPollingRunnerOwnership = "proofPollingRunnerOwnership";

    /// <summary>
    /// Metadata key containing the on-demand external proof polling ownership mode.
    /// </summary>
    public const string ExternalProofPollingOwnership = "externalProofPollingOwnership";

    /// <summary>
    /// Metadata key containing the automatic background proof polling ownership mode.
    /// </summary>
    public const string BackgroundProofPollingOwnership = "backgroundProofPollingOwnership";

    /// <summary>
    /// Metadata key containing the number of declarations that matched the request filters before batch limiting.
    /// </summary>
    public const string CandidateCount = "candidateCount";

    /// <summary>
    /// Metadata key containing the number of proof verification attempts run during the polling pass.
    /// </summary>
    public const string VerificationCount = "verificationCount";

    /// <summary>
    /// Metadata key containing the number of declarations skipped by filters, missing expected proof policy, or batch limits.
    /// </summary>
    public const string SkippedCount = "skippedCount";

    /// <summary>
    /// Metadata key containing the number of declarations verified during the polling pass.
    /// </summary>
    public const string VerifiedCount = "verifiedCount";

    /// <summary>
    /// Metadata key containing the number of declarations rejected during the polling pass.
    /// </summary>
    public const string RejectedCount = "rejectedCount";

    /// <summary>
    /// Metadata key containing the number of polling attempts that did not reach an accepted terminal outcome.
    /// </summary>
    public const string FailedCount = "failedCount";

    /// <summary>
    /// Metadata key containing the effective batch limit used by the polling pass.
    /// </summary>
    public const string BatchLimit = "batchLimit";
}
