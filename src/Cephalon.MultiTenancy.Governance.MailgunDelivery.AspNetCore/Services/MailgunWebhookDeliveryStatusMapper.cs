using Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Configuration;
using Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Hosting;
using Cephalon.MultiTenancy.Governance.Services;
using System.Globalization;
using System.Text.Json;

namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Services;

internal sealed class MailgunWebhookDeliveryStatusMapper(MailgunInvitationDeliveryAspNetCoreOptions options)
{
    private const int MetadataValueLimit = 512;

    public MailgunWebhookDeliveryStatusMappingResult Map(JsonElement element, int index)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return MailgunWebhookDeliveryStatusMappingResult.Skipped(
                index,
                null,
                null,
                null,
                null,
                null,
                "invalid-event-object",
                "The Mailgun webhook item was not a JSON object.");
        }

        var eventType = ReadString(element, "event");
        var mailgunEventId = ReadString(element, "id");
        var rawMessageId = ReadNestedString(element, "message", "headers", "message-id");
        var providerMessageId = ResolveProviderMessageId(rawMessageId);
        var tenantId = ReadCephalonValue(element, "cephalonTenantId");
        var invitationId = ReadCephalonValue(element, "cephalonInvitationId");
        var channel = ReadCephalonValue(element, "cephalonDeliveryChannel");
        var senderId = ReadCephalonValue(element, "cephalonSenderId");
        var correlationId = ReadCephalonValue(element, "cephalonCorrelationId");
        var status = MapStatus(element, eventType);
        if (status is null)
        {
            return MailgunWebhookDeliveryStatusMappingResult.Skipped(
                index,
                mailgunEventId,
                rawMessageId,
                eventType,
                tenantId,
                invitationId,
                "unsupported-event-type",
                "The Mailgun event type is not mapped to a delivery status by current options.");
        }

        if (string.IsNullOrWhiteSpace(tenantId) ||
            string.IsNullOrWhiteSpace(invitationId))
        {
            return MailgunWebhookDeliveryStatusMappingResult.Skipped(
                index,
                mailgunEventId,
                rawMessageId,
                eventType,
                tenantId,
                invitationId,
                "missing-cephalon-context",
                "The Mailgun event did not include Cephalon tenant and invitation user variables.");
        }

        var metadata = BuildMetadata(element, mailgunEventId, rawMessageId, providerMessageId, eventType, index);
        var observedAtUtc = ReadObservedAtUtc(element);
        var request = new TenantInvitationDeliveryStatusReconciliationRequest(
            tenantId: tenantId!,
            invitationId: invitationId!,
            status: status,
            providerMessageId: providerMessageId,
            senderId: senderId,
            channel: channel,
            reason: ResolveReason(element, eventType),
            observedAtUtc: observedAtUtc,
            source: options.GetSource(),
            actor: options.GetActor(),
            correlationId: correlationId,
            recordStatus: options.RecordStatus,
            requireProviderMessageMatch: options.RequireProviderMessageMatch,
            metadata: metadata);

        return MailgunWebhookDeliveryStatusMappingResult.CreateTranslated(
            index,
            mailgunEventId,
            rawMessageId,
            eventType,
            tenantId!,
            invitationId!,
            status,
            request);
    }

    private string? MapStatus(JsonElement element, string? eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            return TenantInvitationDeliveryStatuses.Unknown;
        }

        return eventType.Trim().ToLowerInvariant() switch
        {
            "accepted" => TenantInvitationDeliveryStatuses.Accepted,
            "delivered" => TenantInvitationDeliveryStatuses.Delivered,
            "failed" when string.Equals(ReadString(element, "severity"), "temporary", StringComparison.OrdinalIgnoreCase) =>
                TenantInvitationDeliveryStatuses.Deferred,
            "failed" when string.Equals(ReadString(element, "severity"), "permanent", StringComparison.OrdinalIgnoreCase) =>
                TenantInvitationDeliveryStatuses.Bounced,
            "failed" => TenantInvitationDeliveryStatuses.Failed,
            "complained" or "unsubscribed" => TenantInvitationDeliveryStatuses.Suppressed,
            "opened" or "clicked" when options.MapEngagementEventsAsDelivered => TenantInvitationDeliveryStatuses.Delivered,
            "opened" or "clicked" => null,
            _ => TenantInvitationDeliveryStatuses.Unknown
        };
    }

    private Dictionary<string, string> BuildMetadata(
        JsonElement element,
        string? mailgunEventId,
        string? rawMessageId,
        string? providerMessageId,
        string? eventType,
        int index)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["mailgunWebhook"] = "true",
            ["mailgunWebhookTranslationOwnership"] = "cephalon-managed",
            ["mailgunWebhookSignatureVerification"] = "not-configured",
            ["mailgunWebhookSignatureVerificationOwnership"] = "not-configured",
            ["mailgunWebhookReplayProtection"] = "not-configured",
            ["mailgunWebhookReplayProtectionOwnership"] = "not-configured",
            ["mailgunEventIndex"] = index.ToString(CultureInfo.InvariantCulture),
            ["mailgunProviderMessageIdSource"] = "message.headers.message-id",
            ["mailgunProviderMessageIdNormalization"] = options.NormalizeProviderMessageIdWithAngleBrackets ? "angle-brackets" : "none"
        };

        AddIfPresent(metadata, "mailgunEventId", mailgunEventId);
        AddIfPresent(metadata, "mailgunMessageId", rawMessageId);
        AddIfPresent(metadata, "mailgunProviderMessageId", providerMessageId);
        AddIfPresent(metadata, "mailgunEventType", eventType);
        AddIfPresent(metadata, "mailgunSeverity", ReadString(element, "severity"));
        AddIfPresent(metadata, "mailgunReason", ReadString(element, "reason"));
        AddIfPresent(metadata, "mailgunLogLevel", ReadString(element, "log-level"));
        AddIfPresent(metadata, "mailgunDomain", ReadNestedString(element, "domain", "name"));
        AddIfPresent(metadata, "mailgunDeliveryStatusCode", ReadNestedString(element, "delivery-status", "code"));
        AddIfPresent(metadata, "mailgunDeliveryStatusMessage", ReadNestedString(element, "delivery-status", "message"));
        AddIfPresent(metadata, "mailgunDeliveryStatusDescription", ReadNestedString(element, "delivery-status", "description"));
        AddIfPresent(metadata, "mailgunDeliveryStatusEnhancedCode", ReadNestedString(element, "delivery-status", "enhanced-code"));
        AddIfPresent(metadata, "mailgunDeliveryStatusBounceType", ReadNestedString(element, "delivery-status", "bounce-type"));

        if (!string.IsNullOrWhiteSpace(mailgunEventId))
        {
            metadata[TenantInvitationDeliveryMetadataKeys.DeliveryStatusObservationId] = "mailgun:" + mailgunEventId.Trim();
        }

        var timestamp = ReadUnixTimestampSeconds(element);
        if (timestamp is not null)
        {
            metadata["mailgunEventTimestamp"] = timestamp.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (ReadNestedString(element, "flags", "is-test-mode") is { } testMode)
        {
            metadata["mailgunTestMode"] = testMode.ToLowerInvariant();
        }

        return metadata;
    }

    private string? ResolveProviderMessageId(string? rawMessageId)
    {
        var normalized = Normalize(rawMessageId);
        if (normalized is null ||
            !options.NormalizeProviderMessageIdWithAngleBrackets ||
            (normalized.StartsWith('<') && normalized.EndsWith('>')))
        {
            return normalized;
        }

        return "<" + normalized.Trim('<', '>') + ">";
    }

    private static DateTimeOffset? ReadObservedAtUtc(JsonElement element)
    {
        var timestamp = ReadUnixTimestampSeconds(element);
        if (timestamp is null)
        {
            return null;
        }

        try
        {
            var milliseconds = checked((long)Math.Round(timestamp.Value * 1000, MidpointRounding.AwayFromZero));
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    private static double? ReadUnixTimestampSeconds(JsonElement element)
    {
        if (!element.TryGetProperty("timestamp", out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDouble(out var value) => value,
            JsonValueKind.String when double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) => value,
            _ => null
        };
    }

    private static string? ResolveReason(JsonElement element, string? eventType)
    {
        var reason = ReadString(element, "reason");
        var severity = ReadString(element, "severity");
        var deliveryMessage = ReadNestedString(element, "delivery-status", "message");
        var deliveryCode = ReadNestedString(element, "delivery-status", "code");
        if (!string.IsNullOrWhiteSpace(reason) && !string.IsNullOrWhiteSpace(severity))
        {
            return $"Mailgun reported {eventType ?? "unknown"} with {severity} severity: {reason}.";
        }

        if (!string.IsNullOrWhiteSpace(deliveryCode) && !string.IsNullOrWhiteSpace(deliveryMessage))
        {
            return $"Mailgun reported {eventType ?? "unknown"} with delivery status {deliveryCode}: {deliveryMessage}.";
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            return reason;
        }

        return string.IsNullOrWhiteSpace(eventType)
            ? "Mailgun webhook reported an unknown event."
            : $"Mailgun webhook reported {eventType.Trim()}.";
    }

    private static string? ReadCephalonValue(JsonElement element, string propertyName)
    {
        return ReadString(element, propertyName) ??
            ReadUserVariable(element, propertyName);
    }

    private static string? ReadUserVariable(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty("user-variables", out var userVariables))
        {
            return null;
        }

        if (userVariables.ValueKind == JsonValueKind.Object)
        {
            return ReadString(userVariables, propertyName);
        }

        if (userVariables.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in userVariables.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = ReadString(item, "name") ?? ReadString(item, "key");
            if (string.Equals(name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return ReadString(item, "value");
            }
        }

        return null;
    }

    private static string? ReadNestedString(JsonElement element, string parentName, string propertyName)
    {
        return element.TryGetProperty(parentName, out var parent) && parent.ValueKind == JsonValueKind.Object
            ? ReadString(parent, propertyName)
            : null;
    }

    private static string? ReadNestedString(JsonElement element, string parentName, string childName, string propertyName)
    {
        if (!element.TryGetProperty(parentName, out var parent) ||
            parent.ValueKind != JsonValueKind.Object ||
            !parent.TryGetProperty(childName, out var child) ||
            child.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return ReadString(child, propertyName);
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

    private static void AddIfPresent(Dictionary<string, string> metadata, string key, string? value)
    {
        var normalized = Normalize(value);
        if (normalized is not null)
        {
            metadata[key] = Truncate(normalized, MetadataValueLimit);
        }
    }

    private static string Truncate(string value, int limit)
    {
        return value.Length <= limit
            ? value
            : value[..limit];
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}

internal sealed record MailgunWebhookDeliveryStatusMappingResult(
    int Index,
    string? MailgunEventId,
    string? MailgunMessageId,
    string? MailgunEventType,
    string? TenantId,
    string? InvitationId,
    string? Status,
    string Outcome,
    string Reason,
    TenantInvitationDeliveryStatusReconciliationRequest? Request)
{
    public bool Translated => Request is not null;

    public static MailgunWebhookDeliveryStatusMappingResult CreateTranslated(
        int index,
        string? mailgunEventId,
        string? mailgunMessageId,
        string? mailgunEventType,
        string tenantId,
        string invitationId,
        string status,
        TenantInvitationDeliveryStatusReconciliationRequest request) =>
        new(
            index,
            mailgunEventId,
            mailgunMessageId,
            mailgunEventType,
            tenantId,
            invitationId,
            status,
            "translated",
            "The Mailgun event was translated into a Cephalon delivery status reconciliation request.",
            request);

    public static MailgunWebhookDeliveryStatusMappingResult Skipped(
        int index,
        string? mailgunEventId,
        string? mailgunMessageId,
        string? mailgunEventType,
        string? tenantId,
        string? invitationId,
        string outcome,
        string reason) =>
        new(index, mailgunEventId, mailgunMessageId, mailgunEventType, tenantId, invitationId, null, outcome, reason, null);

    public MailgunInvitationDeliveryStatusCallbackEventResult ToSkippedEventResult() =>
        new(
            Index,
            MailgunEventId,
            MailgunMessageId,
            MailgunEventType,
            TenantId,
            InvitationId,
            Status,
            Outcome,
            translated: false,
            reconciled: false,
            Reason);

    public MailgunInvitationDeliveryStatusCallbackEventResult ToReconciledEventResult(
        TenantInvitationDeliveryStatusReconciliationResult reconciliation) =>
        new(
            Index,
            MailgunEventId,
            MailgunMessageId,
            MailgunEventType,
            TenantId,
            InvitationId,
            Status,
            reconciliation.Outcome,
            translated: true,
            reconciled: reconciliation.Reconciled,
            reconciliation.Reason);
}
