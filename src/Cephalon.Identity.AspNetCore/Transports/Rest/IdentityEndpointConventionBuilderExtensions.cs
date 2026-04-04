using Cephalon.Abstractions.Authorization;
using Cephalon.Identity.AspNetCore.Configuration;
using Microsoft.AspNetCore.Authentication;
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
            var requestFactory = services.GetRequiredService<HttpContextAuthorizationRequestFactory>();
            var evaluator = services.GetRequiredService<Cephalon.Abstractions.Authorization.IAuthorizationEvaluator>();
            var options = services.GetRequiredService<IdentityAspNetCoreOptions>();

            if (!requestFactory.TryCreate(httpContext, metadata, out var request, out var failureReason))
            {
                var challengeResult = await TryCreateAuthenticationBoundaryResultAsync(httpContext, forbid: false).ConfigureAwait(false);
                if (challengeResult is not null)
                {
                    return challengeResult;
                }

                return TypedResults.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication required",
                    detail: failureReason,
                    extensions: new Dictionary<string, object?>
                    {
                        ["policyId"] = metadata.PolicyId
                    });
            }

            var decision = await evaluator.EvaluateAsync(
                request!.Subject,
                request.Resource,
                request.Context,
                httpContext.RequestAborted);
            httpContext.Items[options.AuthorizationDecisionItemKey] = decision;

            if (!decision.IsAllowed)
            {
                var forbidResult = await TryCreateAuthenticationBoundaryResultAsync(httpContext, forbid: true).ConfigureAwait(false);
                if (forbidResult is not null)
                {
                    return forbidResult;
                }

                return TypedResults.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Authorization denied",
                    detail: decision.Reason,
                    extensions: new Dictionary<string, object?>
                    {
                        ["policyId"] = decision.PolicyId ?? metadata.PolicyId,
                        ["modes"] = decision.Modes.Select(static mode => mode.ToString()).ToArray(),
                        ["metadata"] = decision.Metadata
                    });
            }

            return await next(invocationContext);
        };
    }

    private static async ValueTask<IResult?> TryCreateAuthenticationBoundaryResultAsync(HttpContext httpContext, bool forbid)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var endpoint = httpContext.GetEndpoint();
        var cephalonSchemes = endpoint?.Metadata
            .GetMetadata<CephalonAuthenticationSchemesMetadata>()?
            .AuthenticationSchemes ?? [];
        if (cephalonSchemes.Length > 0)
        {
            return forbid
                ? Results.Forbid(authenticationSchemes: cephalonSchemes)
                : Results.Challenge(authenticationSchemes: cephalonSchemes);
        }

        var explicitSchemes = endpoint?.Metadata
            .GetOrderedMetadata<IAuthorizeData>()
            .SelectMany(static metadata => SplitAuthenticationSchemes(metadata.AuthenticationSchemes))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static scheme => scheme, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        if (explicitSchemes.Length > 0)
        {
            return forbid
                ? Results.Forbid(authenticationSchemes: explicitSchemes)
                : Results.Challenge(authenticationSchemes: explicitSchemes);
        }

        var schemeProvider = httpContext.RequestServices.GetService<IAuthenticationSchemeProvider>();
        if (schemeProvider is null)
        {
            return null;
        }

        var defaultScheme = forbid
            ? await schemeProvider.GetDefaultForbidSchemeAsync().ConfigureAwait(false) ??
              await schemeProvider.GetDefaultChallengeSchemeAsync().ConfigureAwait(false)
            : await schemeProvider.GetDefaultChallengeSchemeAsync().ConfigureAwait(false);
        if (defaultScheme is null)
        {
            return null;
        }

        return forbid
            ? Results.Forbid()
            : Results.Challenge();
    }

    private static string[] SplitAuthenticationSchemes(string? schemes)
    {
        return schemes?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static scheme => !string.IsNullOrWhiteSpace(scheme))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static scheme => scheme, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private sealed class CephalonAuthenticationSchemesMetadata(string[] authenticationSchemes)
    {
        public string[] AuthenticationSchemes { get; } = authenticationSchemes;
    }
}
