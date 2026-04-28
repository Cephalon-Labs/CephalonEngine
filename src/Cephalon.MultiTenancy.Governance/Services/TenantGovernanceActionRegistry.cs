namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantGovernanceActionRegistry : ITenantGovernanceActionRegistry
{
    private readonly List<TenantGovernanceActionDescriptor> actions = [];

    public void Add(TenantGovernanceActionDescriptor action)
    {
        ArgumentNullException.ThrowIfNull(action);

        actions.Add(action);
    }

    public IReadOnlyList<TenantGovernanceActionDescriptor> Build()
    {
        return actions
            .GroupBy(
                static action => string.Join(
                    "|",
                    action.TenantId,
                    action.ActionId),
                StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(static action => action.TenantId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static action => action.ActionId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
