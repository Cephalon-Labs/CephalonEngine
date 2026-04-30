using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Configuration;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Services;
using Cephalon.MultiTenancy.Governance.Configuration;
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
    /// durable inboxing, distributed replay protection, and provider polling remain host-managed or future provider-pack
    /// responsibilities. When configured, the endpoint verifies the SNS message signature before translation and skips
    /// duplicate SNS message identifiers already present in the Cephalon delivery-status observation store.
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
                    AmazonSesSnsSignatureVerifier signatureVerifier,
                    AmazonSesInvitationDeliveryStatusCallbackReplayGuard replayGuard,
                    ITenantInvitationDeliveryStatusReconciler reconciler,
                    ITenantInvitationDeliveryStatusObservationStore observationStore,
                    MultiTenancyGovernanceOptions governanceOptions,
                    ILoggerFactory loggerFactory,
                    CancellationToken cancellationToken) =>
                    TranslateCallbackAsync(context, mapper, signatureVerifier, replayGuard, reconciler, observationStore, governanceOptions, loggerFactory, options, routePattern, cancellationToken))
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
                options.AcceptRawSesEventPayloads,
                options.RequireSnsSignatureVerification,
                options.RequireSnsSignatureVersion2,
                options.RequireAllowedSnsTopicArn,
                options.GetAllowedSnsTopicArns().Count,
                options.GetPinnedSnsSigningCertificatePem() is not null,
                options.ValidateSnsSigningCertificateChain,
                options.IsSnsReplayProtectionConfigured(),
                options.GetSnsReplayRetentionSeconds(),
                options.GetSnsReplayCacheLimit(),
                options.IsSnsMessageIdIdempotencyConfigured());

        return endpoints;
    }

    private static async Task<IResult> TranslateCallbackAsync(
        HttpContext context,
        AmazonSesSnsDeliveryStatusMapper mapper,
        AmazonSesSnsSignatureVerifier signatureVerifier,
        AmazonSesInvitationDeliveryStatusCallbackReplayGuard replayGuard,
        ITenantInvitationDeliveryStatusReconciler reconciler,
        ITenantInvitationDeliveryStatusObservationStore observationStore,
        MultiTenancyGovernanceOptions governanceOptions,
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
            var logger = loggerFactory.CreateLogger("Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore");
            var signatureVerification = await signatureVerifier
                .VerifyAsync(document.RootElement, cancellationToken)
                .ConfigureAwait(false);
            if (signatureVerification.FailureStatusCode is not null)
            {
                AmazonSesInvitationDeliveryAspNetCoreLogs.CallbackSignatureRejected(logger, signatureVerification.Outcome);
                return Results.Problem(
                    title: "Amazon SES SNS signature verification failed.",
                    detail: signatureVerification.Detail,
                    statusCode: signatureVerification.FailureStatusCode.Value);
            }

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

            var replayProtection = RecordSnsReplayProtection(options, replayGuard, signatureVerification);
            if (replayProtection.Failure is not null)
            {
                AmazonSesInvitationDeliveryAspNetCoreLogs.CallbackReplayRejected(logger, replayProtection.Outcome);
                return replayProtection.Failure;
            }

            var eventResults = new List<AmazonSesInvitationDeliveryStatusCallbackEventResult>(eventCount);
            var translatedEvents = 0;
            var reconciledEvents = 0;
            var skippedEvents = 0;
            var deniedEvents = 0;
            var duplicateEvents = 0;

            try
            {
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
                    var messageIdIdempotency = EvaluateSnsMessageIdIdempotency(
                        options,
                        governanceOptions,
                        observationStore,
                        mapping);
                    if (messageIdIdempotency.Duplicate)
                    {
                        duplicateEvents++;
                        AmazonSesInvitationDeliveryAspNetCoreLogs.CallbackDuplicateMessageSkipped(
                            logger,
                            messageIdIdempotency.ObservationId!);
                        eventResults.Add(mapping.ToDuplicateEventResult("The Amazon SNS message id was already recorded in the delivery-status observation store."));
                        continue;
                    }

                    var reconciliationRequest = ApplySnsSignatureReplayAndMessageIdIdempotencyMetadata(
                        mapping.Request!,
                        signatureVerification,
                        replayProtection,
                        messageIdIdempotency,
                        observationStore,
                        options);
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
                }
            }
            catch
            {
                ForgetSnsReplayProtection(replayGuard, replayProtection);
                throw;
            }

            if (translatedEvents > 0 && reconciledEvents == 0 && duplicateEvents == 0)
            {
                ForgetSnsReplayProtection(replayGuard, replayProtection);
            }

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
                snsSignatureVerificationRequired: signatureVerification.Configured,
                snsSignatureVerified: signatureVerification.Verified,
                snsSignatureVerificationOutcome: signatureVerification.Outcome,
                events: eventResults,
                snsReplayProtectionEnabled: replayProtection.Configured,
                snsReplayProtectionOutcome: replayProtection.Outcome,
                duplicateEvents: duplicateEvents);

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

    private static SnsReplayProtectionResult RecordSnsReplayProtection(
        AmazonSesInvitationDeliveryAspNetCoreOptions options,
        AmazonSesInvitationDeliveryStatusCallbackReplayGuard replayGuard,
        AmazonSesSnsSignatureVerificationResult signatureVerification)
    {
        if (!options.IsSnsReplayProtectionConfigured() ||
            !signatureVerification.Verified ||
            string.IsNullOrWhiteSpace(signatureVerification.TopicArn) ||
            string.IsNullOrWhiteSpace(signatureVerification.MessageId))
        {
            return SnsReplayProtectionResult.NotConfigured();
        }

        var replayFingerprint = CreateReplayFingerprint(signatureVerification.TopicArn!, signatureVerification.MessageId!);
        var decision = replayGuard.TryRecord(
            replayFingerprint,
            DateTimeOffset.UtcNow,
            TimeSpan.FromSeconds(options.GetSnsReplayRetentionSeconds()),
            options.GetSnsReplayCacheLimit());
        if (decision.Accepted)
        {
            return SnsReplayProtectionResult.Recorded(replayFingerprint);
        }

        return SnsReplayProtectionResult.Fail(
            decision.Outcome,
            replayFingerprint,
            Results.Problem(
                title: "Amazon SES SNS callback replay was rejected.",
                detail: "The verified Amazon SNS message has already been accepted inside the configured process-local replay window.",
                statusCode: StatusCodes.Status409Conflict));
    }

    private static void ForgetSnsReplayProtection(
        AmazonSesInvitationDeliveryStatusCallbackReplayGuard replayGuard,
        SnsReplayProtectionResult replayProtection)
    {
        if (replayProtection.Configured &&
            !string.IsNullOrWhiteSpace(replayProtection.ReplayFingerprint))
        {
            replayGuard.Forget(replayProtection.ReplayFingerprint!);
        }
    }

    private static SnsMessageIdIdempotencyResult EvaluateSnsMessageIdIdempotency(
        AmazonSesInvitationDeliveryAspNetCoreOptions options,
        MultiTenancyGovernanceOptions governanceOptions,
        ITenantInvitationDeliveryStatusObservationStore observationStore,
        AmazonSesSnsDeliveryStatusMappingResult mapping)
    {
        if (!options.IsSnsMessageIdIdempotencyConfigured() ||
            !governanceOptions.EnableInvitationDeliveryStatusObservationStore)
        {
            return SnsMessageIdIdempotencyResult.NotConfigured();
        }

        var request = mapping.Request!;
        if (string.IsNullOrWhiteSpace(mapping.SnsMessageId) ||
            !request.Metadata.TryGetValue(TenantInvitationDeliveryMetadataKeys.DeliveryStatusObservationId, out var observationId) ||
            string.IsNullOrWhiteSpace(observationId) ||
            !observationId.Trim().StartsWith("amazon-ses-sns:", StringComparison.OrdinalIgnoreCase))
        {
            return SnsMessageIdIdempotencyResult.MessageIdMissing();
        }

        var normalizedObservationId = observationId.Trim();
        var duplicate = observationStore.Observations.Any(
            observation => string.Equals(observation.ObservationId, normalizedObservationId, StringComparison.OrdinalIgnoreCase));
        return duplicate
            ? SnsMessageIdIdempotencyResult.DuplicateSkipped(normalizedObservationId)
            : SnsMessageIdIdempotencyResult.PendingRecord(normalizedObservationId);
    }

    private static TenantInvitationDeliveryStatusReconciliationRequest ApplySnsSignatureReplayAndMessageIdIdempotencyMetadata(
        TenantInvitationDeliveryStatusReconciliationRequest request,
        AmazonSesSnsSignatureVerificationResult signatureVerification,
        SnsReplayProtectionResult replayProtection,
        SnsMessageIdIdempotencyResult messageIdIdempotency,
        ITenantInvitationDeliveryStatusObservationStore observationStore,
        AmazonSesInvitationDeliveryAspNetCoreOptions options)
    {
        if (!signatureVerification.Configured &&
            !replayProtection.Configured &&
            !messageIdIdempotency.Configured)
        {
            return request;
        }

        var metadata = CopyMetadata(request.Metadata);
        if (signatureVerification.Configured)
        {
            metadata["amazonSesSnsSignatureVerification"] = signatureVerification.Outcome;
            metadata["amazonSesSnsSignatureVerificationOwnership"] = "cephalon-managed";
            metadata["amazonSesSnsSignaturePayload"] = "sns-canonical-string";
            AddIfPresent(metadata, "amazonSesSnsSignatureVersion", signatureVerification.SignatureVersion);
            AddIfPresent(metadata, "amazonSesSnsSignatureAlgorithm", signatureVerification.Algorithm);
            AddIfPresent(metadata, "amazonSesSnsSignatureTopicArn", signatureVerification.TopicArn);
            AddIfPresent(metadata, "amazonSesSnsSignatureMessageType", signatureVerification.MessageType);
            AddIfPresent(metadata, "amazonSesSnsSignatureMessageId", signatureVerification.MessageId);
            AddIfPresent(metadata, "amazonSesSnsSignatureTimestamp", signatureVerification.Timestamp);
            AddIfPresent(metadata, "amazonSesSnsSigningCertificateUrlHost", signatureVerification.SigningCertificateUrlHost);
            AddIfPresent(metadata, "amazonSesSnsSignatureFingerprint", signatureVerification.SignatureFingerprint);
            AddIfPresent(metadata, "amazonSesSnsSigningCertificateThumbprint", signatureVerification.CertificateThumbprint);
        }

        if (replayProtection.Configured)
        {
            metadata["amazonSesSnsReplayProtection"] = replayProtection.Outcome;
            metadata["amazonSesSnsReplayProtectionOwnership"] = "cephalon-managed";
            metadata["amazonSesSnsReplayProtectionPolicy"] = "sns-message-id";
            metadata["amazonSesSnsReplayProtectionKey"] = "topic-arn+message-id";
            metadata["amazonSesSnsReplayProtectionScope"] = "process-local";
            metadata["amazonSesSnsReplayProtectionDurability"] = "none";
            metadata["amazonSesSnsReplayProtectionRetentionSeconds"] =
                options.GetSnsReplayRetentionSeconds().ToString(CultureInfo.InvariantCulture);
            metadata["amazonSesSnsReplayProtectionCacheLimit"] =
                options.GetSnsReplayCacheLimit().ToString(CultureInfo.InvariantCulture);

            if (!string.IsNullOrWhiteSpace(replayProtection.ReplayFingerprint))
            {
                metadata["amazonSesSnsReplayProtectionFingerprint"] = replayProtection.ReplayFingerprint!;
            }
        }

        if (messageIdIdempotency.Configured)
        {
            metadata["amazonSesSnsMessageIdIdempotency"] = messageIdIdempotency.Outcome;
            metadata["amazonSesSnsMessageIdIdempotencyOwnership"] = "cephalon-managed";
            metadata["amazonSesSnsMessageIdIdempotencyPolicy"] = "sns-message-id";
            metadata["amazonSesSnsMessageIdIdempotencyKey"] = "MessageId";
            metadata["amazonSesSnsMessageIdIdempotencyScope"] = "observation-store";
            metadata["amazonSesSnsMessageIdIdempotencyStoreKind"] = observationStore.StoreKind;
            metadata["amazonSesSnsMessageIdIdempotencyDurability"] =
                observationStore.IsDurable ? "local-file" : "none";

            if (!string.IsNullOrWhiteSpace(messageIdIdempotency.ObservationId))
            {
                metadata["amazonSesSnsMessageIdObservationId"] = messageIdIdempotency.ObservationId!;
            }
        }

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

    private static string CreateReplayFingerprint(string topicArn, string messageId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(topicArn.Trim() + "\n" + messageId.Trim()));
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

    private static void AddIfPresent(Dictionary<string, string> metadata, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            metadata[key] = value.Trim();
        }
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

    private sealed record SnsReplayProtectionResult(
        bool Configured,
        string Outcome,
        string? ReplayFingerprint,
        IResult? Failure)
    {
        public static SnsReplayProtectionResult NotConfigured() =>
            new(false, "not-configured", null, null);

        public static SnsReplayProtectionResult Recorded(string replayFingerprint) =>
            new(true, "recorded", replayFingerprint, null);

        public static SnsReplayProtectionResult Fail(
            string outcome,
            string replayFingerprint,
            IResult failure) =>
            new(true, outcome, replayFingerprint, failure);
    }

    private sealed record SnsMessageIdIdempotencyResult(
        bool Configured,
        bool Duplicate,
        string Outcome,
        string? ObservationId)
    {
        public static SnsMessageIdIdempotencyResult NotConfigured() =>
            new(false, false, "not-configured", null);

        public static SnsMessageIdIdempotencyResult MessageIdMissing() =>
            new(true, false, "message-id-missing", null);

        public static SnsMessageIdIdempotencyResult PendingRecord(string observationId) =>
            new(true, false, "pending-record", observationId);

        public static SnsMessageIdIdempotencyResult DuplicateSkipped(string observationId) =>
            new(true, true, "duplicate-skipped", observationId);
    }

    private sealed record CallbackRequestBodyReadResult(byte[] Body, IResult? Failure)
    {
        public static CallbackRequestBodyReadResult Success(byte[] body) => new(body, null);

        public static CallbackRequestBodyReadResult Fail(IResult failure) => new([], failure);
    }
}
