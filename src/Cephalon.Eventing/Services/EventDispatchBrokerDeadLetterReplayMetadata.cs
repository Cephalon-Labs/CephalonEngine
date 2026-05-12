namespace Cephalon.Eventing.Services;

/// <summary>
/// Builds provider-reported broker dead-letter and replay proof metadata for failed dispatch reports.
/// </summary>
/// <remarks>
/// Cephalon dispatch-store dead-letter commands prove only bounded dispatch-store terminal intent. This helper records
/// the stronger broker DLQ and replay claim only when a provider/runtime reports failed dispatch evidence with broker
/// dead-letter queue, replay action catalog, replay cursor, purge/quarantine, and provider proof ids.
/// </remarks>
public static class EventDispatchBrokerDeadLetterReplayMetadata
{
    /// <summary>
    /// Creates a dispatch report copy enriched with broker dead-letter and replay proof when the inputs support it.
    /// </summary>
    /// <param name="report">The failed dispatch report to copy.</param>
    /// <param name="source">The stable provider or runtime source that reported broker dead-letter and replay ownership.</param>
    /// <param name="brokerDeadLetterQueueId">The broker dead-letter queue proof id.</param>
    /// <param name="brokerReplayActionCatalogId">The broker replay action catalog proof id.</param>
    /// <param name="brokerReplayCursorId">The broker replay cursor proof id.</param>
    /// <param name="brokerPurgeQuarantineId">The broker purge or quarantine proof id.</param>
    /// <param name="providerProofId">The provider-owned broker dead-letter and replay proof id.</param>
    /// <returns>A dispatch report containing the original metadata plus broker dead-letter and replay proof when applicable.</returns>
    public static EventDispatchExecutionReport CreateReport(
        EventDispatchExecutionReport report,
        string source,
        string brokerDeadLetterQueueId,
        string brokerReplayActionCatalogId,
        string brokerReplayCursorId,
        string brokerPurgeQuarantineId,
        string providerProofId)
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
                brokerDeadLetterQueueId,
                brokerReplayActionCatalogId,
                brokerReplayCursorId,
                brokerPurgeQuarantineId,
                providerProofId));
    }

    /// <summary>
    /// Creates a metadata copy enriched with broker dead-letter and replay proof when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch report metadata to copy.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported broker dead-letter and replay ownership.</param>
    /// <param name="brokerDeadLetterQueueId">The broker dead-letter queue proof id.</param>
    /// <param name="brokerReplayActionCatalogId">The broker replay action catalog proof id.</param>
    /// <param name="brokerReplayCursorId">The broker replay cursor proof id.</param>
    /// <param name="brokerPurgeQuarantineId">The broker purge or quarantine proof id.</param>
    /// <param name="providerProofId">The provider-owned broker dead-letter and replay proof id.</param>
    /// <returns>A case-insensitive metadata dictionary containing the original values plus broker dead-letter and replay proof when applicable.</returns>
    public static Dictionary<string, string> CreateMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string outcome,
        string source,
        string brokerDeadLetterQueueId,
        string brokerReplayActionCatalogId,
        string brokerReplayCursorId,
        string brokerPurgeQuarantineId,
        string providerProofId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var result = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        TryApplyMetadata(
            result,
            outcome,
            source,
            brokerDeadLetterQueueId,
            brokerReplayActionCatalogId,
            brokerReplayCursorId,
            brokerPurgeQuarantineId,
            providerProofId);
        return result;
    }

    /// <summary>
    /// Applies broker dead-letter and replay proof to an existing dispatch metadata dictionary when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to enrich.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported broker dead-letter and replay ownership.</param>
    /// <param name="brokerDeadLetterQueueId">The broker dead-letter queue proof id.</param>
    /// <param name="brokerReplayActionCatalogId">The broker replay action catalog proof id.</param>
    /// <param name="brokerReplayCursorId">The broker replay cursor proof id.</param>
    /// <param name="brokerPurgeQuarantineId">The broker purge or quarantine proof id.</param>
    /// <param name="providerProofId">The provider-owned broker dead-letter and replay proof id.</param>
    /// <returns><see langword="true" /> when broker dead-letter and replay proof was applied; otherwise, <see langword="false" />.</returns>
    public static bool TryApplyMetadata(
        IDictionary<string, string> metadata,
        string outcome,
        string source,
        string brokerDeadLetterQueueId,
        string brokerReplayActionCatalogId,
        string brokerReplayCursorId,
        string brokerPurgeQuarantineId,
        string providerProofId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (!string.Equals(outcome, EventDispatchExecutionOutcomes.Failed, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedSource = RequireValue(source, nameof(source));
        var normalizedBrokerDeadLetterQueueId = RequireValue(brokerDeadLetterQueueId, nameof(brokerDeadLetterQueueId));
        var normalizedBrokerReplayActionCatalogId = RequireValue(brokerReplayActionCatalogId, nameof(brokerReplayActionCatalogId));
        var normalizedBrokerReplayCursorId = RequireValue(brokerReplayCursorId, nameof(brokerReplayCursorId));
        var normalizedBrokerPurgeQuarantineId = RequireValue(brokerPurgeQuarantineId, nameof(brokerPurgeQuarantineId));
        var normalizedProviderProofId = RequireValue(providerProofId, nameof(providerProofId));

        metadata[EventDispatchRuntimeMetadataKeys.DeadLetterOutcome] = "provider-broker-dead-letter";
        metadata[EventDispatchRuntimeMetadataKeys.DeadLetterScope] = "broker";
        metadata[EventDispatchRuntimeMetadataKeys.DeadLetterDurability] = "broker";
        metadata[EventDispatchRuntimeMetadataKeys.BrokerDeadLetter] = "true";
        metadata[EventDispatchRuntimeMetadataKeys.BrokerDeadLetterReplayOwnership] = "provider-reported";
        metadata[EventDispatchRuntimeMetadataKeys.BrokerDeadLetterReplayOwnershipSource] = normalizedSource;
        metadata[EventDispatchRuntimeMetadataKeys.BrokerDeadLetterQueueOwnership] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.BrokerDeadLetterQueueId] = normalizedBrokerDeadLetterQueueId;
        metadata[EventDispatchRuntimeMetadataKeys.BrokerReplay] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.BrokerReplayActionCatalog] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.BrokerReplayActionCatalogId] = normalizedBrokerReplayActionCatalogId;
        metadata[EventDispatchRuntimeMetadataKeys.BrokerReplayCursor] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.BrokerReplayCursorId] = normalizedBrokerReplayCursorId;
        metadata[EventDispatchRuntimeMetadataKeys.BrokerPurgeQuarantine] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.BrokerPurgeQuarantineId] = normalizedBrokerPurgeQuarantineId;
        metadata[EventDispatchRuntimeMetadataKeys.BrokerDeadLetterReplayProofId] = normalizedProviderProofId;

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata contains complete provider-reported broker dead-letter and replay proof.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to inspect.</param>
    /// <returns><see langword="true" /> when complete broker dead-letter and replay proof is present; otherwise, <see langword="false" />.</returns>
    public static bool IsBrokerDeadLetterReplayProven(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return HasValue(metadata, EventDispatchRuntimeMetadataKeys.DeadLetterOutcome, "provider-broker-dead-letter") &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.DeadLetterScope, "broker") &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.DeadLetterDurability, "broker") &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.BrokerDeadLetter, "true") &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.BrokerDeadLetterReplayOwnership, "provider-reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.BrokerDeadLetterReplayOwnershipSource) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.BrokerDeadLetterQueueOwnership, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.BrokerDeadLetterQueueId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.BrokerReplay, "reported") &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.BrokerReplayActionCatalog, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.BrokerReplayActionCatalogId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.BrokerReplayCursor, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.BrokerReplayCursorId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.BrokerPurgeQuarantine, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.BrokerPurgeQuarantineId) &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.BrokerDeadLetterReplayProofId);
    }

    private static bool HasValue(IReadOnlyDictionary<string, string> metadata, string key, string expected) =>
        metadata.TryGetValue(key, out var value) &&
        string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);

    private static bool HasNonEmpty(IReadOnlyDictionary<string, string> metadata, string key) =>
        metadata.TryGetValue(key, out var value) &&
        !string.IsNullOrWhiteSpace(value);

    private static string RequireValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return value.Trim();
    }
}
