using Cephalon.MultiTenancy.Governance.MailgunDelivery.Configuration;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.Services;

internal sealed class MailgunInvitationDeliverySender(
    MailgunInvitationDeliveryOptions options,
    IMailgunInvitationDeliveryClient client,
    ILogger<MailgunInvitationDeliverySender> logger) : ITenantInvitationDeliverySender
{
    private static readonly HashSet<string> ReservedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "content-type",
        "content-transfer-encoding",
        "to",
        "from",
        "subject",
        "reply-to",
        "cc",
        "bcc"
    };

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
            var reason = $"Mailgun invitation delivery sender '{SenderId}' does not support channel '{context.Channel}'.";
            MailgunInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, null);

            return new TenantInvitationDeliverySenderResult(
                TenantInvitationDeliveryOutcomes.Suppressed,
                dispatched: false,
                reason: reason,
                dispatchedAtUtc: context.DispatchedAtUtc,
                metadata: BuildBaseMetadata(context, null, null, reason));
        }

        var recipient = ResolveRecipientEmail(context);
        if (recipient is null)
        {
            var reason = $"Mailgun invitation delivery could not resolve a recipient email address from metadata key '{GetRecipientMetadataKey()}'.";
            MailgunInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, null);

            return new TenantInvitationDeliverySenderResult(
                TenantInvitationDeliveryOutcomes.SenderFailed,
                dispatched: false,
                reason: reason,
                dispatchedAtUtc: context.DispatchedAtUtc,
                metadata: BuildBaseMetadata(context, null, null, reason));
        }

        try
        {
            var message = CreateMessage(context, recipient);
            var result = await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
            var metadata = BuildBaseMetadata(context, message, result.Metadata, result.Reason);

            if (result.StatusCode is not null)
            {
                metadata["mailgunStatusCode"] = result.StatusCode.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (result.Accepted)
            {
                MailgunInvitationDeliveryLogs.Accepted(logger, SenderId, context.TenantId, context.InvitationId, result.StatusCode ?? 0);

                return new TenantInvitationDeliverySenderResult(
                    TenantInvitationDeliveryOutcomes.Dispatched,
                    dispatched: true,
                    providerMessageId: result.ProviderMessageId,
                    reason: string.IsNullOrWhiteSpace(result.Reason)
                        ? "Mailgun Messages API accepted the invitation delivery request."
                        : result.Reason,
                    dispatchedAtUtc: context.DispatchedAtUtc,
                    metadata: metadata);
            }

            var reason = string.IsNullOrWhiteSpace(result.Reason)
                ? "Mailgun Messages API did not accept the invitation delivery request."
                : result.Reason;
            MailgunInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, null);

            return new TenantInvitationDeliverySenderResult(
                TenantInvitationDeliveryOutcomes.SenderFailed,
                dispatched: false,
                providerMessageId: result.ProviderMessageId,
                reason: reason,
                dispatchedAtUtc: context.DispatchedAtUtc,
                metadata: metadata);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            var reason = $"Mailgun invitation delivery timed out after {Math.Clamp(options.TimeoutSeconds, 1, 300)} seconds.";
            MailgunInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, exception);

            return new TenantInvitationDeliverySenderResult(
                TenantInvitationDeliveryOutcomes.SenderFailed,
                dispatched: false,
                reason: reason,
                dispatchedAtUtc: context.DispatchedAtUtc,
                metadata: BuildBaseMetadata(context, null, null, reason));
        }
        catch (Exception exception)
        {
            var reason = "Mailgun invitation delivery failed before the Messages API accepted the request.";
            MailgunInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, exception);

            var metadata = BuildBaseMetadata(context, null, null, reason);
            metadata["mailgunExceptionType"] = exception.GetType().Name;

            return new TenantInvitationDeliverySenderResult(
                TenantInvitationDeliveryOutcomes.SenderFailed,
                dispatched: false,
                reason: reason,
                dispatchedAtUtc: context.DispatchedAtUtc,
                metadata: metadata);
        }
    }

    private MailgunInvitationDeliveryMessage CreateMessage(TenantInvitationDeliveryContext context, string recipient)
    {
        var messageId = CreateMessageId(context);
        _ = MailgunInvitationDeliveryAddress.TryCreate(options.FromEmail, options.FromName, out var fromAddress);
        _ = MailgunInvitationDeliveryAddress.TryCreate(recipient, context.DisplayName, out var toAddress);

        return new MailgunInvitationDeliveryMessage(
            messageId,
            FormatAddress(fromAddress!),
            FormatAddress(toAddress!),
            toAddress!.Address,
            RenderTemplate(options.SubjectTemplate, context),
            RenderTemplate(options.TextBodyTemplate, context),
            string.IsNullOrWhiteSpace(options.HtmlBodyTemplate) ? null : RenderTemplate(options.HtmlBodyTemplate, context),
            BuildTags(),
            BuildVariables(context, messageId),
            BuildHeaders(context, messageId),
            options.EnableTestMode);
    }

    private string? ResolveRecipientEmail(TenantInvitationDeliveryContext context)
    {
        var metadataKey = GetRecipientMetadataKey();
        if (context.Metadata.TryGetValue(metadataKey, out var requestRecipient) &&
            IsValidAddress(requestRecipient))
        {
            return requestRecipient.Trim();
        }

        if (context.Invitation.Metadata.TryGetValue(metadataKey, out var invitationRecipient) &&
            IsValidAddress(invitationRecipient))
        {
            return invitationRecipient.Trim();
        }

        return string.Equals(context.InviteeKind, "email", StringComparison.OrdinalIgnoreCase) &&
            IsValidAddress(context.InviteeId)
            ? context.InviteeId.Trim()
            : null;
    }

    private static bool IsValidAddress(string? address)
    {
        return MailgunInvitationDeliveryAddress.TryCreate(address, null, out _);
    }

    private string GetRecipientMetadataKey()
    {
        return string.IsNullOrWhiteSpace(options.RecipientEmailMetadataKey)
            ? "email"
            : options.RecipientEmailMetadataKey.Trim();
    }

    private string[] BuildTags()
    {
        return options.Tags
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Where(static value => value.Length <= 128)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();
    }

    private Dictionary<string, string> BuildVariables(TenantInvitationDeliveryContext context, string messageId)
    {
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in options.Variables)
        {
            if (IsSafeVariable(pair.Key, pair.Value))
            {
                variables[pair.Key.Trim()] = pair.Value.Trim();
            }
        }

        if (options.IncludeContextVariables)
        {
            variables["cephalonTenantId"] = context.TenantId;
            variables["cephalonInvitationId"] = context.InvitationId;
            variables["cephalonDeliveryChannel"] = context.Channel;
            variables["cephalonSenderId"] = SenderId;
            variables["cephalonMessageId"] = messageId;

            if (!string.IsNullOrWhiteSpace(context.CorrelationId))
            {
                variables["cephalonCorrelationId"] = context.CorrelationId;
            }
        }

        return PruneVariables(variables);
    }

    private Dictionary<string, string> BuildHeaders(TenantInvitationDeliveryContext context, string messageId)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (options.IncludeContextHeaders)
        {
            headers["X-Cephalon-Tenant-Id"] = context.TenantId;
            headers["X-Cephalon-Invitation-Id"] = context.InvitationId;
            headers["X-Cephalon-Delivery-Channel"] = context.Channel;
            headers["X-Cephalon-Sender-Id"] = SenderId;
            headers["X-Cephalon-Message-Id"] = messageId;

            if (!string.IsNullOrWhiteSpace(context.CorrelationId))
            {
                headers["X-Cephalon-Correlation-Id"] = context.CorrelationId;
            }
        }

        foreach (var pair in options.Headers)
        {
            if (IsSafeHeader(pair.Key, pair.Value))
            {
                headers[pair.Key.Trim()] = pair.Value.Trim();
            }
        }

        return headers;
    }

    private static bool IsSafeVariable(string? name, string? value)
    {
        return !string.IsNullOrWhiteSpace(name) &&
            !string.IsNullOrWhiteSpace(value) &&
            IsSafeFormFieldName(name) &&
            IsSingleLine(value);
    }

    private static bool IsSafeHeader(string? name, string? value)
    {
        return !string.IsNullOrWhiteSpace(name) &&
            !string.IsNullOrWhiteSpace(value) &&
            !ReservedHeaders.Contains(name.Trim()) &&
            IsSafeHeaderName(name) &&
            IsSingleLine(value);
    }

    private static bool IsSafeFormFieldName(string value)
    {
        return value.Trim().All(static ch => char.IsLetterOrDigit(ch) || ch is '_' or '-' or '.');
    }

    private static bool IsSafeHeaderName(string value)
    {
        return value.Trim().All(static ch => char.IsLetterOrDigit(ch) || ch is '-');
    }

    private static bool IsSingleLine(string value)
    {
        return value.IndexOfAny(['\r', '\n']) < 0;
    }

    private static Dictionary<string, string> PruneVariables(Dictionary<string, string> variables)
    {
        const int byteLimit = 12_000;
        var pruned = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var totalBytes = 0;

        foreach (var pair in variables)
        {
            var key = pair.Key.Trim();
            var value = pair.Value.Trim();
            var pairBytes = Encoding.UTF8.GetByteCount(key) + Encoding.UTF8.GetByteCount(value);
            if (totalBytes + pairBytes > byteLimit)
            {
                continue;
            }

            pruned[key] = value;
            totalBytes += pairBytes;
        }

        return pruned;
    }

    private string CreateMessageId(TenantInvitationDeliveryContext context)
    {
        var material = string.Join(
            '\n',
            "cephalon-mailgun-invitation-delivery",
            "v1",
            options.GetDomainName().ToLowerInvariant(),
            context.TenantId.Trim().ToLowerInvariant(),
            context.InvitationId.Trim().ToLowerInvariant(),
            context.Channel.Trim().ToLowerInvariant(),
            SenderId.ToLowerInvariant());

        return "cephalon-invitation-" + ToBase64Url(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert
            .ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string FormatAddress(MailAddress address)
    {
        return string.IsNullOrWhiteSpace(address.DisplayName)
            ? address.Address
            : $"{address.DisplayName} <{address.Address}>";
    }

    private static string RenderTemplate(string? template, TenantInvitationDeliveryContext context)
    {
        var value = string.IsNullOrWhiteSpace(template) ? string.Empty : template;
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tenantId"] = context.TenantId,
            ["invitationId"] = context.InvitationId,
            ["inviteeId"] = context.InviteeId,
            ["inviteeKind"] = context.InviteeKind,
            ["displayName"] = context.DisplayName ?? context.InviteeId,
            ["roles"] = context.Roles.Count == 0 ? "none" : string.Join(", ", context.Roles),
            ["channel"] = context.Channel,
            ["senderId"] = context.RequestedSenderId ?? string.Empty,
            ["source"] = context.Source ?? string.Empty,
            ["actor"] = context.Actor ?? string.Empty,
            ["correlationId"] = context.CorrelationId ?? string.Empty,
            ["dispatchedAtUtc"] = context.DispatchedAtUtc.ToString("O", CultureInfo.InvariantCulture)
        };

        foreach (var replacement in replacements)
        {
            value = value.Replace("{" + replacement.Key + "}", replacement.Value, StringComparison.OrdinalIgnoreCase);
        }

        return value;
    }

    private Dictionary<string, string> BuildBaseMetadata(
        TenantInvitationDeliveryContext context,
        MailgunInvitationDeliveryMessage? message,
        IReadOnlyDictionary<string, string>? clientMetadata,
        string? reason)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["mailgunSenderId"] = SenderId,
            ["mailgunEndpointHost"] = options.TryGetBaseUrl()?.Host ?? string.Empty,
            ["mailgunDomain"] = options.GetDomainName(),
            ["mailgunTestMode"] = options.EnableTestMode ? "true" : "false",
            ["mailgunRecipientMetadataKey"] = GetRecipientMetadataKey(),
            ["mailgunAcceptedStatusCodes"] = string.Join(",", options.GetAcceptedStatusCodes())
        };

        if (message is not null)
        {
            metadata["mailgunCephalonMessageId"] = message.MessageId;
            metadata["mailgunFrom"] = message.From;
            metadata["mailgunRecipientEmail"] = message.ToEmail;
            metadata["mailgunTagCount"] = message.Tags.Count.ToString(CultureInfo.InvariantCulture);
            metadata["mailgunVariableCount"] = message.Variables.Count.ToString(CultureInfo.InvariantCulture);
            metadata["mailgunHeaderCount"] = message.Headers.Count.ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            metadata["mailgunReason"] = reason.Trim();
        }

        if (clientMetadata is not null)
        {
            foreach (var pair in clientMetadata.Where(static pair => !string.IsNullOrWhiteSpace(pair.Key)))
            {
                metadata[pair.Key.Trim()] = pair.Value;
            }
        }

        return metadata;
    }
}

internal static class MailgunInvitationDeliveryLogs
{
    private static readonly Action<ILogger, string, string, string, int, Exception?> AcceptedMessage =
        LoggerMessage.Define<string, string, string, int>(
            LogLevel.Information,
            new EventId(
                MailgunInvitationDeliveryDiagnosticsConventions.MailgunInvitationDeliveryAccepted.Id,
                MailgunInvitationDeliveryDiagnosticsConventions.MailgunInvitationDeliveryAccepted.Name),
            "Mailgun invitation delivery sender '{SenderId}' accepted invitation '{InvitationId}' for tenant '{TenantId}' with status code {StatusCode}.");

    private static readonly Action<ILogger, string, string, string, string, Exception?> FailedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(
                MailgunInvitationDeliveryDiagnosticsConventions.MailgunInvitationDeliveryFailed.Id,
                MailgunInvitationDeliveryDiagnosticsConventions.MailgunInvitationDeliveryFailed.Name),
            "Mailgun invitation delivery sender '{SenderId}' failed invitation '{InvitationId}' for tenant '{TenantId}'. Reason: {Reason}.");

    public static void Accepted(ILogger logger, string senderId, string tenantId, string invitationId, int statusCode) =>
        AcceptedMessage(logger, senderId, invitationId, tenantId, statusCode, null);

    public static void Failed(ILogger logger, string senderId, string tenantId, string invitationId, string reason, Exception? exception) =>
        FailedMessage(logger, senderId, invitationId, tenantId, reason, exception);
}
