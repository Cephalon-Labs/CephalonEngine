namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Decides whether a tenant-governance action can proceed against the active governance runtime.
/// </summary>
public interface ITenantGovernanceActionDecider
{
    /// <summary>
    /// Decides one tenant-governance action request.
    /// </summary>
    /// <param name="request">The decision request.</param>
    /// <param name="cancellationToken">A token that cancels decision evaluation.</param>
    /// <returns>The decision result.</returns>
    ValueTask<TenantGovernanceActionDecisionResult> DecideAsync(
        TenantGovernanceActionDecisionRequest request,
        CancellationToken cancellationToken = default);
}
