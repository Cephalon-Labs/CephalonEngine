using Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Configuration;
using Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Services;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Hosting;

/// <summary>
/// Maps ASP.NET Core endpoints for Mailgun webhook tenant-invitation delivery status callbacks.
/// </summary>
public static class MailgunInvitationDeliveryStatusEndpointRouteBuilderExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Maps the optional Mailgun webhook tenant-invitation delivery status callback endpoint.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <returns>The same endpoint route builder for fluent routing composition.</returns>
    /// <remarks>
    /// The endpoint translates Mailgun webhook JSON payloads into the host-agnostic
    /// <see cref="ITenantInvitationDeliveryStatusReconciler" />. Mailgun HMAC signature verification, replay-token
    /// protection, durable inboxing, and provider polling remain host-managed or future provider-pack responsibilities.
    /// </remarks>
    public static IEndpointRouteBuilder MapCephalonMailgunInvitationDeliveryStatusCallbacks(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetService<MailgunInvitationDeliveryAspNetCoreOptions>() ??
            new MailgunInvitationDeliveryAspNetCoreOptions();
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
                    MailgunWebhookDeliveryStatusMapper mapper,
                    ITenantInvitationDeliveryStatusReconciler reconciler,
                    ILoggerFactory loggerFactory,
                    CancellationToken cancellationToken) =>
                    TranslateCallbackAsync(context, mapper, reconciler, loggerFactory, options, routePattern, cancellationToken))
            .WithName("CephalonMailgunInvitationDeliveryStatusCallback")
            .Accepts<JsonElement>("application/json")
            .Produces<MailgunInvitationDeliveryStatusCallbackResult>(StatusCodes.Status200OK)
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
            .GetService<MailgunInvitationDeliveryStatusCallbackRuntimeCatalog>()
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
                options.NormalizeProviderMessageIdWithAngleBrackets);

        return endpoints;
    }

    private static async Task<IResult> TranslateCallbackAsync(
        HttpContext context,
        MailgunWebhookDeliveryStatusMapper mapper,
        ITenantInvitationDeliveryStatusReconciler reconciler,
        ILoggerFactory loggerFactory,
        MailgunInvitationDeliveryAspNetCoreOptions options,
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
                title: "Mailgun webhook payload is invalid.",
                detail: "Send a valid JSON object payload from Mailgun webhooks.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        using (document)
        {
            var eventElements = GetEventElements(document.RootElement);
            if (eventElements is null)
            {
                return Results.Problem(
                    title: "Mailgun webhook payload must be an object or array.",
                    detail: "Mailgun webhooks post a JSON event object; arrays are accepted only for controlled replay or tests.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var eventCount = eventElements.Count;
            if (eventCount > options.GetMaxEventsPerRequest())
            {
                return Results.Problem(
                    title: "Mailgun webhook payload contains too many events.",
                    detail: $"The callback request must contain no more than {options.GetMaxEventsPerRequest()} events.",
                    statusCode: StatusCodes.Status413PayloadTooLarge);
            }

            var eventResults = new List<MailgunInvitationDeliveryStatusCallbackEventResult>(eventCount);
            var translatedEvents = 0;
            var reconciledEvents = 0;
            var skippedEvents = 0;
            var deniedEvents = 0;
            var index = 0;

            foreach (var item in eventElements)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var mapping = mapper.Map(item, index);
                if (!mapping.Translated)
                {
                    skippedEvents++;
                    eventResults.Add(mapping.ToSkippedEventResult());
                    index++;
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
                index++;
            }

            var logger = loggerFactory.CreateLogger("Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore");
            MailgunInvitationDeliveryAspNetCoreLogs.CallbackAccepted(
                logger,
                eventCount,
                translatedEvents,
                reconciledEvents,
                skippedEvents);

            var result = new MailgunInvitationDeliveryStatusCallbackResult(
                routePattern,
                eventCount,
                translatedEvents,
                reconciledEvents,
                skippedEvents,
                deniedEvents,
                eventResults);

            return Results.Json(result, SerializerOptions);
        }
    }

    private static List<JsonElement>? GetEventElements(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            return [root];
        }

        if (root.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var events = new List<JsonElement>();
        foreach (var item in root.EnumerateArray())
        {
            events.Add(item);
        }

        return events;
    }

    private static async Task<CallbackRequestBodyReadResult> ReadRequestBodyAsync(
        HttpContext context,
        MailgunInvitationDeliveryAspNetCoreOptions options,
        CancellationToken cancellationToken)
    {
        var maxRequestBodyBytes = options.GetMaxRequestBodyBytes();
        if (context.Request.ContentLength > maxRequestBodyBytes)
        {
            return CallbackRequestBodyReadResult.Fail(Results.Problem(
                title: "Mailgun webhook payload is too large.",
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
                    title: "Mailgun webhook payload is too large.",
                    detail: $"The callback request body must be no larger than {maxRequestBodyBytes} bytes.",
                    statusCode: StatusCodes.Status413PayloadTooLarge));
            }

            buffer.Write(chunk, 0, bytesRead);
        }

        return CallbackRequestBodyReadResult.Success(buffer.ToArray());
    }

    private static async ValueTask<IResult?> AuthorizeAsync(
        HttpContext context,
        MailgunInvitationDeliveryAspNetCoreOptions options)
    {
        if (!options.RequireStatusCallbackAuthorization)
        {
            return null;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Problem(
                title: "Mailgun invitation delivery status callback authorization is required.",
                detail: "The Cephalon Mailgun webhook callback endpoint is fail-closed by default.",
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
                title: "Mailgun invitation delivery status callback authorization cannot be evaluated.",
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
            title: "Mailgun invitation delivery status callback authorization failed.",
            detail: "The authenticated principal is not authorized to accept Mailgun webhook callbacks.",
            statusCode: StatusCodes.Status403Forbidden);
    }

    private static void ApplyAuthorizationMetadata(
        IEndpointRouteBuilder endpoints,
        IEndpointConventionBuilder builder,
        MailgunInvitationDeliveryAspNetCoreOptions options)
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
