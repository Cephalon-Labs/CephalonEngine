namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stable tenant-domain ownership proof publication planning outcomes.
/// </summary>
public static class TenantDomainOwnershipProofPublicationPlanOutcomes
{
    /// <summary>
    /// Publication instructions were generated.
    /// </summary>
    public const string Planned = "planned";

    /// <summary>
    /// Publication planning is disabled by governance options.
    /// </summary>
    public const string Disabled = "disabled";

    /// <summary>
    /// No tenant-domain ownership declaration matched the supplied tenant and domain.
    /// </summary>
    public const string NotFound = "not-found";

    /// <summary>
    /// The domain is already declared for a different tenant.
    /// </summary>
    public const string TenantMismatch = "tenant-mismatch";

    /// <summary>
    /// The requested verification method does not match the existing declaration.
    /// </summary>
    public const string VerificationMethodMismatch = "verification-method-mismatch";

    /// <summary>
    /// Expected proof metadata is missing from the tenant-domain ownership declaration.
    /// </summary>
    public const string MissingExpectedProof = "missing-expected-proof";

    /// <summary>
    /// The verification method does not have built-in publication instructions.
    /// </summary>
    public const string UnsupportedVerificationMethod = "unsupported-verification-method";

    /// <summary>
    /// Runtime state could not be persisted.
    /// </summary>
    public const string StoreFailed = "store-failed";
}
