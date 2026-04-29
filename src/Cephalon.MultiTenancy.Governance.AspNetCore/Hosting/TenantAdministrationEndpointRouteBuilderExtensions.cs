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
/// Maps ASP.NET Core endpoints for Cephalon tenant-administration workflow commands.
/// </summary>
public static class TenantAdministrationEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the optional tenant-administration command endpoint.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <returns>The same endpoint route builder for fluent routing composition.</returns>
    /// <remarks>
    /// The endpoint is opt-in, executes the host-agnostic <see cref="ITenantAdministrationWorkflow" />, and performs
    /// a fail-closed authorization check by default. It does not provide public onboarding, tenant-admin UI,
    /// provider-specific invitation senders, external invitation delivery, or identity-provider synchronization.
    /// </remarks>
    public static IEndpointRouteBuilder MapCephalonTenantAdministrationCommands(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetService<MultiTenancyGovernanceAspNetCoreOptions>() ??
            new MultiTenancyGovernanceAspNetCoreOptions();
        if (!options.EnableTenantAdministrationCommandEndpoint)
        {
            return endpoints;
        }

        var routePattern = Normalize(options.TenantAdministrationCommandRoutePattern) ??
            MultiTenancyGovernanceAspNetCoreOptions.DefaultTenantAdministrationCommandRoutePattern;
        var builder = endpoints
            .MapPost(
                routePattern,
                (
                    HttpContext context,
                    TenantAdministrationWorkflowRequest? request,
                    ITenantAdministrationWorkflow workflow,
                    CancellationToken cancellationToken) =>
                    ApplyCommandAsync(context, request, workflow, options, cancellationToken))
            .WithName("CephalonTenantAdministrationCommand")
            .Accepts<TenantAdministrationWorkflowRequest>("application/json")
            .Produces<TenantAdministrationWorkflowResult>(StatusCodes.Status200OK)
            .Produces<TenantAdministrationWorkflowResult>(StatusCodes.Status400BadRequest)
            .Produces<TenantAdministrationWorkflowResult>(StatusCodes.Status404NotFound)
            .Produces<TenantAdministrationWorkflowResult>(StatusCodes.Status409Conflict)
            .Produces<TenantAdministrationWorkflowResult>(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        if (options.ExcludeTenantAdministrationEndpointFromDescription)
        {
            builder.ExcludeFromDescription();
        }

        ApplyAuthorizationMetadata(endpoints, builder, options);

        endpoints.ServiceProvider
            .GetService<TenantAdministrationEndpointRuntimeCatalog>()
            ?.RecordCommandEndpointMapped(
                routePattern,
                options.RequireTenantAdministrationAuthorization,
                Normalize(options.TenantAdministrationAuthorizationPolicy),
                options.ExcludeTenantAdministrationEndpointFromDescription);

        return endpoints;
    }

    private static async Task<IResult> ApplyCommandAsync(
        HttpContext context,
        TenantAdministrationWorkflowRequest? request,
        ITenantAdministrationWorkflow workflow,
        MultiTenancyGovernanceAspNetCoreOptions options,
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
                title: "Tenant administration command request is required.",
                detail: "Send a JSON TenantAdministrationWorkflowRequest body.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await workflow.ApplyAsync(request, cancellationToken).ConfigureAwait(false);
        return Results.Json(result, statusCode: ResolveStatusCode(result));
    }

    private static async ValueTask<IResult?> AuthorizeAsync(
        HttpContext context,
        MultiTenancyGovernanceAspNetCoreOptions options)
    {
        if (!options.RequireTenantAdministrationAuthorization)
        {
            return null;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Problem(
                title: "Tenant administration authorization is required.",
                detail: "The Cephalon tenant-administration command endpoint is fail-closed by default.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var authorizationPolicy = Normalize(options.TenantAdministrationAuthorizationPolicy);
        if (authorizationPolicy is null)
        {
            return null;
        }

        var authorizationService = context.RequestServices.GetService<IAuthorizationService>();
        if (authorizationService is null)
        {
            return Results.Problem(
                title: "Tenant administration authorization cannot be evaluated.",
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
            title: "Tenant administration authorization failed.",
            detail: "The authenticated principal is not authorized to execute tenant-administration commands.",
            statusCode: StatusCodes.Status403Forbidden);
    }

    private static void ApplyAuthorizationMetadata(
        IEndpointRouteBuilder endpoints,
        IEndpointConventionBuilder builder,
        MultiTenancyGovernanceAspNetCoreOptions options)
    {
        if (!options.RequireTenantAdministrationAuthorization ||
            endpoints.ServiceProvider.GetService<IAuthorizationService>() is null ||
            endpoints.ServiceProvider.GetService<IAuthenticationSchemeProvider>() is null)
        {
            return;
        }

        var authorizationPolicy = Normalize(options.TenantAdministrationAuthorizationPolicy);
        if (authorizationPolicy is null)
        {
            builder.RequireAuthorization();
        }
        else
        {
            builder.RequireAuthorization(authorizationPolicy);
        }
    }

    private static int ResolveStatusCode(TenantAdministrationWorkflowResult result)
    {
        if (result.Applied)
        {
            return StatusCodes.Status200OK;
        }

        return result.Outcome switch
        {
            TenantAdministrationWorkflowOutcomes.Disabled => StatusCodes.Status409Conflict,
            TenantAdministrationWorkflowOutcomes.MembershipTargetRequired => StatusCodes.Status400BadRequest,
            TenantAdministrationWorkflowOutcomes.InvitationTargetRequired => StatusCodes.Status400BadRequest,
            TenantAdministrationWorkflowOutcomes.MembershipNotFound => StatusCodes.Status404NotFound,
            TenantAdministrationWorkflowOutcomes.InvitationNotFound => StatusCodes.Status404NotFound,
            TenantAdministrationWorkflowOutcomes.InvalidInvitationState => StatusCodes.Status409Conflict,
            TenantAdministrationWorkflowOutcomes.StoreFailed => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
