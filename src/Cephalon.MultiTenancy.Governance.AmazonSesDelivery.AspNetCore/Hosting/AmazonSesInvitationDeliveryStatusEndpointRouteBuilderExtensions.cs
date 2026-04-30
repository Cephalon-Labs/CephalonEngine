using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Configuration;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Services;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Hosting;

/// <summary>
/// Maps ASP.NET Core endpoints for SNS-wrapped Amazon SES tenant-invitation delivery status callbacks.
/// </summary>
public static class AmazonSesInvitationDeliveryStatusEndpointRouteBuilderExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Maps the optional Amazon SES over SNS tenant-invitation delivery status callback endpoint.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <returns>The same endpoint route builder for fluent routing composition.</returns>
    /// <remarks>
    /// The endpoint translates SNS HTTP notifications containing Amazon SES event publishing payloads into the
    /// host-agnostic <see cref="ITenantInvitationDeliveryStatusReconciler" />. SNS subscription confirmation,
    /// SNS signature verification, durable inboxing, distributed replay protection, and provider polling remain
    /// host-managed or future provider-pack responsibilities.
    /// </remarks>
    public static IEndpointRouteBuilder MapCephalonAmazonSesInvitationDeliveryStatusCallbacks(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetService<AmazonSesInvitationDeliveryAspNetCoreOptions>() ??
            new AmazonSesInvitationDeliveryAspNetCoreOptions();
        if (!options.EnableStatusCallbackEndpoint)
        {
            return endpoints;
        }

        var routePattern = options.GetRoutePattern();
        var builder = endpoints
            .MapPost(
                routePattern,
                (
                    HttpContext context,
                    AmazonSesSnsDeliveryStatusMapper mapper,
                    ITenantInvitationDeliveryStatusReconciler reconciler,
                    ILoggerFactory loggerFactory,
                    CancellationToken cancellationToken) =>
                    TranslateCallbackAsync(context, mapper, reconciler, loggerFactory, options, routePattern, cancellationToken))
            .WithName("CephalonAmazonSesInvitationDeliveryStatusCallback")
            .Accepts<JsonElement>("application/json")
            .Produces<AmazonSesInvitationDeliveryStatusCallbackResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        if (options.ExcludeStatusCallbackEndpointFromDescription)
        {
            builder.ExcludeFromDescription();
        }

        ApplyAuthorizationMetadata(endpoints, builder, options);

        endpoints.ServiceProvider
            .GetService<AmazonSesInvitationDeliveryStatusCallbackRuntimeCatalog>()
            ?.RecordEndpointMapped(
                routePattern,
                options.RequireStatusCallbackAuthorization,
                Normalize(options.StatusCallbackAuthorizationPolicy),
                options.ExcludeStatusCallbackEndpointFromDescription,
                options.RequireProviderMessageMatch,
                options.RecordStatus,
                options.GetMaxRequestBodyBytes(),
                options.GetMaxEventsPerRequest(),
                options.MapEngagementEventsAsDelivered,
                options.AcceptRawSesEventPayloads);

        return endpoints;
    }

    private static async Task<IResult> TranslateCallbackAsync(
        HttpContext context,
        AmazonSesSnsDeliveryStatusMapper mapper,
        ITenantInvitationDeliveryStatusReconciler reconciler,
        ILoggerFactory loggerFactory,
        AmazonSesInvitationDeliveryAspNetCoreOptions options,
        string routePattern,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await AuthorizeAsync(context, options).ConfigureAwait(false);
        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        var requestBody = await ReadRequestBodyAsync(context, options, cancellationToken).ConfigureAwait(false);
        if (requestBody.Failure is not null)
        {
            return requestBody.Failure;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(requestBody.Body);
        }
        catch (JsonException)
        {
            return Results.Problem(
                title: "Amazon SES over SNS payload is invalid.",
                detail: "Send a valid SNS notification JSON object whose Message field contains an Amazon SES event payload.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        using (document)
        {
            var eventMappings = mapper.MapPayload(document.RootElement);
            if (eventMappings is null)
            {
                return Results.Problem(
                    title: "Amazon SES over SNS payload must be an object or array.",
                    detail: "Amazon SNS HTTP subscriptions post a JSON object. Raw Amazon SES event arrays are accepted only for controlled replay when configured.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var eventCount = eventMappings.Count;
            if (eventCount > options.GetMaxEventsPerRequest())
            {
                return Results.Problem(
                    title: "Amazon SES over SNS payload contains too many events.",
                    detail: $"The callback request must contain no more than {options.GetMaxEventsPerRequest()} events.",
                    statusCode: StatusCodes.Status413PayloadTooLarge);
            }

            var eventResults = new List<AmazonSesInvitationDeliveryStatusCallbackEventResult>(eventCount);
            var translatedEvents = 0;
            var reconciledEvents = 0;
            var skippedEvents = 0;
            var deniedEvents = 0;

            foreach (var mapping in eventMappings)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!mapping.Translated)
                {
                    skippedEvents++;
                    eventResults.Add(mapping.ToSkippedEventResult());
                    continue;
                }

                translatedEvents++;
                var reconciliation = await reconciler
                    .ReconcileAsync(mapping.Request!, cancellationToken)
                    .ConfigureAwait(false);

                if (reconciliation.Reconciled)
                {
                    reconciledEvents++;
                }
                else
                {
                    deniedEvents++;
                }

                eventResults.Add(mapping.ToReconciledEventResult(reconciliation));
            }

            var logger = loggerFactory.CreateLogger("Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore");
            AmazonSesInvitationDeliveryAspNetCoreLogs.CallbackAccepted(
                logger,
                eventCount,
                translatedEvents,
                reconciledEvents,
                skippedEvents);

            var result = new AmazonSesInvitationDeliveryStatusCallbackResult(
                routePattern,
                eventCount,
                translatedEvents,
                reconciledEvents,
                skippedEvents,
                deniedEvents,
                snsSignatureVerificationRequired: false,
                snsSignatureVerified: false,
                snsSignatureVerificationOutcome: "not-configured",
                eventResults);

            return Results.Json(result, SerializerOptions);
        }
    }

    private static async Task<CallbackRequestBodyReadResult> ReadRequestBodyAsync(
        HttpContext context,
        AmazonSesInvitationDeliveryAspNetCoreOptions options,
        CancellationToken cancellationToken)
    {
        var maxRequestBodyBytes = options.GetMaxRequestBodyBytes();
        if (context.Request.ContentLength > maxRequestBodyBytes)
        {
            return CallbackRequestBodyReadResult.Fail(Results.Problem(
                title: "Amazon SES over SNS payload is too large.",
                detail: $"The callback request body must be no larger than {maxRequestBodyBytes} bytes.",
                statusCode: StatusCodes.Status413PayloadTooLarge));
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        while (true)
        {
            var bytesRead = await context.Request.Body
                .ReadAsync(chunk.AsMemory(0, chunk.Length), cancellationToken)
                .ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            if (buffer.Length + bytesRead > maxRequestBodyBytes)
            {
                return CallbackRequestBodyReadResult.Fail(Results.Problem(
                    title: "Amazon SES over SNS payload is too large.",
                    detail: $"The callback request body must be no larger than {maxRequestBodyBytes} bytes.",
                    statusCode: StatusCodes.Status413PayloadTooLarge));
            }

            buffer.Write(chunk, 0, bytesRead);
        }

        return CallbackRequestBodyReadResult.Success(buffer.ToArray());
    }

    private static async ValueTask<IResult?> AuthorizeAsync(
        HttpContext context,
        AmazonSesInvitationDeliveryAspNetCoreOptions options)
    {
        if (!options.RequireStatusCallbackAuthorization)
        {
            return null;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Problem(
                title: "Amazon SES invitation delivery status callback authorization is required.",
                detail: "The Cephalon Amazon SES over SNS callback endpoint is fail-closed by default.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var authorizationPolicy = Normalize(options.StatusCallbackAuthorizationPolicy);
        if (authorizationPolicy is null)
        {
            return null;
        }

        var authorizationService = context.RequestServices.GetService<IAuthorizationService>();
        if (authorizationService is null)
        {
            return Results.Problem(
                title: "Amazon SES invitation delivery status callback authorization cannot be evaluated.",
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
            title: "Amazon SES invitation delivery status callback authorization failed.",
            detail: "The authenticated principal is not authorized to accept Amazon SES over SNS callbacks.",
            statusCode: StatusCodes.Status403Forbidden);
    }

    private static void ApplyAuthorizationMetadata(
        IEndpointRouteBuilder endpoints,
        IEndpointConventionBuilder builder,
        AmazonSesInvitationDeliveryAspNetCoreOptions options)
    {
        if (!options.RequireStatusCallbackAuthorization ||
            endpoints.ServiceProvider.GetService<IAuthorizationService>() is null ||
            endpoints.ServiceProvider.GetService<IAuthenticationSchemeProvider>() is null)
        {
            return;
        }

        var authorizationPolicy = Normalize(options.StatusCallbackAuthorizationPolicy);
        if (authorizationPolicy is null)
        {
            builder.RequireAuthorization();
        }
        else
        {
            builder.RequireAuthorization(authorizationPolicy);
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private sealed record CallbackRequestBodyReadResult(byte[] Body, IResult? Failure)
    {
        public static CallbackRequestBodyReadResult Success(byte[] body) => new(body, null);

        public static CallbackRequestBodyReadResult Fail(IResult failure) => new([], failure);
    }
}
