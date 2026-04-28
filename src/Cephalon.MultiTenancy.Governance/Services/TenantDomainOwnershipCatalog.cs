using Cephalon.MultiTenancy.Governance.Configuration;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantDomainOwnershipCatalog : ITenantDomainOwnershipCatalog
{
    private readonly TenantDomainOwnershipDescriptor[] configuredDomainOwnerships;
    private readonly ITenantDomainOwnershipStore domainOwnershipStore;

    public TenantDomainOwnershipCatalog(
        MultiTenancyGovernanceOptions options,
        IEnumerable<ITenantDomainOwnershipContributor> contributors,
        ITenantDomainOwnershipStore domainOwnershipStore)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contributors);
        ArgumentNullException.ThrowIfNull(domainOwnershipStore);

        var registry = new TenantDomainOwnershipRegistry();
        foreach (var domainOwnership in options.DomainOwnerships)
        {
            registry.Add(domainOwnership);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterDomainOwnerships(registry);
        }

        configuredDomainOwnerships = [.. registry.Build()];
        this.domainOwnershipStore = domainOwnershipStore;
    }

    public IReadOnlyList<TenantDomainOwnershipDescriptor> DomainOwnerships => BuildDomainOwnerships();

    public IReadOnlyList<TenantDomainOwnershipDescriptor> GetByTenantId(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return DomainOwnerships
            .Where(domainOwnership => string.Equals(domainOwnership.TenantId, tenantId.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public IReadOnlyList<TenantDomainOwnershipDescriptor> GetByDomainName(string domainName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainName);

        return DomainOwnerships
            .Where(domainOwnership => string.Equals(
                domainOwnership.DomainName,
                TenantDomainOwnershipDescriptor.NormalizeDomainName(domainName),
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public IReadOnlyList<TenantDomainOwnershipDescriptor> GetByTenantAndDomain(string tenantId, string domainName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(domainName);

        return DomainOwnerships
            .Where(domainOwnership => string.Equals(
                CreateTenantDomainKey(domainOwnership.TenantId, domainOwnership.DomainName),
                CreateTenantDomainKey(tenantId, domainName),
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private IReadOnlyList<TenantDomainOwnershipDescriptor> BuildDomainOwnerships()
    {
        var registry = new TenantDomainOwnershipRegistry();
        foreach (var domainOwnership in domainOwnershipStore.DomainOwnerships)
        {
            registry.Add(domainOwnership);
        }

        foreach (var domainOwnership in configuredDomainOwnerships)
        {
            registry.Add(domainOwnership);
        }

        return registry.Build();
    }

    private static string CreateTenantDomainKey(string tenantId, string domainName)
    {
        return $"{tenantId.Trim()}|{TenantDomainOwnershipDescriptor.NormalizeDomainName(domainName)}";
    }
}
