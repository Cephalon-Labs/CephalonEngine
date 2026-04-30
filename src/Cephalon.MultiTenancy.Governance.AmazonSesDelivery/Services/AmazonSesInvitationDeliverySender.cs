using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Configuration;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Services;

internal sealed class AmazonSesInvitationDeliverySender(
    AmazonSesInvitationDeliveryOptions options,
    IAmazonSesInvitationDeliveryClient client,
    ILogger<AmazonSesInvitationDeliverySender> logger) : ITenantInvitationDeliverySender
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
            var reason = $"Amazon SES invitation delivery sender '{SenderId}' does not support channel '{context.Channel}'.";
            AmazonSesInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, null);

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
            var reason = $"Amazon SES invitation delivery could not resolve a recipient email address from metadata key '{GetRecipientMetadataKey()}'.";
            AmazonSesInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, null);

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
                metadata["amazonSesStatusCode"] = result.StatusCode.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (result.Accepted)
            {
                AmazonSesInvitationDeliveryLogs.Accepted(logger, SenderId, context.TenantId, context.InvitationId, result.StatusCode ?? 0);

                return new TenantInvitationDeliverySenderResult(
                    TenantInvitationDeliveryOutcomes.Dispatched,
                    dispatched: true,
                    providerMessageId: result.ProviderMessageId,
                    reason: string.IsNullOrWhiteSpace(result.Reason)
                        ? "Amazon SES accepted the invitation delivery request."
                        : result.Reason,
                    dispatchedAtUtc: context.DispatchedAtUtc,
                    metadata: metadata);
            }

            var reason = string.IsNullOrWhiteSpace(result.Reason)
                ? "Amazon SES did not accept the invitation delivery request."
                : result.Reason;
            AmazonSesInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, null);

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
            var reason = $"Amazon SES invitation delivery timed out after {Math.Clamp(options.TimeoutSeconds, 1, 300)} seconds.";
            AmazonSesInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, exception);

            return new TenantInvitationDeliverySenderResult(
                TenantInvitationDeliveryOutcomes.SenderFailed,
                dispatched: false,
                reason: reason,
                dispatchedAtUtc: context.DispatchedAtUtc,
                metadata: BuildBaseMetadata(context, null, null, reason));
        }
        catch (Exception exception)
        {
            var reason = "Amazon SES invitation delivery failed before SendEmail accepted the request.";
            AmazonSesInvitationDeliveryLogs.Failed(logger, SenderId, context.TenantId, context.InvitationId, reason, exception);

            var metadata = BuildBaseMetadata(context, null, null, reason);
            metadata["amazonSesExceptionType"] = exception.GetType().Name;

            return new TenantInvitationDeliverySenderResult(
                TenantInvitationDeliveryOutcomes.SenderFailed,
                dispatched: false,
                reason: reason,
                dispatchedAtUtc: context.DispatchedAtUtc,
                metadata: metadata);
        }
    }

    private AmazonSesInvitationDeliveryMessage CreateMessage(TenantInvitationDeliveryContext context, string recipient)
    {
        var messageId = CreateMessageId(context);
        _ = AmazonSesInvitationDeliveryAddress.TryCreate(options.FromEmail, options.FromName, out var fromAddress);

        return new AmazonSesInvitationDeliveryMessage(
            messageId,
            FormatAddress(fromAddress!),
            recipient,
            RenderTemplate(options.SubjectTemplate, context),
            RenderTemplate(options.TextBodyTemplate, context),
            string.IsNullOrWhiteSpace(options.HtmlBodyTemplate) ? null : RenderTemplate(options.HtmlBodyTemplate, context),
            BuildReplyToAddresses(),
            BuildTags(context, messageId),
            options.GetConfigurationSetName());
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
        return AmazonSesInvitationDeliveryAddress.TryCreate(address, null, out _);
    }

    private string GetRecipientMetadataKey()
    {
        return string.IsNullOrWhiteSpace(options.RecipientEmailMetadataKey)
            ? "email"
            : options.RecipientEmailMetadataKey.Trim();
    }

    private string[] BuildReplyToAddresses()
    {
        return options.ReplyToAddresses
            .Where(static value => AmazonSesInvitationDeliveryAddress.TryCreate(value, null, out _))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();
    }

    private Dictionary<string, string> BuildTags(TenantInvitationDeliveryContext context, string messageId)
    {
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in options.Tags)
        {
            AddSafeTag(tags, pair.Key, pair.Value);
        }

        if (options.IncludeContextTags)
        {
            AddSafeTag(tags, "cephalon-tenant-id", context.TenantId);
            AddSafeTag(tags, "cephalon-invitation-id", context.InvitationId);
            AddSafeTag(tags, "cephalon-delivery-channel", context.Channel);
            AddSafeTag(tags, "cephalon-sender-id", SenderId);
            AddSafeTag(tags, "cephalon-message-id", messageId);

            if (!string.IsNullOrWhiteSpace(context.CorrelationId))
            {
                AddSafeTag(tags, "cephalon-correlation-id", context.CorrelationId);
            }
        }

        return tags
            .Take(50)
            .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static void AddSafeTag(Dictionary<string, string> tags, string? name, string? value)
    {
        var normalizedName = NormalizeSesTagValue(name);
        var normalizedValue = NormalizeSesTagValue(value);

        if (!string.IsNullOrWhiteSpace(normalizedName) && !string.IsNullOrWhiteSpace(normalizedValue))
        {
            tags[normalizedName] = normalizedValue;
        }
    }

    private static string NormalizeSesTagValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(capacity: Math.Min(value.Length, 256));
        foreach (var ch in value.Trim())
        {
            if (builder.Length >= 256)
            {
                break;
            }

            builder.Append(char.IsAsciiLetterOrDigit(ch) || ch is '_' or '-' ? ch : '-');
        }

        return builder.ToString().Trim('-');
    }

    private string CreateMessageId(TenantInvitationDeliveryContext context)
    {
        var material = string.Join(
            '\n',
            "cephalon-amazon-ses-invitation-delivery",
            "v1",
            options.GetRegionSystemName() ?? string.Empty,
            options.GetConfigurationSetName() ?? string.Empty,
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

    private static string FormatAddress(AmazonSesInvitationDeliveryAddress address)
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
        AmazonSesInvitationDeliveryMessage? message,
        IReadOnlyDictionary<string, string>? clientMetadata,
        string? reason)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["amazonSesSenderId"] = SenderId,
            ["amazonSesRegionSystemName"] = options.GetRegionSystemName() ?? string.Empty,
            ["amazonSesConfigurationSetName"] = options.GetConfigurationSetName() ?? string.Empty,
            ["amazonSesRecipientMetadataKey"] = GetRecipientMetadataKey(),
            ["amazonSesAcceptedStatusCodes"] = string.Join(",", options.GetAcceptedStatusCodes()),
            ["amazonSesExternalDeliveryOwnership"] = "provider-managed"
        };

        if (message is not null)
        {
            metadata["amazonSesCephalonMessageId"] = message.MessageId;
            metadata["amazonSesFrom"] = message.From;
            metadata["amazonSesRecipientEmail"] = message.ToEmail;
            metadata["amazonSesBodyContentType"] = message.HasHtmlBody ? "HTML" : "Text";
            metadata["amazonSesReplyToAddressCount"] = message.ReplyToAddresses.Count.ToString(CultureInfo.InvariantCulture);
            metadata["amazonSesTagCount"] = message.Tags.Count.ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            metadata["amazonSesReason"] = reason.Trim();
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

internal static class AmazonSesInvitationDeliveryLogs
{
    private static readonly Action<ILogger, string, string, string, int, Exception?> AcceptedMessage =
        LoggerMessage.Define<string, string, string, int>(
            LogLevel.Information,
            new EventId(
                AmazonSesInvitationDeliveryDiagnosticsConventions.AmazonSesInvitationDeliveryAccepted.Id,
                AmazonSesInvitationDeliveryDiagnosticsConventions.AmazonSesInvitationDeliveryAccepted.Name),
            "Amazon SES invitation delivery sender '{SenderId}' accepted invitation '{InvitationId}' for tenant '{TenantId}' with status code {StatusCode}.");

    private static readonly Action<ILogger, string, string, string, string, Exception?> FailedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(
                AmazonSesInvitationDeliveryDiagnosticsConventions.AmazonSesInvitationDeliveryFailed.Id,
                AmazonSesInvitationDeliveryDiagnosticsConventions.AmazonSesInvitationDeliveryFailed.Name),
            "Amazon SES invitation delivery sender '{SenderId}' failed invitation '{InvitationId}' for tenant '{TenantId}'. Reason: {Reason}.");

    public static void Accepted(ILogger logger, string senderId, string tenantId, string invitationId, int statusCode) =>
        AcceptedMessage(logger, senderId, invitationId, tenantId, statusCode, null);

    public static void Failed(ILogger logger, string senderId, string tenantId, string invitationId, string reason, Exception? exception) =>
        FailedMessage(logger, senderId, invitationId, tenantId, reason, exception);
}
