using System.Globalization;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Technologies;
using Cephalon.Identity.Configuration;
using Cephalon.Identity.Policies;

namespace Cephalon.Identity.Services;

internal sealed class IdentityAuthorizationRuntimeSurfaceContributor(
    IdentityRuntimeOptions options,
    AppProfile appProfile,
    IAuthorizationPolicyCatalog policyCatalog,
    IEnumerable<IAuthorizationEvaluator> evaluators) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        if (!options.EnableRuntimeSurface)
        {
            return new TechnologyRuntimeSurface(
                technologyId: "identity-access",
                surfaceId: "identity-authorization",
                displayName: "Identity Authorization",
                description: "Projects the active Cephalon identity and authorization runtime answer.",
                entries: []);
        }

        var policies = policyCatalog.Policies
            .OrderBy(static policy => policy.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var selectedModes = appProfile.Identity.AuthorizationModes
            .Where(static mode => !string.IsNullOrWhiteSpace(mode))
            .Select(static mode => mode.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static mode => mode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var evaluatorTypes = evaluators
            .Select(static evaluator => evaluator.GetType().FullName ?? evaluator.GetType().Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static typeName => typeName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var defaultEvaluatorType = evaluatorTypes.FirstOrDefault(static typeName =>
            string.Equals(typeName, typeof(MetadataDrivenAuthorizationEvaluator).FullName, StringComparison.Ordinal));
        var defaultEvaluatorState = options.EnableDefaultEvaluator
            ? defaultEvaluatorType is null ? "not-configured" : "configured"
            : "disabled";
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["identitySelection"] = appProfile.Identity.Enabled switch
            {
                true => "enabled",
                false => "disabled",
                _ => "not-configured"
            },
            ["selectedAuthorizationModeCount"] = selectedModes.Length.ToString(CultureInfo.InvariantCulture),
            ["selectedAuthorizationModes"] = selectedModes.Length == 0 ? "none" : string.Join(",", selectedModes),
            ["policyCount"] = policies.Length.ToString(CultureInfo.InvariantCulture),
            ["rbacPolicyCount"] = policies.Count(static policy => policy.Modes.Contains(AuthorizationMode.Rbac)).ToString(CultureInfo.InvariantCulture),
            ["abacPolicyCount"] = policies.Count(static policy => policy.Modes.Contains(AuthorizationMode.Abac)).ToString(CultureInfo.InvariantCulture),
            ["policyModeCount"] = policies.Count(static policy => policy.Modes.Contains(AuthorizationMode.Policy)).ToString(CultureInfo.InvariantCulture),
            ["policyIds"] = policies.Length == 0 ? "none" : string.Join(",", policies.Select(static policy => policy.Id)),
            ["authorizationEvaluatorCount"] = evaluatorTypes.Length.ToString(CultureInfo.InvariantCulture),
            ["authorizationEvaluatorTypes"] = evaluatorTypes.Length == 0 ? "none" : string.Join(",", evaluatorTypes),
            ["defaultEvaluator"] = defaultEvaluatorState,
            ["defaultEvaluatorType"] = options.EnableDefaultEvaluator
                ? defaultEvaluatorType ?? "none"
                : "none",
            ["defaultEvaluatorEnabled"] = options.EnableDefaultEvaluator ? "true" : "false",
            ["requireExplicitPolicy"] = options.RequireExplicitPolicy ? "true" : "false",
            ["runtimeSurface"] = "enabled",
            ["declarativeConventions"] = string.Join(",",
                IdentityPolicyMetadataKeys.RequiredRoles,
                IdentityPolicyMetadataKeys.RequiredRoleMatch,
                IdentityPolicyMetadataKeys.RequireOwner,
                IdentityPolicyMetadataKeys.RequireTenantMatch,
                $"{IdentityPolicyMetadataKeys.SubjectAttributePrefix}*",
                $"{IdentityPolicyMetadataKeys.ResourceAttributePrefix}*",
                $"{IdentityPolicyMetadataKeys.ContextAttributePrefix}*")
        };

        return new TechnologyRuntimeSurface(
            technologyId: "identity-access",
            surfaceId: "identity-authorization",
            displayName: "Identity Authorization",
            description: "Projects the active Cephalon identity and authorization runtime answer.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "identity-runtime",
                    displayName: "Identity Runtime",
                    description: "Summarizes selected authorization modes, contributed policies, and the active evaluator path for the identity companion pack.",
                    metadata: metadata)
            ]);
    }
}
