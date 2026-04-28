namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Collects tenant-governance action descriptors contributed to the active governance runtime.
/// </summary>
public interface ITenantGovernanceActionRegistry
{
    /// <summary>
    /// Adds a tenant-governance action descriptor to the registry.
    /// </summary>
    /// <param name="action">The governance action descriptor to contribute.</param>
    void Add(TenantGovernanceActionDescriptor action);
}
