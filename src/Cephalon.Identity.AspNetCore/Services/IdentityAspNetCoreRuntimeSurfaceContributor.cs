using System.Globalization;
using Cephalon.Abstractions.Technologies;
using Cephalon.Audit.Services;
using Cephalon.Identity.AspNetCore.Configuration;
using Cephalon.Identity.AspNetCore.Transports.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.Identity.AspNetCore.Services;

internal sealed class IdentityAspNetCoreRuntimeSurfaceContributor(
    IEnumerable<EndpointDataSource> endpointDataSources,
    IdentityAspNetCoreOptions options,
    IEnumerable<IAuditActorAccessor> auditActorAccessors,
    IEnumerable<IAuditRecorder> auditRecorders) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var protectedEndpoints = endpointDataSources
            .SelectMany(static dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(DescribeEndpoint)
            .Where(static endpoint => endpoint is not null)
            .Select(static endpoint => endpoint!)
            .ToArray();
        var policyIds = protectedEndpoints
            .SelectMany(static endpoint => endpoint.PolicyIds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static policyId => policyId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var integrationModes = protectedEndpoints
            .SelectMany(static endpoint => endpoint.IntegrationModes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static mode => mode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var auditActorBridgeActive = auditActorAccessors.Any(static accessor => accessor is HttpContextAuditActorAccessor);
        var auditPackActive = auditRecorders.Any();
        var minimalApiCount = protectedEndpoints.Count(static endpoint => endpoint.IntegrationModes.Contains("minimal-api", StringComparer.OrdinalIgnoreCase));
        var mvcCount = protectedEndpoints.Count(static endpoint => endpoint.IntegrationModes.Contains("mvc", StringComparer.OrdinalIgnoreCase));
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["adapter"] = "aspnetcore",
            ["protectedEndpointCount"] = protectedEndpoints.Length.ToString(CultureInfo.InvariantCulture),
            ["protectedPolicyCount"] = policyIds.Length.ToString(CultureInfo.InvariantCulture),
            ["protectedPolicyIds"] = policyIds.Length == 0 ? "none" : string.Join(",", policyIds),
            ["integrationModeCount"] = integrationModes.Length.ToString(CultureInfo.InvariantCulture),
            ["integrationModes"] = integrationModes.Length == 0 ? "none" : string.Join(",", integrationModes),
            ["minimalApiProtectedEndpointCount"] = minimalApiCount.ToString(CultureInfo.InvariantCulture),
            ["mvcProtectedEndpointCount"] = mvcCount.ToString(CultureInfo.InvariantCulture),
            ["allowAnonymousOverrideCount"] = protectedEndpoints.Count(static endpoint => endpoint.AllowAnonymous).ToString(CultureInfo.InvariantCulture),
            ["subjectIdClaimTypeCount"] = options.SubjectIdClaimTypes.Count.ToString(CultureInfo.InvariantCulture),
            ["tenantRouteKeyCount"] = options.TenantRouteKeys.Count.ToString(CultureInfo.InvariantCulture),
            ["resourceIdRouteKeyCount"] = options.ResourceIdRouteKeys.Count.ToString(CultureInfo.InvariantCulture),
            ["auditActorBridgeStatus"] = auditActorBridgeActive
                ? (auditPackActive ? "active" : "available")
                : (auditPackActive ? "custom-or-disabled" : "not-configured"),
            ["auditActorBridgeSource"] = auditActorBridgeActive ? "claims-principal" : "none",
            ["includeAllClaimsAsSubjectAttributes"] = options.IncludeAllClaimsAsSubjectAttributes ? "true" : "false",
            ["includeRouteValuesAsResourceAttributes"] = options.IncludeRouteValuesAsResourceAttributes ? "true" : "false",
            ["includeQueryStringAsContextAttributes"] = options.IncludeQueryStringAsContextAttributes ? "true" : "false",
            ["includeHeadersAsContextAttributes"] = options.IncludeHeadersAsContextAttributes ? "true" : "false"
        };

        return new TechnologyRuntimeSurface(
            technologyId: "identity-access",
            surfaceId: "identity-aspnetcore",
            displayName: "Identity ASP.NET Core Boundary",
            description: "Projects how the Cephalon ASP.NET Core identity adapter is protecting HTTP endpoints.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "identity-aspnetcore-boundary",
                    displayName: "Identity ASP.NET Core Boundary",
                    description: "Summarizes protected ASP.NET Core endpoints, policy ids, anonymous overrides, adapter integration modes, and the low-ceremony audit actor bridge state.",
                    metadata: metadata)
            ]);
    }

    private static ProtectedEndpointDescription? DescribeEndpoint(RouteEndpoint endpoint)
    {
        var minimalMetadata = endpoint.Metadata
            .GetOrderedMetadata<RestAuthorizationRequestMetadata>()
            .ToArray();
        var mvcMetadata = endpoint.Metadata
            .GetOrderedMetadata<RequireCephalonAuthorizationAttribute>()
            .Select(static attribute => attribute.CreateMetadata())
            .ToArray();
        if (minimalMetadata.Length == 0 && mvcMetadata.Length == 0)
        {
            return null;
        }

        var policyIds = minimalMetadata
            .Concat(mvcMetadata)
            .Select(static metadata => metadata.PolicyId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static policyId => policyId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var integrationModes = new List<string>(capacity: 2);
        if (minimalMetadata.Length > 0)
        {
            integrationModes.Add("minimal-api");
        }

        if (mvcMetadata.Length > 0)
        {
            integrationModes.Add("mvc");
        }

        return new ProtectedEndpointDescription(
            policyIds,
            integrationModes,
            endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null);
    }

    private sealed record ProtectedEndpointDescription(
        string[] PolicyIds,
        IReadOnlyList<string> IntegrationModes,
        bool AllowAnonymous);
}
