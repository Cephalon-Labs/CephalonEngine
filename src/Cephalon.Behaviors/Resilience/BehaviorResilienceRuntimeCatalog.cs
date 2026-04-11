using Cephalon.Abstractions.Resilience;

namespace Cephalon.Behaviors.Resilience;

internal sealed class BehaviorResilienceRuntimeCatalog : IBehaviorResilienceRuntimeCatalog
{
    private readonly BehaviorResiliencePolicyCatalog _catalog;
    private readonly BehaviorCircuitBreakerStateRegistry _circuitBreakerStates;

    public BehaviorResilienceRuntimeCatalog(
        BehaviorResiliencePolicyCatalog catalog,
        BehaviorCircuitBreakerStateRegistry? circuitBreakerStates = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        _catalog = catalog;
        _circuitBreakerStates = circuitBreakerStates ?? new BehaviorCircuitBreakerStateRegistry();
    }

    public IReadOnlyList<BehaviorResilienceRuntimeDescriptor> Policies => _catalog.Policies
        .Select(ToDescriptor)
        .ToArray();

    public BehaviorResilienceRuntimeDescriptor? GetById(string policyId)
    {
        if (string.IsNullOrWhiteSpace(policyId))
        {
            return null;
        }

        var policy = _catalog.GetById(policyId.Trim());
        return policy is null
            ? null
            : ToDescriptor(policy);
    }

    public BehaviorResilienceRuntimeDescriptor? Resolve(string behaviorId, string? transportId = null)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            return null;
        }

        var resolution = _catalog.Resolve(behaviorId, transportId);
        return resolution.Policy is null
            ? null
            : ToDescriptor(resolution.Policy);
    }

    private BehaviorResilienceRuntimeDescriptor ToDescriptor(ResolvedBehaviorResiliencePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return policy.ToDescriptor(_circuitBreakerStates.Get(policy.Id));
    }
}
