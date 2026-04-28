using Cephalon.MultiTenancy.Governance.Configuration;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantGovernanceActionCatalog : ITenantGovernanceActionCatalog
{
    private readonly Dictionary<string, TenantGovernanceActionDescriptor[]> byTenantId;
    private readonly Dictionary<string, TenantGovernanceActionDescriptor[]> byActionId;
    private readonly Dictionary<string, TenantGovernanceActionDescriptor[]> byTenantAndAction;

    public TenantGovernanceActionCatalog(
        MultiTenancyGovernanceOptions options,
        IEnumerable<ITenantGovernanceActionContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contributors);

        var registry = new TenantGovernanceActionRegistry();
        foreach (var action in options.GovernanceActions)
        {
            registry.Add(action);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterGovernanceActions(registry);
        }

        Actions = registry.Build();
        byTenantId = Actions
            .GroupBy(static action => action.TenantId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        byActionId = Actions
            .GroupBy(static action => action.ActionId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        byTenantAndAction = Actions
            .GroupBy(
                static action => CreateTenantActionKey(action.TenantId, action.ActionId),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<TenantGovernanceActionDescriptor> Actions { get; }

    public IReadOnlyList<TenantGovernanceActionDescriptor> GetByTenantId(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return byTenantId.TryGetValue(tenantId.Trim(), out var actions)
            ? actions
            : [];
    }

    public IReadOnlyList<TenantGovernanceActionDescriptor> GetByActionId(string actionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);

        return byActionId.TryGetValue(actionId.Trim(), out var actions)
            ? actions
            : [];
    }

    public IReadOnlyList<TenantGovernanceActionDescriptor> GetByTenantAndAction(string tenantId, string actionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);

        return byTenantAndAction.TryGetValue(CreateTenantActionKey(tenantId, actionId), out var actions)
            ? actions
            : [];
    }

    private static string CreateTenantActionKey(string tenantId, string actionId)
    {
        return $"{tenantId.Trim()}|{actionId.Trim()}";
    }
}
