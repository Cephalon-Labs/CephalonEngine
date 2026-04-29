using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Hosting;

internal sealed class SendGridInvitationDeliveryStatusRuntimeSurfaceContributor(
    SendGridInvitationDeliveryAspNetCoreOptions options,
    SendGridInvitationDeliveryStatusCallbackRuntimeCatalog runtimeCatalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var endpoint = runtimeCatalog.Endpoint;
        var endpointEnabled = options.EnableStatusCallbackEndpoint;
        var endpointMapped = endpoint is not null;
        var routePattern = endpoint?.RoutePattern ?? options.GetRoutePattern();
        var requireAuthorization = endpoint?.RequireAuthorization ?? options.RequireStatusCallbackAuthorization;
        var authorizationPolicy = Normalize(endpoint?.AuthorizationPolicy) ?? Normalize(options.StatusCallbackAuthorizationPolicy);
        var excludeFromDescription = endpoint?.ExcludeFromDescription ?? options.ExcludeStatusCallbackEndpointFromDescription;
        var requireProviderMessageMatch = endpoint?.RequireProviderMessageMatch ?? options.RequireProviderMessageMatch;
        var recordStatus = endpoint?.RecordStatus ?? options.RecordStatus;
        var maxRequestBodyBytes = endpoint?.MaxRequestBodyBytes ?? options.GetMaxRequestBodyBytes();
        var maxEventsPerRequest = endpoint?.MaxEventsPerRequest ?? options.GetMaxEventsPerRequest();
        var mapEngagementEventsAsDelivered = endpoint?.MapEngagementEventsAsDelivered ?? options.MapEngagementEventsAsDelivered;
        var normalizeProviderMessageId = endpoint?.NormalizeProviderMessageIdFromSgMessageId ?? options.NormalizeProviderMessageIdFromSgMessageId;
        var requireSignedEventWebhook = endpoint?.RequireSignedEventWebhook ?? options.RequireSignedEventWebhook;
        var signedEventWebhookPublicKeyConfigured = endpoint?.SignedEventWebhookPublicKeyConfigured ?? options.GetSignedEventWebhookPublicKey() is not null;
        var signedEventWebhookSignatureHeaderName = endpoint?.SignedEventWebhookSignatureHeaderName ?? options.GetSignedEventWebhookSignatureHeaderName();
        var signedEventWebhookTimestampHeaderName = endpoint?.SignedEventWebhookTimestampHeaderName ?? options.GetSignedEventWebhookTimestampHeaderName();
        var signedEventWebhookSignatureToleranceSeconds = endpoint?.SignedEventWebhookSignatureToleranceSeconds ?? options.GetSignedEventWebhookSignatureToleranceSeconds();
        var signatureVerificationOwnership = requireSignedEventWebhook ? "cephalon-managed" : "not-configured";
        var runtimeState = !endpointEnabled
            ? "disabled"
            : endpointMapped ? "mapped" : "configured-not-mapped";
        var endpointOwnership = !endpointEnabled
            ? "not-configured"
            : endpointMapped ? "cephalon-managed" : "host-mapping-required";

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = endpointOwnership,
            ["package"] = "Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore",
            ["adapter"] = "aspnetcore",
            ["provider"] = "sendgrid",
            ["runtimeState"] = runtimeState,
            ["endpointEnabled"] = endpointEnabled.ToString().ToLowerInvariant(),
            ["endpointMapped"] = endpointMapped.ToString().ToLowerInvariant(),
            ["tenantInvitationDeliveryStatusCallbackEndpointOwnership"] = endpointOwnership,
            ["sendGridEventWebhookTranslationOwnership"] = endpointOwnership,
            ["sendGridEventWebhookInboxOwnership"] = "application-managed",
            ["sendGridEventWebhookSignatureVerificationOwnership"] = signatureVerificationOwnership,
            ["sendGridEventWebhookSignatureVerificationRequired"] = requireSignedEventWebhook.ToString().ToLowerInvariant(),
            ["sendGridEventWebhookSignaturePublicKeyConfigured"] = signedEventWebhookPublicKeyConfigured.ToString().ToLowerInvariant(),
            ["sendGridEventWebhookSignatureAlgorithm"] = "ecdsa-sha256",
            ["sendGridEventWebhookSignaturePayload"] = "timestamp+raw-body",
            ["sendGridEventWebhookSignatureHeaderName"] = signedEventWebhookSignatureHeaderName,
            ["sendGridEventWebhookSignatureTimestampHeaderName"] = signedEventWebhookTimestampHeaderName,
            ["sendGridEventWebhookSignatureToleranceSeconds"] = signedEventWebhookSignatureToleranceSeconds.ToString(CultureInfo.InvariantCulture),
            ["sendGridEventWebhookOAuthVerificationOwnership"] = requireAuthorization ? "host-managed-authorization" : "not-configured",
            ["sendGridEventWebhookReplayProtectionOwnership"] = "application-managed",
            ["tenantInvitationDeliveryStatusReconcilerDependency"] = "ITenantInvitationDeliveryStatusReconciler",
            ["routePattern"] = routePattern,
            ["httpMethod"] = "POST",
            ["requestBodyContract"] = "SendGrid Event Webhook JSON array",
            ["responseBodyContract"] = "SendGridInvitationDeliveryStatusCallbackResult",
            ["requireAuthorization"] = requireAuthorization.ToString().ToLowerInvariant(),
            ["authorizationPolicyConfigured"] = (authorizationPolicy is not null).ToString().ToLowerInvariant(),
            ["authorizationPolicy"] = authorizationPolicy ?? "none",
            ["excludeFromDescription"] = excludeFromDescription.ToString().ToLowerInvariant(),
            ["requireProviderMessageMatch"] = requireProviderMessageMatch.ToString().ToLowerInvariant(),
            ["recordStatus"] = recordStatus.ToString().ToLowerInvariant(),
            ["maxRequestBodyBytes"] = maxRequestBodyBytes.ToString(CultureInfo.InvariantCulture),
            ["maxEventsPerRequest"] = maxEventsPerRequest.ToString(CultureInfo.InvariantCulture),
            ["mapEngagementEventsAsDelivered"] = mapEngagementEventsAsDelivered.ToString().ToLowerInvariant(),
            ["normalizeProviderMessageIdFromSgMessageId"] = normalizeProviderMessageId.ToString().ToLowerInvariant(),
            ["source"] = options.GetSource(),
            ["actor"] = options.GetActor(),
            ["providerPollingOwnership"] = "application-managed",
            ["externalDeliveryStatusOwnership"] = "provider-managed"
        };

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-invitation-delivery-sendgrid-status-callbacks",
            displayName: "SendGrid Tenant Invitation Delivery Status Callbacks",
            description: "Projects the optional ASP.NET Core SendGrid Event Webhook translator that reconciles provider delivery events through the Cephalon governance pipeline.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "sendgrid-event-webhook-status-callback-endpoint",
                    displayName: "SendGrid Event Webhook Status Callback Endpoint",
                    description: "Summarizes whether the SendGrid Event Webhook endpoint has been mapped and which callback translation, authorization, and ownership posture protects it.",
                    metadata: metadata)
            ]);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}

internal sealed class SendGridInvitationDeliveryStatusCallbackRuntimeCatalog
{
    private readonly object syncRoot = new();
    private SendGridInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot? endpoint;

    public SendGridInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot? Endpoint
    {
        get
        {
            lock (syncRoot)
            {
                return endpoint;
            }
        }
    }

    public void RecordEndpointMapped(
        string routePattern,
        bool requireAuthorization,
        string? authorizationPolicy,
        bool excludeFromDescription,
        bool requireProviderMessageMatch,
        bool recordStatus,
        int maxRequestBodyBytes,
        int maxEventsPerRequest,
        bool mapEngagementEventsAsDelivered,
        bool normalizeProviderMessageIdFromSgMessageId,
        bool requireSignedEventWebhook,
        bool signedEventWebhookPublicKeyConfigured,
        string signedEventWebhookSignatureHeaderName,
        string signedEventWebhookTimestampHeaderName,
        int signedEventWebhookSignatureToleranceSeconds)
    {
        lock (syncRoot)
        {
            endpoint = new SendGridInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot(
                routePattern,
                requireAuthorization,
                authorizationPolicy,
                excludeFromDescription,
                requireProviderMessageMatch,
                recordStatus,
                maxRequestBodyBytes,
                maxEventsPerRequest,
                mapEngagementEventsAsDelivered,
                normalizeProviderMessageIdFromSgMessageId,
                requireSignedEventWebhook,
                signedEventWebhookPublicKeyConfigured,
                signedEventWebhookSignatureHeaderName,
                signedEventWebhookTimestampHeaderName,
                signedEventWebhookSignatureToleranceSeconds);
        }
    }
}

internal sealed record SendGridInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot(
    string RoutePattern,
    bool RequireAuthorization,
    string? AuthorizationPolicy,
    bool ExcludeFromDescription,
    bool RequireProviderMessageMatch,
    bool RecordStatus,
    int MaxRequestBodyBytes,
    int MaxEventsPerRequest,
    bool MapEngagementEventsAsDelivered,
    bool NormalizeProviderMessageIdFromSgMessageId,
    bool RequireSignedEventWebhook,
    bool SignedEventWebhookPublicKeyConfigured,
    string SignedEventWebhookSignatureHeaderName,
    string SignedEventWebhookTimestampHeaderName,
    int SignedEventWebhookSignatureToleranceSeconds);
