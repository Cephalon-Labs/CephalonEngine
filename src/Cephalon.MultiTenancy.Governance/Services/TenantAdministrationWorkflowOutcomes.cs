namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable tenant-administration workflow outcomes.
/// </summary>
public static class TenantAdministrationWorkflowOutcomes
{
    /// <summary>
    /// The requested tenant-administration command was applied.
    /// </summary>
    public const string Applied = "applied";

    /// <summary>
    /// Tenant-administration workflow execution is disabled.
    /// </summary>
    public const string Disabled = "disabled";

    /// <summary>
    /// A membership command was missing its required principal target.
    /// </summary>
    public const string MembershipTargetRequired = "membership-target-required";

    /// <summary>
    /// A membership command targeted a membership that does not exist.
    /// </summary>
    public const string MembershipNotFound = "membership-not-found";

    /// <summary>
    /// An invitation command was missing its required invitation target.
    /// </summary>
    public const string InvitationTargetRequired = "invitation-target-required";

    /// <summary>
    /// An invitation command targeted an invitation that does not exist.
    /// </summary>
    public const string InvitationNotFound = "invitation-not-found";

    /// <summary>
    /// An invitation command targeted an invitation whose state cannot transition through the requested command.
    /// </summary>
    public const string InvalidInvitationState = "invalid-invitation-state";

    /// <summary>
    /// A governance store failed before the requested command could be reported as applied.
    /// </summary>
    public const string StoreFailed = "store-failed";
}
