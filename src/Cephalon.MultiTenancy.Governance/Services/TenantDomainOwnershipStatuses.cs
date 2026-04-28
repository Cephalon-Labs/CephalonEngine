namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable tenant-domain ownership statuses understood by the governance runtime.
/// </summary>
public static class TenantDomainOwnershipStatuses
{
    /// <summary>
    /// The tenant domain ownership has been verified and can be validated.
    /// </summary>
    public const string Verified = "verified";

    /// <summary>
    /// The tenant domain ownership is declared but not yet verified.
    /// </summary>
    public const string Pending = "pending";

    /// <summary>
    /// The tenant domain ownership proof was rejected.
    /// </summary>
    public const string Rejected = "rejected";

    /// <summary>
    /// The tenant domain ownership has been suspended.
    /// </summary>
    public const string Suspended = "suspended";

    /// <summary>
    /// The tenant domain ownership is no longer within its valid time window.
    /// </summary>
    public const string Expired = "expired";
}
