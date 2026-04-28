namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable tenant-administration workflow command identifiers.
/// </summary>
public static class TenantAdministrationWorkflowCommands
{
    /// <summary>
    /// Creates or replaces an active tenant membership.
    /// </summary>
    public const string GrantMembership = "grant-membership";

    /// <summary>
    /// Suspends an existing tenant membership.
    /// </summary>
    public const string SuspendMembership = "suspend-membership";

    /// <summary>
    /// Expires an existing tenant membership.
    /// </summary>
    public const string ExpireMembership = "expire-membership";

    /// <summary>
    /// Creates or replaces a pending tenant invitation.
    /// </summary>
    public const string IssueInvitation = "issue-invitation";

    /// <summary>
    /// Accepts an existing pending tenant invitation.
    /// </summary>
    public const string AcceptInvitation = "accept-invitation";

    /// <summary>
    /// Revokes an existing pending tenant invitation.
    /// </summary>
    public const string RevokeInvitation = "revoke-invitation";

    /// <summary>
    /// Expires an existing tenant invitation.
    /// </summary>
    public const string ExpireInvitation = "expire-invitation";
}
