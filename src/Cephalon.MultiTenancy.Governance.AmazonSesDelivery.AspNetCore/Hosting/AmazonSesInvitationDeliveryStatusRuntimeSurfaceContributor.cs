using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.Configuration;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Hosting;

internal sealed class AmazonSesInvitationDeliveryStatusRuntimeSurfaceContributor(
    AmazonSesInvitationDeliveryAspNetCoreOptions options,
    AmazonSesInvitationDeliveryStatusCallbackRuntimeCatalog runtimeCatalog,
    IServiceProvider serviceProvider) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var governanceOptions = serviceProvider.GetService<MultiTenancyGovernanceOptions>();
        var observationStoreConfigured = governanceOptions?.EnableInvitationDeliveryStatusObservationStore == true;
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
        var requireSnsSignatureVerification = endpoint?.RequireSnsSignatureVerification ?? options.RequireSnsSignatureVerification;
        var requireSnsSignatureVersion2 = endpoint?.RequireSnsSignatureVersion2 ?? options.RequireSnsSignatureVersion2;
        var requireAllowedSnsTopicArn = endpoint?.RequireAllowedSnsTopicArn ?? options.RequireAllowedSnsTopicArn;
        var allowedSnsTopicArnCount = endpoint?.AllowedSnsTopicArnCount ?? options.GetAllowedSnsTopicArns().Count;
        var pinnedSnsSigningCertificateConfigured = endpoint?.PinnedSnsSigningCertificateConfigured ?? options.GetPinnedSnsSigningCertificatePem() is not null;
        var validateSnsSigningCertificateChain = endpoint?.ValidateSnsSigningCertificateChain ?? options.ValidateSnsSigningCertificateChain;
        var snsReplayProtectionConfigured = endpoint?.SnsReplayProtectionConfigured ?? options.IsSnsReplayProtectionConfigured();
        var snsReplayRetentionSeconds = endpoint?.SnsReplayRetentionSeconds ?? options.GetSnsReplayRetentionSeconds();
        var snsReplayCacheLimit = endpoint?.SnsReplayCacheLimit ?? options.GetSnsReplayCacheLimit();
        var snsMessageIdIdempotencyConfigured =
            (endpoint?.SnsMessageIdIdempotencyConfigured ?? options.IsSnsMessageIdIdempotencyConfigured()) &&
            observationStoreConfigured;
        var snsSubscriptionConfirmationConfigured =
            endpoint?.SnsSubscriptionConfirmationConfigured ?? options.IsSnsSubscriptionConfirmationConfigured();
        var snsUnsubscribeConfirmationObservationConfigured =
            endpoint?.SnsUnsubscribeConfirmationObservationConfigured ?? options.IsSnsUnsubscribeConfirmationObservationConfigured();
        var snsSubscriptionConfirmationTimeoutSeconds =
            (int)(endpoint?.SnsSubscriptionConfirmationTimeout ?? options.GetSnsSubscriptionConfirmationTimeout()).TotalSeconds;
        var signatureVerificationOwnership = requireSnsSignatureVerification ? "cephalon-managed" : "not-configured";
        var replayProtectionOwnership = snsReplayProtectionConfigured ? "cephalon-managed" : "not-configured";
        var messageIdIdempotencyOwnership = snsMessageIdIdempotencyConfigured ? "cephalon-managed" : "not-configured";
        var subscriptionConfirmationOwnership = snsSubscriptionConfirmationConfigured ? "cephalon-managed" : "application-managed";
        var unsubscribeConfirmationObservationOwnership = snsUnsubscribeConfirmationObservationConfigured ? "cephalon-managed" : "application-managed";
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
            ["amazonSesSnsSubscriptionConfirmationConfigured"] = snsSubscriptionConfirmationConfigured.ToString().ToLowerInvariant(),
            ["amazonSesSnsSubscriptionConfirmationOwnership"] = subscriptionConfirmationOwnership,
            ["amazonSesSnsSubscriptionConfirmationRequiresSignature"] = "true",
            ["amazonSesSnsSubscriptionConfirmationHttpMethod"] = "GET",
            ["amazonSesSnsSubscriptionConfirmationUrlPolicy"] = snsSubscriptionConfirmationConfigured ? "https-sns-confirm-subscription" : "not-configured",
            ["amazonSesSnsSubscriptionConfirmationTimeoutSeconds"] = snsSubscriptionConfirmationTimeoutSeconds.ToString(CultureInfo.InvariantCulture),
            ["amazonSesSnsSubscriptionConfirmationClient"] = "IAmazonSesSnsSubscriptionConfirmationClient",
            ["amazonSesSnsUnsubscribeConfirmationObservationConfigured"] = snsUnsubscribeConfirmationObservationConfigured.ToString().ToLowerInvariant(),
            ["amazonSesSnsUnsubscribeConfirmationObservationOwnership"] = unsubscribeConfirmationObservationOwnership,
            ["amazonSesSnsUnsubscribeConfirmationRequiresSignature"] = "true",
            ["amazonSesSnsUnsubscribeConfirmationAction"] = "observe-only",
            ["amazonSesSnsUnsubscribeConfirmationSubscribeUrlPolicy"] = snsUnsubscribeConfirmationObservationConfigured ? "validated-never-invoked" : "not-configured",
            ["amazonSesSnsSignatureVerificationOwnership"] = signatureVerificationOwnership,
            ["amazonSesSnsSignatureVerificationRequired"] = requireSnsSignatureVerification.ToString().ToLowerInvariant(),
            ["amazonSesSnsSignatureVersion2Required"] = requireSnsSignatureVersion2.ToString().ToLowerInvariant(),
            ["amazonSesSnsAllowedTopicArnRequired"] = requireAllowedSnsTopicArn.ToString().ToLowerInvariant(),
            ["amazonSesSnsAllowedTopicArnCount"] = allowedSnsTopicArnCount.ToString(CultureInfo.InvariantCulture),
            ["amazonSesSnsPinnedSigningCertificateConfigured"] = pinnedSnsSigningCertificateConfigured.ToString().ToLowerInvariant(),
            ["amazonSesSnsSigningCertificateChainValidation"] = validateSnsSigningCertificateChain.ToString().ToLowerInvariant(),
            ["amazonSesSnsSignatureAlgorithm"] = requireSnsSignatureVerification
                ? (requireSnsSignatureVersion2 ? "rsa-sha256" : "rsa-sha1-or-rsa-sha256")
                : "not-configured",
            ["amazonSesSnsSignaturePayload"] = requireSnsSignatureVerification ? "sns-canonical-string" : "not-configured",
            ["amazonSesSnsSigningCertificateUrlPolicy"] = requireSnsSignatureVerification ? "https-sns-amazonaws-pem" : "not-configured",
            ["amazonSesSnsReplayProtectionConfigured"] = snsReplayProtectionConfigured.ToString().ToLowerInvariant(),
            ["amazonSesSnsReplayProtectionOwnership"] = replayProtectionOwnership,
            ["amazonSesSnsReplayProtectionPolicy"] = snsReplayProtectionConfigured ? "sns-message-id" : "none",
            ["amazonSesSnsReplayProtectionKey"] = snsReplayProtectionConfigured ? "topic-arn+message-id" : "none",
            ["amazonSesSnsReplayProtectionScope"] = snsReplayProtectionConfigured ? "process-local" : "none",
            ["amazonSesSnsReplayProtectionDurability"] = "none",
            ["amazonSesSnsReplayProtectionRetentionSeconds"] = snsReplayRetentionSeconds.ToString(CultureInfo.InvariantCulture),
            ["amazonSesSnsReplayProtectionCacheLimit"] = snsReplayCacheLimit.ToString(CultureInfo.InvariantCulture),
            ["amazonSesSnsReplayProtectionRequiresSignature"] = "true",
            ["amazonSesSnsMessageIdIdempotencyConfigured"] = snsMessageIdIdempotencyConfigured.ToString().ToLowerInvariant(),
            ["amazonSesSnsMessageIdIdempotencyOwnership"] = messageIdIdempotencyOwnership,
            ["amazonSesSnsMessageIdIdempotencyPolicy"] = snsMessageIdIdempotencyConfigured ? "sns-message-id" : "none",
            ["amazonSesSnsMessageIdIdempotencyKey"] = snsMessageIdIdempotencyConfigured ? "MessageId" : "none",
            ["amazonSesSnsMessageIdIdempotencyScope"] = snsMessageIdIdempotencyConfigured ? "observation-store" : "none",
            ["amazonSesSnsMessageIdIdempotencyDurability"] = "observation-store-dependent",
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
        bool acceptRawSesEventPayloads,
        bool requireSnsSignatureVerification,
        bool requireSnsSignatureVersion2,
        bool requireAllowedSnsTopicArn,
        int allowedSnsTopicArnCount,
        bool pinnedSnsSigningCertificateConfigured,
        bool validateSnsSigningCertificateChain,
        bool snsReplayProtectionConfigured,
        int snsReplayRetentionSeconds,
        int snsReplayCacheLimit,
        bool snsMessageIdIdempotencyConfigured,
        bool snsSubscriptionConfirmationConfigured,
        bool snsUnsubscribeConfirmationObservationConfigured,
        TimeSpan snsSubscriptionConfirmationTimeout)
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
                acceptRawSesEventPayloads,
                requireSnsSignatureVerification,
                requireSnsSignatureVersion2,
                requireAllowedSnsTopicArn,
                allowedSnsTopicArnCount,
                pinnedSnsSigningCertificateConfigured,
                validateSnsSigningCertificateChain,
                snsReplayProtectionConfigured,
                snsReplayRetentionSeconds,
                snsReplayCacheLimit,
                snsMessageIdIdempotencyConfigured,
                snsSubscriptionConfirmationConfigured,
                snsUnsubscribeConfirmationObservationConfigured,
                snsSubscriptionConfirmationTimeout);
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
    bool AcceptRawSesEventPayloads,
    bool RequireSnsSignatureVerification,
    bool RequireSnsSignatureVersion2,
    bool RequireAllowedSnsTopicArn,
    int AllowedSnsTopicArnCount,
    bool PinnedSnsSigningCertificateConfigured,
    bool ValidateSnsSigningCertificateChain,
    bool SnsReplayProtectionConfigured,
    int SnsReplayRetentionSeconds,
    int SnsReplayCacheLimit,
    bool SnsMessageIdIdempotencyConfigured,
    bool SnsSubscriptionConfirmationConfigured,
    bool SnsUnsubscribeConfirmationObservationConfigured,
    TimeSpan SnsSubscriptionConfirmationTimeout);
