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
                CreateCompanionEntry(
                    id: "tenant-invitations",
                    displayName: "Tenant Invitations",
                    description: "Invite, acceptance, expiration, and revocation workflows for tenant access.",
                    futureSurfaceId: "tenant-invitations"),
                CreateCompanionEntry(
                    id: "tenant-domain-ownership",
                    displayName: "Tenant Domain Ownership",
                    description: "Domain claim, verification, conflict, and lifecycle workflows for tenant-owned domains.",
                    futureSurfaceId: "tenant-domain-ownership"),
                CreateCompanionEntry(
                    id: "tenant-governance-workflows",
                    displayName: "Tenant Governance Workflows",
                    description: "Approval, policy, lifecycle, and remediation workflows around tenant administration.",
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
