namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable tenant-domain ownership proof evaluation outcome labels.
/// </summary>
public static class TenantDomainOwnershipProofEvaluationOutcomes
{
    /// <summary>
    /// The observed proof matched the expected proof and the domain ownership was verified.
    /// </summary>
    public const string Verified = "verified";

    /// <summary>
    /// The observed proof did not match the expected proof and the domain ownership was rejected.
    /// </summary>
    public const string Rejected = "rejected";

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
    /// The request and descriptor did not contain an expected proof value to compare against.
    /// </summary>
    public const string MissingExpectedProof = "missing-expected-proof";

    /// <summary>
    /// The request did not contain an observed proof value to evaluate.
    /// </summary>
    public const string MissingObservedProof = "missing-observed-proof";

    /// <summary>
    /// Proof evaluation matched or mismatched, but the verification workflow refused or failed the transition.
    /// </summary>
    public const string WorkflowDenied = "workflow-denied";

    /// <summary>
    /// The built-in proof evaluator is disabled.
    /// </summary>
    public const string Disabled = "disabled";
}
