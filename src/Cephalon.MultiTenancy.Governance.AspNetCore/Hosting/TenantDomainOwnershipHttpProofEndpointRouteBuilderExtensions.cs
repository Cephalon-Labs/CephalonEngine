using Cephalon.MultiTenancy.Governance.AspNetCore.Configuration;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

/// <summary>
/// Maps ASP.NET Core endpoints for tenant-domain ownership HTTP proof files published by Cephalon governance.
/// </summary>
public static class TenantDomainOwnershipHttpProofEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the tenant-domain ownership HTTP proof publication endpoint.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <returns>The same endpoint route builder for fluent routing composition.</returns>
    /// <remarks>
    /// This endpoint is opt-in and reads proof-file state from
    /// <see cref="ITenantDomainOwnershipHttpProofPublicationCatalog" />. The core governance package remains
    /// host-agnostic and only records the proof state that this adapter serves.
    /// </remarks>
    public static IEndpointRouteBuilder MapCephalonTenantDomainOwnershipHttpProofs(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetService<MultiTenancyGovernanceAspNetCoreOptions>() ??
            new MultiTenancyGovernanceAspNetCoreOptions();
        if (!options.EnableHttpProofPublicationEndpoint)
        {
            return endpoints;
        }

        var routePattern = string.IsNullOrWhiteSpace(options.RoutePattern)
            ? "/.well-known/cephalon/{**proofPath}"
            : options.RoutePattern.Trim();
        var builder = endpoints
            .MapGet(
                routePattern,
                (HttpContext context, ITenantDomainOwnershipHttpProofPublicationCatalog catalog) =>
                    ServePublishedProof(context, catalog, options))
            .WithName("CephalonTenantDomainOwnershipHttpProof");

        if (options.ExcludeFromDescription)
        {
            builder.ExcludeFromDescription();
        }

        return endpoints;
    }

    private static IResult ServePublishedProof(
        HttpContext context,
        ITenantDomainOwnershipHttpProofPublicationCatalog catalog,
        MultiTenancyGovernanceAspNetCoreOptions options)
    {
        var hostName = context.Request.Host.Host;
        var path = context.Request.Path.Value ?? string.Empty;
        var proof = catalog.GetByHostAndPath(hostName, path);
        if (proof is null)
        {
            return Results.NotFound();
        }

        if (!string.IsNullOrWhiteSpace(options.CacheControlHeader))
        {
            context.Response.Headers.CacheControl = options.CacheControlHeader.Trim();
        }

        return Results.Text(proof.HttpFileContent, proof.HttpContentType, Encoding.UTF8);
    }
}
