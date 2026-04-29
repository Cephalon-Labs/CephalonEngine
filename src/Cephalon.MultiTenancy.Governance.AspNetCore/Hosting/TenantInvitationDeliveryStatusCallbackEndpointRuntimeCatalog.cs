namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

internal sealed class TenantInvitationDeliveryStatusCallbackEndpointRuntimeCatalog
{
    private readonly object syncRoot = new();
    private TenantInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot? callbackEndpoint;

    public TenantInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot? CallbackEndpoint
    {
        get
        {
            lock (syncRoot)
            {
                return callbackEndpoint;
            }
        }
    }

    public void RecordCallbackEndpointMapped(
        string routePattern,
        bool requireAuthorization,
        string? authorizationPolicy,
        bool excludeFromDescription,
        bool requireProviderMessageMatch)
    {
        lock (syncRoot)
        {
            callbackEndpoint = new TenantInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot(
                routePattern,
                requireAuthorization,
                authorizationPolicy,
                excludeFromDescription,
                requireProviderMessageMatch);
        }
    }
}

internal sealed record TenantInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot(
    string RoutePattern,
    bool RequireAuthorization,
    string? AuthorizationPolicy,
    bool ExcludeFromDescription,
    bool RequireProviderMessageMatch);
