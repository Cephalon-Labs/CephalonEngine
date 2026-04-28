namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Allows a module to contribute tenant-domain ownership descriptors into the active governance runtime.
/// </summary>
public interface ITenantDomainOwnershipContributor
{
    /// <summary>
    /// Registers one or more tenant-domain ownership descriptors with the supplied registry.
    /// </summary>
    /// <param name="domainOwnerships">The registry that collects contributed domain ownership descriptors.</param>
    void RegisterDomainOwnerships(ITenantDomainOwnershipRegistry domainOwnerships);
}
