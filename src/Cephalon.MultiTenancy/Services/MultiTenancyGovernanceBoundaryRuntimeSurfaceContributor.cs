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
            description: "Projects the split between the shipped tenant-resolution core and future tenant-governance companion workflows.",
            entries:
            [
                CreateCoreEntry(),
                CreateCompanionEntry(
                    id: "tenant-membership",
                    displayName: "Tenant Membership",
                    description: "User, group, role, and organization membership workflows for tenants.",
                    futureSurfaceId: "tenant-memberships"),
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
