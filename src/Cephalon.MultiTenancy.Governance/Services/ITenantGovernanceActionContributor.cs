namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Allows a module to contribute tenant-governance approval or remediation actions into the active governance runtime.
/// </summary>
public interface ITenantGovernanceActionContributor
{
    /// <summary>
    /// Registers one or more tenant-governance actions with the supplied registry.
    /// </summary>
    /// <param name="actions">The registry that collects contributed governance action descriptors.</param>
    void RegisterGovernanceActions(ITenantGovernanceActionRegistry actions);
}
