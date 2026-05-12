namespace Cephalon.Eventing.Services;

/// <summary>
/// Builds provider-reported subscription idempotency proof metadata for successful subscription reports.
/// </summary>
/// <remarks>
/// Process-local completed-execution suppression and inbox-backed duplicate-completed checks do not automatically
/// prove broker deduplication, exactly-once processing, durable inbox command ownership, generic inbox command
/// ownership, or cross-node idempotency leases. This helper records that stronger provider/runtime proof only when
/// a successful subscription observation supplies the complete idempotency proof set.
/// </remarks>
public static class EventSubscriptionProviderIdempotencyMetadata
{
    /// <summary>
    /// Creates a subscription report copy enriched with provider-reported idempotency proof when the inputs support it.
    /// </summary>
    /// <param name="report">The successful subscription execution report to copy.</param>
    /// <param name="source">The stable provider or runtime source that reported subscription idempotency proof.</param>
    /// <param name="providerIdempotencyKey">The provider idempotency key used to prove duplicate suppression.</param>
    /// <param name="brokerDeduplicationId">The broker deduplication proof id.</param>
    /// <param name="exactlyOnceProofId">The exactly-once subscription processing proof id.</param>
    /// <param name="durableInboxCommandId">The durable inbox command proof id.</param>
    /// <param name="genericInboxCommandId">The generic inbox command proof id.</param>
    /// <param name="idempotencyLeaseId">The cross-node idempotency lease proof id.</param>
    /// <returns>A subscription report containing the original metadata plus provider idempotency proof when applicable.</returns>
    public static EventSubscriptionExecutionReport CreateReport(
        EventSubscriptionExecutionReport report,
        string source,
        string providerIdempotencyKey,
        string brokerDeduplicationId,
        string exactlyOnceProofId,
        string durableInboxCommandId,
        string genericInboxCommandId,
        string idempotencyLeaseId)
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
                providerIdempotencyKey,
                brokerDeduplicationId,
                exactlyOnceProofId,
                durableInboxCommandId,
                genericInboxCommandId,
                idempotencyLeaseId));
    }

    /// <summary>
    /// Creates a metadata copy enriched with provider-reported idempotency proof when the inputs support it.
    /// </summary>
    /// <param name="metadata">The subscription execution metadata to copy.</param>
    /// <param name="outcome">The subscription execution outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported subscription idempotency proof.</param>
    /// <param name="providerIdempotencyKey">The provider idempotency key used to prove duplicate suppression.</param>
    /// <param name="brokerDeduplicationId">The broker deduplication proof id.</param>
    /// <param name="exactlyOnceProofId">The exactly-once subscription processing proof id.</param>
    /// <param name="durableInboxCommandId">The durable inbox command proof id.</param>
    /// <param name="genericInboxCommandId">The generic inbox command proof id.</param>
    /// <param name="idempotencyLeaseId">The cross-node idempotency lease proof id.</param>
    /// <returns>A case-insensitive metadata dictionary containing the original values plus provider idempotency proof when applicable.</returns>
    public static Dictionary<string, string> CreateMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string outcome,
        string source,
        string providerIdempotencyKey,
        string brokerDeduplicationId,
        string exactlyOnceProofId,
        string durableInboxCommandId,
        string genericInboxCommandId,
        string idempotencyLeaseId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var result = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        TryApplyMetadata(
            result,
            outcome,
            source,
            providerIdempotencyKey,
            brokerDeduplicationId,
            exactlyOnceProofId,
            durableInboxCommandId,
            genericInboxCommandId,
            idempotencyLeaseId);
        return result;
    }

    /// <summary>
    /// Applies provider-reported idempotency proof to an existing subscription metadata dictionary when the inputs support it.
    /// </summary>
    /// <param name="metadata">The subscription metadata dictionary to enrich.</param>
    /// <param name="outcome">The subscription execution outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported subscription idempotency proof.</param>
    /// <param name="providerIdempotencyKey">The provider idempotency key used to prove duplicate suppression.</param>
    /// <param name="brokerDeduplicationId">The broker deduplication proof id.</param>
    /// <param name="exactlyOnceProofId">The exactly-once subscription processing proof id.</param>
    /// <param name="durableInboxCommandId">The durable inbox command proof id.</param>
    /// <param name="genericInboxCommandId">The generic inbox command proof id.</param>
    /// <param name="idempotencyLeaseId">The cross-node idempotency lease proof id.</param>
    /// <returns><see langword="true" /> when provider idempotency proof was applied; otherwise, <see langword="false" />.</returns>
    public static bool TryApplyMetadata(
        IDictionary<string, string> metadata,
        string outcome,
        string source,
        string providerIdempotencyKey,
        string brokerDeduplicationId,
        string exactlyOnceProofId,
        string durableInboxCommandId,
        string genericInboxCommandId,
        string idempotencyLeaseId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (!string.Equals(outcome, EventSubscriptionExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedSource = RequireValue(source, nameof(source));
        var normalizedProviderIdempotencyKey = RequireValue(providerIdempotencyKey, nameof(providerIdempotencyKey));
        var normalizedBrokerDeduplicationId = RequireValue(brokerDeduplicationId, nameof(brokerDeduplicationId));
        var normalizedExactlyOnceProofId = RequireValue(exactlyOnceProofId, nameof(exactlyOnceProofId));
        var normalizedDurableInboxCommandId = RequireValue(durableInboxCommandId, nameof(durableInboxCommandId));
        var normalizedGenericInboxCommandId = RequireValue(genericInboxCommandId, nameof(genericInboxCommandId));
        var normalizedIdempotencyLeaseId = RequireValue(idempotencyLeaseId, nameof(idempotencyLeaseId));

        metadata[EventSubscriptionRuntimeMetadataKeys.MessageDeduplication] = "provider-deduplicated";
        metadata[EventSubscriptionRuntimeMetadataKeys.ProviderIdempotency] = "provider-reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.ProviderIdempotencySource] = normalizedSource;
        metadata[EventSubscriptionRuntimeMetadataKeys.ProviderIdempotencyKey] = normalizedProviderIdempotencyKey;
        metadata[EventSubscriptionRuntimeMetadataKeys.BrokerDeduplication] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.BrokerDeduplicationId] = normalizedBrokerDeduplicationId;
        metadata[EventSubscriptionRuntimeMetadataKeys.ExactlyOnceDelivery] = "provider-proven";
        metadata[EventSubscriptionRuntimeMetadataKeys.ExactlyOnceDeliveryProofId] = normalizedExactlyOnceProofId;
        metadata[EventSubscriptionRuntimeMetadataKeys.DurableInboxCommandOwnership] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.DurableInboxCommandId] = normalizedDurableInboxCommandId;
        metadata[EventSubscriptionRuntimeMetadataKeys.GenericInboxCommandOwnership] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.GenericInboxCommandId] = normalizedGenericInboxCommandId;
        metadata[EventSubscriptionRuntimeMetadataKeys.CrossNodeIdempotencyLease] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.CrossNodeIdempotencyLeaseId] = normalizedIdempotencyLeaseId;

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata contains complete provider-reported idempotency proof.
    /// </summary>
    /// <param name="metadata">The subscription metadata dictionary to inspect.</param>
    /// <returns><see langword="true" /> when complete provider idempotency proof is present; otherwise, <see langword="false" />.</returns>
    public static bool IsProviderIdempotencyProven(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.MessageDeduplication, "provider-deduplicated") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ProviderIdempotency, "provider-reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.BrokerDeduplication, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ExactlyOnceDelivery, "provider-proven") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.DurableInboxCommandOwnership, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.GenericInboxCommandOwnership, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.CrossNodeIdempotencyLease, "reported") &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ProviderIdempotencySource) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ProviderIdempotencyKey) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.BrokerDeduplicationId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ExactlyOnceDeliveryProofId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.DurableInboxCommandId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.GenericInboxCommandId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.CrossNodeIdempotencyLeaseId);
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
