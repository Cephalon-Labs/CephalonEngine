namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

internal sealed class TenantAdministrationEndpointRuntimeCatalog
{
    private readonly object syncRoot = new();
    private TenantAdministrationEndpointRuntimeSnapshot? commandEndpoint;

    public TenantAdministrationEndpointRuntimeSnapshot? CommandEndpoint
    {
        get
        {
            lock (syncRoot)
            {
                return commandEndpoint;
            }
        }
    }

    public void RecordCommandEndpointMapped(
        string routePattern,
        bool requireAuthorization,
        string? authorizationPolicy,
        bool excludeFromDescription)
    {
        lock (syncRoot)
        {
            commandEndpoint = new TenantAdministrationEndpointRuntimeSnapshot(
                routePattern,
                requireAuthorization,
                authorizationPolicy,
                excludeFromDescription);
        }
    }
}

internal sealed record TenantAdministrationEndpointRuntimeSnapshot(
    string RoutePattern,
    bool RequireAuthorization,
    string? AuthorizationPolicy,
    bool ExcludeFromDescription);
