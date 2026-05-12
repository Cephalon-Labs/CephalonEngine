namespace Cephalon.Eventing.Services;

/// <summary>
/// Builds provider-reported downstream delivery-completion metadata for successful dispatch reports.
/// </summary>
/// <remarks>
/// Dispatch success only says the active dispatcher completed its local work. This helper records a stronger,
/// provider-reported downstream completion proof only when the report outcome is <c>succeeded</c> and the provider
/// supplies an explicit delivery receipt id. Subscriber acknowledgement and destination commit evidence are recorded
/// independently so the engine does not imply exactly-once delivery from a generic provider receipt.
/// </remarks>
public static class EventDispatchDeliveryCompletionMetadata
{
    /// <summary>
    /// Creates a dispatch report copy enriched with provider-reported delivery-completion proof when the inputs support it.
    /// </summary>
    /// <param name="report">The successful dispatch report to copy.</param>
    /// <param name="source">The stable provider or runtime source that reported delivery completion.</param>
    /// <param name="providerReceiptId">The provider delivery receipt id.</param>
    /// <param name="subscriberAcknowledgementId">An optional subscriber acknowledgement id.</param>
    /// <param name="destinationCommitId">An optional destination commit id.</param>
    /// <returns>A dispatch report containing the original metadata plus delivery-completion proof when applicable.</returns>
    public static EventDispatchExecutionReport CreateReport(
        EventDispatchExecutionReport report,
        string source,
        string providerReceiptId,
        string? subscriberAcknowledgementId = null,
        string? destinationCommitId = null)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new EventDispatchExecutionReport(
            outboxId: report.OutboxId,
            channelId: report.ChannelId,
            outcome: report.Outcome,
            observedAtUtc: report.ObservedAtUtc,
            messageId: report.MessageId,
            attempt: report.Attempt,
            error: report.Error,
            metadata: CreateMetadata(
                report.Metadata,
                report.Outcome,
                source,
                providerReceiptId,
                subscriberAcknowledgementId,
                destinationCommitId));
    }

    /// <summary>
    /// Creates a metadata copy enriched with provider-reported delivery-completion proof when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch report metadata to copy.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported delivery completion.</param>
    /// <param name="providerReceiptId">The provider delivery receipt id.</param>
    /// <param name="subscriberAcknowledgementId">An optional subscriber acknowledgement id.</param>
    /// <param name="destinationCommitId">An optional destination commit id.</param>
    /// <returns>A case-insensitive metadata dictionary containing the original values plus delivery-completion proof when applicable.</returns>
    public static Dictionary<string, string> CreateMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string outcome,
        string source,
        string providerReceiptId,
        string? subscriberAcknowledgementId = null,
        string? destinationCommitId = null)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var result = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        TryApplyMetadata(result, outcome, source, providerReceiptId, subscriberAcknowledgementId, destinationCommitId);
        return result;
    }

    /// <summary>
    /// Applies provider-reported delivery-completion proof to an existing dispatch metadata dictionary when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to enrich.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported delivery completion.</param>
    /// <param name="providerReceiptId">The provider delivery receipt id.</param>
    /// <param name="subscriberAcknowledgementId">An optional subscriber acknowledgement id.</param>
    /// <param name="destinationCommitId">An optional destination commit id.</param>
    /// <returns><see langword="true" /> when delivery-completion proof was applied; otherwise, <see langword="false" />.</returns>
    public static bool TryApplyMetadata(
        IDictionary<string, string> metadata,
        string outcome,
        string source,
        string providerReceiptId,
        string? subscriberAcknowledgementId = null,
        string? destinationCommitId = null)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (!string.Equals(outcome, EventDispatchExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedSource = RequireValue(source, nameof(source));
        var normalizedProviderReceiptId = RequireValue(providerReceiptId, nameof(providerReceiptId));
        var normalizedSubscriberAcknowledgementId = NormalizeOptionalValue(subscriberAcknowledgementId);
        var normalizedDestinationCommitId = NormalizeOptionalValue(destinationCommitId);

        metadata[EventDispatchRuntimeMetadataKeys.DownstreamDeliveryCompletion] = "provider-reported";
        metadata[EventDispatchRuntimeMetadataKeys.DownstreamDeliveryCompletionSource] = normalizedSource;
        metadata[EventDispatchRuntimeMetadataKeys.ProviderDeliveryReceipt] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.ProviderDeliveryReceiptId] = normalizedProviderReceiptId;
        metadata[EventDispatchRuntimeMetadataKeys.SubscriberAcknowledgement] = normalizedSubscriberAcknowledgementId is null
            ? "not-claimed"
            : "reported";
        metadata[EventDispatchRuntimeMetadataKeys.DestinationCommit] = normalizedDestinationCommitId is null
            ? "not-claimed"
            : "reported";
        metadata[EventDispatchRuntimeMetadataKeys.ExactlyOnceDelivery] = "not-claimed";

        if (normalizedSubscriberAcknowledgementId is not null)
        {
            metadata[EventDispatchRuntimeMetadataKeys.SubscriberAcknowledgementId] = normalizedSubscriberAcknowledgementId;
        }

        if (normalizedDestinationCommitId is not null)
        {
            metadata[EventDispatchRuntimeMetadataKeys.DestinationCommitId] = normalizedDestinationCommitId;
        }

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata contains provider-reported downstream delivery-completion proof.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to inspect.</param>
    /// <returns><see langword="true" /> when provider-reported delivery-completion proof is present; otherwise, <see langword="false" />.</returns>
    public static bool IsCompleted(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.DownstreamDeliveryCompletion, out var value) &&
            string.Equals(value, "provider-reported", StringComparison.OrdinalIgnoreCase);
    }

    private static string RequireValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
