using Cephalon.MultiTenancy.Governance.AspNetCore.Configuration;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

/// <summary>
/// Maps ASP.NET Core endpoints for tenant-invitation delivery dispatch requests.
/// </summary>
public static class TenantInvitationDeliveryDispatchEndpointRouteBuilderExtensions
{
    internal const string DefaultDispatchSource = "aspnetcore-invitation-delivery-dispatch";

    /// <summary>
    /// Maps the optional tenant-invitation delivery dispatch endpoint.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <returns>The same endpoint route builder for fluent routing composition.</returns>
    /// <remarks>
    /// The endpoint is opt-in, executes the host-agnostic <see cref="ITenantInvitationDeliveryDispatcher" />, and
    /// performs a fail-closed authorization check by default. It does not implement provider-specific senders,
    /// durable retry queues, public onboarding, tenant-admin UI, provider polling, or identity-provider
    /// synchronization.
    /// </remarks>
    public static IEndpointRouteBuilder MapCephalonTenantInvitationDeliveryDispatches(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetService<MultiTenancyGovernanceAspNetCoreOptions>() ??
            new MultiTenancyGovernanceAspNetCoreOptions();
        if (!options.EnableTenantInvitationDeliveryDispatchEndpoint)
        {
            return endpoints;
        }

        var routePattern = Normalize(options.TenantInvitationDeliveryDispatchRoutePattern) ??
            MultiTenancyGovernanceAspNetCoreOptions.DefaultTenantInvitationDeliveryDispatchRoutePattern;
        var builder = endpoints
            .MapPost(
                routePattern,
                (
                    HttpContext context,
                    TenantInvitationDeliveryRequest? request,
                    ITenantInvitationDeliveryDispatcher dispatcher,
                    CancellationToken cancellationToken) =>
                    DispatchAsync(context, request, dispatcher, options, routePattern, cancellationToken))
            .WithName("CephalonTenantInvitationDeliveryDispatch")
            .Accepts<TenantInvitationDeliveryRequest>("application/json")
            .Produces<TenantInvitationDeliveryResult>(StatusCodes.Status200OK)
            .Produces<TenantInvitationDeliveryResult>(StatusCodes.Status400BadRequest)
            .Produces<TenantInvitationDeliveryResult>(StatusCodes.Status404NotFound)
            .Produces<TenantInvitationDeliveryResult>(StatusCodes.Status409Conflict)
            .Produces<TenantInvitationDeliveryResult>(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        if (options.ExcludeTenantInvitationDeliveryDispatchEndpointFromDescription)
        {
            builder.ExcludeFromDescription();
        }

        ApplyAuthorizationMetadata(endpoints, builder, options);

        endpoints.ServiceProvider
            .GetService<TenantInvitationDeliveryDispatchEndpointRuntimeCatalog>()
            ?.RecordDispatchEndpointMapped(
                routePattern,
                options.RequireTenantInvitationDeliveryDispatchAuthorization,
                Normalize(options.TenantInvitationDeliveryDispatchAuthorizationPolicy),
                options.ExcludeTenantInvitationDeliveryDispatchEndpointFromDescription);

        return endpoints;
    }

    private static async Task<IResult> DispatchAsync(
        HttpContext context,
        TenantInvitationDeliveryRequest? request,
        ITenantInvitationDeliveryDispatcher dispatcher,
        MultiTenancyGovernanceAspNetCoreOptions options,
        string routePattern,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await AuthorizeAsync(context, options).ConfigureAwait(false);
        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        if (request is null)
        {
            return Results.Problem(
                title: "Tenant invitation delivery dispatch request is required.",
                detail: "Send a JSON TenantInvitationDeliveryRequest body.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var metadata = CopyMetadata(request.Metadata);
        metadata["aspNetCoreInvitationDeliveryDispatch"] = "true";
        metadata["aspNetCoreInvitationDeliveryDispatchRoute"] = routePattern;
        metadata["invitationDeliveryDispatchEndpointOwnership"] = "cephalon-managed";

        var dispatchRequest = new TenantInvitationDeliveryRequest(
            request.TenantId,
            request.InvitationId,
            request.Channel,
            request.SenderId,
            Normalize(request.Source) ?? DefaultDispatchSource,
            request.Actor,
            request.AtUtc,
            request.CorrelationId,
            request.RecordDelivery,
            metadata);

        var result = await dispatcher.DispatchAsync(dispatchRequest, cancellationToken).ConfigureAwait(false);
        return Results.Json(result, statusCode: ResolveStatusCode(result));
    }

    private static async ValueTask<IResult?> AuthorizeAsync(
        HttpContext context,
        MultiTenancyGovernanceAspNetCoreOptions options)
    {
        if (!options.RequireTenantInvitationDeliveryDispatchAuthorization)
        {
            return null;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Problem(
                title: "Tenant invitation delivery dispatch authorization is required.",
                detail: "The Cephalon tenant-invitation delivery dispatch endpoint is fail-closed by default.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var authorizationPolicy = Normalize(options.TenantInvitationDeliveryDispatchAuthorizationPolicy);
        if (authorizationPolicy is null)
        {
            return null;
        }

        var authorizationService = context.RequestServices.GetService<IAuthorizationService>();
        if (authorizationService is null)
        {
            return Results.Problem(
                title: "Tenant invitation delivery dispatch authorization cannot be evaluated.",
                detail: "Register ASP.NET Core authorization services or disable endpoint authorization deliberately.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var authorization = await authorizationService
            .AuthorizeAsync(context.User, context, authorizationPolicy)
            .ConfigureAwait(false);
        if (authorization.Succeeded)
        {
            return null;
        }

        return Results.Problem(
            title: "Tenant invitation delivery dispatch authorization failed.",
            detail: "The authenticated principal is not authorized to dispatch tenant-invitation deliveries.",
            statusCode: StatusCodes.Status403Forbidden);
    }

    private static void ApplyAuthorizationMetadata(
        IEndpointRouteBuilder endpoints,
        IEndpointConventionBuilder builder,
        MultiTenancyGovernanceAspNetCoreOptions options)
    {
        if (!options.RequireTenantInvitationDeliveryDispatchAuthorization ||
            endpoints.ServiceProvider.GetService<IAuthorizationService>() is null ||
            endpoints.ServiceProvider.GetService<IAuthenticationSchemeProvider>() is null)
        {
            return;
        }

        var authorizationPolicy = Normalize(options.TenantInvitationDeliveryDispatchAuthorizationPolicy);
        if (authorizationPolicy is null)
        {
            builder.RequireAuthorization();
        }
        else
        {
            builder.RequireAuthorization(authorizationPolicy);
        }
    }

    private static int ResolveStatusCode(TenantInvitationDeliveryResult result)
    {
        if (result.Dispatched)
        {
            return StatusCodes.Status200OK;
        }

        return result.Outcome switch
        {
            TenantInvitationDeliveryOutcomes.Disabled => StatusCodes.Status409Conflict,
            TenantInvitationDeliveryOutcomes.InvitationNotFound => StatusCodes.Status404NotFound,
            TenantInvitationDeliveryOutcomes.InvitationNotPending => StatusCodes.Status409Conflict,
            TenantInvitationDeliveryOutcomes.InvitationExpired => StatusCodes.Status409Conflict,
            TenantInvitationDeliveryOutcomes.SenderNotConfigured => StatusCodes.Status503ServiceUnavailable,
            TenantInvitationDeliveryOutcomes.SenderFailed => StatusCodes.Status503ServiceUnavailable,
            TenantInvitationDeliveryOutcomes.Suppressed => StatusCodes.Status409Conflict,
            TenantInvitationDeliveryOutcomes.StoreFailed => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest
        };
    }

    private static Dictionary<string, string> CopyMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return metadata
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
