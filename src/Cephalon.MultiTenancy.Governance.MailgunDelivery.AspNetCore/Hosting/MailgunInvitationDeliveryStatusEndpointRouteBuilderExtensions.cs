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
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
    /// <see cref="ITenantInvitationDeliveryStatusReconciler" />. It can also verify Mailgun HMAC-SHA256 webhook
    /// signatures when configured. Replay-token protection, durable inboxing, and provider polling remain host-managed
    /// or future provider-pack responsibilities.
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
                options.NormalizeProviderMessageIdWithAngleBrackets,
                options.RequireSignedWebhook,
                options.GetWebhookSigningKey() is not null,
                options.GetSignedWebhookSignatureToleranceSeconds(),
                options.AcceptParentSignature);

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

        var logger = loggerFactory.CreateLogger("Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore");
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

            var signatureVerification = VerifySignedWebhook(document.RootElement, options);
            if (signatureVerification.Failure is not null)
            {
                MailgunInvitationDeliveryAspNetCoreLogs.CallbackSignatureRejected(logger, signatureVerification.Outcome);
                return signatureVerification.Failure;
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
                var reconciliationRequest = ApplySignatureVerificationMetadata(mapping.Request!, signatureVerification, options);
                var reconciliation = await reconciler
                    .ReconcileAsync(reconciliationRequest, cancellationToken)
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
                signatureVerification.Configured,
                signatureVerification.Verified,
                signatureVerification.Outcome,
                signatureVerification.SignatureField,
                eventResults);

            return Results.Json(result, SerializerOptions);
        }
    }

    private static List<JsonElement>? GetEventElements(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            return [UnwrapEventData(root)];
        }

        if (root.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var events = new List<JsonElement>();
        foreach (var item in root.EnumerateArray())
        {
            events.Add(item.ValueKind == JsonValueKind.Object ? UnwrapEventData(item) : item);
        }

        return events;
    }

    private static JsonElement UnwrapEventData(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("event-data", out var eventData) &&
            eventData.ValueKind == JsonValueKind.Object
                ? eventData
                : element;
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

    private static SignedWebhookVerificationResult VerifySignedWebhook(
        JsonElement root,
        MailgunInvitationDeliveryAspNetCoreOptions options)
    {
        if (!options.RequireSignedWebhook)
        {
            return SignedWebhookVerificationResult.NotConfigured();
        }

        var signingKey = options.GetWebhookSigningKey();
        if (signingKey is null)
        {
            return SignedWebhookVerificationResult.Fail(
                "signing-key-missing",
                Results.Problem(
                    title: "Mailgun webhook signing key is required.",
                    detail: "Configure WebhookSigningKey before requiring Mailgun webhook signature verification.",
                    statusCode: StatusCodes.Status500InternalServerError));
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return SignedWebhookVerificationResult.Fail(
                "signature-envelope-missing",
                SignatureProblem(
                    "Mailgun webhook signature is required.",
                    "Signed Mailgun webhook verification expects a JSON object containing signature, timestamp, and token values."));
        }

        var signatureSource = ResolveSignatureSource(root);
        var token = ReadString(signatureSource, "token");
        if (token is null)
        {
            return SignedWebhookVerificationResult.Fail(
                "token-missing",
                SignatureProblem(
                    "Mailgun webhook token is required.",
                    "Set the token value supplied by Mailgun in the webhook signature payload."));
        }

        var timestamp = ReadString(signatureSource, "timestamp");
        if (timestamp is null)
        {
            return SignedWebhookVerificationResult.Fail(
                "timestamp-missing",
                SignatureProblem(
                    "Mailgun webhook timestamp is required.",
                    "Set the Unix timestamp supplied by Mailgun in the webhook signature payload."));
        }

        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var timestampSeconds))
        {
            return SignedWebhookVerificationResult.Fail(
                "timestamp-invalid",
                SignatureProblem(
                    "Mailgun webhook timestamp is invalid.",
                    "The Mailgun webhook signature timestamp must be a Unix timestamp in seconds."));
        }

        DateTimeOffset signedAtUtc;
        try
        {
            signedAtUtc = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return SignedWebhookVerificationResult.Fail(
                "timestamp-invalid",
                SignatureProblem(
                    "Mailgun webhook timestamp is invalid.",
                    "The Mailgun webhook signature timestamp is outside the supported Unix timestamp range."));
        }

        var ageSeconds = (int)Math.Abs(Math.Round((DateTimeOffset.UtcNow - signedAtUtc).TotalSeconds));
        var toleranceSeconds = options.GetSignedWebhookSignatureToleranceSeconds();
        if (ageSeconds > toleranceSeconds)
        {
            return SignedWebhookVerificationResult.Fail(
                "timestamp-out-of-tolerance",
                SignatureProblem(
                    "Mailgun webhook timestamp is outside the allowed tolerance.",
                    $"The Mailgun webhook timestamp must be within {toleranceSeconds} seconds of the current UTC time."));
        }

        var signature = ReadString(signatureSource, "signature");
        var parentSignature = ReadString(signatureSource, "parent-signature");
        if (signature is null && (!options.AcceptParentSignature || parentSignature is null))
        {
            return SignedWebhookVerificationResult.Fail(
                "signature-missing",
                SignatureProblem(
                    "Mailgun webhook signature is required.",
                    "Set the signature value supplied by Mailgun in the webhook signature payload."));
        }

        var expectedSignature = CreateMailgunSignature(signingKey, timestamp, token);
        if (signature is not null && SignatureMatches(expectedSignature, signature))
        {
            return SignedWebhookVerificationResult.CreateVerified(
                timestampSeconds,
                ageSeconds,
                CreateSha256Fingerprint(signature),
                "signature");
        }

        if (options.AcceptParentSignature &&
            parentSignature is not null &&
            SignatureMatches(expectedSignature, parentSignature))
        {
            return SignedWebhookVerificationResult.CreateVerified(
                timestampSeconds,
                ageSeconds,
                CreateSha256Fingerprint(parentSignature),
                "parent-signature");
        }

        return SignedWebhookVerificationResult.Fail(
            "signature-invalid",
            SignatureProblem(
                "Mailgun webhook signature is invalid.",
                "The Mailgun webhook token and timestamp do not match the supplied HMAC-SHA256 signature."));
    }

    private static JsonElement ResolveSignatureSource(JsonElement root)
    {
        return root.TryGetProperty("signature", out var signatureSource) &&
            signatureSource.ValueKind == JsonValueKind.Object
                ? signatureSource
                : root;
    }

    private static TenantInvitationDeliveryStatusReconciliationRequest ApplySignatureVerificationMetadata(
        TenantInvitationDeliveryStatusReconciliationRequest request,
        SignedWebhookVerificationResult signatureVerification,
        MailgunInvitationDeliveryAspNetCoreOptions options)
    {
        if (!signatureVerification.Configured)
        {
            return request;
        }

        var metadata = CopyMetadata(request.Metadata);
        metadata["mailgunWebhookSignatureVerification"] = "verified";
        metadata["mailgunWebhookSignatureVerificationOwnership"] = "cephalon-managed";
        metadata["mailgunWebhookSignatureAlgorithm"] = "hmac-sha256";
        metadata["mailgunWebhookSignaturePayload"] = "timestamp+token";
        metadata["mailgunWebhookSignatureTimestamp"] =
            signatureVerification.Timestamp!.Value.ToString(CultureInfo.InvariantCulture);
        metadata["mailgunWebhookSignatureAgeSeconds"] =
            signatureVerification.AgeSeconds!.Value.ToString(CultureInfo.InvariantCulture);
        metadata["mailgunWebhookSignatureFingerprint"] = signatureVerification.SignatureFingerprint!;
        metadata["mailgunWebhookSignatureField"] = signatureVerification.SignatureField ?? "signature";
        metadata["mailgunWebhookParentSignatureAccepted"] = options.AcceptParentSignature.ToString().ToLowerInvariant();

        return new TenantInvitationDeliveryStatusReconciliationRequest(
            tenantId: request.TenantId,
            invitationId: request.InvitationId,
            status: request.Status,
            providerMessageId: request.ProviderMessageId,
            senderId: request.SenderId,
            channel: request.Channel,
            reason: request.Reason,
            observedAtUtc: request.ObservedAtUtc,
            source: request.Source,
            actor: request.Actor,
            correlationId: request.CorrelationId,
            recordStatus: request.RecordStatus,
            requireProviderMessageMatch: request.RequireProviderMessageMatch,
            metadata: metadata);
    }

    private static string CreateMailgunSignature(string signingKey, string timestamp, string token)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(timestamp + token));
        return Convert.ToHexString(signatureBytes).ToLowerInvariant();
    }

    private static bool SignatureMatches(string expectedHexSignature, string suppliedHexSignature)
    {
        var normalizedSuppliedSignature = suppliedHexSignature.Trim().ToLowerInvariant();
        if (expectedHexSignature.Length != normalizedSuppliedSignature.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expectedHexSignature),
            Encoding.ASCII.GetBytes(normalizedSuppliedSignature));
    }

    private static string CreateSha256Fingerprint(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
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

    private static IResult SignatureProblem(string title, string detail)
    {
        return Results.Problem(title: title, detail: detail, statusCode: StatusCodes.Status401Unauthorized);
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

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => Normalize(property.GetString()),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => property.ToString(),
            _ => null
        };
    }

    private sealed record CallbackRequestBodyReadResult(byte[] Body, IResult? Failure)
    {
        public static CallbackRequestBodyReadResult Success(byte[] body) => new(body, null);

        public static CallbackRequestBodyReadResult Fail(IResult failure) => new([], failure);
    }

    private sealed record SignedWebhookVerificationResult(
        bool Configured,
        bool Verified,
        long? Timestamp,
        int? AgeSeconds,
        string? SignatureFingerprint,
        string? SignatureField,
        string Outcome,
        IResult? Failure)
    {
        public static SignedWebhookVerificationResult NotConfigured() =>
            new(false, false, null, null, null, null, "not-configured", null);

        public static SignedWebhookVerificationResult CreateVerified(
            long timestamp,
            int ageSeconds,
            string signatureFingerprint,
            string signatureField) =>
            new(true, true, timestamp, ageSeconds, signatureFingerprint, signatureField, "verified", null);

        public static SignedWebhookVerificationResult Fail(string outcome, IResult failure) =>
            new(true, false, null, null, null, null, outcome, failure);
    }
}
