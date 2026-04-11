using Cephalon.Abstractions.Resilience;

namespace Cephalon.Behaviors.Resilience;

internal sealed class BehaviorResilienceRuntimeCatalog : IBehaviorResilienceRuntimeCatalog
{
    private readonly BehaviorResiliencePolicyCatalog _catalog;
    private readonly BehaviorResilienceRuntimeDescriptor[] _policies;
    private readonly Dictionary<string, BehaviorResilienceRuntimeDescriptor> _policiesById;

    public BehaviorResilienceRuntimeCatalog(BehaviorResiliencePolicyCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        _catalog = catalog;
        _policies = catalog.Policies
            .Select(static policy => policy.ToDescriptor())
            .ToArray();
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

    public BehaviorResilienceRuntimeDescriptor? Resolve(string behaviorId, string? transportId = null)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            return null;
        }

        var resolution = _catalog.Resolve(behaviorId, transportId);
        return resolution.Policy is null
            ? null
            : GetById(resolution.Policy.Id);
    }
}
