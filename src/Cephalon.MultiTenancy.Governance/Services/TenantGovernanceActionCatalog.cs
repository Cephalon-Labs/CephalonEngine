using Cephalon.MultiTenancy.Governance.Configuration;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantGovernanceActionCatalog : ITenantGovernanceActionCatalog
{
    private readonly TenantGovernanceActionDescriptor[] configuredActions;
    private readonly TenantGovernanceActionRuntimeStore runtimeStore;

    public TenantGovernanceActionCatalog(
        MultiTenancyGovernanceOptions options,
        IEnumerable<ITenantGovernanceActionContributor> contributors,
        TenantGovernanceActionRuntimeStore runtimeStore)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contributors);
        ArgumentNullException.ThrowIfNull(runtimeStore);

        var registry = new TenantGovernanceActionRegistry();
        foreach (var action in options.GovernanceActions)
        {
            registry.Add(action);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterGovernanceActions(registry);
        }

        configuredActions = [.. registry.Build()];
        this.runtimeStore = runtimeStore;
    }

    public IReadOnlyList<TenantGovernanceActionDescriptor> Actions => BuildActions();

    public IReadOnlyList<TenantGovernanceActionDescriptor> GetByTenantId(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return Actions
            .Where(action => string.Equals(action.TenantId, tenantId.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public IReadOnlyList<TenantGovernanceActionDescriptor> GetByActionId(string actionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);

        return Actions
            .Where(action => string.Equals(action.ActionId, actionId.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public IReadOnlyList<TenantGovernanceActionDescriptor> GetByTenantAndAction(string tenantId, string actionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);

        return Actions
            .Where(action => string.Equals(
                CreateTenantActionKey(action.TenantId, action.ActionId),
                CreateTenantActionKey(tenantId, actionId),
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private IReadOnlyList<TenantGovernanceActionDescriptor> BuildActions()
    {
        var registry = new TenantGovernanceActionRegistry();
        foreach (var action in runtimeStore.Actions)
        {
            registry.Add(action);
        }

        foreach (var action in configuredActions)
        {
            registry.Add(action);
        }

        return registry.Build();
    }

    private static string CreateTenantActionKey(string tenantId, string actionId)
    {
        return $"{tenantId.Trim()}|{actionId.Trim()}";
    }
}
