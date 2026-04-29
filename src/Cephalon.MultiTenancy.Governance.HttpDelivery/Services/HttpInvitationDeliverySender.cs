using Cephalon.MultiTenancy.Governance.HttpDelivery.Configuration;
using Cephalon.MultiTenancy.Governance.HttpDelivery.Hosting;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cephalon.MultiTenancy.Governance.HttpDelivery.Services;

internal sealed class HttpInvitationDeliverySender(
    IHttpClientFactory httpClientFactory,
    HttpInvitationDeliveryOptions options,
    ILogger<HttpInvitationDeliverySender> logger) : ITenantInvitationDeliverySender
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public string SenderId => options.SenderId.Trim();

    public async ValueTask<TenantInvitationDeliverySenderResult> SendAsync(
        TenantInvitationDeliveryContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (options.SupportedChannels.Count > 0 &&
            !options.SupportedChannels.Contains(context.Channel, StringComparer.OrdinalIgnoreCase))
        {
            var reason = $"HTTP invitation delivery sender '{SenderId}' does not support channel '{context.Channel}'.";
            HttpInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, null);

            return new TenantInvitationDeliverySenderResult(
                TenantInvitationDeliveryOutcomes.Suppressed,
                dispatched: false,
                reason: reason,
                dispatchedAtUtc: context.DispatchedAtUtc,
                metadata: BuildBaseMetadata(null, reason));
        }

        var endpoint = options.TryGetEndpoint();
        if (endpoint is null)
        {
            var reason = "HTTP invitation delivery endpoint is not configured.";
            HttpInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, null);

            return new TenantInvitationDeliverySenderResult(
                TenantInvitationDeliveryOutcomes.SenderFailed,
                dispatched: false,
                reason: reason,
                dispatchedAtUtc: context.DispatchedAtUtc,
                metadata: BuildBaseMetadata(null, reason));
        }

        IdempotencyDescriptor? idempotency = null;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(options.GetTimeout());

            var maxAttempts = options.GetMaxAttempts();
            var retryDelay = options.GetRetryDelay();
            var requestBody = JsonSerializer.Serialize(BuildPayload(context), SerializerOptions);
            idempotency = BuildIdempotency(context);
            var client = httpClientFactory.CreateClient(HttpInvitationDeliveryServiceCollectionExtensions.HttpClientName);
            var retried = false;
            string? retryReason = null;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var finalAttempt = attempt == maxAttempts;
                try
                {
                    using var request = CreateRequest(endpoint, context, requestBody, idempotency);
                    using var response = await client
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                        .ConfigureAwait(false);

                    var statusCode = (int)response.StatusCode;
                    var providerMessageId = GetProviderMessageId(response);
                    var accepted = IsAccepted(statusCode, response.IsSuccessStatusCode);

                    if (!accepted && !finalAttempt && ShouldRetryStatus(statusCode))
                    {
                        retryReason = $"HTTP status code {statusCode}.";
                        retried = true;
                        response.Dispose();
                        await DelayBeforeRetryAsync(retryDelay, timeout.Token).ConfigureAwait(false);
                        continue;
                    }

                    var metadata = BuildBaseMetadata(response.StatusCode, response.ReasonPhrase, attempt, maxAttempts, retried, retryReason, idempotency);

                    if (options.IncludeResponseBodyInMetadata)
                    {
                        metadata["httpResponseBody"] = await ReadResponseBodyAsync(response, timeout.Token).ConfigureAwait(false);
                    }

                    if (accepted)
                    {
                        HttpInvitationDeliveryLogs.Accepted(logger, SenderId, context.TenantId, context.InvitationId, statusCode);

                        return new TenantInvitationDeliverySenderResult(
                            TenantInvitationDeliveryOutcomes.Dispatched,
                            dispatched: true,
                            providerMessageId: providerMessageId,
                            reason: $"HTTP invitation delivery webhook accepted dispatch with status code {statusCode}.",
                            dispatchedAtUtc: context.DispatchedAtUtc,
                            metadata: metadata);
                    }

                    var reason = $"HTTP invitation delivery webhook returned status code {statusCode}.";
                    HttpInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, null);

                    return new TenantInvitationDeliverySenderResult(
                        TenantInvitationDeliveryOutcomes.SenderFailed,
                        dispatched: false,
                        providerMessageId: providerMessageId,
                        reason: reason,
                        dispatchedAtUtc: context.DispatchedAtUtc,
                        metadata: metadata);
                }
                catch (HttpRequestException exception) when (!finalAttempt && options.RetryTransportFailures)
                {
                    retryReason = exception.GetType().Name;
                    retried = true;
                    await DelayBeforeRetryAsync(retryDelay, timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
                {
                    var reason = $"HTTP invitation delivery webhook timed out after {Math.Max(1, options.TimeoutSeconds)} seconds.";
                    HttpInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, exception);

                    return new TenantInvitationDeliverySenderResult(
                        TenantInvitationDeliveryOutcomes.SenderFailed,
                        dispatched: false,
                        reason: reason,
                        dispatchedAtUtc: context.DispatchedAtUtc,
                        metadata: BuildBaseMetadata(null, reason, attempt, maxAttempts, retried, retryReason, idempotency));
                }
                catch (Exception exception)
                {
                    var reason = "HTTP invitation delivery webhook failed before accepting dispatch.";
                    HttpInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, exception);

                    var metadata = BuildBaseMetadata(null, reason, attempt, maxAttempts, retried, retryReason, idempotency);
                    metadata["httpExceptionType"] = exception.GetType().Name;

                    return new TenantInvitationDeliverySenderResult(
                        TenantInvitationDeliveryOutcomes.SenderFailed,
                        dispatched: false,
                        reason: reason,
                        dispatchedAtUtc: context.DispatchedAtUtc,
                        metadata: metadata);
                }
            }
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            var reason = $"HTTP invitation delivery webhook timed out after {Math.Max(1, options.TimeoutSeconds)} seconds.";
            HttpInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, exception);

            return new TenantInvitationDeliverySenderResult(
                TenantInvitationDeliveryOutcomes.SenderFailed,
                dispatched: false,
                reason: reason,
                dispatchedAtUtc: context.DispatchedAtUtc,
                metadata: BuildBaseMetadata(null, reason, idempotency: idempotency));
        }
        catch (Exception exception)
        {
            var reason = "HTTP invitation delivery webhook failed before accepting dispatch.";
            HttpInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, exception);

            var metadata = BuildBaseMetadata(null, reason, idempotency: idempotency);
            metadata["httpExceptionType"] = exception.GetType().Name;

            return new TenantInvitationDeliverySenderResult(
                TenantInvitationDeliveryOutcomes.SenderFailed,
                dispatched: false,
                reason: reason,
                dispatchedAtUtc: context.DispatchedAtUtc,
                metadata: metadata);
        }

        throw new InvalidOperationException("HTTP invitation delivery completed without producing a sender result.");
    }

    private HttpRequestMessage CreateRequest(
        Uri endpoint,
        TenantInvitationDeliveryContext context,
        string requestBody,
        IdempotencyDescriptor? idempotency)
    {
        var request = new HttpRequestMessage(options.GetHttpMethod(), endpoint)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };
        AddHeaders(request);
        AddIdempotencyHeader(request, idempotency);
        AddSignatureHeaders(request, context, requestBody);

        return request;
    }

    private IdempotencyDescriptor? BuildIdempotency(TenantInvitationDeliveryContext context)
    {
        if (!options.EnableIdempotencyHeader)
        {
            return null;
        }

        var headerName = GetHeaderNameOrDefault(options.IdempotencyHeaderName, "X-Cephalon-Idempotency-Key");
        var metadataKey = string.IsNullOrWhiteSpace(options.IdempotencyMetadataKey)
            ? "idempotencyKey"
            : options.IdempotencyMetadataKey.Trim();

        if (context.Metadata.TryGetValue(metadataKey, out var configuredKey) &&
            !string.IsNullOrWhiteSpace(configuredKey))
        {
            return new IdempotencyDescriptor(headerName, NormalizeConfiguredIdempotencyKey(configuredKey), "metadata");
        }

        return new IdempotencyDescriptor(headerName, CreateDerivedIdempotencyKey(context), "derived");
    }

    private string CreateDerivedIdempotencyKey(TenantInvitationDeliveryContext context)
    {
        var material = string.Join(
            '\n',
            "cephalon-http-invitation-delivery",
            "v1",
            context.TenantId.Trim().ToLowerInvariant(),
            context.InvitationId.Trim().ToLowerInvariant(),
            context.Channel.Trim().ToLowerInvariant(),
            SenderId.ToLowerInvariant());

        return "cephalon-invitation-" + ToBase64Url(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    private static string NormalizeConfiguredIdempotencyKey(string key)
    {
        var normalized = key.Trim();
        return ShouldHashConfiguredIdempotencyKey(normalized)
            ? "cephalon-custom-" + ToBase64Url(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)))
            : normalized;
    }

    private static bool ShouldHashConfiguredIdempotencyKey(string key)
    {
        if (key.Length > 512)
        {
            return true;
        }

        foreach (var character in key)
        {
            if (character < '!' || character > '~')
            {
                return true;
            }
        }

        return false;
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert
            .ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private HttpInvitationDeliveryPayload BuildPayload(TenantInvitationDeliveryContext context)
    {
        return new HttpInvitationDeliveryPayload
        {
            TenantId = context.TenantId,
            InvitationId = context.InvitationId,
            InviteeId = context.InviteeId,
            InviteeKind = context.InviteeKind,
            DisplayName = context.DisplayName,
            Channel = context.Channel,
            RequestedSenderId = context.RequestedSenderId,
            Source = context.Source,
            Actor = context.Actor,
            CorrelationId = context.CorrelationId,
            DispatchedAtUtc = context.DispatchedAtUtc,
            Roles = context.Roles,
            Metadata = options.IncludeRequestMetadata ? context.Metadata : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            InvitationMetadata = options.IncludeInvitationMetadata ? context.Invitation.Metadata : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };
    }

    private void AddSignatureHeaders(HttpRequestMessage request, TenantInvitationDeliveryContext context, string requestBody)
    {
        if (!options.IsSigningEnabled)
        {
            return;
        }

        var signatureHeaderName = GetHeaderNameOrDefault(options.SignatureHeaderName, "X-Cephalon-Webhook-Signature");
        var timestampHeaderName = GetHeaderNameOrDefault(options.SignatureTimestampHeaderName, "X-Cephalon-Webhook-Signature-Timestamp");
        var keyIdHeaderName = GetHeaderNameOrDefault(options.SignatureKeyIdHeaderName, "X-Cephalon-Webhook-Key-Id");
        var timestamp = context.DispatchedAtUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var signature = CreateSignature(options.SigningSecret!, timestamp, requestBody);

        SetRequestHeader(request, timestampHeaderName, timestamp);
        SetRequestHeader(request, signatureHeaderName, signature);

        if (!string.IsNullOrWhiteSpace(options.SigningKeyId))
        {
            SetRequestHeader(request, keyIdHeaderName, options.SigningKeyId.Trim());
        }
    }

    private static void AddIdempotencyHeader(HttpRequestMessage request, IdempotencyDescriptor? idempotency)
    {
        if (idempotency is null)
        {
            return;
        }

        SetRequestHeader(request, idempotency.HeaderName, idempotency.Key);
    }

    private static string CreateSignature(string secret, string timestamp, string requestBody)
    {
        var signedPayload = $"{timestamp}.{requestBody}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        return "v1=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GetHeaderNameOrDefault(string? headerName, string defaultHeaderName)
    {
        return string.IsNullOrWhiteSpace(headerName) ? defaultHeaderName : headerName.Trim();
    }

    private static void SetRequestHeader(HttpRequestMessage request, string headerName, string value)
    {
        request.Headers.Remove(headerName);
        request.Headers.TryAddWithoutValidation(headerName, value);
    }

    private void AddHeaders(HttpRequestMessage request)
    {
        foreach (var pair in options.Headers)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
            {
                continue;
            }

            if (!request.Headers.TryAddWithoutValidation(pair.Key, pair.Value))
            {
                request.Content?.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
            }
        }
    }

    private bool IsAccepted(int statusCode, bool isSuccessStatusCode)
    {
        return options.ExpectedStatusCodes.Count > 0
            ? options.ExpectedStatusCodes.Contains(statusCode)
            : isSuccessStatusCode;
    }

    private bool ShouldRetryStatus(int statusCode)
    {
        return options.RetryStatusCodes.Contains(statusCode);
    }

    private static async ValueTask DelayBeforeRetryAsync(TimeSpan retryDelay, CancellationToken cancellationToken)
    {
        if (retryDelay <= TimeSpan.Zero)
        {
            return;
        }

        await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
    }

    private string? GetProviderMessageId(HttpResponseMessage response)
    {
        if (string.IsNullOrWhiteSpace(options.ProviderMessageIdHeaderName))
        {
            return null;
        }

        return response.Headers.TryGetValues(options.ProviderMessageIdHeaderName.Trim(), out var values)
            ? values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?.Trim()
            : null;
    }

    private async ValueTask<string> ReadResponseBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var limit = options.GetResponseBodyMetadataLimit();
        if (limit == 0 || body.Length <= limit)
        {
            return body;
        }

        return body[..limit];
    }

    private Dictionary<string, string> BuildBaseMetadata(
        HttpStatusCode? statusCode,
        string? reason,
        int attemptCount = 0,
        int? maxAttempts = null,
        bool retried = false,
        string? retryReason = null,
        IdempotencyDescriptor? idempotency = null)
    {
        var configuredMaxAttempts = maxAttempts ?? options.GetMaxAttempts();
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["httpSenderId"] = SenderId,
            ["httpEndpointHost"] = options.TryGetEndpoint()?.Host ?? string.Empty,
            ["httpSigned"] = options.IsSigningEnabled ? "true" : "false",
            ["httpAttemptCount"] = Math.Max(0, attemptCount).ToString(CultureInfo.InvariantCulture),
            ["httpMaxAttempts"] = configuredMaxAttempts.ToString(CultureInfo.InvariantCulture),
            ["httpRetried"] = retried ? "true" : "false"
        };

        if (configuredMaxAttempts > 1)
        {
            metadata["httpRetryDelayMilliseconds"] = Math.Clamp(options.RetryDelayMilliseconds, 0, 60_000).ToString(CultureInfo.InvariantCulture);
            metadata["httpRetryTransportFailures"] = options.RetryTransportFailures ? "true" : "false";
            if (options.RetryStatusCodes.Count > 0)
            {
                metadata["httpRetryStatusCodes"] = string.Join(",", options.RetryStatusCodes);
            }
        }

        if (options.IsSigningEnabled && !string.IsNullOrWhiteSpace(options.SigningKeyId))
        {
            metadata["httpSigningKeyId"] = options.SigningKeyId.Trim();
        }

        if (idempotency is not null)
        {
            metadata["httpIdempotencyHeaderName"] = idempotency.HeaderName;
            metadata["httpIdempotencyKey"] = idempotency.Key;
            metadata["httpIdempotencyKeySource"] = idempotency.Source;
        }

        if (statusCode is not null)
        {
            metadata["httpStatusCode"] = ((int)statusCode.Value).ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            metadata["httpReason"] = reason.Trim();
        }

        if (!string.IsNullOrWhiteSpace(retryReason))
        {
            metadata["httpRetryReason"] = retryReason.Trim();
        }

        return metadata;
    }

    private sealed record IdempotencyDescriptor(string HeaderName, string Key, string Source);
}

internal static class HttpInvitationDeliveryLogs
{
    private static readonly Action<ILogger, string, string, string, int, Exception?> AcceptedMessage =
        LoggerMessage.Define<string, string, string, int>(
            LogLevel.Information,
            new EventId(
                HttpInvitationDeliveryDiagnosticsConventions.HttpInvitationDeliveryAccepted.Id,
                HttpInvitationDeliveryDiagnosticsConventions.HttpInvitationDeliveryAccepted.Name),
            "HTTP invitation delivery sender '{SenderId}' accepted invitation '{InvitationId}' for tenant '{TenantId}' with status code {StatusCode}.");

    private static readonly Action<ILogger, string, string, string, string, Exception?> FailedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(
                HttpInvitationDeliveryDiagnosticsConventions.HttpInvitationDeliveryFailed.Id,
                HttpInvitationDeliveryDiagnosticsConventions.HttpInvitationDeliveryFailed.Name),
            "HTTP invitation delivery sender '{SenderId}' failed invitation '{InvitationId}' for tenant '{TenantId}'. Reason: {Reason}.");

    public static void Accepted(ILogger logger, string senderId, string tenantId, string invitationId, int statusCode) =>
        AcceptedMessage(logger, senderId, tenantId, invitationId, statusCode, null);

    public static void Failed(ILogger logger, string senderId, string tenantId, string invitationId, string reason, Exception? exception) =>
        FailedMessage(logger, senderId, tenantId, invitationId, reason, exception);
}
