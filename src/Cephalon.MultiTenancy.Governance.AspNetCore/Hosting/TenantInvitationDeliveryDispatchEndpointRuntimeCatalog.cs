namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

internal sealed class TenantInvitationDeliveryDispatchEndpointRuntimeCatalog
{
    private readonly object syncRoot = new();
    private TenantInvitationDeliveryDispatchEndpointRuntimeSnapshot? dispatchEndpoint;

    public TenantInvitationDeliveryDispatchEndpointRuntimeSnapshot? DispatchEndpoint
    {
        get
        {
            lock (syncRoot)
            {
                return dispatchEndpoint;
            }
        }
    }

    public void RecordDispatchEndpointMapped(
        string routePattern,
        bool requireAuthorization,
        string? authorizationPolicy,
        bool excludeFromDescription)
    {
        lock (syncRoot)
        {
            dispatchEndpoint = new TenantInvitationDeliveryDispatchEndpointRuntimeSnapshot(
                routePattern,
                requireAuthorization,
                authorizationPolicy,
                excludeFromDescription);
        }
    }
}

internal sealed record TenantInvitationDeliveryDispatchEndpointRuntimeSnapshot(
    string RoutePattern,
    bool RequireAuthorization,
    string? AuthorizationPolicy,
    bool ExcludeFromDescription);
