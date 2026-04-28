namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable tenant-governance action kinds understood by the governance runtime.
/// </summary>
public static class TenantGovernanceActionKinds
{
    /// <summary>
    /// A governance action that changes tenant membership.
    /// </summary>
    public const string MembershipChange = "membership-change";

    /// <summary>
    /// A governance action that changes an invitation lifecycle.
    /// </summary>
    public const string InvitationLifecycle = "invitation-lifecycle";

    /// <summary>
    /// A governance action that changes declared tenant-domain ownership posture.
    /// </summary>
    public const string DomainOwnership = "domain-ownership";

    /// <summary>
    /// A governance action that represents an operator remediation.
    /// </summary>
    public const string Remediation = "remediation";

    /// <summary>
    /// A governance action that represents tenant administration.
    /// </summary>
    public const string TenantAdministration = "tenant-administration";
}
