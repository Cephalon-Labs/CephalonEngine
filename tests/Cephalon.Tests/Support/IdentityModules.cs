using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Identity.Policies;

namespace Cephalon.Tests.Support;

internal sealed class IdentityAuthorizationTestModule : ModuleBase, IAuthorizationPolicyContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "identity-authorization-tests",
        displayName: "Identity Authorization Tests",
        description: "Contributes declarative authorization policies for the Cephalon.Identity companion-pack tests.",
        tags: ["identity", "authorization", "tests"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void RegisterPolicies(IAuthorizationPolicyRegistry policies)
    {
        policies.Add(new AuthorizationPolicyDescriptor(
            id: "tenant-admin",
            displayName: "Tenant Administrator",
            description: "Allows tenant administrators to manage tenant-owned resources.",
            modes: [AuthorizationMode.Rbac, AuthorizationMode.Policy],
            tags: ["tenant", "admin"],
            metadata: new Dictionary<string, string>
            {
                [IdentityPolicyMetadataKeys.RequiredRoles] = "tenant-admin"
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: "tenant-boundary",
            displayName: "Tenant Boundary",
            description: "Prevents cross-tenant access unless identity, resource, and runtime context align.",
            modes: [AuthorizationMode.Abac, AuthorizationMode.Policy],
            tags: ["tenant", "boundary"],
            metadata: new Dictionary<string, string>
            {
                [IdentityPolicyMetadataKeys.RequireTenantMatch] = "true",
                [$"{IdentityPolicyMetadataKeys.SubjectAttributePrefix}region"] = "apac",
                [$"{IdentityPolicyMetadataKeys.ResourceAttributePrefix}classification"] = "internal"
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: "document-owner",
            displayName: "Document Owner",
            description: "Allows document owners to update their own tenant-scoped documents.",
            modes: [AuthorizationMode.Policy],
            tags: ["document", "owner"],
            metadata: new Dictionary<string, string>
            {
                [IdentityPolicyMetadataKeys.RequireOwner] = "true",
                [$"{IdentityPolicyMetadataKeys.ContextAttributePrefix}operation"] = "update"
            }));
    }
}
