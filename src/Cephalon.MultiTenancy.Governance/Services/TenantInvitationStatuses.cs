namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable tenant-invitation statuses understood by the governance runtime.
/// </summary>
public static class TenantInvitationStatuses
{
    /// <summary>
    /// The invitation can still be validated.
    /// </summary>
    public const string Pending = "pending";

    /// <summary>
    /// The invitation has already been accepted.
    /// </summary>
    public const string Accepted = "accepted";

    /// <summary>
    /// The invitation has been revoked before acceptance.
    /// </summary>
    public const string Revoked = "revoked";

    /// <summary>
    /// The invitation is no longer within its valid time window.
    /// </summary>
    public const string Expired = "expired";
}
