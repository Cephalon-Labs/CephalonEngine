namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stable tenant-domain ownership proof challenge issuance outcomes.
/// </summary>
public static class TenantDomainOwnershipProofChallengeOutcomes
{
    /// <summary>
    /// Challenge issuance created or refreshed the pending proof challenge.
    /// </summary>
    public const string Issued = "issued";

    /// <summary>
    /// Challenge issuance is disabled by governance options.
    /// </summary>
    public const string Disabled = "disabled";

    /// <summary>
    /// The domain is already declared for a different tenant.
    /// </summary>
    public const string TenantMismatch = "tenant-mismatch";

    /// <summary>
    /// The requested verification method does not match the existing declaration.
    /// </summary>
    public const string VerificationMethodMismatch = "verification-method-mismatch";

    /// <summary>
    /// The domain is already verified and does not need a new challenge.
    /// </summary>
    public const string AlreadyVerified = "already-verified";

    /// <summary>
    /// The current declaration status cannot receive a new proof challenge.
    /// </summary>
    public const string InvalidStatus = "invalid-status";

    /// <summary>
    /// Runtime state could not be persisted.
    /// </summary>
    public const string StoreFailed = "store-failed";
}
