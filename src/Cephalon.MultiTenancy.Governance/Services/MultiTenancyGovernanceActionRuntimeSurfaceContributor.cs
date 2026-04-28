using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class MultiTenancyGovernanceActionRuntimeSurfaceContributor(
    MultiTenancyGovernanceOptions options,
    ITenantGovernanceActionCatalog catalog,
    TenantGovernanceActionRuntimeStore runtimeStore,
    IEnumerable<ITenantGovernanceActionContributor> contributors) : ITechnologyRuntimeContributor
{
    private readonly ITenantGovernanceActionContributor[] contributors = contributors.ToArray();

    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var entries = new List<TechnologyRuntimeEntry>
        {
            CreateSummaryEntry()
        };

        entries.AddRange(catalog.Actions
            .GroupBy(static action => action.TenantId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(CreateTenantEntry));

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-governance-actions",
            displayName: "Tenant Governance Actions",
            description: "Projects tenant-governance approval and remediation action decision truth from the governance companion pack.",
            entries: entries);
    }

    private TechnologyRuntimeEntry CreateSummaryEntry()
    {
        var statusBreakdown = catalog.Actions
            .GroupBy(static action => action.Status, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static group => $"{group.Key}:{group.Count().ToString(CultureInfo.InvariantCulture)}")
            .ToArray();
        var actionKindBreakdown = catalog.Actions
            .GroupBy(static action => action.ActionKind, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static group => $"{group.Key}:{group.Count().ToString(CultureInfo.InvariantCulture)}")
            .ToArray();
        var subjectKindBreakdown = catalog.Actions
            .GroupBy(static action => action.SubjectKind, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static group => $"{group.Key}:{group.Count().ToString(CultureInfo.InvariantCulture)}")
            .ToArray();

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = "cephalon-managed",
            ["package"] = "Cephalon.MultiTenancy.Governance",
            ["runtimeState"] = catalog.Actions.Count > 0 ? "configured" : "empty",
            ["actionCount"] = catalog.Actions.Count.ToString(CultureInfo.InvariantCulture),
            ["tenantCount"] = catalog.Actions
                .Select(static action => action.TenantId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count()
                .ToString(CultureInfo.InvariantCulture),
            ["contributorCount"] = contributors.Length.ToString(CultureInfo.InvariantCulture),
            ["configuredActionCount"] = options.GovernanceActions.Count.ToString(CultureInfo.InvariantCulture),
            ["runtimeActionCount"] = runtimeStore.Count.ToString(CultureInfo.InvariantCulture),
            ["decisionEnabled"] = options.EnableGovernanceActionDecision.ToString().ToLowerInvariant(),
            ["decisionOwnership"] = options.EnableGovernanceActionDecision ? "cephalon-managed" : "not-configured",
            ["workflowEnabled"] = options.EnableGovernanceActionWorkflow.ToString().ToLowerInvariant(),
            ["workflowExecutionOwnership"] = options.EnableGovernanceActionWorkflow ? "cephalon-managed" : "not-configured",
            ["durableStoreOwnership"] = "application-managed",
            ["notificationDeliveryOwnership"] = "application-managed",
            ["statusBreakdown"] = statusBreakdown.Length == 0 ? "none" : string.Join(",", statusBreakdown),
            ["actionKindBreakdown"] = actionKindBreakdown.Length == 0 ? "none" : string.Join(",", actionKindBreakdown),
            ["subjectKindBreakdown"] = subjectKindBreakdown.Length == 0 ? "none" : string.Join(",", subjectKindBreakdown)
        };

        return new TechnologyRuntimeEntry(
            id: "tenant-governance-action-runtime",
            displayName: "Tenant Governance Action Runtime",
            description: "Summarizes approval/remediation action catalog size, contributor count, status posture, action-kind posture, managed decision ownership, and in-process workflow ownership.",
            metadata: metadata);
    }

    private static TechnologyRuntimeEntry CreateTenantEntry(IGrouping<string, TenantGovernanceActionDescriptor> group)
    {
        var actions = group.ToArray();
        var approvedCount = CountByStatus(actions, TenantGovernanceActionStatuses.Approved);
        var pendingCount = CountByStatus(actions, TenantGovernanceActionStatuses.PendingApproval);
        var rejectedCount = CountByStatus(actions, TenantGovernanceActionStatuses.Rejected);
        var remediationRequiredCount = CountByStatus(actions, TenantGovernanceActionStatuses.RemediationRequired);
        var remediatedCount = CountByStatus(actions, TenantGovernanceActionStatuses.Remediated);
        var expiredCount = CountByStatus(actions, TenantGovernanceActionStatuses.Expired);
        var actionKindBreakdown = actions
            .GroupBy(static action => action.ActionKind, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static kind => kind.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static kind => $"{kind.Key}:{kind.Count().ToString(CultureInfo.InvariantCulture)}")
            .ToArray();
        var subjectKindBreakdown = actions
            .GroupBy(static action => action.SubjectKind, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static kind => kind.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static kind => $"{kind.Key}:{kind.Count().ToString(CultureInfo.InvariantCulture)}")
            .ToArray();
        var sourceModuleIds = actions
            .Select(static action => action.SourceModuleId)
            .Where(static sourceModuleId => !string.IsNullOrWhiteSpace(sourceModuleId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static sourceModuleId => sourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = "cephalon-managed",
            ["package"] = "Cephalon.MultiTenancy.Governance",
            ["tenantId"] = group.Key,
            ["actionCount"] = actions.Length.ToString(CultureInfo.InvariantCulture),
            ["approvedActionCount"] = approvedCount.ToString(CultureInfo.InvariantCulture),
            ["pendingApprovalActionCount"] = pendingCount.ToString(CultureInfo.InvariantCulture),
            ["rejectedActionCount"] = rejectedCount.ToString(CultureInfo.InvariantCulture),
            ["remediationRequiredActionCount"] = remediationRequiredCount.ToString(CultureInfo.InvariantCulture),
            ["remediatedActionCount"] = remediatedCount.ToString(CultureInfo.InvariantCulture),
            ["expiredActionCount"] = expiredCount.ToString(CultureInfo.InvariantCulture),
            ["actionKindBreakdown"] = actionKindBreakdown.Length == 0 ? "none" : string.Join(",", actionKindBreakdown),
            ["subjectKindBreakdown"] = subjectKindBreakdown.Length == 0 ? "none" : string.Join(",", subjectKindBreakdown),
            ["sourceModuleIds"] = sourceModuleIds.Length == 0 ? "none" : string.Join(",", sourceModuleIds)
        };

        return new TechnologyRuntimeEntry(
            id: $"tenant-governance-actions:{group.Key}",
            displayName: $"Tenant Governance Actions: {group.Key}",
            description: "Summarizes approval and remediation action posture for one tenant without exposing individual action metadata.",
            metadata: metadata);
    }

    private static int CountByStatus(TenantGovernanceActionDescriptor[] actions, string status)
    {
        return actions.Count(action => string.Equals(action.Status, status, StringComparison.OrdinalIgnoreCase));
    }
}
