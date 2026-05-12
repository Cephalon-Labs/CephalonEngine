namespace Cephalon.Eventing.Services;

/// <summary>
/// Builds provider-reported broker inbound-consumption proof metadata for successful subscription reports.
/// </summary>
/// <remarks>
/// Declared subscriptions, direct in-process execution, hosted execution bindings, and optional provider adapters
/// do not automatically prove that Cephalon owns a broker consumer loop. This helper records that stronger claim
/// only when a provider/runtime reports a successful subscription observation with consumer-loop, acknowledgement,
/// lease, retry/poison, and offset-checkpoint proof.
/// </remarks>
public static class EventSubscriptionBrokerInboundConsumptionMetadata
{
    /// <summary>
    /// Creates a subscription report copy enriched with provider-reported broker inbound-consumption proof when the inputs support it.
    /// </summary>
    /// <param name="report">The successful subscription execution report to copy.</param>
    /// <param name="source">The stable provider or runtime source that reported broker inbound consumption.</param>
    /// <param name="consumerLoopId">The provider consumer-loop proof id.</param>
    /// <param name="acknowledgementId">The inbound acknowledgement proof id.</param>
    /// <param name="leaseId">The consumer lease or ownership-token proof id.</param>
    /// <param name="retryPolicy">The provider retry policy reported for inbound consumption.</param>
    /// <param name="poisonMessageHandling">The poison-message handling posture reported by the provider.</param>
    /// <param name="offsetCheckpointId">The consumer offset-checkpoint proof id.</param>
    /// <returns>A subscription report containing the original metadata plus broker inbound-consumption proof when applicable.</returns>
    public static EventSubscriptionExecutionReport CreateReport(
        EventSubscriptionExecutionReport report,
        string source,
        string consumerLoopId,
        string acknowledgementId,
        string leaseId,
        string retryPolicy,
        string poisonMessageHandling,
        string offsetCheckpointId)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new EventSubscriptionExecutionReport(
            subscriptionId: report.SubscriptionId,
            outcome: report.Outcome,
            observedAtUtc: report.ObservedAtUtc,
            messageId: report.MessageId,
            attempt: report.Attempt,
            error: report.Error,
            metadata: CreateMetadata(
                report.Metadata,
                report.Outcome,
                source,
                consumerLoopId,
                acknowledgementId,
                leaseId,
                retryPolicy,
                poisonMessageHandling,
                offsetCheckpointId));
    }

    /// <summary>
    /// Creates a metadata copy enriched with provider-reported broker inbound-consumption proof when the inputs support it.
    /// </summary>
    /// <param name="metadata">The subscription execution metadata to copy.</param>
    /// <param name="outcome">The subscription execution outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported broker inbound consumption.</param>
    /// <param name="consumerLoopId">The provider consumer-loop proof id.</param>
    /// <param name="acknowledgementId">The inbound acknowledgement proof id.</param>
    /// <param name="leaseId">The consumer lease or ownership-token proof id.</param>
    /// <param name="retryPolicy">The provider retry policy reported for inbound consumption.</param>
    /// <param name="poisonMessageHandling">The poison-message handling posture reported by the provider.</param>
    /// <param name="offsetCheckpointId">The consumer offset-checkpoint proof id.</param>
    /// <returns>A case-insensitive metadata dictionary containing the original values plus broker inbound-consumption proof when applicable.</returns>
    public static Dictionary<string, string> CreateMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string outcome,
        string source,
        string consumerLoopId,
        string acknowledgementId,
        string leaseId,
        string retryPolicy,
        string poisonMessageHandling,
        string offsetCheckpointId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var result = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        TryApplyMetadata(
            result,
            outcome,
            source,
            consumerLoopId,
            acknowledgementId,
            leaseId,
            retryPolicy,
            poisonMessageHandling,
            offsetCheckpointId);
        return result;
    }

    /// <summary>
    /// Applies provider-reported broker inbound-consumption proof to an existing subscription metadata dictionary when the inputs support it.
    /// </summary>
    /// <param name="metadata">The subscription metadata dictionary to enrich.</param>
    /// <param name="outcome">The subscription execution outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported broker inbound consumption.</param>
    /// <param name="consumerLoopId">The provider consumer-loop proof id.</param>
    /// <param name="acknowledgementId">The inbound acknowledgement proof id.</param>
    /// <param name="leaseId">The consumer lease or ownership-token proof id.</param>
    /// <param name="retryPolicy">The provider retry policy reported for inbound consumption.</param>
    /// <param name="poisonMessageHandling">The poison-message handling posture reported by the provider.</param>
    /// <param name="offsetCheckpointId">The consumer offset-checkpoint proof id.</param>
    /// <returns><see langword="true" /> when broker inbound-consumption proof was applied; otherwise, <see langword="false" />.</returns>
    public static bool TryApplyMetadata(
        IDictionary<string, string> metadata,
        string outcome,
        string source,
        string consumerLoopId,
        string acknowledgementId,
        string leaseId,
        string retryPolicy,
        string poisonMessageHandling,
        string offsetCheckpointId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (!string.Equals(outcome, EventSubscriptionExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedSource = RequireValue(source, nameof(source));
        var normalizedConsumerLoopId = RequireValue(consumerLoopId, nameof(consumerLoopId));
        var normalizedAcknowledgementId = RequireValue(acknowledgementId, nameof(acknowledgementId));
        var normalizedLeaseId = RequireValue(leaseId, nameof(leaseId));
        var normalizedRetryPolicy = RequireValue(retryPolicy, nameof(retryPolicy));
        var normalizedPoisonMessageHandling = RequireValue(poisonMessageHandling, nameof(poisonMessageHandling));
        var normalizedOffsetCheckpointId = RequireValue(offsetCheckpointId, nameof(offsetCheckpointId));

        metadata[EventSubscriptionRuntimeMetadataKeys.BrokerInboundConsumption] = "provider-reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.BrokerInboundConsumptionSource] = normalizedSource;
        metadata[EventSubscriptionRuntimeMetadataKeys.BrokerConsumerLoop] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.BrokerConsumerLoopId] = normalizedConsumerLoopId;
        metadata[EventSubscriptionRuntimeMetadataKeys.InboundAcknowledgement] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.InboundAcknowledgementId] = normalizedAcknowledgementId;
        metadata[EventSubscriptionRuntimeMetadataKeys.ConsumerLease] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.ConsumerLeaseId] = normalizedLeaseId;
        metadata[EventSubscriptionRuntimeMetadataKeys.InboundRetryPolicy] = normalizedRetryPolicy;
        metadata[EventSubscriptionRuntimeMetadataKeys.PoisonMessageHandling] = normalizedPoisonMessageHandling;
        metadata[EventSubscriptionRuntimeMetadataKeys.ConsumerOffsetCheckpoint] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.ConsumerOffsetCheckpointId] = normalizedOffsetCheckpointId;

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata contains complete provider-reported broker inbound-consumption proof.
    /// </summary>
    /// <param name="metadata">The subscription metadata dictionary to inspect.</param>
    /// <returns><see langword="true" /> when complete broker inbound-consumption proof is present; otherwise, <see langword="false" />.</returns>
    public static bool IsBrokerConsumed(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.BrokerInboundConsumption, "provider-reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.BrokerConsumerLoop, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.InboundAcknowledgement, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ConsumerLease, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ConsumerOffsetCheckpoint, "reported") &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.InboundRetryPolicy) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.PoisonMessageHandling) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.BrokerConsumerLoopId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.InboundAcknowledgementId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ConsumerLeaseId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ConsumerOffsetCheckpointId);
    }

    private static bool HasMetadataValue(
        IReadOnlyDictionary<string, string> metadata,
        string key,
        string value)
    {
        var hasValue = metadata.TryGetValue(key, out var candidate) && !string.IsNullOrWhiteSpace(candidate);
        if (!hasValue)
        {
            return false;
        }

        return string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasMeaningfulMetadataValue(
        IReadOnlyDictionary<string, string> metadata,
        string key)
    {
        return metadata.TryGetValue(key, out var candidate) &&
            !string.IsNullOrWhiteSpace(candidate) &&
            !string.Equals(candidate, "not-claimed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(candidate, "not-reported", StringComparison.OrdinalIgnoreCase);
    }

    private static string RequireValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return value.Trim();
    }
}
