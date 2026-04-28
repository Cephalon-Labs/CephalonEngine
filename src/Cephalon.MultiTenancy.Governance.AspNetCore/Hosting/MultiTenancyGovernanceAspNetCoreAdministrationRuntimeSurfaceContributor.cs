using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.AspNetCore.Configuration;

namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

internal sealed class MultiTenancyGovernanceAspNetCoreAdministrationRuntimeSurfaceContributor(
    MultiTenancyGovernanceAspNetCoreOptions options,
    TenantAdministrationEndpointRuntimeCatalog runtimeCatalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var commandEndpoint = runtimeCatalog.CommandEndpoint;
        var endpointEnabled = options.EnableTenantAdministrationCommandEndpoint;
        var endpointMapped = commandEndpoint is not null;
        var routePattern = commandEndpoint?.RoutePattern ??
            Normalize(options.TenantAdministrationCommandRoutePattern) ??
            MultiTenancyGovernanceAspNetCoreOptions.DefaultTenantAdministrationCommandRoutePattern;
        var requireAuthorization = commandEndpoint?.RequireAuthorization ??
            options.RequireTenantAdministrationAuthorization;
        var authorizationPolicy = Normalize(commandEndpoint?.AuthorizationPolicy) ??
            Normalize(options.TenantAdministrationAuthorizationPolicy);
        var excludeFromDescription = commandEndpoint?.ExcludeFromDescription ??
            options.ExcludeTenantAdministrationEndpointFromDescription;
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
            ["tenantAdminEndpointOwnership"] = endpointOwnership,
            ["tenantAdministrationWorkflowDependency"] = "ITenantAdministrationWorkflow",
            ["routePattern"] = routePattern,
            ["httpMethod"] = "POST",
            ["requestBodyContract"] = "TenantAdministrationWorkflowRequest",
            ["responseBodyContract"] = "TenantAdministrationWorkflowResult",
            ["requireAuthorization"] = requireAuthorization.ToString().ToLowerInvariant(),
            ["authorizationPolicyConfigured"] = (authorizationPolicy is not null).ToString().ToLowerInvariant(),
            ["authorizationPolicy"] = authorizationPolicy ?? "none",
            ["excludeFromDescription"] = excludeFromDescription.ToString().ToLowerInvariant(),
            ["publicOnboardingOwnership"] = "application-managed",
            ["tenantAdminUiOwnership"] = "application-managed",
            ["invitationDeliveryOwnership"] = "application-managed",
            ["identityProviderSyncOwnership"] = "application-managed"
        };

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-administration-http-endpoints",
            displayName: "Tenant Administration HTTP Endpoints",
            description: "Projects the optional ASP.NET Core tenant-administration command endpoint mapped over the host-agnostic governance workflow.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "tenant-administration-command-endpoint",
                    displayName: "Tenant Administration Command Endpoint",
                    description: "Summarizes whether the ASP.NET Core host adapter has mapped the POST tenant-administration workflow endpoint and which authorization posture protects it.",
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
