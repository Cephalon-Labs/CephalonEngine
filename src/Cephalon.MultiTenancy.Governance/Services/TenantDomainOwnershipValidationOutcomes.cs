namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable outcomes returned by tenant-domain ownership validation.
/// </summary>
public static class TenantDomainOwnershipValidationOutcomes
{
    /// <summary>
    /// The domain ownership descriptor is verified and satisfies the validation request.
    /// </summary>
    public const string Valid = "valid";

    /// <summary>
    /// No domain ownership descriptor matched the supplied domain.
    /// </summary>
    public const string NotFound = "not-found";

    /// <summary>
    /// The domain exists but belongs to a different tenant.
    /// </summary>
    public const string TenantMismatch = "tenant-mismatch";

    /// <summary>
    /// The domain ownership descriptor is still pending verification.
    /// </summary>
    public const string Pending = "pending";

    /// <summary>
    /// The domain ownership descriptor was rejected.
    /// </summary>
    public const string Rejected = "rejected";

    /// <summary>
    /// The domain ownership descriptor is suspended.
    /// </summary>
    public const string Suspended = "suspended";

    /// <summary>
    /// The domain ownership descriptor is expired or outside its valid time window.
    /// </summary>
    public const string Expired = "expired";

    /// <summary>
    /// Domain ownership validation is disabled by host configuration.
    /// </summary>
    public const string Disabled = "disabled";
}
