using Microsoft.AspNetCore.Authorization;
using Cephalon.Identity.AspNetCore.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Identity.AspNetCore.Transports.Rest;

/// <summary>
/// Adds Cephalon-specific authorization conventions to REST route handlers and groups.
/// </summary>
public static class IdentityEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Declares the ASP.NET Core authentication schemes that should own challenge and forbid responses for an endpoint or route group.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint or route-group builder to annotate.</param>
    /// <param name="authenticationSchemes">The authentication scheme names to use for boundary responses.</param>
    /// <returns>The same builder for fluent chaining.</returns>
    public static TBuilder WithCephalonAuthenticationSchemes<TBuilder>(
        this TBuilder builder,
        params string[] authenticationSchemes)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var normalizedSchemes = authenticationSchemes?
            .Where(static scheme => !string.IsNullOrWhiteSpace(scheme))
            .Select(static scheme => scheme.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static scheme => scheme, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        if (normalizedSchemes.Length == 0)
        {
            throw new ArgumentException("At least one authentication scheme must be provided.", nameof(authenticationSchemes));
        }

        builder.WithMetadata(new CephalonAuthenticationSchemesMetadata(normalizedSchemes));
        return builder;
    }

    /// <summary>
    /// Requires a Cephalon authorization decision before a REST route handler can execute.
    /// </summary>
    /// <param name="builder">The route handler builder to protect.</param>
    /// <param name="policyId">The Cephalon authorization policy id that must allow the request.</param>
    /// <param name="action">
    /// The optional action to evaluate. When omitted, the adapter maps the HTTP method to a conventional action such as
    /// <c>read</c>, <c>create</c>, <c>update</c>, or <c>delete</c>.
    /// </param>
    /// <param name="resourceType">
    /// The optional logical resource type. When omitted, the adapter derives it from the final literal segment in the
    /// endpoint route pattern.
    /// </param>
    /// <param name="resourceIdRouteKey">
    /// The optional route-value key that provides the resource identifier. When omitted, the adapter falls back to the
    /// configured resource-id route keys.
    /// </param>
    /// <param name="tenantRouteKey">
    /// The optional route-value key that provides the tenant identifier. When omitted, the adapter falls back to the
    /// configured tenant route keys and tenant headers.
    /// </param>
    /// <param name="ownerSubjectIdRouteKey">
    /// The optional route-value key that provides the owning subject identifier for owner-based policies.
    /// </param>
    /// <returns>The same route handler builder for fluent convention chaining.</returns>
    /// <remarks>
    /// This helper keeps ASP.NET Core principal and route parsing in the host layer while still evaluating the shared
    /// Cephalon authorization contracts through <see cref="Cephalon.Abstractions.Authorization.IAuthorizationEvaluator" />.
    /// </remarks>
    public static RouteHandlerBuilder RequireCephalonAuthorization(
        this RouteHandlerBuilder builder,
        string policyId,
        string? action = null,
        string? resourceType = null,
        string? resourceIdRouteKey = null,
        string? tenantRouteKey = null,
        string? ownerSubjectIdRouteKey = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var metadata = CreateMetadata(policyId, action, resourceType, resourceIdRouteKey, tenantRouteKey, ownerSubjectIdRouteKey);
        builder.WithMetadata(metadata);
        builder.AddEndpointFilterFactory((_, next) => CreateFilter(metadata, next));
        return builder;
    }

    /// <summary>
    /// Requires a Cephalon authorization decision before every REST route handler in the route group can execute.
    /// </summary>
    /// <param name="builder">The route group builder to protect.</param>
    /// <param name="policyId">The Cephalon authorization policy id that must allow the request.</param>
    /// <param name="action">
    /// The optional action to evaluate. When omitted, the adapter maps the HTTP method to a conventional action such as
    /// <c>read</c>, <c>create</c>, <c>update</c>, or <c>delete</c>.
    /// </param>
    /// <param name="resourceType">
    /// The optional logical resource type. When omitted, the adapter derives it from the final literal segment in the
    /// endpoint route pattern.
    /// </param>
    /// <param name="resourceIdRouteKey">
    /// The optional route-value key that provides the resource identifier. When omitted, the adapter falls back to the
    /// configured resource-id route keys.
    /// </param>
    /// <param name="tenantRouteKey">
    /// The optional route-value key that provides the tenant identifier. When omitted, the adapter falls back to the
    /// configured tenant route keys and tenant headers.
    /// </param>
    /// <param name="ownerSubjectIdRouteKey">
    /// The optional route-value key that provides the owning subject identifier for owner-based policies.
    /// </param>
    /// <returns>The same route group builder for fluent convention chaining.</returns>
    public static RouteGroupBuilder RequireCephalonAuthorization(
        this RouteGroupBuilder builder,
        string policyId,
        string? action = null,
        string? resourceType = null,
        string? resourceIdRouteKey = null,
        string? tenantRouteKey = null,
        string? ownerSubjectIdRouteKey = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var metadata = CreateMetadata(policyId, action, resourceType, resourceIdRouteKey, tenantRouteKey, ownerSubjectIdRouteKey);
        builder.WithMetadata(metadata);
        builder.AddEndpointFilterFactory((_, next) => CreateFilter(metadata, next));
        return builder;
    }

    private static RestAuthorizationRequestMetadata CreateMetadata(
        string policyId,
        string? action,
        string? resourceType,
        string? resourceIdRouteKey,
        string? tenantRouteKey,
        string? ownerSubjectIdRouteKey)
    {
        return new RestAuthorizationRequestMetadata(
            policyId,
            action,
            resourceType,
            resourceIdRouteKey,
            tenantRouteKey,
            ownerSubjectIdRouteKey);
    }

    private static EndpointFilterDelegate CreateFilter(
        RestAuthorizationRequestMetadata metadata,
        EndpointFilterDelegate next)
    {
        return async invocationContext =>
        {
            var httpContext = invocationContext.HttpContext;
            var services = httpContext.RequestServices;
            var executor = services.GetRequiredService<CephalonAuthorizationBoundaryExecutor>();
            var result = await executor.ExecuteAsync(
                httpContext,
                metadata,
                allowAnonymous: httpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null,
                httpContext.RequestAborted).ConfigureAwait(false);
            if (!result.IsAllowed)
            {
                return result.ToMinimalApiResult();
            }

            return await next(invocationContext);
        };
    }
}
