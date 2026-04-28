namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines built-in tenant-governance action workflow commands.
/// </summary>
public static class TenantGovernanceActionWorkflowCommands
{
    /// <summary>
    /// Creates a pending tenant-governance action.
    /// </summary>
    public const string Request = "request";

    /// <summary>
    /// Approves a pending tenant-governance action.
    /// </summary>
    public const string Approve = "approve";

    /// <summary>
    /// Rejects a pending or remediation-required tenant-governance action.
    /// </summary>
    public const string Reject = "reject";

    /// <summary>
    /// Marks a pending or approved tenant-governance action as requiring remediation.
    /// </summary>
    public const string RequireRemediation = "require-remediation";

    /// <summary>
    /// Marks a remediation-required tenant-governance action as remediated.
    /// </summary>
    public const string MarkRemediated = "mark-remediated";

    /// <summary>
    /// Expires a non-terminal tenant-governance action.
    /// </summary>
    public const string Expire = "expire";
}
