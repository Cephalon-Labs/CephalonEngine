using Cephalon.MultiTenancy.Governance.Configuration;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantMembershipCatalog : ITenantMembershipCatalog
{
    private readonly Dictionary<string, TenantMembershipDescriptor[]> byTenantId;
    private readonly Dictionary<string, TenantMembershipDescriptor[]> byPrincipalId;
    private readonly Dictionary<string, TenantMembershipDescriptor[]> byTenantAndPrincipal;
    private readonly Dictionary<string, TenantMembershipDescriptor[]> byTenantPrincipalAndKind;

    public TenantMembershipCatalog(
        MultiTenancyGovernanceOptions options,
        IEnumerable<ITenantMembershipContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contributors);

        var registry = new TenantMembershipRegistry();
        foreach (var membership in options.Memberships)
        {
            registry.Add(membership);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterMemberships(registry);
        }

        Memberships = registry.Build();
        byTenantId = Memberships
            .GroupBy(static membership => membership.TenantId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        byPrincipalId = Memberships
            .GroupBy(static membership => membership.PrincipalId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        byTenantAndPrincipal = Memberships
            .GroupBy(static membership => CreateTenantPrincipalKey(membership.TenantId, membership.PrincipalId), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        byTenantPrincipalAndKind = Memberships
            .GroupBy(
                static membership => CreateTenantPrincipalKindKey(membership.TenantId, membership.PrincipalKind, membership.PrincipalId),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<TenantMembershipDescriptor> Memberships { get; }

    public IReadOnlyList<TenantMembershipDescriptor> GetByTenantId(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return byTenantId.TryGetValue(tenantId.Trim(), out var memberships)
            ? memberships
            : [];
    }

    public IReadOnlyList<TenantMembershipDescriptor> GetByPrincipalId(string principalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(principalId);

        return byPrincipalId.TryGetValue(principalId.Trim(), out var memberships)
            ? memberships
            : [];
    }

    public IReadOnlyList<TenantMembershipDescriptor> GetByTenantAndPrincipal(string tenantId, string principalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(principalId);

        return byTenantAndPrincipal.TryGetValue(CreateTenantPrincipalKey(tenantId, principalId), out var memberships)
            ? memberships
            : [];
    }

    public IReadOnlyList<TenantMembershipDescriptor> GetByTenantPrincipalAndKind(string tenantId, string principalKind, string principalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(principalKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(principalId);

        return byTenantPrincipalAndKind.TryGetValue(CreateTenantPrincipalKindKey(tenantId, principalKind, principalId), out var memberships)
            ? memberships
            : [];
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
