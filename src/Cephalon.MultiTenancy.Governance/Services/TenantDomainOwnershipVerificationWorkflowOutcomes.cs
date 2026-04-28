namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable tenant-domain ownership verification workflow transition outcomes.
/// </summary>
public static class TenantDomainOwnershipVerificationWorkflowOutcomes
{
    /// <summary>
    /// The workflow transition created a new pending tenant-domain ownership declaration.
    /// </summary>
    public const string Created = "created";

    /// <summary>
    /// The workflow transition updated an existing tenant-domain ownership declaration.
    /// </summary>
    public const string Applied = "applied";

    /// <summary>
    /// No tenant-domain ownership declaration matched the supplied identifiers.
    /// </summary>
    public const string NotFound = "not-found";

    /// <summary>
    /// The matching tenant-domain ownership declaration belongs to a different tenant.
    /// </summary>
    public const string TenantMismatch = "tenant-mismatch";

    /// <summary>
    /// The matching tenant-domain ownership declaration uses a different verification method.
    /// </summary>
    public const string VerificationMethodMismatch = "verification-method-mismatch";

    /// <summary>
    /// The requested workflow transition is not valid from the current domain ownership status.
    /// </summary>
    public const string InvalidTransition = "invalid-transition";

    /// <summary>
    /// The requested workflow transition could not be persisted.
    /// </summary>
    public const string StoreFailed = "store-failed";

    /// <summary>
    /// Tenant-domain ownership verification workflow execution is disabled.
    /// </summary>
    public const string Disabled = "disabled";
}
