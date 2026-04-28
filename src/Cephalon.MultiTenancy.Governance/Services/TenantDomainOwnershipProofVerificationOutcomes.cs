namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stable tenant-domain ownership proof verification runner outcome labels.
/// </summary>
public static class TenantDomainOwnershipProofVerificationOutcomes
{
    /// <summary>
    /// The observed proof matched and the domain ownership declaration was verified.
    /// </summary>
    public const string Verified = "verified";

    /// <summary>
    /// The observed proof mismatched and the domain ownership declaration was rejected.
    /// </summary>
    public const string Rejected = "rejected";

    /// <summary>
    /// A new or refreshed proof challenge was issued and publication instructions were produced.
    /// </summary>
    public const string ChallengeIssued = "challenge-issued";

    /// <summary>
    /// Publication instructions were produced, but no observed proof was available to evaluate.
    /// </summary>
    public const string PublicationPlanned = "publication-planned";

    /// <summary>
    /// The domain ownership declaration was already verified and no new proof run was needed.
    /// </summary>
    public const string AlreadyVerified = "already-verified";

    /// <summary>
    /// Proof verification runner execution is disabled by governance options.
    /// </summary>
    public const string Disabled = "disabled";

    /// <summary>
    /// No tenant-domain ownership declaration matched the supplied tenant and domain.
    /// </summary>
    public const string NotFound = "not-found";

    /// <summary>
    /// A declaration for the supplied domain belongs to a different tenant.
    /// </summary>
    public const string TenantMismatch = "tenant-mismatch";

    /// <summary>
    /// The matching domain ownership declaration uses a different verification method.
    /// </summary>
    public const string VerificationMethodMismatch = "verification-method-mismatch";

    /// <summary>
    /// Expected proof metadata is missing and challenge issuance was not available or not requested.
    /// </summary>
    public const string MissingExpectedProof = "missing-expected-proof";

    /// <summary>
    /// No observed proof was supplied and no built-in collector can collect the requested method.
    /// </summary>
    public const string MissingObservedProof = "missing-observed-proof";

    /// <summary>
    /// The requested verification method is not supported by the runner.
    /// </summary>
    public const string UnsupportedVerificationMethod = "unsupported-verification-method";

    /// <summary>
    /// Challenge issuance failed before proof verification could proceed.
    /// </summary>
    public const string ChallengeFailed = "challenge-failed";

    /// <summary>
    /// Publication planning failed before proof verification could proceed.
    /// </summary>
    public const string PublicationPlanFailed = "publication-plan-failed";

    /// <summary>
    /// HTTP proof collection is required but the built-in collector is not registered.
    /// </summary>
    public const string HttpCollectionUnavailable = "http-collection-unavailable";

    /// <summary>
    /// HTTP proof collection failed before a proof could be evaluated.
    /// </summary>
    public const string HttpCollectionFailed = "http-collection-failed";

    /// <summary>
    /// Proof evaluation failed before a terminal workflow outcome could be applied.
    /// </summary>
    public const string EvaluationFailed = "evaluation-failed";

    /// <summary>
    /// Runtime state could not be persisted.
    /// </summary>
    public const string StoreFailed = "store-failed";
}
