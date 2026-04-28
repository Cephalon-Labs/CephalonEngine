namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable metadata keys used by tenant-domain ownership proof evaluation.
/// </summary>
public static class TenantDomainOwnershipProofMetadataKeys
{
    /// <summary>
    /// Generic expected proof value used when a method-specific expected value is not present.
    /// </summary>
    public const string ExpectedProof = "expectedProof";

    /// <summary>
    /// Expected DNS TXT proof value for DNS-based domain ownership verification.
    /// </summary>
    public const string ExpectedDnsTxtProof = "expectedDnsTxtProof";

    /// <summary>
    /// Expected HTTP file or well-known endpoint proof value for HTTP-based domain ownership verification.
    /// </summary>
    public const string ExpectedHttpFileProof = "expectedHttpFileProof";

    /// <summary>
    /// Last proof evaluation outcome recorded on the domain ownership descriptor.
    /// </summary>
    public const string LastProofEvaluationOutcome = "lastProofEvaluationOutcome";

    /// <summary>
    /// Source that reported the observed proof evidence.
    /// </summary>
    public const string LastProofEvaluationSource = "lastProofEvaluationSource";

    /// <summary>
    /// SHA-256 fingerprint of the observed proof value considered by the evaluator.
    /// </summary>
    public const string LastProofEvaluationObservedFingerprint = "lastProofEvaluationObservedFingerprint";

    /// <summary>
    /// SHA-256 fingerprint of the expected proof value considered by the evaluator.
    /// </summary>
    public const string LastProofEvaluationExpectedFingerprint = "lastProofEvaluationExpectedFingerprint";

    /// <summary>
    /// Actor that requested or reported the proof evaluation when known.
    /// </summary>
    public const string LastProofEvaluationActor = "lastProofEvaluationActor";

    /// <summary>
    /// Correlation identifier for the last proof evaluation when known.
    /// </summary>
    public const string LastProofEvaluationCorrelationId = "lastProofEvaluationCorrelationId";

    /// <summary>
    /// Ownership marker for proof evaluation performed by the governance companion.
    /// </summary>
    public const string ProofEvaluationOwnership = "proofEvaluationOwnership";
}
