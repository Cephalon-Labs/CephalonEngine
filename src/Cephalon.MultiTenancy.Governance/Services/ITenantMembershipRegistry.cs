namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Collects tenant memberships contributed to the active governance runtime.
/// </summary>
public interface ITenantMembershipRegistry
{
    /// <summary>
    /// Adds a tenant-membership descriptor to the registry.
    /// </summary>
    /// <param name="membership">The membership descriptor to contribute.</param>
    void Add(TenantMembershipDescriptor membership);
}
