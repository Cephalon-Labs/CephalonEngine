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
/// Maps ASP.NET Core endpoints for normalized tenant-invitation delivery status callbacks.
/// </summary>
public static class TenantInvitationDeliveryStatusCallbackEndpointRouteBuilderExtensions
{
    private const string DefaultCallbackSource = "aspnetcore-delivery-status-callback";

    /// <summary>
    /// Maps the optional tenant-invitation delivery status callback endpoint.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <returns>The same endpoint route builder for fluent routing composition.</returns>
    /// <remarks>
    /// The endpoint is opt-in, executes the host-agnostic <see cref="ITenantInvitationDeliveryStatusReconciler" />,
    /// and performs a fail-closed authorization check by default. It accepts normalized status observations only; provider
    /// webhook payload translation, provider signature verification, provider polling, and provider-specific status
    /// vocabularies remain application-managed or future provider-pack responsibilities.
    /// </remarks>
    public static IEndpointRouteBuilder MapCephalonTenantInvitationDeliveryStatusCallbacks(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetService<MultiTenancyGovernanceAspNetCoreOptions>() ??
            new MultiTenancyGovernanceAspNetCoreOptions();
        if (!options.EnableTenantInvitationDeliveryStatusCallbackEndpoint)
        {
            return endpoints;
        }

        var routePattern = Normalize(options.TenantInvitationDeliveryStatusCallbackRoutePattern) ??
            MultiTenancyGovernanceAspNetCoreOptions.DefaultTenantInvitationDeliveryStatusCallbackRoutePattern;
        var builder = endpoints
            .MapPost(
                routePattern,
                (
                    HttpContext context,
                    TenantInvitationDeliveryStatusCallbackRequest? request,
                    ITenantInvitationDeliveryStatusReconciler reconciler,
                    CancellationToken cancellationToken) =>
                    ReconcileCallbackAsync(context, request, reconciler, options, routePattern, cancellationToken))
            .WithName("CephalonTenantInvitationDeliveryStatusCallback")
            .Accepts<TenantInvitationDeliveryStatusCallbackRequest>("application/json")
            .Produces<TenantInvitationDeliveryStatusReconciliationResult>(StatusCodes.Status200OK)
            .Produces<TenantInvitationDeliveryStatusReconciliationResult>(StatusCodes.Status400BadRequest)
            .Produces<TenantInvitationDeliveryStatusReconciliationResult>(StatusCodes.Status404NotFound)
            .Produces<TenantInvitationDeliveryStatusReconciliationResult>(StatusCodes.Status409Conflict)
            .Produces<TenantInvitationDeliveryStatusReconciliationResult>(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        if (options.ExcludeTenantInvitationDeliveryStatusCallbackEndpointFromDescription)
        {
            builder.ExcludeFromDescription();
        }

        ApplyAuthorizationMetadata(endpoints, builder, options);

        endpoints.ServiceProvider
            .GetService<TenantInvitationDeliveryStatusCallbackEndpointRuntimeCatalog>()
            ?.RecordCallbackEndpointMapped(
                routePattern,
                options.RequireTenantInvitationDeliveryStatusCallbackAuthorization,
                Normalize(options.TenantInvitationDeliveryStatusCallbackAuthorizationPolicy),
                options.ExcludeTenantInvitationDeliveryStatusCallbackEndpointFromDescription,
                options.RequireTenantInvitationDeliveryStatusCallbackProviderMessageMatch);

        return endpoints;
    }

    private static async Task<IResult> ReconcileCallbackAsync(
        HttpContext context,
        TenantInvitationDeliveryStatusCallbackRequest? request,
        ITenantInvitationDeliveryStatusReconciler reconciler,
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
                title: "Tenant invitation delivery status callback request is required.",
                detail: "Send a JSON TenantInvitationDeliveryStatusCallbackRequest body.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var validationResult = Validate(request);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var metadata = CopyMetadata(request.Metadata);
        metadata["aspNetCoreDeliveryStatusCallback"] = "true";
        metadata["aspNetCoreDeliveryStatusCallbackRoute"] = routePattern;
        metadata["deliveryStatusCallbackIngressOwnership"] = "cephalon-managed";

        var reconciliationRequest = new TenantInvitationDeliveryStatusReconciliationRequest(
            tenantId: request.TenantId!,
            invitationId: request.InvitationId!,
            status: request.Status!,
            providerMessageId: request.ProviderMessageId,
            senderId: request.SenderId,
            channel: request.Channel,
            reason: request.Reason,
            observedAtUtc: request.ObservedAtUtc,
            source: Normalize(request.Source) ?? DefaultCallbackSource,
            actor: request.Actor,
            correlationId: request.CorrelationId,
            recordStatus: request.RecordStatus,
            requireProviderMessageMatch: options.RequireTenantInvitationDeliveryStatusCallbackProviderMessageMatch ||
                request.RequireProviderMessageMatch,
            metadata: metadata);

        var result = await reconciler.ReconcileAsync(reconciliationRequest, cancellationToken).ConfigureAwait(false);
        return Results.Json(result, statusCode: ResolveStatusCode(result));
    }

    private static IResult? Validate(TenantInvitationDeliveryStatusCallbackRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
        {
            return Results.Problem(
                title: "Tenant id is required.",
                detail: "Set TenantId to the tenant identifier that owns the invitation.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(request.InvitationId))
        {
            return Results.Problem(
                title: "Invitation id is required.",
                detail: "Set InvitationId to the invitation identifier to reconcile.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return Results.Problem(
                title: "Delivery status is required.",
                detail: "Set Status to the provider or receiver delivery status.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return null;
    }

    private static async ValueTask<IResult?> AuthorizeAsync(
        HttpContext context,
        MultiTenancyGovernanceAspNetCoreOptions options)
    {
        if (!options.RequireTenantInvitationDeliveryStatusCallbackAuthorization)
        {
            return null;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Problem(
                title: "Tenant invitation delivery status callback authorization is required.",
                detail: "The Cephalon tenant-invitation delivery status callback endpoint is fail-closed by default.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var authorizationPolicy = Normalize(options.TenantInvitationDeliveryStatusCallbackAuthorizationPolicy);
        if (authorizationPolicy is null)
        {
            return null;
        }

        var authorizationService = context.RequestServices.GetService<IAuthorizationService>();
        if (authorizationService is null)
        {
            return Results.Problem(
                title: "Tenant invitation delivery status callback authorization cannot be evaluated.",
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
            title: "Tenant invitation delivery status callback authorization failed.",
            detail: "The authenticated principal is not authorized to reconcile tenant-invitation delivery status callbacks.",
            statusCode: StatusCodes.Status403Forbidden);
    }

    private static void ApplyAuthorizationMetadata(
        IEndpointRouteBuilder endpoints,
        IEndpointConventionBuilder builder,
        MultiTenancyGovernanceAspNetCoreOptions options)
    {
        if (!options.RequireTenantInvitationDeliveryStatusCallbackAuthorization ||
            endpoints.ServiceProvider.GetService<IAuthorizationService>() is null ||
            endpoints.ServiceProvider.GetService<IAuthenticationSchemeProvider>() is null)
        {
            return;
        }

        var authorizationPolicy = Normalize(options.TenantInvitationDeliveryStatusCallbackAuthorizationPolicy);
        if (authorizationPolicy is null)
        {
            builder.RequireAuthorization();
        }
        else
        {
            builder.RequireAuthorization(authorizationPolicy);
        }
    }

    private static int ResolveStatusCode(TenantInvitationDeliveryStatusReconciliationResult result)
    {
        if (result.Reconciled)
        {
            return StatusCodes.Status200OK;
        }

        return result.Outcome switch
        {
            TenantInvitationDeliveryStatusReconciliationOutcomes.Disabled => StatusCodes.Status409Conflict,
            TenantInvitationDeliveryStatusReconciliationOutcomes.InvitationNotFound => StatusCodes.Status404NotFound,
            TenantInvitationDeliveryStatusReconciliationOutcomes.ProviderMessageMissing => StatusCodes.Status409Conflict,
            TenantInvitationDeliveryStatusReconciliationOutcomes.ProviderMessageMismatch => StatusCodes.Status409Conflict,
            TenantInvitationDeliveryStatusReconciliationOutcomes.StoreFailed => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest
        };
    }

    private static Dictionary<string, string> CopyMetadata(IDictionary<string, string>? metadata)
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
