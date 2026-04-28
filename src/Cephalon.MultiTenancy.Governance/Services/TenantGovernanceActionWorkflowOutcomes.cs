namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines built-in tenant-governance action workflow transition outcomes.
/// </summary>
public static class TenantGovernanceActionWorkflowOutcomes
{
    /// <summary>
    /// The workflow transition created a new tenant-governance action.
    /// </summary>
    public const string Created = "created";

    /// <summary>
    /// The workflow transition updated an existing tenant-governance action.
    /// </summary>
    public const string Applied = "applied";

    /// <summary>
    /// No tenant-governance action matched the supplied identifiers.
    /// </summary>
    public const string NotFound = "not-found";

    /// <summary>
    /// The matching tenant-governance action belongs to a different tenant.
    /// </summary>
    public const string TenantMismatch = "tenant-mismatch";

    /// <summary>
    /// The matching tenant-governance action has a different action kind.
    /// </summary>
    public const string ActionKindMismatch = "action-kind-mismatch";

    /// <summary>
    /// The matching tenant-governance action has a different subject boundary.
    /// </summary>
    public const string SubjectMismatch = "subject-mismatch";

    /// <summary>
    /// The requested workflow transition is not valid from the current action status.
    /// </summary>
    public const string InvalidTransition = "invalid-transition";

    /// <summary>
    /// Tenant-governance action workflow execution is disabled.
    /// </summary>
    public const string Disabled = "disabled";
}
