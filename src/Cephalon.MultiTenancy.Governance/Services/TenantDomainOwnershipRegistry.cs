namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantDomainOwnershipRegistry : ITenantDomainOwnershipRegistry
{
    private readonly List<TenantDomainOwnershipDescriptor> domainOwnerships = [];

    public void Add(TenantDomainOwnershipDescriptor domainOwnership)
    {
        ArgumentNullException.ThrowIfNull(domainOwnership);

        domainOwnerships.Add(domainOwnership);
    }

    public IReadOnlyList<TenantDomainOwnershipDescriptor> Build()
    {
        return domainOwnerships
            .GroupBy(
                static domainOwnership => string.Join(
                    "|",
                    domainOwnership.TenantId,
                    domainOwnership.DomainName),
                StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(static domainOwnership => domainOwnership.TenantId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static domainOwnership => domainOwnership.DomainName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
