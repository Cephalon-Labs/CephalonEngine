namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Applies host-agnostic tenant-governance action workflow transitions.
/// </summary>
public interface ITenantGovernanceActionWorkflow
{
    /// <summary>
    /// Applies one tenant-governance action workflow transition.
    /// </summary>
    /// <param name="request">The workflow transition request.</param>
    /// <param name="cancellationToken">A token that cancels the transition.</param>
    /// <returns>The workflow transition result.</returns>
    ValueTask<TenantGovernanceActionWorkflowResult> ApplyAsync(
        TenantGovernanceActionWorkflowRequest request,
        CancellationToken cancellationToken = default);
}
