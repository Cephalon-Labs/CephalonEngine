namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Exposes the merged tenant-governance action set available to the active governance runtime.
/// </summary>
public interface ITenantGovernanceActionCatalog
{
    /// <summary>
    /// Gets the effective governance action set after host options and module contributors have both been applied.
    /// </summary>
    IReadOnlyList<TenantGovernanceActionDescriptor> Actions { get; }

    /// <summary>
    /// Gets governance action descriptors for one tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to resolve.</param>
    /// <returns>The matching governance action descriptors.</returns>
    IReadOnlyList<TenantGovernanceActionDescriptor> GetByTenantId(string tenantId);

    /// <summary>
    /// Gets governance action descriptors by action identifier across all tenants.
    /// </summary>
    /// <param name="actionId">The action identifier to resolve.</param>
    /// <returns>The matching governance action descriptors.</returns>
    IReadOnlyList<TenantGovernanceActionDescriptor> GetByActionId(string actionId);

    /// <summary>
    /// Gets governance action descriptors by tenant and action identifier.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to resolve.</param>
    /// <param name="actionId">The action identifier to resolve.</param>
    /// <returns>The matching governance action descriptors.</returns>
    IReadOnlyList<TenantGovernanceActionDescriptor> GetByTenantAndAction(string tenantId, string actionId);
}
