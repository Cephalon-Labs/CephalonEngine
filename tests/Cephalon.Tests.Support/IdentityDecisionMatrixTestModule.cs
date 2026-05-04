using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Identity.Policies;

namespace Cephalon.Tests.Support;

/// <summary>
/// Contributes a wide bank of edge-case authorization policies that exercise every metadata-rule
/// branch of the built-in <c>MetadataDrivenAuthorizationEvaluator</c> through the public
/// <see cref="IAuthorizationEvaluator"/> seam.
/// </summary>
internal sealed class IdentityDecisionMatrixTestModule : ModuleBase, IAuthorizationPolicyContributor
{
    public const string RolesAnyDefaultPolicyId = "matrix-roles-any-default";
    public const string RolesAllExplicitPolicyId = "matrix-roles-all";
    public const string RolesEmptyPolicyId = "matrix-roles-empty";
    public const string SubjectAttributePolicyId = "matrix-subject-attribute";
    public const string SubjectAttributeEmptyValuePolicyId = "matrix-subject-attribute-empty";
    public const string SubjectSpecialDisplayNamePolicyId = "matrix-subject-special-displayname";
    public const string ResourceAttributePolicyId = "matrix-resource-attribute";
    public const string ResourceSpecialResourceTypePolicyId = "matrix-resource-special-resourcetype";
    public const string ContextAttributePolicyId = "matrix-context-attribute";
    public const string ContextSpecialActionPolicyId = "matrix-context-special-action";
    public const string RequireOwnerPolicyId = "matrix-require-owner";
    public const string RequireTenantMatchPolicyId = "matrix-require-tenant-match";
    public const string CompositeTenantAndAttributePolicyId = "matrix-composite-tenant-attribute";
    public const string NoMetadataRulesPolicyId = "matrix-policy-no-rules";
    public const string ModeFallbackPolicyId = "matrix-mode-fallback";

    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "identity-decision-matrix-tests",
        displayName: "Identity Decision Matrix Tests",
        description: "Contributes edge-case authorization policies for the Cephalon.Identity built-in evaluator decision-matrix tests.",
        tags: ["identity", "authorization", "tests"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void RegisterPolicies(IAuthorizationPolicyRegistry policies)
    {
        policies.Add(new AuthorizationPolicyDescriptor(
            id: RolesAnyDefaultPolicyId,
            displayName: "Roles ANY (default)",
            description: "Subject must hold at least one of the listed roles; relies on the default ANY match.",
            modes: [AuthorizationMode.Rbac, AuthorizationMode.Policy],
            metadata: new Dictionary<string, string>
            {
                [IdentityPolicyMetadataKeys.RequiredRoles] = "admin,manager"
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: RolesAllExplicitPolicyId,
            displayName: "Roles ALL (explicit)",
            description: "Subject must hold every listed role; uses the explicit ALL match.",
            modes: [AuthorizationMode.Rbac, AuthorizationMode.Policy],
            metadata: new Dictionary<string, string>
            {
                [IdentityPolicyMetadataKeys.RequiredRoles] = "admin,manager",
                [IdentityPolicyMetadataKeys.RequiredRoleMatch] = IdentityPolicyMetadataKeys.RequiredRoleMatchAll
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: RolesEmptyPolicyId,
            displayName: "Roles empty",
            description: "Required-roles rule with an empty value list; should always deny.",
            modes: [AuthorizationMode.Rbac, AuthorizationMode.Policy],
            metadata: new Dictionary<string, string>
            {
                [IdentityPolicyMetadataKeys.RequiredRoles] = "   ,  ,"
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: SubjectAttributePolicyId,
            displayName: "Subject attribute",
            description: "Required subject attribute rule against a custom subject attribute.",
            modes: [AuthorizationMode.Abac, AuthorizationMode.Policy],
            metadata: new Dictionary<string, string>
            {
                [$"{IdentityPolicyMetadataKeys.SubjectAttributePrefix}region"] = "apac"
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: SubjectAttributeEmptyValuePolicyId,
            displayName: "Subject attribute empty required value",
            description: "Required subject attribute rule with a whitespace-only required value; should always deny.",
            modes: [AuthorizationMode.Abac, AuthorizationMode.Policy],
            metadata: new Dictionary<string, string>
            {
                [$"{IdentityPolicyMetadataKeys.SubjectAttributePrefix}region"] = "   "
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: SubjectSpecialDisplayNamePolicyId,
            displayName: "Subject special key (displayName)",
            description: "Required subject attribute rule that targets the built-in subject.displayName slot.",
            modes: [AuthorizationMode.Abac, AuthorizationMode.Policy],
            metadata: new Dictionary<string, string>
            {
                [$"{IdentityPolicyMetadataKeys.SubjectAttributePrefix}displayName"] = "Ada"
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: ResourceAttributePolicyId,
            displayName: "Resource attribute",
            description: "Required resource attribute rule against a custom resource attribute.",
            modes: [AuthorizationMode.Abac, AuthorizationMode.Policy],
            metadata: new Dictionary<string, string>
            {
                [$"{IdentityPolicyMetadataKeys.ResourceAttributePrefix}classification"] = "internal"
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: ResourceSpecialResourceTypePolicyId,
            displayName: "Resource special key (resourceType)",
            description: "Required resource attribute rule that targets the built-in resource.resourceType slot.",
            modes: [AuthorizationMode.Abac, AuthorizationMode.Policy],
            metadata: new Dictionary<string, string>
            {
                [$"{IdentityPolicyMetadataKeys.ResourceAttributePrefix}resourceType"] = "document"
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: ContextAttributePolicyId,
            displayName: "Context attribute",
            description: "Required context attribute rule against a custom context attribute.",
            modes: [AuthorizationMode.Abac, AuthorizationMode.Policy],
            metadata: new Dictionary<string, string>
            {
                [$"{IdentityPolicyMetadataKeys.ContextAttributePrefix}operation"] = "update"
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: ContextSpecialActionPolicyId,
            displayName: "Context special key (action)",
            description: "Required context attribute rule that targets the built-in context.action slot.",
            modes: [AuthorizationMode.Abac, AuthorizationMode.Policy],
            metadata: new Dictionary<string, string>
            {
                [$"{IdentityPolicyMetadataKeys.ContextAttributePrefix}action"] = "read"
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: RequireOwnerPolicyId,
            displayName: "Require owner",
            description: "Subject must own the resource; no other rules.",
            modes: [AuthorizationMode.Policy],
            metadata: new Dictionary<string, string>
            {
                [IdentityPolicyMetadataKeys.RequireOwner] = "true"
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: RequireTenantMatchPolicyId,
            displayName: "Require tenant match",
            description: "Subject, resource, and context must align on a single tenant; no other rules.",
            modes: [AuthorizationMode.Policy],
            metadata: new Dictionary<string, string>
            {
                [IdentityPolicyMetadataKeys.RequireTenantMatch] = "true"
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: CompositeTenantAndAttributePolicyId,
            displayName: "Composite tenant-match plus subject attribute",
            description: "Combines tenant match with a subject attribute rule to verify ruleCount aggregation.",
            modes: [AuthorizationMode.Abac, AuthorizationMode.Policy],
            metadata: new Dictionary<string, string>
            {
                [IdentityPolicyMetadataKeys.RequireTenantMatch] = "true",
                [$"{IdentityPolicyMetadataKeys.SubjectAttributePrefix}region"] = "apac"
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: NoMetadataRulesPolicyId,
            displayName: "Policy without metadata rules",
            description: "Active policy that contributes no metadata rules; the built-in evaluator should deny.",
            modes: [AuthorizationMode.Policy]));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: ModeFallbackPolicyId,
            displayName: "Mode-fallback policy",
            description: "Policy without declared modes; the evaluator should fall back to the configured authorization modes.",
            metadata: new Dictionary<string, string>
            {
                [IdentityPolicyMetadataKeys.RequiredRoles] = "auditor"
            }));
    }
}
