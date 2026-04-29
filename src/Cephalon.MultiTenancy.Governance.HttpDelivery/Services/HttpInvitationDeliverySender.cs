using Cephalon.MultiTenancy.Governance.HttpDelivery.Configuration;
using Cephalon.MultiTenancy.Governance.HttpDelivery.Hosting;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
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

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(options.GetTimeout());

            using var request = new HttpRequestMessage(options.GetHttpMethod(), endpoint)
            {
                Content = JsonContent.Create(BuildPayload(context), options: SerializerOptions)
            };
            AddHeaders(request);

            var client = httpClientFactory.CreateClient(HttpInvitationDeliveryServiceCollectionExtensions.HttpClientName);
            using var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                .ConfigureAwait(false);

            var statusCode = (int)response.StatusCode;
            var metadata = BuildBaseMetadata(response.StatusCode, response.ReasonPhrase);
            var providerMessageId = GetProviderMessageId(response);

            if (options.IncludeResponseBodyInMetadata)
            {
                metadata["httpResponseBody"] = await ReadResponseBodyAsync(response, timeout.Token).ConfigureAwait(false);
            }

            if (IsAccepted(statusCode, response.IsSuccessStatusCode))
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
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            var reason = $"HTTP invitation delivery webhook timed out after {Math.Max(1, options.TimeoutSeconds)} seconds.";
            HttpInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, exception);

            return new TenantInvitationDeliverySenderResult(
                TenantInvitationDeliveryOutcomes.SenderFailed,
                dispatched: false,
                reason: reason,
                dispatchedAtUtc: context.DispatchedAtUtc,
                metadata: BuildBaseMetadata(null, reason));
        }
        catch (Exception exception)
        {
            var reason = "HTTP invitation delivery webhook failed before accepting dispatch.";
            HttpInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, exception);

            var metadata = BuildBaseMetadata(null, reason);
            metadata["httpExceptionType"] = exception.GetType().Name;

            return new TenantInvitationDeliverySenderResult(
                TenantInvitationDeliveryOutcomes.SenderFailed,
                dispatched: false,
                reason: reason,
                dispatchedAtUtc: context.DispatchedAtUtc,
                metadata: metadata);
        }
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

    private Dictionary<string, string> BuildBaseMetadata(HttpStatusCode? statusCode, string? reason)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["httpSenderId"] = SenderId,
            ["httpEndpointHost"] = options.TryGetEndpoint()?.Host ?? string.Empty
        };

        if (statusCode is not null)
        {
            metadata["httpStatusCode"] = ((int)statusCode.Value).ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            metadata["httpReason"] = reason.Trim();
        }

        return metadata;
    }
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
