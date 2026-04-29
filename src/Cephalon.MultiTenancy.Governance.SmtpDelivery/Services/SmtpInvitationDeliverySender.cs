using Cephalon.MultiTenancy.Governance.Services;
using Cephalon.MultiTenancy.Governance.SmtpDelivery.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cephalon.MultiTenancy.Governance.SmtpDelivery.Services;

internal sealed class SmtpInvitationDeliverySender(
    SmtpInvitationDeliveryOptions options,
    ISmtpInvitationDeliveryClient client,
    ILogger<SmtpInvitationDeliverySender> logger) : ITenantInvitationDeliverySender
{
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
            var reason = $"SMTP invitation delivery sender '{SenderId}' does not support channel '{context.Channel}'.";
            SmtpInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, null);

            return new TenantInvitationDeliverySenderResult(
                TenantInvitationDeliveryOutcomes.Suppressed,
                dispatched: false,
                reason: reason,
                dispatchedAtUtc: context.DispatchedAtUtc,
                metadata: BuildBaseMetadata(context, null, null, reason));
        }

        var recipient = ResolveRecipientAddress(context);
        if (recipient is null)
        {
            var reason = $"SMTP invitation delivery could not resolve a recipient address from metadata key '{GetRecipientMetadataKey()}'.";
            SmtpInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, null);

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
            var providerMessageId = string.IsNullOrWhiteSpace(result.ProviderMessageId)
                ? message.MessageId
                : result.ProviderMessageId;

            if (result.Accepted)
            {
                SmtpInvitationDeliveryLogs.Accepted(logger, SenderId, context.TenantId, context.InvitationId, options.Host!.Trim());

                return new TenantInvitationDeliverySenderResult(
                    TenantInvitationDeliveryOutcomes.Dispatched,
                    dispatched: true,
                    providerMessageId: providerMessageId,
                    reason: string.IsNullOrWhiteSpace(result.Reason)
                        ? "SMTP relay accepted the invitation delivery message."
                        : result.Reason,
                    dispatchedAtUtc: context.DispatchedAtUtc,
                    metadata: metadata);
            }

            var reason = string.IsNullOrWhiteSpace(result.Reason)
                ? "SMTP relay did not accept the invitation delivery message."
                : result.Reason;
            SmtpInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, null);

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
            var reason = $"SMTP invitation delivery timed out after {Math.Clamp(options.TimeoutSeconds, 1, 300)} seconds.";
            SmtpInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, exception);

            return new TenantInvitationDeliverySenderResult(
                TenantInvitationDeliveryOutcomes.SenderFailed,
                dispatched: false,
                reason: reason,
                dispatchedAtUtc: context.DispatchedAtUtc,
                metadata: BuildBaseMetadata(context, null, null, reason));
        }
        catch (Exception exception)
        {
            var reason = "SMTP invitation delivery failed before the relay accepted the message.";
            SmtpInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, exception);

            var metadata = BuildBaseMetadata(context, null, null, reason);
            metadata["smtpExceptionType"] = exception.GetType().Name;

            return new TenantInvitationDeliverySenderResult(
                TenantInvitationDeliveryOutcomes.SenderFailed,
                dispatched: false,
                reason: reason,
                dispatchedAtUtc: context.DispatchedAtUtc,
                metadata: metadata);
        }
    }

    private SmtpInvitationDeliveryMessage CreateMessage(TenantInvitationDeliveryContext context, string recipient)
    {
        var messageId = CreateMessageId(context);
        var headers = BuildHeaders(context, messageId);

        return new SmtpInvitationDeliveryMessage(
            messageId,
            options.FromAddress!,
            options.FromDisplayName,
            recipient,
            context.DisplayName,
            RenderTemplate(options.SubjectTemplate, context),
            RenderTemplate(options.TextBodyTemplate, context),
            string.IsNullOrWhiteSpace(options.HtmlBodyTemplate) ? null : RenderTemplate(options.HtmlBodyTemplate, context),
            headers);
    }

    private string? ResolveRecipientAddress(TenantInvitationDeliveryContext context)
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
        return SmtpInvitationDeliveryAddress.TryCreate(address, null, out _);
    }

    private string GetRecipientMetadataKey()
    {
        return string.IsNullOrWhiteSpace(options.RecipientAddressMetadataKey)
            ? "email"
            : options.RecipientAddressMetadataKey.Trim();
    }

    private Dictionary<string, string> BuildHeaders(TenantInvitationDeliveryContext context, string messageId)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Message-Id"] = messageId
        };

        if (options.IncludeContextHeaders)
        {
            headers["X-Cephalon-Tenant-Id"] = context.TenantId;
            headers["X-Cephalon-Invitation-Id"] = context.InvitationId;
            headers["X-Cephalon-Delivery-Channel"] = context.Channel;
            headers["X-Cephalon-Sender-Id"] = SenderId;

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

    private static bool IsSafeHeader(string? name, string? value)
    {
        return !string.IsNullOrWhiteSpace(name) &&
            !string.IsNullOrWhiteSpace(value) &&
            IsSingleLine(name) &&
            IsSingleLine(value);
    }

    private static bool IsSingleLine(string value)
    {
        return value.IndexOfAny(['\r', '\n']) < 0;
    }

    private string CreateMessageId(TenantInvitationDeliveryContext context)
    {
        var material = string.Join(
            '\n',
            "cephalon-smtp-invitation-delivery",
            "v1",
            context.TenantId.Trim().ToLowerInvariant(),
            context.InvitationId.Trim().ToLowerInvariant(),
            context.Channel.Trim().ToLowerInvariant(),
            SenderId.ToLowerInvariant());
        var hash = Convert
            .ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(material)))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return $"<cephalon-invitation-{hash}@{options.GetMessageIdDomain()}>";
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
        SmtpInvitationDeliveryMessage? message,
        IReadOnlyDictionary<string, string>? clientMetadata,
        string? reason)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["smtpSenderId"] = SenderId,
            ["smtpRelayHost"] = options.Host?.Trim() ?? string.Empty,
            ["smtpRelayPort"] = options.GetPort().ToString(CultureInfo.InvariantCulture),
            ["smtpUseSsl"] = options.UseSsl ? "true" : "false",
            ["smtpRecipientMetadataKey"] = GetRecipientMetadataKey()
        };

        if (message is not null)
        {
            metadata["smtpMessageId"] = message.MessageId;
            metadata["smtpFromAddress"] = message.FromAddress;
            metadata["smtpRecipientAddress"] = message.ToAddress;
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            metadata["smtpReason"] = reason.Trim();
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

internal static class SmtpInvitationDeliveryLogs
{
    private static readonly Action<ILogger, string, string, string, string, Exception?> AcceptedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Information,
            new EventId(
                SmtpInvitationDeliveryDiagnosticsConventions.SmtpInvitationDeliveryAccepted.Id,
                SmtpInvitationDeliveryDiagnosticsConventions.SmtpInvitationDeliveryAccepted.Name),
            "SMTP invitation delivery sender '{SenderId}' accepted invitation '{InvitationId}' for tenant '{TenantId}' using relay '{RelayHost}'.");

    private static readonly Action<ILogger, string, string, string, string, Exception?> FailedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(
                SmtpInvitationDeliveryDiagnosticsConventions.SmtpInvitationDeliveryFailed.Id,
                SmtpInvitationDeliveryDiagnosticsConventions.SmtpInvitationDeliveryFailed.Name),
            "SMTP invitation delivery sender '{SenderId}' failed invitation '{InvitationId}' for tenant '{TenantId}'. Reason: {Reason}.");

    public static void Accepted(ILogger logger, string senderId, string tenantId, string invitationId, string relayHost) =>
        AcceptedMessage(logger, senderId, invitationId, tenantId, relayHost, null);

    public static void Failed(ILogger logger, string senderId, string tenantId, string invitationId, string reason, Exception? exception) =>
        FailedMessage(logger, senderId, invitationId, tenantId, reason, exception);
}
