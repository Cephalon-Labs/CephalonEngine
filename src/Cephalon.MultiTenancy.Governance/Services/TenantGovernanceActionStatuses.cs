namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable tenant-governance action statuses understood by the governance runtime.
/// </summary>
public static class TenantGovernanceActionStatuses
{
    /// <summary>
    /// The action is declared but still waiting for approval.
    /// </summary>
    public const string PendingApproval = "pending-approval";

    /// <summary>
    /// The action has been approved and can be decided as allowed.
    /// </summary>
    public const string Approved = "approved";

    /// <summary>
    /// The action was rejected.
    /// </summary>
    public const string Rejected = "rejected";

    /// <summary>
    /// The action requires remediation before it can proceed.
    /// </summary>
    public const string RemediationRequired = "remediation-required";

    /// <summary>
    /// The action has been remediated and can be decided as allowed.
    /// </summary>
    public const string Remediated = "remediated";

    /// <summary>
    /// The action is no longer within its valid time window.
    /// </summary>
    public const string Expired = "expired";
}
