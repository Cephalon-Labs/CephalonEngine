using Cephalon.MultiTenancy.Governance.Configuration;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantDomainOwnershipCatalog : ITenantDomainOwnershipCatalog
{
    private readonly Dictionary<string, TenantDomainOwnershipDescriptor[]> byTenantId;
    private readonly Dictionary<string, TenantDomainOwnershipDescriptor[]> byDomainName;
    private readonly Dictionary<string, TenantDomainOwnershipDescriptor[]> byTenantAndDomain;

    public TenantDomainOwnershipCatalog(
        MultiTenancyGovernanceOptions options,
        IEnumerable<ITenantDomainOwnershipContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contributors);

        var registry = new TenantDomainOwnershipRegistry();
        foreach (var domainOwnership in options.DomainOwnerships)
        {
            registry.Add(domainOwnership);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterDomainOwnerships(registry);
        }

        DomainOwnerships = registry.Build();
        byTenantId = DomainOwnerships
            .GroupBy(static domainOwnership => domainOwnership.TenantId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        byDomainName = DomainOwnerships
            .GroupBy(static domainOwnership => domainOwnership.DomainName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        byTenantAndDomain = DomainOwnerships
            .GroupBy(
                static domainOwnership => CreateTenantDomainKey(domainOwnership.TenantId, domainOwnership.DomainName),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<TenantDomainOwnershipDescriptor> DomainOwnerships { get; }

    public IReadOnlyList<TenantDomainOwnershipDescriptor> GetByTenantId(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return byTenantId.TryGetValue(tenantId.Trim(), out var domainOwnerships)
            ? domainOwnerships
            : [];
    }

    public IReadOnlyList<TenantDomainOwnershipDescriptor> GetByDomainName(string domainName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainName);

        return byDomainName.TryGetValue(TenantDomainOwnershipDescriptor.NormalizeDomainName(domainName), out var domainOwnerships)
            ? domainOwnerships
            : [];
    }

    public IReadOnlyList<TenantDomainOwnershipDescriptor> GetByTenantAndDomain(string tenantId, string domainName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(domainName);

        return byTenantAndDomain.TryGetValue(CreateTenantDomainKey(tenantId, domainName), out var domainOwnerships)
            ? domainOwnerships
            : [];
    }

    private static string CreateTenantDomainKey(string tenantId, string domainName)
    {
        return $"{tenantId.Trim()}|{TenantDomainOwnershipDescriptor.NormalizeDomainName(domainName)}";
    }
}
