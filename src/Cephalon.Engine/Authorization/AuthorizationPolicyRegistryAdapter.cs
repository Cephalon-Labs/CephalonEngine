using Cephalon.Abstractions.Authorization;

namespace Cephalon.Engine.Authorization;

internal sealed class AuthorizationPolicyRegistryAdapter(
    string moduleId,
    List<AuthorizationPolicyDescriptor> policies) : IAuthorizationPolicyRegistry
{
    public void Add(AuthorizationPolicyDescriptor policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (policy.Metadata.TryGetValue("sourceModuleId", out var declaredSourceModuleId) &&
            !string.Equals(declaredSourceModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Authorization policy '{policy.Id}' declared source module '{declaredSourceModuleId}', but it was contributed by module '{moduleId}'.");
        }

        var metadata = new Dictionary<string, string>(policy.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["sourceModuleId"] = moduleId
        };

        policies.Add(new AuthorizationPolicyDescriptor(
            policy.Id,
            policy.DisplayName,
            policy.Description,
            policy.Modes,
            policy.Tags,
            metadata));
    }
}
