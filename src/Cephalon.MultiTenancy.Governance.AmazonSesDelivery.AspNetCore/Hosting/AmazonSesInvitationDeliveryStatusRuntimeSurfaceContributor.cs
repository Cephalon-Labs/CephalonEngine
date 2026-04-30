using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Hosting;

internal sealed class AmazonSesInvitationDeliveryStatusRuntimeSurfaceContributor(
    AmazonSesInvitationDeliveryAspNetCoreOptions options,
    AmazonSesInvitationDeliveryStatusCallbackRuntimeCatalog runtimeCatalog) : ITechnologyRuntimeContributor
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
        var acceptRawSesEventPayloads = endpoint?.AcceptRawSesEventPayloads ?? options.AcceptRawSesEventPayloads;
        var runtimeState = !endpointEnabled
            ? "disabled"
            : endpointMapped ? "mapped" : "configured-not-mapped";
        var endpointOwnership = !endpointEnabled
            ? "not-configured"
            : endpointMapped ? "cephalon-managed" : "host-mapping-required";

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = endpointOwnership,
            ["package"] = "Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore",
            ["adapter"] = "aspnetcore",
            ["provider"] = "amazon-ses",
            ["transport"] = "amazon-sns-http",
            ["runtimeState"] = runtimeState,
            ["endpointEnabled"] = endpointEnabled.ToString().ToLowerInvariant(),
            ["endpointMapped"] = endpointMapped.ToString().ToLowerInvariant(),
            ["tenantInvitationDeliveryStatusCallbackEndpointOwnership"] = endpointOwnership,
            ["amazonSesSnsTranslationOwnership"] = endpointOwnership,
            ["amazonSesSnsInboxOwnership"] = "application-managed",
            ["amazonSesSnsSubscriptionConfirmationOwnership"] = "application-managed",
            ["amazonSesSnsSignatureVerificationOwnership"] = "not-configured",
            ["amazonSesSnsSignatureVerificationRequired"] = "false",
            ["amazonSesSnsSignatureAlgorithm"] = "not-configured",
            ["amazonSesSnsReplayProtectionOwnership"] = "not-configured",
            ["amazonSesSnsEventIdIdempotencyOwnership"] = "not-configured",
            ["tenantInvitationDeliveryStatusReconcilerDependency"] = "ITenantInvitationDeliveryStatusReconciler",
            ["routePattern"] = routePattern,
            ["httpMethod"] = "POST",
            ["requestBodyContract"] = "Amazon SNS HTTP notification JSON object containing an Amazon SES event Message",
            ["responseBodyContract"] = "AmazonSesInvitationDeliveryStatusCallbackResult",
            ["requireAuthorization"] = requireAuthorization.ToString().ToLowerInvariant(),
            ["authorizationPolicyConfigured"] = (authorizationPolicy is not null).ToString().ToLowerInvariant(),
            ["authorizationPolicy"] = authorizationPolicy ?? "none",
            ["excludeFromDescription"] = excludeFromDescription.ToString().ToLowerInvariant(),
            ["requireProviderMessageMatch"] = requireProviderMessageMatch.ToString().ToLowerInvariant(),
            ["recordStatus"] = recordStatus.ToString().ToLowerInvariant(),
            ["maxRequestBodyBytes"] = maxRequestBodyBytes.ToString(CultureInfo.InvariantCulture),
            ["maxEventsPerRequest"] = maxEventsPerRequest.ToString(CultureInfo.InvariantCulture),
            ["mapEngagementEventsAsDelivered"] = mapEngagementEventsAsDelivered.ToString().ToLowerInvariant(),
            ["acceptRawSesEventPayloads"] = acceptRawSesEventPayloads.ToString().ToLowerInvariant(),
            ["source"] = options.GetSource(),
            ["actor"] = options.GetActor(),
            ["providerPollingOwnership"] = "application-managed",
            ["externalDeliveryStatusOwnership"] = "provider-managed"
        };

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-invitation-delivery-amazon-ses-status-callbacks",
            displayName: "Amazon SES Tenant Invitation Delivery Status Callbacks",
            description: "Projects the optional ASP.NET Core Amazon SES over SNS translator that reconciles provider delivery events through the Cephalon governance pipeline.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "amazon-ses-sns-status-callback-endpoint",
                    displayName: "Amazon SES SNS Status Callback Endpoint",
                    description: "Summarizes whether the Amazon SES over SNS endpoint has been mapped and which callback translation, authorization, and ownership posture protects it.",
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

internal sealed class AmazonSesInvitationDeliveryStatusCallbackRuntimeCatalog
{
    private readonly object syncRoot = new();
    private AmazonSesInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot? endpoint;

    public AmazonSesInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot? Endpoint
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
        bool acceptRawSesEventPayloads)
    {
        lock (syncRoot)
        {
            endpoint = new AmazonSesInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot(
                routePattern,
                requireAuthorization,
                authorizationPolicy,
                excludeFromDescription,
                requireProviderMessageMatch,
                recordStatus,
                maxRequestBodyBytes,
                maxEventsPerRequest,
                mapEngagementEventsAsDelivered,
                acceptRawSesEventPayloads);
        }
    }
}

internal sealed record AmazonSesInvitationDeliveryStatusCallbackEndpointRuntimeSnapshot(
    string RoutePattern,
    bool RequireAuthorization,
    string? AuthorizationPolicy,
    bool ExcludeFromDescription,
    bool RequireProviderMessageMatch,
    bool RecordStatus,
    int MaxRequestBodyBytes,
    int MaxEventsPerRequest,
    bool MapEngagementEventsAsDelivered,
    bool AcceptRawSesEventPayloads);
