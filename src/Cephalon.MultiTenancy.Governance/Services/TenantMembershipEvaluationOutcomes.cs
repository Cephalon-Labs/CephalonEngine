namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable outcomes returned by tenant-membership evaluation.
/// </summary>
public static class TenantMembershipEvaluationOutcomes
{
    /// <summary>
    /// Gets the outcome used when membership evaluation grants access.
    /// </summary>
    public const string Allowed = "allowed";

    /// <summary>
    /// Gets the outcome used when no matching membership exists.
    /// </summary>
    public const string NoMembership = "no-membership";

    /// <summary>
    /// Gets the outcome used when matching memberships are suspended.
    /// </summary>
    public const string Suspended = "suspended";

    /// <summary>
    /// Gets the outcome used when matching memberships are expired or outside their validity window.
    /// </summary>
    public const string Expired = "expired";

    /// <summary>
    /// Gets the outcome used when matching memberships do not satisfy required roles.
    /// </summary>
    public const string MissingRole = "missing-role";

    /// <summary>
    /// Gets the outcome used when membership evaluation is disabled.
    /// </summary>
    public const string Disabled = "disabled";
}
