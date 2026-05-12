namespace Cephalon.Eventing.Services;

/// <summary>
/// Builds provider-reported exactly-once delivery proof metadata for successful dispatch reports.
/// </summary>
/// <remarks>
/// Exactly-once delivery is a stronger claim than dispatch success, provider receipt, subscriber acknowledgement,
/// or destination commit by themselves. This helper records the claim only when a provider/runtime supplies all
/// completion evidence plus an explicit exactly-once proof id. Cephalon carries the proof without making any
/// provider package, including Wolverine, part of the core authoring surface.
/// </remarks>
public static class EventDispatchExactlyOnceDeliveryProofMetadata
{
    /// <summary>
    /// Creates a dispatch report copy enriched with provider-reported exactly-once delivery proof when the inputs support it.
    /// </summary>
    /// <param name="report">The successful dispatch report to copy.</param>
    /// <param name="source">The stable provider or runtime source that reported the exactly-once proof.</param>
    /// <param name="providerReceiptId">The provider delivery receipt id.</param>
    /// <param name="subscriberAcknowledgementId">The subscriber acknowledgement id.</param>
    /// <param name="destinationCommitId">The destination commit id.</param>
    /// <param name="exactlyOnceProofId">The provider exactly-once delivery proof id.</param>
    /// <param name="strategy">An optional provider strategy name for the exactly-once proof.</param>
    /// <returns>A dispatch report containing the original metadata plus exactly-once proof when applicable.</returns>
    public static EventDispatchExecutionReport CreateReport(
        EventDispatchExecutionReport report,
        string source,
        string providerReceiptId,
        string subscriberAcknowledgementId,
        string destinationCommitId,
        string exactlyOnceProofId,
        string? strategy = null)
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
                destinationCommitId,
                exactlyOnceProofId,
                strategy));
    }

    /// <summary>
    /// Creates a metadata copy enriched with provider-reported exactly-once delivery proof when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch report metadata to copy.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported the exactly-once proof.</param>
    /// <param name="providerReceiptId">The provider delivery receipt id.</param>
    /// <param name="subscriberAcknowledgementId">The subscriber acknowledgement id.</param>
    /// <param name="destinationCommitId">The destination commit id.</param>
    /// <param name="exactlyOnceProofId">The provider exactly-once delivery proof id.</param>
    /// <param name="strategy">An optional provider strategy name for the exactly-once proof.</param>
    /// <returns>A case-insensitive metadata dictionary containing the original values plus exactly-once proof when applicable.</returns>
    public static Dictionary<string, string> CreateMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string outcome,
        string source,
        string providerReceiptId,
        string subscriberAcknowledgementId,
        string destinationCommitId,
        string exactlyOnceProofId,
        string? strategy = null)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var result = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        TryApplyMetadata(
            result,
            outcome,
            source,
            providerReceiptId,
            subscriberAcknowledgementId,
            destinationCommitId,
            exactlyOnceProofId,
            strategy);
        return result;
    }

    /// <summary>
    /// Applies provider-reported exactly-once delivery proof to an existing dispatch metadata dictionary when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to enrich.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported the exactly-once proof.</param>
    /// <param name="providerReceiptId">The provider delivery receipt id.</param>
    /// <param name="subscriberAcknowledgementId">The subscriber acknowledgement id.</param>
    /// <param name="destinationCommitId">The destination commit id.</param>
    /// <param name="exactlyOnceProofId">The provider exactly-once delivery proof id.</param>
    /// <param name="strategy">An optional provider strategy name for the exactly-once proof.</param>
    /// <returns><see langword="true" /> when exactly-once proof was applied; otherwise, <see langword="false" />.</returns>
    public static bool TryApplyMetadata(
        IDictionary<string, string> metadata,
        string outcome,
        string source,
        string providerReceiptId,
        string subscriberAcknowledgementId,
        string destinationCommitId,
        string exactlyOnceProofId,
        string? strategy = null)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (!string.Equals(outcome, EventDispatchExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedSource = RequireValue(source, nameof(source));
        var normalizedProviderReceiptId = RequireValue(providerReceiptId, nameof(providerReceiptId));
        var normalizedSubscriberAcknowledgementId = RequireValue(subscriberAcknowledgementId, nameof(subscriberAcknowledgementId));
        var normalizedDestinationCommitId = RequireValue(destinationCommitId, nameof(destinationCommitId));
        var normalizedExactlyOnceProofId = RequireValue(exactlyOnceProofId, nameof(exactlyOnceProofId));
        var normalizedStrategy = NormalizeOptionalValue(strategy);

        EventDispatchDeliveryCompletionMetadata.TryApplyMetadata(
            metadata,
            outcome,
            normalizedSource,
            normalizedProviderReceiptId,
            normalizedSubscriberAcknowledgementId,
            normalizedDestinationCommitId);

        metadata[EventDispatchRuntimeMetadataKeys.ExactlyOnceDelivery] = "provider-proven";
        metadata[EventDispatchRuntimeMetadataKeys.ExactlyOnceDeliverySource] = normalizedSource;
        metadata[EventDispatchRuntimeMetadataKeys.ExactlyOnceDeliveryProofId] = normalizedExactlyOnceProofId;

        if (normalizedStrategy is not null)
        {
            metadata[EventDispatchRuntimeMetadataKeys.ExactlyOnceDeliveryStrategy] = normalizedStrategy;
        }

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata contains provider-proven exactly-once delivery proof.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to inspect.</param>
    /// <returns><see langword="true" /> when provider-proven exactly-once proof is present; otherwise, <see langword="false" />.</returns>
    public static bool IsProviderProven(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.ExactlyOnceDelivery, out var value) &&
            string.Equals(value, "provider-proven", StringComparison.OrdinalIgnoreCase) &&
            metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.ExactlyOnceDeliveryProofId, out var proofId) &&
            !string.IsNullOrWhiteSpace(proofId);
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
