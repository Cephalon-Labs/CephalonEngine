namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantMembershipRegistry : ITenantMembershipRegistry
{
    private readonly List<TenantMembershipDescriptor> memberships = [];

    public void Add(TenantMembershipDescriptor membership)
    {
        ArgumentNullException.ThrowIfNull(membership);

        memberships.Add(membership);
    }

    public IReadOnlyList<TenantMembershipDescriptor> Build()
    {
        return memberships
            .GroupBy(
                static membership => string.Join(
                    "|",
                    membership.TenantId,
                    membership.PrincipalKind,
                    membership.PrincipalId),
                StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(static membership => membership.TenantId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static membership => membership.PrincipalKind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static membership => membership.PrincipalId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
