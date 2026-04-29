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
        bool requireProviderMessageMatch,
        bool callbackSignatureVerificationConfigured,
        string signatureHeaderName,
        string signatureTimestampHeaderName,
        string signatureKeyIdHeaderName,
        bool signatureKeyIdConfigured,
        int signatureToleranceSeconds)
    {
        lock (syncRoot)
        {
            callbackEndpoint = new TenantInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot(
                routePattern,
                requireAuthorization,
                authorizationPolicy,
                excludeFromDescription,
                requireProviderMessageMatch,
                callbackSignatureVerificationConfigured,
                signatureHeaderName,
                signatureTimestampHeaderName,
                signatureKeyIdHeaderName,
                signatureKeyIdConfigured,
                signatureToleranceSeconds);
        }
    }
}

internal sealed record TenantInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot(
    string RoutePattern,
    bool RequireAuthorization,
    string? AuthorizationPolicy,
    bool ExcludeFromDescription,
    bool RequireProviderMessageMatch,
    bool CallbackSignatureVerificationConfigured,
    string SignatureHeaderName,
    string SignatureTimestampHeaderName,
    string SignatureKeyIdHeaderName,
    bool SignatureKeyIdConfigured,
    int SignatureToleranceSeconds);
