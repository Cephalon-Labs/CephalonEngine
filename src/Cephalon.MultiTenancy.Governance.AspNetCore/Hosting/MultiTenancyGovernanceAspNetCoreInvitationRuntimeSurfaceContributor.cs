using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.AspNetCore.Configuration;

namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

internal sealed class MultiTenancyGovernanceAspNetCoreInvitationRuntimeSurfaceContributor(
    MultiTenancyGovernanceAspNetCoreOptions options,
    TenantInvitationDeliveryStatusEndpointRuntimeCatalog runtimeCatalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var callbackEndpoint = runtimeCatalog.CallbackEndpoint;
        var observationEndpoint = runtimeCatalog.ObservationEndpoint;
        var endpointEnabled = options.EnableTenantInvitationDeliveryStatusCallbackEndpoint;
        var endpointMapped = callbackEndpoint is not null;
        var routePattern = callbackEndpoint?.RoutePattern ??
            Normalize(options.TenantInvitationDeliveryStatusCallbackRoutePattern) ??
            MultiTenancyGovernanceAspNetCoreOptions.DefaultTenantInvitationDeliveryStatusCallbackRoutePattern;
        var requireAuthorization = callbackEndpoint?.RequireAuthorization ??
            options.RequireTenantInvitationDeliveryStatusCallbackAuthorization;
        var authorizationPolicy = Normalize(callbackEndpoint?.AuthorizationPolicy) ??
            Normalize(options.TenantInvitationDeliveryStatusCallbackAuthorizationPolicy);
        var excludeFromDescription = callbackEndpoint?.ExcludeFromDescription ??
            options.ExcludeTenantInvitationDeliveryStatusCallbackEndpointFromDescription;
        var requireProviderMessageMatch = callbackEndpoint?.RequireProviderMessageMatch ??
            options.RequireTenantInvitationDeliveryStatusCallbackProviderMessageMatch;
        var callbackSignatureVerificationConfigured = callbackEndpoint?.CallbackSignatureVerificationConfigured ??
            !string.IsNullOrWhiteSpace(options.TenantInvitationDeliveryStatusCallbackSigningSecret);
        var signatureHeaderName = callbackEndpoint?.SignatureHeaderName ??
            Normalize(options.TenantInvitationDeliveryStatusCallbackSignatureHeaderName) ??
            MultiTenancyGovernanceAspNetCoreOptions.DefaultTenantInvitationDeliveryStatusCallbackSignatureHeaderName;
        var signatureTimestampHeaderName = callbackEndpoint?.SignatureTimestampHeaderName ??
            Normalize(options.TenantInvitationDeliveryStatusCallbackSignatureTimestampHeaderName) ??
            MultiTenancyGovernanceAspNetCoreOptions.DefaultTenantInvitationDeliveryStatusCallbackSignatureTimestampHeaderName;
        var signatureKeyIdHeaderName = callbackEndpoint?.SignatureKeyIdHeaderName ??
            Normalize(options.TenantInvitationDeliveryStatusCallbackSignatureKeyIdHeaderName) ??
            MultiTenancyGovernanceAspNetCoreOptions.DefaultTenantInvitationDeliveryStatusCallbackSignatureKeyIdHeaderName;
        var signatureKeyIdConfigured = callbackEndpoint?.SignatureKeyIdConfigured ??
            !string.IsNullOrWhiteSpace(options.TenantInvitationDeliveryStatusCallbackSigningKeyId);
        var signatureToleranceSeconds = callbackEndpoint?.SignatureToleranceSeconds ??
            Math.Max(1, options.TenantInvitationDeliveryStatusCallbackSignatureToleranceSeconds);
        var callbackReplayProtectionConfigured = callbackEndpoint?.CallbackReplayProtectionConfigured ??
            IsCallbackReplayProtectionConfigured(options, callbackSignatureVerificationConfigured);
        var replayRetentionSeconds = callbackEndpoint?.ReplayRetentionSeconds ??
            Math.Max(1, options.TenantInvitationDeliveryStatusCallbackReplayRetentionSeconds);
        var replayCacheLimit = callbackEndpoint?.ReplayCacheLimit ??
            Math.Max(1, options.TenantInvitationDeliveryStatusCallbackReplayCacheLimit);
        var runtimeState = !endpointEnabled
            ? "disabled"
            : endpointMapped ? "mapped" : "configured-not-mapped";
        var endpointOwnership = !endpointEnabled
            ? "not-configured"
            : endpointMapped ? "cephalon-managed" : "host-mapping-required";
        var observationEndpointEnabled = options.EnableTenantInvitationDeliveryStatusObservationEndpoint;
        var observationEndpointMapped = observationEndpoint is not null;
        var observationRoutePattern = observationEndpoint?.RoutePattern ??
            Normalize(options.TenantInvitationDeliveryStatusObservationRoutePattern) ??
            MultiTenancyGovernanceAspNetCoreOptions.DefaultTenantInvitationDeliveryStatusObservationRoutePattern;
        var observationRequireAuthorization = observationEndpoint?.RequireAuthorization ??
            options.RequireTenantInvitationDeliveryStatusObservationAuthorization;
        var observationAuthorizationPolicy = Normalize(observationEndpoint?.AuthorizationPolicy) ??
            Normalize(options.TenantInvitationDeliveryStatusObservationAuthorizationPolicy);
        var observationExcludeFromDescription = observationEndpoint?.ExcludeFromDescription ??
            options.ExcludeTenantInvitationDeliveryStatusObservationEndpointFromDescription;
        var observationDefaultLimit = observationEndpoint?.DefaultLimit ?? GetObservationDefaultLimit(options);
        var observationMaxLimit = observationEndpoint?.MaxLimit ?? GetObservationMaxLimit(options);
        var observationRuntimeState = !observationEndpointEnabled
            ? "disabled"
            : observationEndpointMapped ? "mapped" : "configured-not-mapped";
        var observationEndpointOwnership = !observationEndpointEnabled
            ? "not-configured"
            : observationEndpointMapped ? "cephalon-managed" : "host-mapping-required";

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = endpointOwnership,
            ["package"] = "Cephalon.MultiTenancy.Governance.AspNetCore",
            ["adapter"] = "aspnetcore",
            ["runtimeState"] = runtimeState,
            ["endpointEnabled"] = endpointEnabled.ToString().ToLowerInvariant(),
            ["endpointMapped"] = endpointMapped.ToString().ToLowerInvariant(),
            ["tenantInvitationDeliveryStatusCallbackEndpointOwnership"] = endpointOwnership,
            ["normalizedCallbackIngressOwnership"] = endpointOwnership,
            ["tenantInvitationDeliveryStatusReconcilerDependency"] = "ITenantInvitationDeliveryStatusReconciler",
            ["routePattern"] = routePattern,
            ["httpMethod"] = "POST",
            ["requestBodyContract"] = "TenantInvitationDeliveryStatusCallbackRequest",
            ["responseBodyContract"] = "TenantInvitationDeliveryStatusReconciliationResult",
            ["requireAuthorization"] = requireAuthorization.ToString().ToLowerInvariant(),
            ["authorizationPolicyConfigured"] = (authorizationPolicy is not null).ToString().ToLowerInvariant(),
            ["authorizationPolicy"] = authorizationPolicy ?? "none",
            ["excludeFromDescription"] = excludeFromDescription.ToString().ToLowerInvariant(),
            ["requireProviderMessageMatch"] = requireProviderMessageMatch.ToString().ToLowerInvariant(),
            ["callbackSignatureVerificationConfigured"] = callbackSignatureVerificationConfigured.ToString().ToLowerInvariant(),
            ["callbackSignatureVerificationOwnership"] = callbackSignatureVerificationConfigured ? endpointOwnership : "not-configured",
            ["signatureHeaderName"] = signatureHeaderName,
            ["signatureTimestampHeaderName"] = signatureTimestampHeaderName,
            ["signatureKeyIdHeaderName"] = signatureKeyIdHeaderName,
            ["signatureKeyIdConfigured"] = signatureKeyIdConfigured.ToString().ToLowerInvariant(),
            ["signatureToleranceSeconds"] = signatureToleranceSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["callbackReplayProtectionConfigured"] = callbackReplayProtectionConfigured.ToString().ToLowerInvariant(),
            ["callbackReplayProtectionOwnership"] = callbackReplayProtectionConfigured ? endpointOwnership : "not-configured",
            ["callbackReplayProtectionPolicy"] = callbackReplayProtectionConfigured ? "signed-callback" : "none",
            ["callbackReplayProtectionKey"] = callbackReplayProtectionConfigured ? "signature-fingerprint" : "none",
            ["callbackReplayProtectionScope"] = callbackReplayProtectionConfigured ? "process-local" : "none",
            ["callbackReplayProtectionDurability"] = "none",
            ["callbackReplayProtectionRetentionSeconds"] = replayRetentionSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["callbackReplayProtectionCacheLimit"] = replayCacheLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["callbackReplayProtectionRequiresSignature"] = "true",
            ["tenantInvitationDeliveryStatusObservationEndpointOwnership"] = observationEndpointOwnership,
            ["observationEndpointEnabled"] = observationEndpointEnabled.ToString().ToLowerInvariant(),
            ["observationEndpointMapped"] = observationEndpointMapped.ToString().ToLowerInvariant(),
            ["observationEndpointRuntimeState"] = observationRuntimeState,
            ["observationRoutePattern"] = observationRoutePattern,
            ["observationHttpMethod"] = "GET",
            ["observationResponseBodyContract"] = "TenantInvitationDeliveryStatusObservationQueryResult",
            ["observationStoreDependency"] = "ITenantInvitationDeliveryStatusObservationStore",
            ["observationStoreReadOwnership"] = observationEndpointOwnership,
            ["observationSummaryOwnership"] = observationEndpointOwnership,
            ["observationSummaryScope"] = "filtered-normalized-observations",
            ["observationSummaryDimensions"] = "status,attention,outcome,source,channel,sender,tenant",
            ["observationAttentionSummaryOwnership"] = observationEndpointOwnership,
            ["observationAttentionSummaryScope"] = "matched-normalized-observation-attention",
            ["observationAttentionCategories"] = TenantInvitationDeliveryStatusObservationAttentionCategories.KnownValues,
            ["observationRemediationHintOwnership"] = observationEndpointOwnership,
            ["observationRemediationHintScope"] = "matched-normalized-observation-attention",
            ["observationRemediationActions"] = TenantInvitationDeliveryStatusObservationRemediationActions.KnownValues,
            ["observationRequireAuthorization"] = observationRequireAuthorization.ToString().ToLowerInvariant(),
            ["observationAuthorizationPolicyConfigured"] = (observationAuthorizationPolicy is not null).ToString().ToLowerInvariant(),
            ["observationAuthorizationPolicy"] = observationAuthorizationPolicy ?? "none",
            ["observationExcludeFromDescription"] = observationExcludeFromDescription.ToString().ToLowerInvariant(),
            ["observationDefaultLimit"] = observationDefaultLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["observationMaxLimit"] = observationMaxLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["observationEndpointScope"] = "normalized-observation-history",
            ["observationEndpointDurability"] = "store-dependent",
            ["providerSpecificCallbackInboxOwnership"] = "application-managed",
            ["providerSpecificCallbackTranslationOwnership"] = "application-managed",
            ["providerSpecificSignatureVerificationOwnership"] = "application-managed",
            ["providerPollingOwnership"] = "application-managed",
            ["externalDeliveryStatusOwnership"] = "provider-managed"
        };

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-invitation-delivery-status-http-endpoints",
            displayName: "Tenant Invitation Delivery Status HTTP Endpoints",
            description: "Projects the optional ASP.NET Core delivery status callback endpoint mapped over the host-agnostic invitation delivery status reconciler.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "tenant-invitation-delivery-status-callback-endpoint",
                    displayName: "Tenant Invitation Delivery Status Callback Endpoint",
                    description: "Summarizes whether the ASP.NET Core host adapter has mapped the POST normalized delivery status callback endpoint and which authorization posture protects it.",
                    metadata: metadata)
            ]);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static bool IsCallbackReplayProtectionConfigured(
        MultiTenancyGovernanceAspNetCoreOptions options,
        bool callbackSignatureVerificationConfigured)
    {
        return options.EnableTenantInvitationDeliveryStatusCallbackReplayProtection &&
            callbackSignatureVerificationConfigured;
    }

    private static int GetObservationDefaultLimit(MultiTenancyGovernanceAspNetCoreOptions options)
    {
        return Math.Clamp(
            options.TenantInvitationDeliveryStatusObservationDefaultLimit,
            1,
            GetObservationMaxLimit(options));
    }

    private static int GetObservationMaxLimit(MultiTenancyGovernanceAspNetCoreOptions options)
    {
        return Math.Clamp(options.TenantInvitationDeliveryStatusObservationMaxLimit, 1, 1_000_000);
    }
}
