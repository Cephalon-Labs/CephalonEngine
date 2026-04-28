namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Exposes the merged tenant-domain ownership set available to the active governance runtime.
/// </summary>
public interface ITenantDomainOwnershipCatalog
{
    /// <summary>
    /// Gets the effective domain ownership set after host options and module contributors have both been applied.
    /// </summary>
    IReadOnlyList<TenantDomainOwnershipDescriptor> DomainOwnerships { get; }

    /// <summary>
    /// Gets domain ownership descriptors for one tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to resolve.</param>
    /// <returns>The matching domain ownership descriptors.</returns>
    IReadOnlyList<TenantDomainOwnershipDescriptor> GetByTenantId(string tenantId);

    /// <summary>
    /// Gets domain ownership descriptors by canonical domain name across all tenants.
    /// </summary>
    /// <param name="domainName">The domain name to resolve.</param>
    /// <returns>The matching domain ownership descriptors.</returns>
    IReadOnlyList<TenantDomainOwnershipDescriptor> GetByDomainName(string domainName);

    /// <summary>
    /// Gets domain ownership descriptors by tenant and domain name.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to resolve.</param>
    /// <param name="domainName">The domain name to resolve.</param>
    /// <returns>The matching domain ownership descriptors.</returns>
    IReadOnlyList<TenantDomainOwnershipDescriptor> GetByTenantAndDomain(string tenantId, string domainName);
}
