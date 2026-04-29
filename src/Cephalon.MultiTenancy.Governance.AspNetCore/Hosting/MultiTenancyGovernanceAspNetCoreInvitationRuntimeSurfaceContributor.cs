using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.AspNetCore.Configuration;

namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

internal sealed class MultiTenancyGovernanceAspNetCoreInvitationRuntimeSurfaceContributor(
    MultiTenancyGovernanceAspNetCoreOptions options,
    TenantInvitationDeliveryStatusCallbackEndpointRuntimeCatalog runtimeCatalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var callbackEndpoint = runtimeCatalog.CallbackEndpoint;
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
        var runtimeState = !endpointEnabled
            ? "disabled"
            : endpointMapped ? "mapped" : "configured-not-mapped";
        var endpointOwnership = !endpointEnabled
            ? "not-configured"
            : endpointMapped ? "cephalon-managed" : "host-mapping-required";

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
}
