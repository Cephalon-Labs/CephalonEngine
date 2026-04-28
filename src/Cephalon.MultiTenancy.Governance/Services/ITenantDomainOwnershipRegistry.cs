namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Collects tenant-domain ownership descriptors contributed to the active governance runtime.
/// </summary>
public interface ITenantDomainOwnershipRegistry
{
    /// <summary>
    /// Adds a tenant-domain ownership descriptor to the registry.
    /// </summary>
    /// <param name="domainOwnership">The domain ownership descriptor to contribute.</param>
    void Add(TenantDomainOwnershipDescriptor domainOwnership);
}
