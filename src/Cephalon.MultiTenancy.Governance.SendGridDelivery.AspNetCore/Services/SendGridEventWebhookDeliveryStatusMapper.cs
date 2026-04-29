using Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Configuration;
using Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Hosting;
using Cephalon.MultiTenancy.Governance.Services;
using System.Globalization;
using System.Text.Json;

namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Services;

internal sealed class SendGridEventWebhookDeliveryStatusMapper(SendGridInvitationDeliveryAspNetCoreOptions options)
{
    private const int MetadataValueLimit = 512;

    public SendGridEventWebhookDeliveryStatusMappingResult Map(JsonElement element, int index)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return SendGridEventWebhookDeliveryStatusMappingResult.Skipped(
                index,
                null,
                null,
                null,
                null,
                null,
                "invalid-event-object",
                "The SendGrid Event Webhook item was not a JSON object.");
        }

        var eventType = ReadString(element, "event");
        var sendGridEventId = ReadString(element, "sg_event_id");
        var sendGridMessageId = ReadString(element, "sg_message_id");
        var tenantId = ReadCephalonValue(element, "cephalonTenantId");
        var invitationId = ReadCephalonValue(element, "cephalonInvitationId");
        var channel = ReadCephalonValue(element, "cephalonDeliveryChannel");
        var senderId = ReadCephalonValue(element, "cephalonSenderId");
        var correlationId = ReadCephalonValue(element, "cephalonCorrelationId");
        var status = MapStatus(eventType);
        if (status is null)
        {
            return SendGridEventWebhookDeliveryStatusMappingResult.Skipped(
                index,
                sendGridEventId,
                sendGridMessageId,
                eventType,
                tenantId,
                invitationId,
                "unsupported-event-type",
                "The SendGrid event type is not mapped to a delivery status by current options.");
        }

        if (string.IsNullOrWhiteSpace(tenantId) ||
            string.IsNullOrWhiteSpace(invitationId))
        {
            return SendGridEventWebhookDeliveryStatusMappingResult.Skipped(
                index,
                sendGridEventId,
                sendGridMessageId,
                eventType,
                tenantId,
                invitationId,
                "missing-cephalon-context",
                "The SendGrid event did not include Cephalon tenant and invitation custom arguments.");
        }

        var metadata = BuildMetadata(element, sendGridEventId, sendGridMessageId, eventType, index);
        var providerMessageId = ResolveProviderMessageId(sendGridMessageId);
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

        return SendGridEventWebhookDeliveryStatusMappingResult.CreateTranslated(
            index,
            sendGridEventId,
            sendGridMessageId,
            eventType,
            tenantId,
            invitationId,
            status,
            request);
    }

    private string? MapStatus(string? eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            return TenantInvitationDeliveryStatuses.Unknown;
        }

        return eventType.Trim().ToLowerInvariant() switch
        {
            "processed" => TenantInvitationDeliveryStatuses.Accepted,
            "delivered" => TenantInvitationDeliveryStatuses.Delivered,
            "deferred" => TenantInvitationDeliveryStatuses.Deferred,
            "bounce" => TenantInvitationDeliveryStatuses.Bounced,
            "dropped" => TenantInvitationDeliveryStatuses.Suppressed,
            "spamreport" => TenantInvitationDeliveryStatuses.Suppressed,
            "unsubscribe" => TenantInvitationDeliveryStatuses.Suppressed,
            "group_unsubscribe" => TenantInvitationDeliveryStatuses.Suppressed,
            "open" or "click" when options.MapEngagementEventsAsDelivered => TenantInvitationDeliveryStatuses.Delivered,
            "group_resubscribe" => null,
            "open" or "click" => null,
            _ => TenantInvitationDeliveryStatuses.Unknown
        };
    }

    private Dictionary<string, string> BuildMetadata(
        JsonElement element,
        string? sendGridEventId,
        string? sendGridMessageId,
        string? eventType,
        int index)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sendGridEventWebhook"] = "true",
            ["sendGridEventWebhookTranslationOwnership"] = "cephalon-managed",
            ["sendGridEventWebhookSignatureVerification"] = "not-configured",
            ["sendGridEventWebhookSignatureVerificationOwnership"] = "application-managed",
            ["sendGridEventWebhookReplayProtectionOwnership"] = "application-managed",
            ["sendGridEventIndex"] = index.ToString(CultureInfo.InvariantCulture),
            ["sendGridProviderMessageIdSource"] = options.NormalizeProviderMessageIdFromSgMessageId
                ? "sg_message_id-prefix"
                : "sg_message_id"
        };

        AddIfPresent(metadata, "sendGridEventId", sendGridEventId);
        AddIfPresent(metadata, "sendGridMessageId", sendGridMessageId);
        AddIfPresent(metadata, "sendGridEventType", eventType);
        AddIfPresent(metadata, "sendGridStatus", ReadString(element, "status"));
        AddIfPresent(metadata, "sendGridBounceType", ReadString(element, "type"));
        AddIfPresent(metadata, "sendGridReason", ReadString(element, "reason"));

        if (!string.IsNullOrWhiteSpace(sendGridEventId))
        {
            metadata[TenantInvitationDeliveryMetadataKeys.DeliveryStatusObservationId] = "sendgrid:" + sendGridEventId.Trim();
        }

        var timestamp = ReadUnixTimestamp(element);
        if (timestamp is not null)
        {
            metadata["sendGridEventTimestamp"] = timestamp.Value.ToString(CultureInfo.InvariantCulture);
        }

        return metadata;
    }

    private string? ResolveProviderMessageId(string? sendGridMessageId)
    {
        var normalized = Normalize(sendGridMessageId);
        if (normalized is null ||
            !options.NormalizeProviderMessageIdFromSgMessageId)
        {
            return normalized;
        }

        var dotIndex = normalized.IndexOf('.', StringComparison.Ordinal);
        return dotIndex <= 0
            ? normalized
            : normalized[..dotIndex];
    }

    private static DateTimeOffset? ReadObservedAtUtc(JsonElement element)
    {
        var timestamp = ReadUnixTimestamp(element);
        if (timestamp is null)
        {
            return null;
        }

        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp.Value);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static long? ReadUnixTimestamp(JsonElement element)
    {
        if (!element.TryGetProperty("timestamp", out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetInt64(out var value) => value,
            JsonValueKind.String when long.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) => value,
            _ => null
        };
    }

    private static string? ResolveReason(JsonElement element, string? eventType)
    {
        var reason = ReadString(element, "reason");
        if (!string.IsNullOrWhiteSpace(reason))
        {
            return reason;
        }

        var status = ReadString(element, "status");
        if (!string.IsNullOrWhiteSpace(status))
        {
            return $"SendGrid reported {eventType ?? "unknown"} with status {status}.";
        }

        return string.IsNullOrWhiteSpace(eventType)
            ? "SendGrid Event Webhook reported an unknown event."
            : $"SendGrid Event Webhook reported {eventType.Trim()}.";
    }

    private static string? ReadCephalonValue(JsonElement element, string propertyName)
    {
        return ReadString(element, propertyName) ??
            ReadNestedString(element, "custom_args", propertyName) ??
            ReadNestedString(element, "unique_args", propertyName);
    }

    private static string? ReadNestedString(JsonElement element, string parentName, string propertyName)
    {
        return element.TryGetProperty(parentName, out var parent) && parent.ValueKind == JsonValueKind.Object
            ? ReadString(parent, propertyName)
            : null;
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

internal sealed record SendGridEventWebhookDeliveryStatusMappingResult(
    int Index,
    string? SendGridEventId,
    string? SendGridMessageId,
    string? SendGridEventType,
    string? TenantId,
    string? InvitationId,
    string? Status,
    string Outcome,
    string Reason,
    TenantInvitationDeliveryStatusReconciliationRequest? Request)
{
    public bool Translated => Request is not null;

    public static SendGridEventWebhookDeliveryStatusMappingResult CreateTranslated(
        int index,
        string? sendGridEventId,
        string? sendGridMessageId,
        string? sendGridEventType,
        string tenantId,
        string invitationId,
        string status,
        TenantInvitationDeliveryStatusReconciliationRequest request) =>
        new(
            index,
            sendGridEventId,
            sendGridMessageId,
            sendGridEventType,
            tenantId,
            invitationId,
            status,
            "translated",
            "The SendGrid event was translated into a Cephalon delivery status reconciliation request.",
            request);

    public static SendGridEventWebhookDeliveryStatusMappingResult Skipped(
        int index,
        string? sendGridEventId,
        string? sendGridMessageId,
        string? sendGridEventType,
        string? tenantId,
        string? invitationId,
        string outcome,
        string reason) =>
        new(index, sendGridEventId, sendGridMessageId, sendGridEventType, tenantId, invitationId, null, outcome, reason, null);

    public SendGridInvitationDeliveryStatusCallbackEventResult ToSkippedEventResult() =>
        new(
            Index,
            SendGridEventId,
            SendGridMessageId,
            SendGridEventType,
            TenantId,
            InvitationId,
            Status,
            Outcome,
            translated: false,
            reconciled: false,
            Reason);

    public SendGridInvitationDeliveryStatusCallbackEventResult ToReconciledEventResult(
        TenantInvitationDeliveryStatusReconciliationResult reconciliation) =>
        new(
            Index,
            SendGridEventId,
            SendGridMessageId,
            SendGridEventType,
            TenantId,
            InvitationId,
            Status,
            reconciliation.Outcome,
            translated: true,
            reconciled: reconciliation.Reconciled,
            reconciliation.Reason);
}
