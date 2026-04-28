using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class MultiTenancyGovernanceInvitationRuntimeSurfaceContributor(
    MultiTenancyGovernanceOptions options,
    ITenantInvitationCatalog catalog,
    IEnumerable<ITenantInvitationContributor> contributors) : ITechnologyRuntimeContributor
{
    private readonly ITenantInvitationContributor[] contributors = contributors.ToArray();

    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var entries = new List<TechnologyRuntimeEntry>
        {
            CreateSummaryEntry()
        };

        entries.AddRange(catalog.Invitations
            .GroupBy(static invitation => invitation.TenantId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(CreateTenantEntry));

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-invitations",
            displayName: "Tenant Invitations",
            description: "Projects tenant invitation catalog and Cephalon-managed invitation validation truth from the governance companion pack.",
            entries: entries);
    }

    private TechnologyRuntimeEntry CreateSummaryEntry()
    {
        var statusBreakdown = catalog.Invitations
            .GroupBy(static invitation => invitation.Status, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static group => $"{group.Key}:{group.Count().ToString(CultureInfo.InvariantCulture)}")
            .ToArray();
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = "cephalon-managed",
            ["package"] = "Cephalon.MultiTenancy.Governance",
            ["runtimeState"] = catalog.Invitations.Count > 0 ? "configured" : "empty",
            ["invitationCount"] = catalog.Invitations.Count.ToString(CultureInfo.InvariantCulture),
            ["tenantCount"] = catalog.Invitations
                .Select(static invitation => invitation.TenantId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count()
                .ToString(CultureInfo.InvariantCulture),
            ["contributorCount"] = contributors.Length.ToString(CultureInfo.InvariantCulture),
            ["configuredInvitationCount"] = options.Invitations.Count.ToString(CultureInfo.InvariantCulture),
            ["validationEnabled"] = options.EnableInvitationValidation.ToString().ToLowerInvariant(),
            ["validationOwnership"] = options.EnableInvitationValidation ? "cephalon-managed" : "not-configured",
            ["basePackageOwnership"] = "separate-companion",
            ["statusBreakdown"] = statusBreakdown.Length == 0 ? "none" : string.Join(",", statusBreakdown)
        };

        return new TechnologyRuntimeEntry(
            id: "tenant-invitation-runtime",
            displayName: "Tenant Invitation Runtime",
            description: "Summarizes tenant invitation catalog size, contributor count, status posture, and managed validation ownership.",
            metadata: metadata);
    }

    private static TechnologyRuntimeEntry CreateTenantEntry(IGrouping<string, TenantInvitationDescriptor> group)
    {
        var invitations = group.ToArray();
        var pendingCount = invitations.Count(static invitation =>
            string.Equals(invitation.Status, TenantInvitationStatuses.Pending, StringComparison.OrdinalIgnoreCase));
        var acceptedCount = invitations.Count(static invitation =>
            string.Equals(invitation.Status, TenantInvitationStatuses.Accepted, StringComparison.OrdinalIgnoreCase));
        var revokedCount = invitations.Count(static invitation =>
            string.Equals(invitation.Status, TenantInvitationStatuses.Revoked, StringComparison.OrdinalIgnoreCase));
        var expiredCount = invitations.Count(static invitation =>
            string.Equals(invitation.Status, TenantInvitationStatuses.Expired, StringComparison.OrdinalIgnoreCase));
        var roles = invitations
            .SelectMany(static invitation => invitation.Roles)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static role => role, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var inviteeKindBreakdown = invitations
            .GroupBy(static invitation => invitation.InviteeKind, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static kind => kind.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static kind => $"{kind.Key}:{kind.Count().ToString(CultureInfo.InvariantCulture)}")
            .ToArray();
        var sourceModuleIds = invitations
            .Select(static invitation => invitation.SourceModuleId)
            .Where(static sourceModuleId => !string.IsNullOrWhiteSpace(sourceModuleId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static sourceModuleId => sourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = "cephalon-managed",
            ["package"] = "Cephalon.MultiTenancy.Governance",
            ["tenantId"] = group.Key,
            ["invitationCount"] = invitations.Length.ToString(CultureInfo.InvariantCulture),
            ["pendingInvitationCount"] = pendingCount.ToString(CultureInfo.InvariantCulture),
            ["acceptedInvitationCount"] = acceptedCount.ToString(CultureInfo.InvariantCulture),
            ["revokedInvitationCount"] = revokedCount.ToString(CultureInfo.InvariantCulture),
            ["expiredInvitationCount"] = expiredCount.ToString(CultureInfo.InvariantCulture),
            ["roleCount"] = roles.Length.ToString(CultureInfo.InvariantCulture),
            ["roles"] = roles.Length == 0 ? "none" : string.Join(",", roles),
            ["inviteeKindBreakdown"] = inviteeKindBreakdown.Length == 0 ? "none" : string.Join(",", inviteeKindBreakdown),
            ["sourceModuleIds"] = sourceModuleIds.Length == 0 ? "none" : string.Join(",", sourceModuleIds)
        };

        return new TechnologyRuntimeEntry(
            id: $"tenant-invitations:{group.Key}",
            displayName: $"Tenant Invitations: {group.Key}",
            description: "Summarizes invitation posture for one tenant without exposing individual invitee identifiers.",
            metadata: metadata);
    }
}
