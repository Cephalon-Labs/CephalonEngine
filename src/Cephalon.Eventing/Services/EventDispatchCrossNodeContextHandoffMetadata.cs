namespace Cephalon.Eventing.Services;

/// <summary>
/// Builds provider-reported cross-node context-handoff metadata for dispatch reports.
/// </summary>
/// <remarks>
/// Cross-node handoff is only marked when provider-side context persistence has already been proven on the dispatch
/// metadata, consumer-side extraction metadata is present, and the reported producer and consumer node ids are distinct.
/// This keeps local dispatch-store persistence and local subscription extraction from being mistaken for a distributed
/// handoff.
/// </remarks>
public static class EventDispatchCrossNodeContextHandoffMetadata
{
    /// <summary>
    /// Creates a dispatch report copy enriched with provider-reported cross-node handoff proof when the inputs support it.
    /// </summary>
    /// <param name="report">The provider-side dispatch report to copy.</param>
    /// <param name="consumerContextMetadata">The consumer-side context extraction metadata observed by the provider or runtime.</param>
    /// <param name="source">The stable provider or runtime source that observed the cross-node handoff.</param>
    /// <param name="producerNodeId">The producer-side node identifier.</param>
    /// <param name="consumerNodeId">The consumer-side node identifier.</param>
    /// <returns>A dispatch report containing the original metadata plus cross-node handoff proof when applicable.</returns>
    public static EventDispatchExecutionReport CreateReport(
        EventDispatchExecutionReport report,
        IReadOnlyDictionary<string, string> consumerContextMetadata,
        string source,
        string producerNodeId,
        string consumerNodeId)
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
                consumerContextMetadata,
                source,
                producerNodeId,
                consumerNodeId));
    }

    /// <summary>
    /// Creates a metadata copy enriched with provider-reported cross-node handoff proof when the inputs support it.
    /// </summary>
    /// <param name="metadata">The provider-side dispatch report metadata to copy.</param>
    /// <param name="consumerContextMetadata">The consumer-side context extraction metadata observed by the provider or runtime.</param>
    /// <param name="source">The stable provider or runtime source that observed the cross-node handoff.</param>
    /// <param name="producerNodeId">The producer-side node identifier.</param>
    /// <param name="consumerNodeId">The consumer-side node identifier.</param>
    /// <returns>A case-insensitive metadata dictionary containing the original values plus handoff proof when applicable.</returns>
    public static Dictionary<string, string> CreateMetadata(
        IReadOnlyDictionary<string, string> metadata,
        IReadOnlyDictionary<string, string> consumerContextMetadata,
        string source,
        string producerNodeId,
        string consumerNodeId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var result = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        TryApplyMetadata(result, consumerContextMetadata, source, producerNodeId, consumerNodeId);
        return result;
    }

    /// <summary>
    /// Applies provider-reported cross-node handoff proof to an existing dispatch metadata dictionary when the inputs support it.
    /// </summary>
    /// <param name="metadata">The provider-side dispatch metadata dictionary to enrich.</param>
    /// <param name="consumerContextMetadata">The consumer-side context extraction metadata observed by the provider or runtime.</param>
    /// <param name="source">The stable provider or runtime source that observed the cross-node handoff.</param>
    /// <param name="producerNodeId">The producer-side node identifier.</param>
    /// <param name="consumerNodeId">The consumer-side node identifier.</param>
    /// <returns><see langword="true" /> when cross-node handoff proof was applied; otherwise, <see langword="false" />.</returns>
    public static bool TryApplyMetadata(
        IDictionary<string, string> metadata,
        IReadOnlyDictionary<string, string> consumerContextMetadata,
        string source,
        string producerNodeId,
        string consumerNodeId)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(consumerContextMetadata);

        var normalizedSource = RequireValue(source, nameof(source));
        var normalizedProducerNodeId = RequireValue(producerNodeId, nameof(producerNodeId));
        var normalizedConsumerNodeId = RequireValue(consumerNodeId, nameof(consumerNodeId));

        if (string.Equals(normalizedProducerNodeId, normalizedConsumerNodeId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!HasProviderSideContextPersistence(metadata) || !HasConsumerContextExtraction(consumerContextMetadata))
        {
            return false;
        }

        metadata[EventDispatchRuntimeMetadataKeys.CrossNodeContextHandoff] = "provider-reported";
        metadata[EventDispatchRuntimeMetadataKeys.CrossNodeContextHandoffSource] = normalizedSource;
        metadata[EventDispatchRuntimeMetadataKeys.CrossNodeContextHandoffProducerNodeId] = normalizedProducerNodeId;
        metadata[EventDispatchRuntimeMetadataKeys.CrossNodeContextHandoffConsumerNodeId] = normalizedConsumerNodeId;
        metadata[EventDispatchRuntimeMetadataKeys.CrossNodeContextHandoffHeaderCount] = GetValueOrDefault(
            consumerContextMetadata,
            EventSubscriptionRuntimeMetadataKeys.ConsumerContextHeaderCount);
        metadata[EventDispatchRuntimeMetadataKeys.CrossNodeContextHandoffHeaderNames] = GetValueOrDefault(
            consumerContextMetadata,
            EventSubscriptionRuntimeMetadataKeys.ConsumerContextHeaderNames);
        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata contains provider-reported cross-node context handoff proof.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to inspect.</param>
    /// <returns><see langword="true" /> when provider-reported cross-node handoff proof is present; otherwise, <see langword="false" />.</returns>
    public static bool IsHandoffProven(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.CrossNodeContextHandoff, out var value) &&
            string.Equals(value, "provider-reported", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasProviderSideContextPersistence(IDictionary<string, string> metadata) =>
        metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.ProviderSideContextPersistence, out var persistence) &&
        string.Equals(persistence, "dispatch-store-persisted", StringComparison.OrdinalIgnoreCase) &&
        metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaderNames, out var headerNames) &&
        HasPropagationHeaderNames(headerNames);

    private static bool HasConsumerContextExtraction(IReadOnlyDictionary<string, string> consumerContextMetadata) =>
        consumerContextMetadata.TryGetValue(EventSubscriptionRuntimeMetadataKeys.ConsumerContextExtraction, out var consumerContextExtraction) &&
        string.Equals(consumerContextExtraction, "extracted", StringComparison.OrdinalIgnoreCase) &&
        consumerContextMetadata.TryGetValue(EventSubscriptionRuntimeMetadataKeys.ConsumerContextHeaderNames, out var headerNames) &&
        HasPropagationHeaderNames(headerNames);

    private static bool HasPropagationHeaderNames(string headerNames)
    {
        if (string.IsNullOrWhiteSpace(headerNames))
        {
            return false;
        }

        return headerNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(static headerName =>
                string.Equals(headerName, EventContextHeaderNames.TenantId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(headerName, EventContextHeaderNames.CorrelationId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(headerName, EventContextHeaderNames.CausationId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(headerName, EventContextHeaderNames.Baggage, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetValueOrDefault(IReadOnlyDictionary<string, string> metadata, string key) =>
        metadata.TryGetValue(key, out var value) ? value : string.Empty;

    private static string RequireValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return value.Trim();
    }
}
