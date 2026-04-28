namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Builds tenant-domain ownership proof publication instructions from an issued proof challenge.
/// </summary>
/// <remarks>
/// The planner owns deterministic instruction generation and optional runtime metadata recording. It does not
/// mutate DNS records, host HTTP proof files, or poll external endpoints; applications or provider packs publish
/// and observe the planned proof outside the engine.
/// </remarks>
public interface ITenantDomainOwnershipProofPublicationPlanner
{
    /// <summary>
    /// Builds publication instructions for a tenant-domain ownership proof challenge.
    /// </summary>
    /// <param name="request">The proof publication planning request.</param>
    /// <param name="cancellationToken">A token that cancels planning before runtime state is stored.</param>
    /// <returns>The publication instructions and runtime state outcome.</returns>
    ValueTask<TenantDomainOwnershipProofPublicationPlanResult> PlanAsync(
        TenantDomainOwnershipProofPublicationPlanRequest request,
        CancellationToken cancellationToken = default);
}
