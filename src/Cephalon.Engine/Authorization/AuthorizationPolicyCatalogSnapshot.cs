using Cephalon.Abstractions.Authorization;

namespace Cephalon.Engine.Authorization;

internal sealed class AuthorizationPolicyCatalogSnapshot : IAuthorizationPolicyCatalog
{
    private readonly IReadOnlyList<AuthorizationPolicyDescriptor> policies;
    private readonly Dictionary<string, AuthorizationPolicyDescriptor> policiesById;
    private readonly Dictionary<AuthorizationMode, IReadOnlyList<AuthorizationPolicyDescriptor>> policiesByMode;

    public AuthorizationPolicyCatalogSnapshot(IEnumerable<AuthorizationPolicyDescriptor> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);

        this.policies = policies.ToArray();
        policiesById = this.policies.ToDictionary(static policy => policy.Id, StringComparer.OrdinalIgnoreCase);
        policiesByMode = this.policies
            .SelectMany(static policy => policy.Modes.Select(mode => new KeyValuePair<AuthorizationMode, AuthorizationPolicyDescriptor>(mode, policy)))
            .GroupBy(static pair => pair.Key)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<AuthorizationPolicyDescriptor>)group.Select(static pair => pair.Value).ToArray());
    }

    public IReadOnlyList<AuthorizationPolicyDescriptor> Policies => policies;

    public AuthorizationPolicyDescriptor? GetById(string policyId)
    {
        if (string.IsNullOrWhiteSpace(policyId))
        {
            return null;
        }

        return policiesById.TryGetValue(policyId.Trim(), out var policy)
            ? policy
            : null;
    }

    public IReadOnlyList<AuthorizationPolicyDescriptor> GetByMode(AuthorizationMode mode)
    {
        return policiesByMode.TryGetValue(mode, out var matches)
            ? matches
            : [];
    }
}
