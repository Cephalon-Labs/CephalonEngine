using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Hosting;

internal sealed class MailgunInvitationDeliveryStatusRuntimeSurfaceContributor(
    MailgunInvitationDeliveryAspNetCoreOptions options,
    MailgunInvitationDeliveryStatusCallbackRuntimeCatalog runtimeCatalog) : ITechnologyRuntimeContributor
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
        var normalizeProviderMessageId = endpoint?.NormalizeProviderMessageIdWithAngleBrackets ?? options.NormalizeProviderMessageIdWithAngleBrackets;
        var requireSignedWebhook = endpoint?.RequireSignedWebhook ?? options.RequireSignedWebhook;
        var signedWebhookSigningKeyConfigured = endpoint?.SignedWebhookSigningKeyConfigured ?? options.GetWebhookSigningKey() is not null;
        var signedWebhookSignatureToleranceSeconds = endpoint?.SignedWebhookSignatureToleranceSeconds ?? options.GetSignedWebhookSignatureToleranceSeconds();
        var acceptParentSignature = endpoint?.AcceptParentSignature ?? options.AcceptParentSignature;
        var signatureVerificationOwnership = requireSignedWebhook ? "cephalon-managed" : "not-configured";
        var runtimeState = !endpointEnabled
            ? "disabled"
            : endpointMapped ? "mapped" : "configured-not-mapped";
        var endpointOwnership = !endpointEnabled
            ? "not-configured"
            : endpointMapped ? "cephalon-managed" : "host-mapping-required";

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = endpointOwnership,
            ["package"] = "Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore",
            ["adapter"] = "aspnetcore",
            ["provider"] = "mailgun",
            ["runtimeState"] = runtimeState,
            ["endpointEnabled"] = endpointEnabled.ToString().ToLowerInvariant(),
            ["endpointMapped"] = endpointMapped.ToString().ToLowerInvariant(),
            ["tenantInvitationDeliveryStatusCallbackEndpointOwnership"] = endpointOwnership,
            ["mailgunWebhookTranslationOwnership"] = endpointOwnership,
            ["mailgunWebhookInboxOwnership"] = "application-managed",
            ["mailgunWebhookSignatureVerificationOwnership"] = signatureVerificationOwnership,
            ["mailgunWebhookSignatureVerificationRequired"] = requireSignedWebhook.ToString().ToLowerInvariant(),
            ["mailgunWebhookSigningKeyConfigured"] = signedWebhookSigningKeyConfigured.ToString().ToLowerInvariant(),
            ["mailgunWebhookSignatureAlgorithm"] = "hmac-sha256",
            ["mailgunWebhookSignaturePayload"] = "timestamp+token",
            ["mailgunWebhookSignatureField"] = "signature.signature",
            ["mailgunWebhookParentSignatureField"] = "signature.parent-signature",
            ["mailgunWebhookParentSignatureAccepted"] = acceptParentSignature.ToString().ToLowerInvariant(),
            ["mailgunWebhookSignatureToleranceSeconds"] = signedWebhookSignatureToleranceSeconds.ToString(CultureInfo.InvariantCulture),
            ["mailgunWebhookReplayProtectionOwnership"] = "not-configured",
            ["mailgunWebhookReplayProtectionPolicy"] = "none",
            ["mailgunWebhookReplayProtectionScope"] = "none",
            ["mailgunWebhookReplayProtectionDurability"] = "none",
            ["tenantInvitationDeliveryStatusReconcilerDependency"] = "ITenantInvitationDeliveryStatusReconciler",
            ["routePattern"] = routePattern,
            ["httpMethod"] = "POST",
            ["requestBodyContract"] = "Mailgun webhook JSON object or array",
            ["responseBodyContract"] = "MailgunInvitationDeliveryStatusCallbackResult",
            ["requireAuthorization"] = requireAuthorization.ToString().ToLowerInvariant(),
            ["authorizationPolicyConfigured"] = (authorizationPolicy is not null).ToString().ToLowerInvariant(),
            ["authorizationPolicy"] = authorizationPolicy ?? "none",
            ["excludeFromDescription"] = excludeFromDescription.ToString().ToLowerInvariant(),
            ["requireProviderMessageMatch"] = requireProviderMessageMatch.ToString().ToLowerInvariant(),
            ["recordStatus"] = recordStatus.ToString().ToLowerInvariant(),
            ["maxRequestBodyBytes"] = maxRequestBodyBytes.ToString(CultureInfo.InvariantCulture),
            ["maxEventsPerRequest"] = maxEventsPerRequest.ToString(CultureInfo.InvariantCulture),
            ["mapEngagementEventsAsDelivered"] = mapEngagementEventsAsDelivered.ToString().ToLowerInvariant(),
            ["normalizeProviderMessageIdWithAngleBrackets"] = normalizeProviderMessageId.ToString().ToLowerInvariant(),
            ["source"] = options.GetSource(),
            ["actor"] = options.GetActor(),
            ["providerPollingOwnership"] = "application-managed",
            ["externalDeliveryStatusOwnership"] = "provider-managed"
        };

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-invitation-delivery-mailgun-status-callbacks",
            displayName: "Mailgun Tenant Invitation Delivery Status Callbacks",
            description: "Projects the optional ASP.NET Core Mailgun webhook translator that reconciles provider delivery events through the Cephalon governance pipeline.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "mailgun-webhook-status-callback-endpoint",
                    displayName: "Mailgun Webhook Status Callback Endpoint",
                    description: "Summarizes whether the Mailgun webhook endpoint has been mapped and which callback translation, authorization, and ownership posture protects it.",
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

internal sealed class MailgunInvitationDeliveryStatusCallbackRuntimeCatalog
{
    private readonly object syncRoot = new();
    private MailgunInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot? endpoint;

    public MailgunInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot? Endpoint
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
        bool normalizeProviderMessageIdWithAngleBrackets,
        bool requireSignedWebhook,
        bool signedWebhookSigningKeyConfigured,
        int signedWebhookSignatureToleranceSeconds,
        bool acceptParentSignature)
    {
        lock (syncRoot)
        {
            endpoint = new MailgunInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot(
                routePattern,
                requireAuthorization,
                authorizationPolicy,
                excludeFromDescription,
                requireProviderMessageMatch,
                recordStatus,
                maxRequestBodyBytes,
                maxEventsPerRequest,
                mapEngagementEventsAsDelivered,
                normalizeProviderMessageIdWithAngleBrackets,
                requireSignedWebhook,
                signedWebhookSigningKeyConfigured,
                signedWebhookSignatureToleranceSeconds,
                acceptParentSignature);
        }
    }
}

internal sealed record MailgunInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot(
    string RoutePattern,
    bool RequireAuthorization,
    string? AuthorizationPolicy,
    bool ExcludeFromDescription,
    bool RequireProviderMessageMatch,
    bool RecordStatus,
    int MaxRequestBodyBytes,
    int MaxEventsPerRequest,
    bool MapEngagementEventsAsDelivered,
    bool NormalizeProviderMessageIdWithAngleBrackets,
    bool RequireSignedWebhook,
    bool SignedWebhookSigningKeyConfigured,
    int SignedWebhookSignatureToleranceSeconds,
    bool AcceptParentSignature);
