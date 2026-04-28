namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Applies host-agnostic tenant administration commands over Cephalon-managed governance stores.
/// </summary>
public interface ITenantAdministrationWorkflow
{
    /// <summary>
    /// Applies one tenant administration command.
    /// </summary>
    /// <param name="request">The tenant administration command request.</param>
    /// <param name="cancellationToken">A token that cancels the command.</param>
    /// <returns>The evaluated tenant administration command result.</returns>
    ValueTask<TenantAdministrationWorkflowResult> ApplyAsync(
        TenantAdministrationWorkflowRequest request,
        CancellationToken cancellationToken = default);
}
