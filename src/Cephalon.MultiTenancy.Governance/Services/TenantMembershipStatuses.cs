namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable tenant-membership status identifiers.
/// </summary>
public static class TenantMembershipStatuses
{
    /// <summary>
    /// Gets the status used when the membership can participate in evaluation.
    /// </summary>
    public const string Active = "active";

    /// <summary>
    /// Gets the status used when the membership exists but is blocked from evaluation.
    /// </summary>
    public const string Suspended = "suspended";

    /// <summary>
    /// Gets the status used when the membership has intentionally expired.
    /// </summary>
    public const string Expired = "expired";
}
