using Cephalon.Abstractions.Resilience;

namespace Cephalon.Behaviors.Resilience;

internal sealed class BehaviorResilienceRuntimeCatalog : IBehaviorResilienceRuntimeCatalog
{
    private readonly BehaviorResilienceRuntimeDescriptor[] _policies;
    private readonly Dictionary<string, BehaviorResilienceRuntimeDescriptor> _policiesById;

    public BehaviorResilienceRuntimeCatalog(IReadOnlyList<BehaviorResilienceRuntimeDescriptor> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);

        _policies = policies.ToArray();
        _policiesById = _policies.ToDictionary(
            static policy => policy.Id,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<BehaviorResilienceRuntimeDescriptor> Policies => _policies;

    public BehaviorResilienceRuntimeDescriptor? GetById(string policyId)
    {
        if (string.IsNullOrWhiteSpace(policyId))
        {
            return null;
        }

        return _policiesById.TryGetValue(policyId.Trim(), out var policy)
            ? policy
            : null;
    }
}
