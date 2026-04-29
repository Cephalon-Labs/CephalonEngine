namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

internal sealed class TenantInvitationDeliveryStatusEndpointRuntimeCatalog
{
    private readonly object syncRoot = new();
    private TenantInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot? callbackEndpoint;
    private TenantInvitationDeliveryStatusObservationEndpointRuntimeSnapshot? observationEndpoint;

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

    public TenantInvitationDeliveryStatusObservationEndpointRuntimeSnapshot? ObservationEndpoint
    {
        get
        {
            lock (syncRoot)
            {
                return observationEndpoint;
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
        int signatureToleranceSeconds,
        bool callbackReplayProtectionConfigured,
        int replayRetentionSeconds,
        int replayCacheLimit)
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
                signatureToleranceSeconds,
                callbackReplayProtectionConfigured,
                replayRetentionSeconds,
                replayCacheLimit);
        }
    }

    public void RecordObservationEndpointMapped(
        string routePattern,
        bool requireAuthorization,
        string? authorizationPolicy,
        bool excludeFromDescription,
        int defaultLimit,
        int maxLimit)
    {
        lock (syncRoot)
        {
            observationEndpoint = new TenantInvitationDeliveryStatusObservationEndpointRuntimeSnapshot(
                routePattern,
                requireAuthorization,
                authorizationPolicy,
                excludeFromDescription,
                defaultLimit,
                maxLimit);
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
    int SignatureToleranceSeconds,
    bool CallbackReplayProtectionConfigured,
    int ReplayRetentionSeconds,
    int ReplayCacheLimit);

internal sealed record TenantInvitationDeliveryStatusObservationEndpointRuntimeSnapshot(
    string RoutePattern,
    bool RequireAuthorization,
    string? AuthorizationPolicy,
    bool ExcludeFromDescription,
    int DefaultLimit,
    int MaxLimit);
