namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantGovernanceActionRuntimeStore
{
    private readonly object gate = new();
    private readonly Dictionary<string, TenantGovernanceActionDescriptor> actions = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<TenantGovernanceActionDescriptor> Actions
    {
        get
        {
            lock (gate)
            {
                return actions.Values
                    .OrderBy(static action => action.TenantId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static action => action.ActionId, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }
    }

    public int Count
    {
        get
        {
            lock (gate)
            {
                return actions.Count;
            }
        }
    }

    public void Upsert(TenantGovernanceActionDescriptor action)
    {
        ArgumentNullException.ThrowIfNull(action);

        lock (gate)
        {
            actions[CreateKey(action.TenantId, action.ActionId)] = action;
        }
    }

    private static string CreateKey(string tenantId, string actionId)
    {
        return $"{tenantId.Trim()}|{actionId.Trim()}";
    }
}
