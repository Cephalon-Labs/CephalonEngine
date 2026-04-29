using Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Configuration;
using Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Services;
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

namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Hosting;

/// <summary>
/// Maps ASP.NET Core endpoints for SendGrid Event Webhook tenant-invitation delivery status callbacks.
/// </summary>
public static class SendGridInvitationDeliveryStatusEndpointRouteBuilderExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Maps the optional SendGrid Event Webhook tenant-invitation delivery status callback endpoint.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <returns>The same endpoint route builder for fluent routing composition.</returns>
    /// <remarks>
    /// The endpoint translates SendGrid Event Webhook JSON arrays into the host-agnostic
    /// <see cref="ITenantInvitationDeliveryStatusReconciler" />. It can also verify SendGrid signed Event Webhook
    /// signatures when configured. OAuth token validation, durable inboxing, and distributed replay protection remain
    /// host-managed or future provider-pack responsibilities.
    /// </remarks>
    public static IEndpointRouteBuilder MapCephalonSendGridInvitationDeliveryStatusCallbacks(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetService<SendGridInvitationDeliveryAspNetCoreOptions>() ??
            new SendGridInvitationDeliveryAspNetCoreOptions();
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
                    SendGridEventWebhookDeliveryStatusMapper mapper,
                    ITenantInvitationDeliveryStatusReconciler reconciler,
                    ILoggerFactory loggerFactory,
                    CancellationToken cancellationToken) =>
                    TranslateCallbackAsync(context, mapper, reconciler, loggerFactory, options, routePattern, cancellationToken))
            .WithName("CephalonSendGridInvitationDeliveryStatusCallback")
            .Accepts<JsonElement>("application/json")
            .Produces<SendGridInvitationDeliveryStatusCallbackResult>(StatusCodes.Status200OK)
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
            .GetService<SendGridInvitationDeliveryStatusCallbackRuntimeCatalog>()
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
                options.NormalizeProviderMessageIdFromSgMessageId,
                options.RequireSignedEventWebhook,
                options.GetSignedEventWebhookPublicKey() is not null,
                options.GetSignedEventWebhookSignatureHeaderName(),
                options.GetSignedEventWebhookTimestampHeaderName(),
                options.GetSignedEventWebhookSignatureToleranceSeconds());

        return endpoints;
    }

    private static async Task<IResult> TranslateCallbackAsync(
        HttpContext context,
        SendGridEventWebhookDeliveryStatusMapper mapper,
        ITenantInvitationDeliveryStatusReconciler reconciler,
        ILoggerFactory loggerFactory,
        SendGridInvitationDeliveryAspNetCoreOptions options,
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

        var logger = loggerFactory.CreateLogger("Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore");
        var signatureVerification = VerifySignedEventWebhook(context, options, requestBody.Body);
        if (signatureVerification.Failure is not null)
        {
            SendGridInvitationDeliveryAspNetCoreLogs.CallbackSignatureRejected(logger, signatureVerification.Outcome);
            return signatureVerification.Failure;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(requestBody.Body);
        }
        catch (JsonException)
        {
            return Results.Problem(
                title: "SendGrid Event Webhook payload is invalid.",
                detail: "Send a valid JSON array payload from Twilio SendGrid Event Webhook.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return Results.Problem(
                    title: "SendGrid Event Webhook payload must be an array.",
                    detail: "Twilio SendGrid Event Webhook posts a JSON array of event objects.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var eventCount = document.RootElement.GetArrayLength();
            if (eventCount > options.GetMaxEventsPerRequest())
            {
                return Results.Problem(
                    title: "SendGrid Event Webhook payload contains too many events.",
                    detail: $"The callback request must contain no more than {options.GetMaxEventsPerRequest()} events.",
                    statusCode: StatusCodes.Status413PayloadTooLarge);
            }

            var eventResults = new List<SendGridInvitationDeliveryStatusCallbackEventResult>(eventCount);
            var translatedEvents = 0;
            var reconciledEvents = 0;
            var skippedEvents = 0;
            var deniedEvents = 0;
            var index = 0;

            foreach (var item in document.RootElement.EnumerateArray())
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
                var reconciliationRequest = ApplySignatureMetadata(mapping.Request!, signatureVerification);
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

            SendGridInvitationDeliveryAspNetCoreLogs.CallbackAccepted(
                logger,
                eventCount,
                translatedEvents,
                reconciledEvents,
                skippedEvents);

            var result = new SendGridInvitationDeliveryStatusCallbackResult(
                routePattern,
                eventCount,
                translatedEvents,
                reconciledEvents,
                skippedEvents,
                deniedEvents,
                eventResults,
                signatureVerification.Configured,
                signatureVerification.Verified,
                signatureVerification.Outcome);

            return Results.Json(result, SerializerOptions);
        }
    }

    private static async Task<CallbackRequestBodyReadResult> ReadRequestBodyAsync(
        HttpContext context,
        SendGridInvitationDeliveryAspNetCoreOptions options,
        CancellationToken cancellationToken)
    {
        var maxRequestBodyBytes = options.GetMaxRequestBodyBytes();
        if (context.Request.ContentLength > maxRequestBodyBytes)
        {
            return CallbackRequestBodyReadResult.Fail(Results.Problem(
                title: "SendGrid Event Webhook payload is too large.",
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
                    title: "SendGrid Event Webhook payload is too large.",
                    detail: $"The callback request body must be no larger than {maxRequestBodyBytes} bytes.",
                    statusCode: StatusCodes.Status413PayloadTooLarge));
            }

            buffer.Write(chunk, 0, bytesRead);
        }

        return CallbackRequestBodyReadResult.Success(buffer.ToArray());
    }

    private static SignedEventWebhookVerificationResult VerifySignedEventWebhook(
        HttpContext context,
        SendGridInvitationDeliveryAspNetCoreOptions options,
        byte[] requestBody)
    {
        if (!options.RequireSignedEventWebhook)
        {
            return SignedEventWebhookVerificationResult.NotConfigured();
        }

        var publicKey = options.GetSignedEventWebhookPublicKey();
        if (publicKey is null)
        {
            return SignedEventWebhookVerificationResult.Fail(
                "public-key-missing",
                Results.Problem(
                    title: "SendGrid signed Event Webhook public key is required.",
                    detail: "Configure SignedEventWebhookPublicKey before requiring SendGrid signed Event Webhook verification.",
                    statusCode: StatusCodes.Status500InternalServerError));
        }

        if (!TryImportPublicKey(publicKey, out var importedKey))
        {
            return SignedEventWebhookVerificationResult.Fail(
                "public-key-invalid",
                Results.Problem(
                    title: "SendGrid signed Event Webhook public key is invalid.",
                    detail: "Configure SignedEventWebhookPublicKey as a PEM public key or Base64-encoded SubjectPublicKeyInfo value.",
                    statusCode: StatusCodes.Status500InternalServerError));
        }

        using var ecdsa = importedKey;
        var signatureHeaderName = options.GetSignedEventWebhookSignatureHeaderName();
        if (!TryGetSingleHeader(context, signatureHeaderName, out var signature))
        {
            return SignedEventWebhookVerificationResult.Fail(
                "signature-missing",
                SignatureProblem(
                    "SendGrid signed Event Webhook signature is required.",
                    $"Set the {signatureHeaderName} header to the SendGrid Event Webhook signature."));
        }

        var timestampHeaderName = options.GetSignedEventWebhookTimestampHeaderName();
        if (!TryGetSingleHeader(context, timestampHeaderName, out var timestamp))
        {
            return SignedEventWebhookVerificationResult.Fail(
                "timestamp-missing",
                SignatureProblem(
                    "SendGrid signed Event Webhook timestamp is required.",
                    $"Set the {timestampHeaderName} header to the Unix timestamp included in the SendGrid signature."));
        }

        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var timestampSeconds))
        {
            return SignedEventWebhookVerificationResult.Fail(
                "timestamp-invalid",
                SignatureProblem(
                    "SendGrid signed Event Webhook timestamp is invalid.",
                    "The SendGrid Event Webhook timestamp must be a Unix timestamp in seconds."));
        }

        DateTimeOffset signedAtUtc;
        try
        {
            signedAtUtc = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return SignedEventWebhookVerificationResult.Fail(
                "timestamp-invalid",
                SignatureProblem(
                    "SendGrid signed Event Webhook timestamp is invalid.",
                    "The SendGrid Event Webhook timestamp is outside the supported Unix timestamp range."));
        }

        var ageSeconds = (int)Math.Abs(Math.Round((DateTimeOffset.UtcNow - signedAtUtc).TotalSeconds));
        var toleranceSeconds = options.GetSignedEventWebhookSignatureToleranceSeconds();
        if (ageSeconds > toleranceSeconds)
        {
            return SignedEventWebhookVerificationResult.Fail(
                "timestamp-out-of-tolerance",
                SignatureProblem(
                    "SendGrid signed Event Webhook timestamp is outside the allowed tolerance.",
                    $"The SendGrid Event Webhook timestamp must be within {toleranceSeconds} seconds of the current UTC time."));
        }

        byte[] signatureBytes;
        try
        {
            signatureBytes = Convert.FromBase64String(signature!);
        }
        catch (FormatException)
        {
            return SignedEventWebhookVerificationResult.Fail(
                "signature-invalid",
                SignatureProblem(
                    "SendGrid signed Event Webhook signature is invalid.",
                    "The SendGrid Event Webhook signature must be Base64 encoded."));
        }

        if (!VerifyEcdsaSha256Signature(ecdsa!, timestamp!, requestBody, signatureBytes))
        {
            return SignedEventWebhookVerificationResult.Fail(
                "signature-invalid",
                SignatureProblem(
                    "SendGrid signed Event Webhook signature is invalid.",
                    "The callback request body does not match the supplied SendGrid Event Webhook signature."));
        }

        return SignedEventWebhookVerificationResult.CreateVerified(
            timestampSeconds,
            ageSeconds,
            CreateSha256Fingerprint(signature!));
    }

    private static TenantInvitationDeliveryStatusReconciliationRequest ApplySignatureMetadata(
        TenantInvitationDeliveryStatusReconciliationRequest request,
        SignedEventWebhookVerificationResult signatureVerification)
    {
        if (!signatureVerification.Configured)
        {
            return request;
        }

        var metadata = CopyMetadata(request.Metadata);
        metadata["sendGridEventWebhookSignatureVerification"] = "verified";
        metadata["sendGridEventWebhookSignatureVerificationOwnership"] = "cephalon-managed";
        metadata["sendGridEventWebhookSignatureAlgorithm"] = "ecdsa-sha256";
        metadata["sendGridEventWebhookSignaturePayload"] = "timestamp+raw-body";
        metadata["sendGridEventWebhookSignatureTimestamp"] =
            signatureVerification.Timestamp!.Value.ToString(CultureInfo.InvariantCulture);
        metadata["sendGridEventWebhookSignatureAgeSeconds"] =
            signatureVerification.AgeSeconds!.Value.ToString(CultureInfo.InvariantCulture);
        metadata["sendGridEventWebhookSignatureFingerprint"] = signatureVerification.SignatureFingerprint!;

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

    private static bool TryImportPublicKey(string publicKey, out ECDsa? ecdsa)
    {
        ecdsa = ECDsa.Create();
        try
        {
            ecdsa.ImportFromPem(publicKey.AsSpan());
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
        }

        try
        {
            var der = Convert.FromBase64String(RemoveWhitespace(publicKey));
            ecdsa.ImportSubjectPublicKeyInfo(der, out _);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException or FormatException)
        {
            ecdsa.Dispose();
            ecdsa = null;
            return false;
        }
    }

    private static bool VerifyEcdsaSha256Signature(
        ECDsa ecdsa,
        string timestamp,
        byte[] requestBody,
        byte[] signature)
    {
        var signedPayload = CreateSignedWebhookPayload(timestamp, requestBody);
        var hash = SHA256.HashData(signedPayload);
        try
        {
            if (ecdsa.VerifyHash(hash, signature, DSASignatureFormat.Rfc3279DerSequence))
            {
                return true;
            }
        }
        catch (CryptographicException)
        {
        }

        try
        {
            return signature.Length == 64 &&
                ecdsa.VerifyHash(hash, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static byte[] CreateSignedWebhookPayload(string timestamp, byte[] requestBody)
    {
        var timestampBytes = Encoding.UTF8.GetBytes(timestamp);
        var signedPayload = new byte[timestampBytes.Length + requestBody.Length];
        Buffer.BlockCopy(timestampBytes, 0, signedPayload, 0, timestampBytes.Length);
        Buffer.BlockCopy(requestBody, 0, signedPayload, timestampBytes.Length, requestBody.Length);
        return signedPayload;
    }

    private static string CreateSha256Fingerprint(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string RemoveWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (!char.IsWhiteSpace(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
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
        SendGridInvitationDeliveryAspNetCoreOptions options)
    {
        if (!options.RequireStatusCallbackAuthorization)
        {
            return null;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Problem(
                title: "SendGrid invitation delivery status callback authorization is required.",
                detail: "The Cephalon SendGrid Event Webhook callback endpoint is fail-closed by default.",
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
                title: "SendGrid invitation delivery status callback authorization cannot be evaluated.",
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
            title: "SendGrid invitation delivery status callback authorization failed.",
            detail: "The authenticated principal is not authorized to accept SendGrid Event Webhook callbacks.",
            statusCode: StatusCodes.Status403Forbidden);
    }

    private static void ApplyAuthorizationMetadata(
        IEndpointRouteBuilder endpoints,
        IEndpointConventionBuilder builder,
        SendGridInvitationDeliveryAspNetCoreOptions options)
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

    private static bool TryGetSingleHeader(HttpContext context, string headerName, out string? value)
    {
        value = null;
        if (!context.Request.Headers.TryGetValue(headerName, out var values) ||
            values.Count != 1 ||
            string.IsNullOrWhiteSpace(values[0]))
        {
            return false;
        }

        value = values[0]!.Trim();
        return true;
    }

    private sealed record CallbackRequestBodyReadResult(byte[] Body, IResult? Failure)
    {
        public static CallbackRequestBodyReadResult Success(byte[] body) => new(body, null);

        public static CallbackRequestBodyReadResult Fail(IResult failure) => new([], failure);
    }

    private sealed record SignedEventWebhookVerificationResult(
        bool Configured,
        bool Verified,
        long? Timestamp,
        int? AgeSeconds,
        string? SignatureFingerprint,
        string Outcome,
        IResult? Failure)
    {
        public static SignedEventWebhookVerificationResult NotConfigured() =>
            new(false, false, null, null, null, "not-configured", null);

        public static SignedEventWebhookVerificationResult CreateVerified(
            long timestamp,
            int ageSeconds,
            string signatureFingerprint) =>
            new(true, true, timestamp, ageSeconds, signatureFingerprint, "verified", null);

        public static SignedEventWebhookVerificationResult Fail(string outcome, IResult failure) =>
            new(true, false, null, null, null, outcome, failure);
    }
}
