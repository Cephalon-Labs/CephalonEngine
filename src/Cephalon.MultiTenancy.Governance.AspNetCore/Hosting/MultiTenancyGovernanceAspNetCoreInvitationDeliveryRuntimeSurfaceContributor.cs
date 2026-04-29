using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.AspNetCore.Configuration;

namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

internal sealed class MultiTenancyGovernanceAspNetCoreInvitationDeliveryRuntimeSurfaceContributor(
    MultiTenancyGovernanceAspNetCoreOptions options,
    TenantInvitationDeliveryDispatchEndpointRuntimeCatalog runtimeCatalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var endpoint = runtimeCatalog.DispatchEndpoint;
        var endpointEnabled = options.EnableTenantInvitationDeliveryDispatchEndpoint;
        var endpointMapped = endpoint is not null;
        var routePattern = endpoint?.RoutePattern ??
            Normalize(options.TenantInvitationDeliveryDispatchRoutePattern) ??
            MultiTenancyGovernanceAspNetCoreOptions.DefaultTenantInvitationDeliveryDispatchRoutePattern;
        var requireAuthorization = endpoint?.RequireAuthorization ??
            options.RequireTenantInvitationDeliveryDispatchAuthorization;
        var authorizationPolicy = Normalize(endpoint?.AuthorizationPolicy) ??
            Normalize(options.TenantInvitationDeliveryDispatchAuthorizationPolicy);
        var excludeFromDescription = endpoint?.ExcludeFromDescription ??
            options.ExcludeTenantInvitationDeliveryDispatchEndpointFromDescription;
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
            ["tenantInvitationDeliveryDispatchEndpointOwnership"] = endpointOwnership,
            ["invitationDeliveryDispatchActionOwnership"] = endpointOwnership,
            ["invitationDeliveryDispatcherDependency"] = "ITenantInvitationDeliveryDispatcher",
            ["routePattern"] = routePattern,
            ["httpMethod"] = "POST",
            ["requestBodyContract"] = "TenantInvitationDeliveryRequest",
            ["responseBodyContract"] = "TenantInvitationDeliveryResult",
            ["requireAuthorization"] = requireAuthorization.ToString().ToLowerInvariant(),
            ["authorizationPolicyConfigured"] = (authorizationPolicy is not null).ToString().ToLowerInvariant(),
            ["authorizationPolicy"] = authorizationPolicy ?? "none",
            ["excludeFromDescription"] = excludeFromDescription.ToString().ToLowerInvariant(),
            ["dispatchSource"] = TenantInvitationDeliveryDispatchEndpointRouteBuilderExtensions.DefaultDispatchSource,
            ["dispatchMetadataMarker"] = "aspNetCoreInvitationDeliveryDispatch",
            ["dispatchRecordsOutcome"] = "request-dependent",
            ["externalDeliveryOwnership"] = "sender-dependent",
            ["providerSpecificSenderOwnership"] = "application-managed",
            ["durableRetryQueueOwnership"] = "application-managed",
            ["publicOnboardingOwnership"] = "application-managed",
            ["tenantAdminUiOwnership"] = "application-managed",
            ["identityProviderSyncOwnership"] = "application-managed",
            ["providerPollingOwnership"] = "application-managed",
            ["deliveryStatusCallbackOwnership"] = "separate-status-endpoint"
        };

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-invitation-delivery-http-endpoints",
            displayName: "Tenant Invitation Delivery HTTP Endpoints",
            description: "Projects the optional ASP.NET Core delivery dispatch endpoint mapped over the host-agnostic tenant invitation delivery dispatcher.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "tenant-invitation-delivery-dispatch-endpoint",
                    displayName: "Tenant Invitation Delivery Dispatch Endpoint",
                    description: "Summarizes whether the ASP.NET Core host adapter has mapped the POST delivery dispatch action endpoint and which authorization posture protects it.",
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
