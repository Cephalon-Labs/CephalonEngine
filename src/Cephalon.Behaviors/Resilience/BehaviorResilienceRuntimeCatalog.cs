using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Resilience;
using Cephalon.Resilience;

namespace Cephalon.Behaviors.Resilience;

internal sealed class BehaviorResilienceRuntimeCatalog : IBehaviorResilienceRuntimeCatalog
{
    private readonly BehaviorResiliencePolicyCatalog _catalog;
    private readonly BehaviorCircuitBreakerStateRegistry _circuitBreakerStates;
    private readonly BehaviorIdempotencyResolver _idempotencyResolver;

    public BehaviorResilienceRuntimeCatalog(
        BehaviorResiliencePolicyCatalog catalog,
        BehaviorCircuitBreakerStateRegistry circuitBreakerStates,
        BehaviorIdempotencyResolver idempotencyResolver)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(circuitBreakerStates);
        ArgumentNullException.ThrowIfNull(idempotencyResolver);

        _catalog = catalog;
        _circuitBreakerStates = circuitBreakerStates;
        _idempotencyResolver = idempotencyResolver;
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
            : ToDescriptor(
                resolution.Policy,
                _idempotencyResolver.Resolve(behaviorId.Trim()));
    }

    private BehaviorResilienceRuntimeDescriptor ToDescriptor(ResolvedBehaviorResiliencePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return policy.ToDescriptor(_circuitBreakerStates.Get(policy.Id));
    }

    private BehaviorResilienceRuntimeDescriptor ToDescriptor(
        ResolvedBehaviorResiliencePolicy policy,
        BehaviorIdempotencyMode behaviorIdempotency)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return policy.ToDescriptor(
            _circuitBreakerStates.Get(policy.Id),
            behaviorIdempotency);
    }
}
