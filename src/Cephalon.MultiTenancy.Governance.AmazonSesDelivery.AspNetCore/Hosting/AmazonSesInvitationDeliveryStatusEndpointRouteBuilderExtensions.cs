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
using System.Net.Http;
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
    /// host-agnostic <see cref="ITenantInvitationDeliveryStatusReconciler" />. Durable inboxing, distributed replay
    /// protection, and provider polling remain host-managed or future provider-pack responsibilities. When configured,
    /// the endpoint verifies the SNS message signature before translation, confirms verified SNS subscription requests,
    /// observes verified unsubscribe-confirmation lifecycle messages without restoring subscriptions, and skips
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
                    IAmazonSesSnsSubscriptionConfirmationClient subscriptionConfirmationClient,
                    ITenantInvitationDeliveryStatusReconciler reconciler,
                    ITenantInvitationDeliveryStatusObservationStore observationStore,
                    MultiTenancyGovernanceOptions governanceOptions,
                    ILoggerFactory loggerFactory,
                    CancellationToken cancellationToken) =>
                    TranslateCallbackAsync(context, mapper, signatureVerifier, replayGuard, subscriptionConfirmationClient, reconciler, observationStore, governanceOptions, loggerFactory, options, routePattern, cancellationToken))
            .WithName("CephalonAmazonSesInvitationDeliveryStatusCallback")
            .Accepts<JsonElement>("application/json")
            .Produces<AmazonSesInvitationDeliveryStatusCallbackResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status502BadGateway)
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
                options.IsSnsMessageIdIdempotencyConfigured(),
                options.IsSnsSubscriptionConfirmationConfigured(),
                options.IsSnsUnsubscribeConfirmationObservationConfigured(),
                options.GetSnsSubscriptionConfirmationTimeout());

        return endpoints;
    }

    private static async Task<IResult> TranslateCallbackAsync(
        HttpContext context,
        AmazonSesSnsDeliveryStatusMapper mapper,
        AmazonSesSnsSignatureVerifier signatureVerifier,
        AmazonSesInvitationDeliveryStatusCallbackReplayGuard replayGuard,
        IAmazonSesSnsSubscriptionConfirmationClient subscriptionConfirmationClient,
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

            var subscriptionConfirmation = await TryConfirmSnsSubscriptionAsync(
                    document.RootElement,
                    signatureVerification,
                    replayGuard,
                    subscriptionConfirmationClient,
                    logger,
                    options,
                    routePattern,
                    cancellationToken)
                .ConfigureAwait(false);
            if (subscriptionConfirmation.Handled)
            {
                return subscriptionConfirmation.Result!;
            }

            var unsubscribeConfirmation = TryObserveSnsUnsubscribeConfirmation(
                document.RootElement,
                signatureVerification,
                replayGuard,
                logger,
                options,
                routePattern);
            if (unsubscribeConfirmation.Handled)
            {
                return unsubscribeConfirmation.Result!;
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

    private static async Task<SnsLifecycleMessageHandlingResult> TryConfirmSnsSubscriptionAsync(
        JsonElement root,
        AmazonSesSnsSignatureVerificationResult signatureVerification,
        AmazonSesInvitationDeliveryStatusCallbackReplayGuard replayGuard,
        IAmazonSesSnsSubscriptionConfirmationClient subscriptionConfirmationClient,
        ILogger logger,
        AmazonSesInvitationDeliveryAspNetCoreOptions options,
        string routePattern,
        CancellationToken cancellationToken)
    {
        if (!IsSnsSubscriptionConfirmation(root))
        {
            return SnsLifecycleMessageHandlingResult.NotHandled();
        }

        if (!options.EnableSnsSubscriptionConfirmation)
        {
            return SnsLifecycleMessageHandlingResult.NotHandled();
        }

        if (!options.IsSnsSubscriptionConfirmationConfigured() || !signatureVerification.Verified)
        {
            return SnsLifecycleMessageHandlingResult.FromResult(Results.Problem(
                title: "Amazon SES SNS subscription confirmation is not safely configured.",
                detail: "Enable SNS signature verification and allow-list the expected topic before enabling automatic subscription confirmation.",
                statusCode: StatusCodes.Status500InternalServerError));
        }

        if (!TryReadString(root, "TopicArn", out var topicArn) ||
            !TryReadString(root, "MessageId", out var messageId) ||
            !TryReadString(root, "Token", out var token) ||
            !TryReadString(root, "Timestamp", out var timestamp) ||
            !TryReadString(root, "SubscribeURL", out var subscribeUrlValue))
        {
            return SnsLifecycleMessageHandlingResult.FromResult(Results.Problem(
                title: "Amazon SES SNS subscription confirmation is invalid.",
                detail: "A verified SNS subscription-confirmation envelope must include TopicArn, MessageId, Token, Timestamp, and SubscribeURL.",
                statusCode: StatusCodes.Status400BadRequest));
        }

        if (!TryCreateTrustedSnsSubscribeUrl(subscribeUrlValue, out var subscribeUrl, out var urlOutcome))
        {
            return SnsLifecycleMessageHandlingResult.FromResult(Results.Problem(
                title: "Amazon SES SNS subscription confirmation URL is not trusted.",
                detail: $"The SubscribeURL failed the configured HTTPS Amazon SNS confirmation policy: {urlOutcome}.",
                statusCode: StatusCodes.Status400BadRequest));
        }

        var replayProtection = RecordSnsReplayProtection(options, replayGuard, signatureVerification);
        if (replayProtection.Failure is not null)
        {
            AmazonSesInvitationDeliveryAspNetCoreLogs.CallbackReplayRejected(logger, replayProtection.Outcome);
            return SnsLifecycleMessageHandlingResult.FromResult(replayProtection.Failure);
        }

        AmazonSesSnsSubscriptionConfirmationResult confirmation;
        try
        {
            confirmation = await subscriptionConfirmationClient
                .ConfirmAsync(
                    new AmazonSesSnsSubscriptionConfirmationRequest(
                        topicArn,
                        messageId,
                        token,
                        subscribeUrl,
                        timestamp),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested &&
            ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            ForgetSnsReplayProtection(replayGuard, replayProtection);
            AmazonSesInvitationDeliveryAspNetCoreLogs.SubscriptionConfirmationFailed(logger, messageId, "client-failed");
            return SnsLifecycleMessageHandlingResult.FromResult(Results.Problem(
                title: "Amazon SES SNS subscription confirmation failed.",
                detail: "The configured SNS subscription-confirmation client could not complete the provider confirmation request.",
                statusCode: StatusCodes.Status502BadGateway));
        }

        if (!confirmation.Succeeded)
        {
            ForgetSnsReplayProtection(replayGuard, replayProtection);
            AmazonSesInvitationDeliveryAspNetCoreLogs.SubscriptionConfirmationFailed(logger, messageId, confirmation.Outcome);
            return SnsLifecycleMessageHandlingResult.FromResult(Results.Problem(
                title: "Amazon SES SNS subscription confirmation failed.",
                detail: confirmation.Reason,
                statusCode: StatusCodes.Status502BadGateway));
        }

        AmazonSesInvitationDeliveryAspNetCoreLogs.SubscriptionConfirmationConfirmed(logger, messageId, confirmation.Outcome);
        var result = new AmazonSesInvitationDeliveryStatusCallbackResult(
            routePattern,
            totalEvents: 1,
            translatedEvents: 0,
            reconciledEvents: 0,
            skippedEvents: 1,
            deniedEvents: 0,
            snsSignatureVerificationRequired: signatureVerification.Configured,
            snsSignatureVerified: signatureVerification.Verified,
            snsSignatureVerificationOutcome: signatureVerification.Outcome,
            events:
            [
                new AmazonSesInvitationDeliveryStatusCallbackEventResult(
                    index: 0,
                    snsMessageId: messageId,
                    snsMessageType: "SubscriptionConfirmation",
                    amazonSesMessageId: null,
                    amazonSesEventType: null,
                    tenantId: null,
                    invitationId: null,
                    status: null,
                    outcome: "subscription-confirmed",
                    translated: false,
                    reconciled: false,
                    reason: confirmation.Reason)
            ],
            snsReplayProtectionEnabled: replayProtection.Configured,
            snsReplayProtectionOutcome: replayProtection.Outcome,
            snsSubscriptionConfirmationEnabled: true,
            snsSubscriptionConfirmationOutcome: confirmation.Outcome,
            subscriptionConfirmationAttempts: 1,
            subscriptionConfirmationsSucceeded: 1);

        return SnsLifecycleMessageHandlingResult.FromResult(Results.Json(result, SerializerOptions));
    }

    private static SnsLifecycleMessageHandlingResult TryObserveSnsUnsubscribeConfirmation(
        JsonElement root,
        AmazonSesSnsSignatureVerificationResult signatureVerification,
        AmazonSesInvitationDeliveryStatusCallbackReplayGuard replayGuard,
        ILogger logger,
        AmazonSesInvitationDeliveryAspNetCoreOptions options,
        string routePattern)
    {
        if (!IsSnsUnsubscribeConfirmation(root))
        {
            return SnsLifecycleMessageHandlingResult.NotHandled();
        }

        if (!options.EnableSnsUnsubscribeConfirmationObservation)
        {
            return SnsLifecycleMessageHandlingResult.NotHandled();
        }

        if (!options.IsSnsUnsubscribeConfirmationObservationConfigured() || !signatureVerification.Verified)
        {
            return SnsLifecycleMessageHandlingResult.FromResult(Results.Problem(
                title: "Amazon SES SNS unsubscribe confirmation observation is not safely configured.",
                detail: "Enable SNS signature verification and allow-list the expected topic before reporting unsubscribe-confirmation lifecycle messages.",
                statusCode: StatusCodes.Status500InternalServerError));
        }

        if (!TryReadString(root, "TopicArn", out _) ||
            !TryReadString(root, "MessageId", out var messageId) ||
            !TryReadString(root, "Token", out _) ||
            !TryReadString(root, "Timestamp", out _) ||
            !TryReadString(root, "SubscribeURL", out var subscribeUrlValue))
        {
            return SnsLifecycleMessageHandlingResult.FromResult(Results.Problem(
                title: "Amazon SES SNS unsubscribe confirmation is invalid.",
                detail: "A verified SNS unsubscribe-confirmation envelope must include TopicArn, MessageId, Token, Timestamp, and SubscribeURL.",
                statusCode: StatusCodes.Status400BadRequest));
        }

        if (!TryCreateTrustedSnsSubscribeUrl(subscribeUrlValue, out _, out var urlOutcome))
        {
            return SnsLifecycleMessageHandlingResult.FromResult(Results.Problem(
                title: "Amazon SES SNS unsubscribe confirmation URL is not trusted.",
                detail: $"The SubscribeURL failed the configured HTTPS Amazon SNS re-confirmation policy: {urlOutcome}. The endpoint never invokes this URL automatically.",
                statusCode: StatusCodes.Status400BadRequest));
        }

        var replayProtection = RecordSnsReplayProtection(options, replayGuard, signatureVerification);
        if (replayProtection.Failure is not null)
        {
            AmazonSesInvitationDeliveryAspNetCoreLogs.CallbackReplayRejected(logger, replayProtection.Outcome);
            return SnsLifecycleMessageHandlingResult.FromResult(replayProtection.Failure);
        }

        const string outcome = "observed";
        AmazonSesInvitationDeliveryAspNetCoreLogs.UnsubscribeConfirmationObserved(logger, messageId, outcome);
        var result = new AmazonSesInvitationDeliveryStatusCallbackResult(
            routePattern,
            totalEvents: 1,
            translatedEvents: 0,
            reconciledEvents: 0,
            skippedEvents: 1,
            deniedEvents: 0,
            snsSignatureVerificationRequired: signatureVerification.Configured,
            snsSignatureVerified: signatureVerification.Verified,
            snsSignatureVerificationOutcome: signatureVerification.Outcome,
            events:
            [
                new AmazonSesInvitationDeliveryStatusCallbackEventResult(
                    index: 0,
                    snsMessageId: messageId,
                    snsMessageType: "UnsubscribeConfirmation",
                    amazonSesMessageId: null,
                    amazonSesEventType: null,
                    tenantId: null,
                    invitationId: null,
                    status: null,
                    outcome: "unsubscribe-confirmation-observed",
                    translated: false,
                    reconciled: false,
                    reason: "The verified SNS unsubscribe-confirmation envelope was observed. The SubscribeURL was not invoked because it would restore the subscription.")
            ],
            snsReplayProtectionEnabled: replayProtection.Configured,
            snsReplayProtectionOutcome: replayProtection.Outcome,
            snsUnsubscribeConfirmationObservationEnabled: true,
            snsUnsubscribeConfirmationOutcome: outcome,
            unsubscribeConfirmationsObserved: 1);

        return SnsLifecycleMessageHandlingResult.FromResult(Results.Json(result, SerializerOptions));
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

    private static bool IsSnsSubscriptionConfirmation(JsonElement root) =>
        TryReadString(root, "Type", out var messageType) &&
        string.Equals(messageType, "SubscriptionConfirmation", StringComparison.Ordinal);

    private static bool IsSnsUnsubscribeConfirmation(JsonElement root) =>
        TryReadString(root, "Type", out var messageType) &&
        string.Equals(messageType, "UnsubscribeConfirmation", StringComparison.Ordinal);

    private static bool TryReadString(JsonElement root, string propertyName, out string value)
    {
        value = string.Empty;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var raw = property.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        value = raw.Trim();
        return true;
    }

    private static bool TryCreateTrustedSnsSubscribeUrl(string value, out Uri subscribeUrl, out string outcome)
    {
        subscribeUrl = null!;
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var parsed))
        {
            outcome = "subscribe-url-invalid";
            return false;
        }

        if (!string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            outcome = "subscribe-url-not-https";
            return false;
        }

        if (!IsTrustedSnsEndpointHost(parsed.IdnHost.ToLowerInvariant()))
        {
            outcome = "subscribe-url-host-untrusted";
            return false;
        }

        if (!string.Equals(parsed.AbsolutePath, "/", StringComparison.Ordinal) ||
            !string.IsNullOrEmpty(parsed.Fragment))
        {
            outcome = "subscribe-url-path-untrusted";
            return false;
        }

        if (!ContainsConfirmSubscriptionAction(parsed.Query))
        {
            outcome = "subscribe-url-action-untrusted";
            return false;
        }

        subscribeUrl = parsed;
        outcome = "verified";
        return true;
    }

    private static bool ContainsConfirmSubscriptionAction(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        var trimmed = query[0] == '?' ? query[1..] : query;
        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = pair.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(pair[..separatorIndex]);
            var value = Uri.UnescapeDataString(pair[(separatorIndex + 1)..]);
            if (string.Equals(key, "Action", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(value, "ConfirmSubscription", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTrustedSnsEndpointHost(string host)
    {
        const string AmazonAwsSuffix = ".amazonaws.com";
        const string AmazonAwsChinaSuffix = ".amazonaws.com.cn";

        return IsSingleRegionSnsHost(host, AmazonAwsSuffix) ||
            IsSingleRegionSnsHost(host, AmazonAwsChinaSuffix);

        static bool IsSingleRegionSnsHost(string host, string suffix)
        {
            if (!host.StartsWith("sns.", StringComparison.Ordinal) ||
                !host.EndsWith(suffix, StringComparison.Ordinal))
            {
                return false;
            }

            var region = host["sns.".Length..^suffix.Length];
            return region.Length > 0 &&
                region.IndexOf('.', StringComparison.Ordinal) < 0 &&
                region.Count(static character => character == '-') >= 2 &&
                char.IsAsciiDigit(region[^1]) &&
                region.All(static character => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-');
        }
    }

    private sealed record SnsLifecycleMessageHandlingResult(bool Handled, IResult? Result)
    {
        public static SnsLifecycleMessageHandlingResult NotHandled() => new(false, null);

        public static SnsLifecycleMessageHandlingResult FromResult(IResult result) => new(true, result);
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
