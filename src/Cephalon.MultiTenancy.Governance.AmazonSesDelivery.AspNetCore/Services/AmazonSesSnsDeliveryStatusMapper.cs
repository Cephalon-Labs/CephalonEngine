using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Configuration;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Hosting;
using Cephalon.MultiTenancy.Governance.Services;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Services;

internal sealed class AmazonSesSnsDeliveryStatusMapper(AmazonSesInvitationDeliveryAspNetCoreOptions options)
{
    private const int MetadataValueLimit = 512;

    public IReadOnlyList<AmazonSesSnsDeliveryStatusMappingResult>? MapPayload(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            if (!options.AcceptRawSesEventPayloads)
            {
                return null;
            }

            var results = new List<AmazonSesSnsDeliveryStatusMappingResult>();
            var index = 0;
            foreach (var item in root.EnumerateArray())
            {
                results.Add(MapSesEvent(item, index, null, "RawSesEvent", null, null, null));
                index++;
            }

            return results;
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var snsType = ReadString(root, "Type");
        if (snsType is null)
        {
            return options.AcceptRawSesEventPayloads
                ? [MapSesEvent(root, 0, null, "RawSesEvent", null, null, null)]
                : null;
        }

        var snsMessageId = ReadString(root, "MessageId");
        var snsTopicArn = ReadString(root, "TopicArn");
        var snsTimestamp = ReadDateTimeOffset(root, "Timestamp");

        if (!string.Equals(snsType, "Notification", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                AmazonSesSnsDeliveryStatusMappingResult.Skipped(
                    0,
                    snsMessageId,
                    snsType,
                    null,
                    null,
                    null,
                    null,
                    "sns-message-type-not-translated",
                    "The SNS message type is not a notification carrying an Amazon SES event payload.")
            ];
        }

        var message = ReadString(root, "Message");
        if (message is null)
        {
            return
            [
                AmazonSesSnsDeliveryStatusMappingResult.Skipped(
                    0,
                    snsMessageId,
                    snsType,
                    null,
                    null,
                    null,
                    null,
                    "sns-message-missing",
                    "The SNS notification did not include an Amazon SES event Message value.")
            ];
        }

        try
        {
            using var messageDocument = JsonDocument.Parse(message);
            if (messageDocument.RootElement.ValueKind == JsonValueKind.Array)
            {
                var results = new List<AmazonSesSnsDeliveryStatusMappingResult>();
                var index = 0;
                foreach (var item in messageDocument.RootElement.EnumerateArray())
                {
                    results.Add(MapSesEvent(item, index, snsMessageId, snsType, snsTopicArn, snsTimestamp, ReadString(root, "Subject")));
                    index++;
                }

                return results;
            }

            return [MapSesEvent(messageDocument.RootElement, 0, snsMessageId, snsType, snsTopicArn, snsTimestamp, ReadString(root, "Subject"))];
        }
        catch (JsonException)
        {
            return
            [
                AmazonSesSnsDeliveryStatusMappingResult.Skipped(
                    0,
                    snsMessageId,
                    snsType,
                    null,
                    null,
                    null,
                    null,
                    "ses-message-json-invalid",
                    "The SNS Message field was not a valid Amazon SES event JSON payload.")
            ];
        }
    }

    private AmazonSesSnsDeliveryStatusMappingResult MapSesEvent(
        JsonElement element,
        int index,
        string? snsMessageId,
        string? snsMessageType,
        string? snsTopicArn,
        DateTimeOffset? snsTimestamp,
        string? snsSubject)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return AmazonSesSnsDeliveryStatusMappingResult.Skipped(
                index,
                snsMessageId,
                snsMessageType,
                null,
                null,
                null,
                null,
                "invalid-event-object",
                "The Amazon SES event item was not a JSON object.");
        }

        var eventType = ReadString(element, "eventType") ?? ReadString(element, "notificationType");
        var mail = element.TryGetProperty("mail", out var mailElement) && mailElement.ValueKind == JsonValueKind.Object
            ? mailElement
            : default;
        var amazonSesMessageId = mail.ValueKind == JsonValueKind.Object
            ? ReadString(mail, "messageId")
            : null;
        var tenantId = ReadCephalonTag(mail, "cephalon-tenant-id", "cephalonTenantId");
        var invitationId = ReadCephalonTag(mail, "cephalon-invitation-id", "cephalonInvitationId");
        var channel = ReadCephalonTag(mail, "cephalon-delivery-channel", "cephalonDeliveryChannel");
        var senderId = ReadCephalonTag(mail, "cephalon-sender-id", "cephalonSenderId");
        var correlationId = ReadCephalonTag(mail, "cephalon-correlation-id", "cephalonCorrelationId");
        var status = MapStatus(element, eventType);
        if (status is null)
        {
            return AmazonSesSnsDeliveryStatusMappingResult.Skipped(
                index,
                snsMessageId,
                snsMessageType,
                amazonSesMessageId,
                eventType,
                tenantId,
                invitationId,
                "unsupported-event-type",
                "The Amazon SES event type is not mapped to a delivery status by current options.");
        }

        if (string.IsNullOrWhiteSpace(tenantId) ||
            string.IsNullOrWhiteSpace(invitationId))
        {
            return AmazonSesSnsDeliveryStatusMappingResult.Skipped(
                index,
                snsMessageId,
                snsMessageType,
                amazonSesMessageId,
                eventType,
                tenantId,
                invitationId,
                "missing-cephalon-context",
                "The Amazon SES event did not include Cephalon tenant and invitation message tags.");
        }

        var observedAtUtc = ReadObservedAtUtc(element, mail, snsTimestamp);
        var metadata = BuildMetadata(
            element,
            mail,
            index,
            snsMessageId,
            snsMessageType,
            snsTopicArn,
            snsSubject,
            snsTimestamp,
            amazonSesMessageId,
            eventType,
            observedAtUtc);
        var request = new TenantInvitationDeliveryStatusReconciliationRequest(
            tenantId: tenantId!,
            invitationId: invitationId!,
            status: status,
            providerMessageId: amazonSesMessageId,
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

        return AmazonSesSnsDeliveryStatusMappingResult.CreateTranslated(
            index,
            snsMessageId,
            snsMessageType,
            amazonSesMessageId,
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
            "send" => TenantInvitationDeliveryStatuses.Accepted,
            "delivery" => TenantInvitationDeliveryStatuses.Delivered,
            "bounce" when string.Equals(ReadNestedString(element, "bounce", "bounceType"), "Transient", StringComparison.OrdinalIgnoreCase) =>
                TenantInvitationDeliveryStatuses.Deferred,
            "bounce" => TenantInvitationDeliveryStatuses.Bounced,
            "complaint" => TenantInvitationDeliveryStatuses.Suppressed,
            "reject" => TenantInvitationDeliveryStatuses.Suppressed,
            "rendering failure" => TenantInvitationDeliveryStatuses.Failed,
            "deliverydelay" => TenantInvitationDeliveryStatuses.Deferred,
            "open" or "click" when options.MapEngagementEventsAsDelivered => TenantInvitationDeliveryStatuses.Delivered,
            "open" or "click" or "subscription" => null,
            _ => TenantInvitationDeliveryStatuses.Unknown
        };
    }

    private static Dictionary<string, string> BuildMetadata(
        JsonElement element,
        JsonElement mail,
        int index,
        string? snsMessageId,
        string? snsMessageType,
        string? snsTopicArn,
        string? snsSubject,
        DateTimeOffset? snsTimestamp,
        string? amazonSesMessageId,
        string? eventType,
        DateTimeOffset? observedAtUtc)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["amazonSesSnsNotification"] = "true",
            ["amazonSesSnsTranslationOwnership"] = "cephalon-managed",
            ["amazonSesSnsSignatureVerification"] = "not-configured",
            ["amazonSesSnsSignatureVerificationOwnership"] = "not-configured",
            ["amazonSesSnsReplayProtection"] = "not-configured",
            ["amazonSesSnsReplayProtectionOwnership"] = "not-configured",
            ["amazonSesSnsMessageIdIdempotency"] = "not-configured",
            ["amazonSesSnsMessageIdIdempotencyOwnership"] = "not-configured",
            ["amazonSesEventIndex"] = index.ToString(CultureInfo.InvariantCulture),
            ["amazonSesEventTypeField"] = element.TryGetProperty("eventType", out _) ? "eventType" : "notificationType"
        };

        AddIfPresent(metadata, "amazonSesSnsMessageId", snsMessageId);
        AddIfPresent(metadata, "amazonSesSnsMessageType", snsMessageType);
        AddIfPresent(metadata, "amazonSesSnsTopicArn", snsTopicArn);
        AddIfPresent(metadata, "amazonSesSnsSubject", snsSubject);
        AddIfPresent(metadata, "amazonSesMessageId", amazonSesMessageId);
        AddIfPresent(metadata, "amazonSesEventType", eventType);
        AddIfPresent(metadata, "amazonSesBounceType", ReadNestedString(element, "bounce", "bounceType"));
        AddIfPresent(metadata, "amazonSesBounceSubType", ReadNestedString(element, "bounce", "bounceSubType"));
        AddIfPresent(metadata, "amazonSesBounceFeedbackId", ReadNestedString(element, "bounce", "feedbackId"));
        AddIfPresent(metadata, "amazonSesComplaintFeedbackType", ReadNestedString(element, "complaint", "complaintFeedbackType"));
        AddIfPresent(metadata, "amazonSesComplaintFeedbackId", ReadNestedString(element, "complaint", "feedbackId"));
        AddIfPresent(metadata, "amazonSesDeliverySmtpResponse", ReadNestedString(element, "delivery", "smtpResponse"));
        AddIfPresent(metadata, "amazonSesRejectReason", ReadNestedString(element, "reject", "reason"));
        AddIfPresent(metadata, "amazonSesRenderingFailureErrorMessage", ReadNestedString(element, "failure", "errorMessage"));
        AddIfPresent(metadata, "amazonSesDeliveryDelayType", ReadNestedString(element, "deliveryDelay", "delayType"));

        var tagCount = CountTags(mail);
        if (tagCount is not null)
        {
            metadata["amazonSesTagCount"] = tagCount.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (snsTimestamp is not null)
        {
            metadata["amazonSesSnsTimestamp"] = snsTimestamp.Value.ToString("O", CultureInfo.InvariantCulture);
        }

        if (observedAtUtc is not null)
        {
            metadata["amazonSesObservedAtUtc"] = observedAtUtc.Value.ToString("O", CultureInfo.InvariantCulture);
        }

        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryStatusObservationId] =
            CreateObservationId(snsMessageId, amazonSesMessageId, eventType, observedAtUtc);

        return metadata;
    }

    private static string CreateObservationId(
        string? snsMessageId,
        string? amazonSesMessageId,
        string? eventType,
        DateTimeOffset? observedAtUtc)
    {
        if (!string.IsNullOrWhiteSpace(snsMessageId))
        {
            return "amazon-ses-sns:" + snsMessageId.Trim();
        }

        var material = string.Join(
            "\u001f",
            "amazon-ses",
            amazonSesMessageId ?? string.Empty,
            eventType ?? string.Empty,
            observedAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return "amazon-ses:" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static DateTimeOffset? ReadObservedAtUtc(JsonElement element, JsonElement mail, DateTimeOffset? snsTimestamp)
    {
        return ReadNestedDateTimeOffset(element, "delivery", "timestamp") ??
            ReadNestedDateTimeOffset(element, "bounce", "timestamp") ??
            ReadNestedDateTimeOffset(element, "complaint", "timestamp") ??
            ReadNestedDateTimeOffset(element, "reject", "timestamp") ??
            ReadNestedDateTimeOffset(element, "deliveryDelay", "timestamp") ??
            ReadNestedDateTimeOffset(element, "failure", "timestamp") ??
            ReadDateTimeOffset(mail, "timestamp") ??
            snsTimestamp;
    }

    private static string ResolveReason(JsonElement element, string? eventType)
    {
        var bounceType = ReadNestedString(element, "bounce", "bounceType");
        var bounceSubType = ReadNestedString(element, "bounce", "bounceSubType");
        var complaintFeedbackType = ReadNestedString(element, "complaint", "complaintFeedbackType");
        var deliveryResponse = ReadNestedString(element, "delivery", "smtpResponse");
        var rejectReason = ReadNestedString(element, "reject", "reason");
        var renderingFailure = ReadNestedString(element, "failure", "errorMessage");
        var delayType = ReadNestedString(element, "deliveryDelay", "delayType");

        if (!string.IsNullOrWhiteSpace(bounceType))
        {
            return string.IsNullOrWhiteSpace(bounceSubType)
                ? $"Amazon SES reported a {bounceType} bounce."
                : $"Amazon SES reported a {bounceType}/{bounceSubType} bounce.";
        }

        if (!string.IsNullOrWhiteSpace(complaintFeedbackType))
        {
            return $"Amazon SES reported a complaint with feedback type {complaintFeedbackType}.";
        }

        if (!string.IsNullOrWhiteSpace(deliveryResponse))
        {
            return $"Amazon SES reported delivery: {deliveryResponse}.";
        }

        if (!string.IsNullOrWhiteSpace(rejectReason))
        {
            return $"Amazon SES rejected the message: {rejectReason}.";
        }

        if (!string.IsNullOrWhiteSpace(renderingFailure))
        {
            return $"Amazon SES reported a rendering failure: {renderingFailure}.";
        }

        if (!string.IsNullOrWhiteSpace(delayType))
        {
            return $"Amazon SES reported delivery delay type {delayType}.";
        }

        return string.IsNullOrWhiteSpace(eventType)
            ? "Amazon SES reported an unknown event."
            : $"Amazon SES reported {eventType.Trim()}.";
    }

    private static string? ReadCephalonTag(JsonElement mail, params string[] names)
    {
        if (mail.ValueKind != JsonValueKind.Object ||
            !mail.TryGetProperty("tags", out var tags) ||
            tags.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var name in names)
        {
            if (TryReadTag(tags, name, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static bool TryReadTag(JsonElement tags, string name, out string? value)
    {
        value = null;
        foreach (var property in tags.EnumerateObject())
        {
            if (!string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            value = ReadTagValue(property.Value);
            return value is not null;
        }

        return false;
    }

    private static string? ReadTagValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                var normalized = ReadScalarString(item);
                if (normalized is not null)
                {
                    return normalized;
                }
            }

            return null;
        }

        return ReadScalarString(value);
    }

    private static int? CountTags(JsonElement mail)
    {
        if (mail.ValueKind != JsonValueKind.Object ||
            !mail.TryGetProperty("tags", out var tags) ||
            tags.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return tags.EnumerateObject().Count();
    }

    private static DateTimeOffset? ReadNestedDateTimeOffset(JsonElement element, string parentName, string propertyName)
    {
        if (!element.TryGetProperty(parentName, out var parent) ||
            parent.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return ReadDateTimeOffset(parent, propertyName);
    }

    private static DateTimeOffset? ReadDateTimeOffset(JsonElement element, string propertyName)
    {
        var value = ReadString(element, propertyName);
        if (value is null)
        {
            return null;
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.ToUniversalTime()
            : null;
    }

    private static string? ReadNestedString(JsonElement element, string parentName, string propertyName)
    {
        return element.TryGetProperty(parentName, out var parent) && parent.ValueKind == JsonValueKind.Object
            ? ReadString(parent, propertyName)
            : null;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return ReadScalarString(property);
    }

    private static string? ReadScalarString(JsonElement property)
    {
        var value = property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => property.ToString(),
            _ => null
        };

        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
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

internal sealed record AmazonSesSnsDeliveryStatusMappingResult(
    int Index,
    string? SnsMessageId,
    string? SnsMessageType,
    string? AmazonSesMessageId,
    string? AmazonSesEventType,
    string? TenantId,
    string? InvitationId,
    string? Status,
    string Outcome,
    string Reason,
    TenantInvitationDeliveryStatusReconciliationRequest? Request)
{
    public bool Translated => Request is not null;

    public static AmazonSesSnsDeliveryStatusMappingResult CreateTranslated(
        int index,
        string? snsMessageId,
        string? snsMessageType,
        string? amazonSesMessageId,
        string? amazonSesEventType,
        string tenantId,
        string invitationId,
        string status,
        TenantInvitationDeliveryStatusReconciliationRequest request) =>
        new(
            index,
            snsMessageId,
            snsMessageType,
            amazonSesMessageId,
            amazonSesEventType,
            tenantId,
            invitationId,
            status,
            "translated",
            "The Amazon SES event was translated into a Cephalon delivery status reconciliation request.",
            request);

    public static AmazonSesSnsDeliveryStatusMappingResult Skipped(
        int index,
        string? snsMessageId,
        string? snsMessageType,
        string? amazonSesMessageId,
        string? amazonSesEventType,
        string? tenantId,
        string? invitationId,
        string outcome,
        string reason) =>
        new(index, snsMessageId, snsMessageType, amazonSesMessageId, amazonSesEventType, tenantId, invitationId, null, outcome, reason, null);

    public AmazonSesInvitationDeliveryStatusCallbackEventResult ToSkippedEventResult() =>
        new(
            Index,
            SnsMessageId,
            SnsMessageType,
            AmazonSesMessageId,
            AmazonSesEventType,
            TenantId,
            InvitationId,
            Status,
            Outcome,
            translated: false,
            reconciled: false,
            Reason);

    public AmazonSesInvitationDeliveryStatusCallbackEventResult ToReconciledEventResult(
        TenantInvitationDeliveryStatusReconciliationResult reconciliation) =>
        new(
            Index,
            SnsMessageId,
            SnsMessageType,
            AmazonSesMessageId,
            AmazonSesEventType,
            TenantId,
            InvitationId,
            Status,
            reconciliation.Outcome,
            translated: true,
            reconciled: reconciliation.Reconciled,
            reconciliation.Reason);

    public AmazonSesInvitationDeliveryStatusCallbackEventResult ToDuplicateEventResult(string reason) =>
        new(
            Index,
            SnsMessageId,
            SnsMessageType,
            AmazonSesMessageId,
            AmazonSesEventType,
            TenantId,
            InvitationId,
            Status,
            "duplicate-skipped",
            translated: true,
            reconciled: false,
            reason);
}
