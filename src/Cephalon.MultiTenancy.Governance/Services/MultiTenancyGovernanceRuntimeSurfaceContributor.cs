using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class MultiTenancyGovernanceRuntimeSurfaceContributor(
    MultiTenancyGovernanceOptions options,
    ITenantMembershipCatalog catalog,
    ITenantMembershipStore membershipStore,
    IEnumerable<ITenantMembershipContributor> contributors) : ITechnologyRuntimeContributor
{
    private readonly ITenantMembershipContributor[] contributors = contributors.ToArray();

    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var memberships = catalog.Memberships;
        var entries = new List<TechnologyRuntimeEntry>
        {
            CreateSummaryEntry(memberships)
        };

        entries.AddRange(memberships
            .GroupBy(static membership => membership.TenantId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(CreateTenantEntry));

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-memberships",
            displayName: "Tenant Memberships",
            description: "Projects tenant membership catalog and Cephalon-managed membership evaluation truth from the governance companion pack.",
            entries: entries);
    }

    private TechnologyRuntimeEntry CreateSummaryEntry(IReadOnlyList<TenantMembershipDescriptor> memberships)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = "cephalon-managed",
            ["package"] = "Cephalon.MultiTenancy.Governance",
            ["runtimeState"] = memberships.Count > 0 ? "configured" : "empty",
            ["membershipCount"] = memberships.Count.ToString(CultureInfo.InvariantCulture),
            ["tenantCount"] = memberships
                .Select(static membership => membership.TenantId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count()
                .ToString(CultureInfo.InvariantCulture),
            ["contributorCount"] = contributors.Length.ToString(CultureInfo.InvariantCulture),
            ["configuredMembershipCount"] = options.Memberships.Count.ToString(CultureInfo.InvariantCulture),
            ["runtimeMembershipCount"] = membershipStore.Count.ToString(CultureInfo.InvariantCulture),
            ["membershipStoreKind"] = membershipStore.StoreKind,
            ["membershipStoreDurable"] = membershipStore.IsDurable.ToString().ToLowerInvariant(),
            ["membershipStoreOwnership"] = membershipStore.Ownership,
            ["evaluationEnabled"] = options.EnableMembershipEvaluation.ToString().ToLowerInvariant(),
            ["evaluationOwnership"] = options.EnableMembershipEvaluation ? "cephalon-managed" : "not-configured",
            ["durableStoreOwnership"] = membershipStore.IsDurable ? membershipStore.Ownership : "application-managed",
            ["basePackageOwnership"] = "separate-companion"
        };

        return new TechnologyRuntimeEntry(
            id: "tenant-membership-runtime",
            displayName: "Tenant Membership Runtime",
            description: "Summarizes tenant membership catalog size, contributor count, runtime store posture, and managed evaluation ownership.",
            metadata: metadata);
    }

    private static TechnologyRuntimeEntry CreateTenantEntry(IGrouping<string, TenantMembershipDescriptor> group)
    {
        var memberships = group.ToArray();
        var activeCount = memberships.Count(static membership =>
            string.Equals(membership.Status, TenantMembershipStatuses.Active, StringComparison.OrdinalIgnoreCase));
        var suspendedCount = memberships.Count(static membership =>
            string.Equals(membership.Status, TenantMembershipStatuses.Suspended, StringComparison.OrdinalIgnoreCase));
        var expiredCount = memberships.Count(static membership =>
            string.Equals(membership.Status, TenantMembershipStatuses.Expired, StringComparison.OrdinalIgnoreCase));
        var roles = memberships
            .SelectMany(static membership => membership.Roles)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static role => role, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var principalKindBreakdown = memberships
            .GroupBy(static membership => membership.PrincipalKind, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static kind => kind.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static kind => $"{kind.Key}:{kind.Count().ToString(CultureInfo.InvariantCulture)}")
            .ToArray();
        var sourceModuleIds = memberships
            .Select(static membership => membership.SourceModuleId)
            .Where(static sourceModuleId => !string.IsNullOrWhiteSpace(sourceModuleId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static sourceModuleId => sourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = "cephalon-managed",
            ["package"] = "Cephalon.MultiTenancy.Governance",
            ["tenantId"] = group.Key,
            ["membershipCount"] = memberships.Length.ToString(CultureInfo.InvariantCulture),
            ["activeMembershipCount"] = activeCount.ToString(CultureInfo.InvariantCulture),
            ["suspendedMembershipCount"] = suspendedCount.ToString(CultureInfo.InvariantCulture),
            ["expiredMembershipCount"] = expiredCount.ToString(CultureInfo.InvariantCulture),
            ["roleCount"] = roles.Length.ToString(CultureInfo.InvariantCulture),
            ["roles"] = roles.Length == 0 ? "none" : string.Join(",", roles),
            ["principalKindBreakdown"] = principalKindBreakdown.Length == 0 ? "none" : string.Join(",", principalKindBreakdown),
            ["sourceModuleIds"] = sourceModuleIds.Length == 0 ? "none" : string.Join(",", sourceModuleIds)
        };

        return new TechnologyRuntimeEntry(
            id: $"tenant-membership:{group.Key}",
            displayName: $"Tenant Memberships: {group.Key}",
            description: "Summarizes membership posture for one tenant without exposing individual principal identifiers.",
            metadata: metadata);
    }
}
