namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable outcomes returned by tenant-governance action decisions.
/// </summary>
public static class TenantGovernanceActionDecisionOutcomes
{
    /// <summary>
    /// The governance action is approved or remediated and satisfies the decision request.
    /// </summary>
    public const string Allowed = "allowed";

    /// <summary>
    /// No governance action descriptor matched the supplied action.
    /// </summary>
    public const string NotFound = "not-found";

    /// <summary>
    /// The action exists but belongs to a different tenant.
    /// </summary>
    public const string TenantMismatch = "tenant-mismatch";

    /// <summary>
    /// The action exists but has a different action kind.
    /// </summary>
    public const string ActionKindMismatch = "action-kind-mismatch";

    /// <summary>
    /// The action exists but targets a different subject.
    /// </summary>
    public const string SubjectMismatch = "subject-mismatch";

    /// <summary>
    /// The action is still waiting for approval.
    /// </summary>
    public const string PendingApproval = "pending-approval";

    /// <summary>
    /// The action was rejected.
    /// </summary>
    public const string Rejected = "rejected";

    /// <summary>
    /// The action requires remediation before it can proceed.
    /// </summary>
    public const string RemediationRequired = "remediation-required";

    /// <summary>
    /// The action is expired or outside its valid time window.
    /// </summary>
    public const string Expired = "expired";

    /// <summary>
    /// Governance action decisions are disabled by host configuration.
    /// </summary>
    public const string Disabled = "disabled";
}
