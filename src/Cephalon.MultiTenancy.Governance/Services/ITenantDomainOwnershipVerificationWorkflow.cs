namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Applies in-process tenant-domain ownership verification workflow transitions.
/// </summary>
public interface ITenantDomainOwnershipVerificationWorkflow
{
    /// <summary>
    /// Applies one tenant-domain ownership verification workflow command.
    /// </summary>
    /// <param name="request">The workflow transition request to evaluate.</param>
    /// <param name="cancellationToken">A cancellation token for the workflow operation.</param>
    /// <returns>The evaluated workflow transition result.</returns>
    ValueTask<TenantDomainOwnershipVerificationWorkflowResult> ApplyAsync(
        TenantDomainOwnershipVerificationWorkflowRequest request,
        CancellationToken cancellationToken = default);
}
