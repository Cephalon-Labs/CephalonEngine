using Cephalon.MultiTenancy.Governance.Configuration;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantMembershipCatalog : ITenantMembershipCatalog
{
    private readonly TenantMembershipDescriptor[] configuredMemberships;
    private readonly ITenantMembershipStore membershipStore;

    public TenantMembershipCatalog(
        MultiTenancyGovernanceOptions options,
        IEnumerable<ITenantMembershipContributor> contributors,
        ITenantMembershipStore membershipStore)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contributors);
        ArgumentNullException.ThrowIfNull(membershipStore);

        var registry = new TenantMembershipRegistry();
        foreach (var membership in options.Memberships)
        {
            registry.Add(membership);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterMemberships(registry);
        }

        configuredMemberships = [.. registry.Build()];
        this.membershipStore = membershipStore;
    }

    public IReadOnlyList<TenantMembershipDescriptor> Memberships => BuildMemberships();

    public IReadOnlyList<TenantMembershipDescriptor> GetByTenantId(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return Memberships
            .Where(membership => string.Equals(membership.TenantId, tenantId.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public IReadOnlyList<TenantMembershipDescriptor> GetByPrincipalId(string principalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(principalId);

        return Memberships
            .Where(membership => string.Equals(membership.PrincipalId, principalId.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public IReadOnlyList<TenantMembershipDescriptor> GetByTenantAndPrincipal(string tenantId, string principalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(principalId);

        return Memberships
            .Where(membership => string.Equals(
                CreateTenantPrincipalKey(membership.TenantId, membership.PrincipalId),
                CreateTenantPrincipalKey(tenantId, principalId),
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public IReadOnlyList<TenantMembershipDescriptor> GetByTenantPrincipalAndKind(string tenantId, string principalKind, string principalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(principalKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(principalId);

        return Memberships
            .Where(membership => string.Equals(
                CreateTenantPrincipalKindKey(membership.TenantId, membership.PrincipalKind, membership.PrincipalId),
                CreateTenantPrincipalKindKey(tenantId, principalKind, principalId),
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private IReadOnlyList<TenantMembershipDescriptor> BuildMemberships()
    {
        var registry = new TenantMembershipRegistry();
        foreach (var membership in membershipStore.Memberships)
        {
            registry.Add(membership);
        }

        foreach (var membership in configuredMemberships)
        {
            registry.Add(membership);
        }

        return registry.Build();
    }

    private static string CreateTenantPrincipalKey(string tenantId, string principalId)
    {
        return $"{tenantId.Trim()}|{principalId.Trim()}";
    }

    private static string CreateTenantPrincipalKindKey(string tenantId, string principalKind, string principalId)
    {
        return $"{tenantId.Trim()}|{principalKind.Trim()}|{principalId.Trim()}";
    }
}
