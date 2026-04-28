using Cephalon.Abstractions.Technologies;

namespace Cephalon.MultiTenancy.Services;

internal sealed class MultiTenancyGovernanceBoundaryRuntimeSurfaceContributor : ITechnologyRuntimeContributor
{
    private const string TechnologyId = "multi-tenancy";
    private const string FutureCompanionPackage = "Cephalon.MultiTenancy.Governance";

    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: TechnologyId,
            surfaceId: "tenant-governance-boundaries",
            displayName: "Tenant Governance Boundaries",
            description: "Projects the split between the shipped tenant-resolution core and tenant-governance companion workflows.",
            entries:
            [
                CreateCoreEntry(),
                CreateMembershipEntry(),
                CreateInvitationEntry(),
                CreateDomainOwnershipEntry(),
                CreateGovernanceActionEntry(),
                CreateCompanionEntry(
                    id: "tenant-governance-workflows",
                    displayName: "Tenant Governance Workflows",
                    description: "Broader policy, lifecycle, delivery, synchronization, and tenant-administration workflows.",
                    futureSurfaceId: "tenant-governance-workflows")
            ]);
    }

    private static TechnologyRuntimeEntry CreateCoreEntry()
    {
        return new TechnologyRuntimeEntry(
            id: "tenant-resolution-core",
            displayName: "Tenant Resolution Core",
            description: "The shipped Cephalon.MultiTenancy core owns configuration-driven tenant resolution and ambient tenant context.",
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ownership"] = "cephalon-managed",
                ["package"] = "Cephalon.MultiTenancy",
                ["runtimeState"] = "active",
                ["maturity"] = "M2",
                ["surfaceId"] = "tenant-resolution",
                ["capabilityKey"] = "tenancy.resolution",
                ["basePackageScope"] = "tenant-resolution,ambient-context"
            });
    }

    private static TechnologyRuntimeEntry CreateMembershipEntry()
    {
        return new TechnologyRuntimeEntry(
            id: "tenant-membership",
            displayName: "Tenant Membership",
            description: "User, group, role, and organization membership workflows for tenants.",
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ownership"] = "companion-shipped",
                ["plannedOwnership"] = "companion-available",
                ["basePackageOwnership"] = "not-owned",
                ["basePackageRuntimeState"] = "not-configured",
                ["companionPackage"] = FutureCompanionPackage,
                ["suggestedPackage"] = FutureCompanionPackage,
                ["runtimeState"] = "requires-companion-registration",
                ["surfaceId"] = "tenant-memberships",
                ["maturity"] = "M2",
                ["capabilityKey"] = "tenancy.membership.catalog,tenancy.membership.evaluation",
                ["notes"] = "Cephalon.MultiTenancy intentionally does not execute membership workflows; install and register Cephalon.MultiTenancy.Governance for the shipped catalog and evaluation proof."
            });
    }

    private static TechnologyRuntimeEntry CreateInvitationEntry()
    {
        return new TechnologyRuntimeEntry(
            id: "tenant-invitations",
            displayName: "Tenant Invitations",
            description: "Invitation catalog and validation workflows for tenant access.",
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ownership"] = "companion-shipped",
                ["plannedOwnership"] = "companion-available",
                ["basePackageOwnership"] = "not-owned",
                ["basePackageRuntimeState"] = "not-configured",
                ["companionPackage"] = FutureCompanionPackage,
                ["suggestedPackage"] = FutureCompanionPackage,
                ["runtimeState"] = "requires-companion-registration",
                ["surfaceId"] = "tenant-invitations",
                ["maturity"] = "M2",
                ["capabilityKey"] = "tenancy.invitation.catalog,tenancy.invitation.validation",
                ["notes"] = "Cephalon.MultiTenancy intentionally does not execute invitation workflows; install and register Cephalon.MultiTenancy.Governance for the shipped catalog and validation proof."
            });
    }

    private static TechnologyRuntimeEntry CreateDomainOwnershipEntry()
    {
        return new TechnologyRuntimeEntry(
            id: "tenant-domain-ownership",
            displayName: "Tenant Domain Ownership",
            description: "Declared domain ownership catalog and validation workflows for tenant-owned domains.",
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ownership"] = "companion-shipped",
                ["plannedOwnership"] = "companion-available",
                ["basePackageOwnership"] = "not-owned",
                ["basePackageRuntimeState"] = "not-configured",
                ["companionPackage"] = FutureCompanionPackage,
                ["suggestedPackage"] = FutureCompanionPackage,
                ["runtimeState"] = "requires-companion-registration",
                ["surfaceId"] = "tenant-domain-ownership",
                ["maturity"] = "M2",
                ["capabilityKey"] = "tenancy.domain-ownership.catalog,tenancy.domain-ownership.validation",
                ["notes"] = "Cephalon.MultiTenancy intentionally does not validate domain ownership; install and register Cephalon.MultiTenancy.Governance for the shipped declared-domain catalog and validation proof."
            });
    }

    private static TechnologyRuntimeEntry CreateGovernanceActionEntry()
    {
        return new TechnologyRuntimeEntry(
            id: "tenant-governance-actions",
            displayName: "Tenant Governance Actions",
            description: "Approval and remediation action catalog plus deterministic action decision workflows for tenants.",
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ownership"] = "companion-shipped",
                ["plannedOwnership"] = "companion-available",
                ["basePackageOwnership"] = "not-owned",
                ["basePackageRuntimeState"] = "not-configured",
                ["companionPackage"] = FutureCompanionPackage,
                ["suggestedPackage"] = FutureCompanionPackage,
                ["runtimeState"] = "requires-companion-registration",
                ["surfaceId"] = "tenant-governance-actions",
                ["maturity"] = "M2",
                ["capabilityKey"] = "tenancy.governance-action.catalog,tenancy.governance-action.decision",
                ["notes"] = "Cephalon.MultiTenancy intentionally does not decide governance approvals or remediations; install and register Cephalon.MultiTenancy.Governance for the shipped action catalog and decision proof."
            });
    }

    private static TechnologyRuntimeEntry CreateCompanionEntry(
        string id,
        string displayName,
        string description,
        string futureSurfaceId)
    {
        return new TechnologyRuntimeEntry(
            id,
            displayName,
            description,
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ownership"] = "taxonomy-only",
                ["plannedOwnership"] = "companion-planned",
                ["basePackageOwnership"] = "not-owned",
                ["basePackageRuntimeState"] = "not-configured",
                ["suggestedPackage"] = FutureCompanionPackage,
                ["futureSurfaceId"] = futureSurfaceId,
                ["maturity"] = "M0",
                ["capabilityKey"] = "none",
                ["notes"] = "Cephalon.MultiTenancy intentionally does not execute this workflow until a companion package owns it."
            });
    }
}
