using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class MultiTenancyGovernanceAdministrationRuntimeSurfaceContributor(
    MultiTenancyGovernanceOptions options,
    ITenantMembershipStore membershipStore,
    ITenantInvitationStore invitationStore,
    IEnumerable<ITenantInvitationDeliverySender> deliverySenders) : ITechnologyRuntimeContributor
{
    private readonly ITenantInvitationDeliverySender[] deliverySenders = deliverySenders
        .Where(static sender => !string.IsNullOrWhiteSpace(sender.SenderId))
        .OrderBy(static sender => sender.SenderId, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var deliverySenderConfigured = deliverySenders.Length > 0;
        var invitationDeliveryOwnership = options.EnableInvitationDeliveryDispatch && deliverySenderConfigured
            ? "mixed"
            : "application-managed";
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = options.EnableTenantAdministrationWorkflow ? "cephalon-managed" : "not-configured",
            ["package"] = "Cephalon.MultiTenancy.Governance",
            ["runtimeState"] = options.EnableTenantAdministrationWorkflow ? "enabled" : "disabled",
            ["workflowEnabled"] = options.EnableTenantAdministrationWorkflow.ToString().ToLowerInvariant(),
            ["workflowExecutionOwnership"] = options.EnableTenantAdministrationWorkflow ? "cephalon-managed" : "not-configured",
            ["membershipAdministrationOwnership"] = options.EnableTenantAdministrationWorkflow ? "cephalon-managed" : "not-configured",
            ["invitationAdministrationOwnership"] = options.EnableTenantAdministrationWorkflow ? "cephalon-managed" : "not-configured",
            ["membershipStoreKind"] = membershipStore.StoreKind,
            ["membershipStoreDurable"] = membershipStore.IsDurable.ToString().ToLowerInvariant(),
            ["membershipStoreOwnership"] = membershipStore.Ownership,
            ["membershipStoreCount"] = membershipStore.Count.ToString(CultureInfo.InvariantCulture),
            ["invitationStoreKind"] = invitationStore.StoreKind,
            ["invitationStoreDurable"] = invitationStore.IsDurable.ToString().ToLowerInvariant(),
            ["invitationStoreOwnership"] = invitationStore.Ownership,
            ["invitationStoreCount"] = invitationStore.Count.ToString(CultureInfo.InvariantCulture),
            ["supportedCommands"] = string.Join(
                ",",
                TenantAdministrationWorkflowCommands.GrantMembership,
                TenantAdministrationWorkflowCommands.SuspendMembership,
                TenantAdministrationWorkflowCommands.ExpireMembership,
                TenantAdministrationWorkflowCommands.IssueInvitation,
                TenantAdministrationWorkflowCommands.AcceptInvitation,
                TenantAdministrationWorkflowCommands.RevokeInvitation,
                TenantAdministrationWorkflowCommands.ExpireInvitation),
            ["publicOnboardingOwnership"] = "application-managed",
            ["tenantAdminEndpointOwnership"] = "application-managed",
            ["invitationDeliveryOwnership"] = invitationDeliveryOwnership,
            ["invitationDeliveryDispatchOwnership"] = options.EnableInvitationDeliveryDispatch ? "cephalon-managed" : "not-configured",
            ["invitationDeliveryStatusReconciliationOwnership"] = options.EnableInvitationDeliveryStatusReconciliation ? "cephalon-managed" : "not-configured",
            ["invitationDeliverySenderOwnership"] = deliverySenderConfigured ? "provider-managed" : "not-configured",
            ["externalInvitationDeliveryOwnership"] = deliverySenderConfigured ? "provider-managed" : "application-managed",
            ["externalInvitationDeliveryStatusOwnership"] = options.EnableInvitationDeliveryStatusReconciliation ? "provider-managed" : "application-managed",
            ["invitationDeliverySenderCount"] = deliverySenders.Length.ToString(CultureInfo.InvariantCulture),
            ["identityProviderSyncOwnership"] = "application-managed"
        };

        var entry = new TechnologyRuntimeEntry(
            id: "tenant-administration-runtime",
            displayName: "Tenant Administration Runtime",
            description: "Summarizes host-driven tenant administration workflow ownership over membership and invitation stores.",
            metadata: metadata);

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-administration",
            displayName: "Tenant Administration",
            description: "Projects Cephalon-managed tenant-administration workflow ownership without claiming public onboarding, provider-specific notification delivery/status callbacks, or identity-provider synchronization.",
            entries: [entry]);
    }
}
