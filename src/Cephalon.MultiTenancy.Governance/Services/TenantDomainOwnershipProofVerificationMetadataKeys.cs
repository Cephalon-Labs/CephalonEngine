namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stable metadata keys returned by tenant-domain ownership proof verification runs.
/// </summary>
public static class TenantDomainOwnershipProofVerificationMetadataKeys
{
    /// <summary>
    /// Metadata key for the latest proof verification runner outcome.
    /// </summary>
    public const string LastProofVerificationOutcome = "lastProofVerificationOutcome";

    /// <summary>
    /// Metadata key for the source that requested the latest proof verification run.
    /// </summary>
    public const string LastProofVerificationSource = "lastProofVerificationSource";

    /// <summary>
    /// Metadata key for the actor that requested the latest proof verification run.
    /// </summary>
    public const string LastProofVerificationActor = "lastProofVerificationActor";

    /// <summary>
    /// Metadata key for the correlation identifier attached to the latest proof verification run.
    /// </summary>
    public const string LastProofVerificationCorrelationId = "lastProofVerificationCorrelationId";

    /// <summary>
    /// Metadata key for the UTC timestamp when the latest proof verification run executed.
    /// </summary>
    public const string LastProofVerificationRanAtUtc = "lastProofVerificationRanAtUtc";

    /// <summary>
    /// Metadata key for the challenge issuance outcome observed by the latest proof verification run.
    /// </summary>
    public const string LastProofVerificationChallengeOutcome = "lastProofVerificationChallengeOutcome";

    /// <summary>
    /// Metadata key for the publication planning outcome observed by the latest proof verification run.
    /// </summary>
    public const string LastProofVerificationPublicationPlanOutcome = "lastProofVerificationPublicationPlanOutcome";

    /// <summary>
    /// Metadata key for the HTTP proof collection outcome observed by the latest proof verification run.
    /// </summary>
    public const string LastProofVerificationHttpCollectionOutcome = "lastProofVerificationHttpCollectionOutcome";

    /// <summary>
    /// Metadata key for the DNS TXT proof collection outcome observed by the latest proof verification run.
    /// </summary>
    public const string LastProofVerificationDnsTxtCollectionOutcome = "lastProofVerificationDnsTxtCollectionOutcome";

    /// <summary>
    /// Metadata key for the proof evaluation outcome observed by the latest proof verification run.
    /// </summary>
    public const string LastProofVerificationEvaluationOutcome = "lastProofVerificationEvaluationOutcome";

    /// <summary>
    /// Metadata key for proof verification runner ownership.
    /// </summary>
    public const string ProofVerificationRunnerOwnership = "proofVerificationRunnerOwnership";

    /// <summary>
    /// Metadata key for HTTP proof collection ownership.
    /// </summary>
    public const string HttpProofCollectionOwnership = "httpProofCollectionOwnership";

    /// <summary>
    /// Metadata key for DNS TXT proof collection ownership.
    /// </summary>
    public const string DnsTxtProofCollectionOwnership = "dnsTxtProofCollectionOwnership";

    /// <summary>
    /// Metadata key for external proof polling ownership.
    /// </summary>
    public const string ExternalProofPollingOwnership = "externalProofPollingOwnership";
}
