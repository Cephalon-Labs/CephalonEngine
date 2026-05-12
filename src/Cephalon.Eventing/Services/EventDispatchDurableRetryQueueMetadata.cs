namespace Cephalon.Eventing.Services;

/// <summary>
/// Builds provider-reported durable retry queue proof metadata for retry-scheduled dispatch reports.
/// </summary>
/// <remarks>
/// Retry-scheduled dispatch observations only say the active dispatcher decided to retry. This helper records a
/// stronger, provider-reported durable retry proof only when the report outcome is <c>retry-scheduled</c> and the
/// provider supplies queue, persistence, broker error queue, poison queue, coordination, and lease evidence.
/// Cephalon carries the proof without making Wolverine or any other provider package part of the core surface.
/// </remarks>
public static class EventDispatchDurableRetryQueueMetadata
{
    /// <summary>
    /// Creates a dispatch report copy enriched with provider-reported durable retry queue proof when the inputs support it.
    /// </summary>
    /// <param name="report">The retry-scheduled dispatch report to copy.</param>
    /// <param name="source">The stable provider or runtime source that reported durable retry queue ownership.</param>
    /// <param name="durableRetryQueueId">The durable retry queue id.</param>
    /// <param name="retryPersistenceId">The retry persistence record or store id.</param>
    /// <param name="brokerErrorQueueId">The broker error queue id.</param>
    /// <param name="poisonQueueId">The poison queue id.</param>
    /// <param name="retryCoordinationId">The cross-node retry coordination id.</param>
    /// <param name="retryLeaseId">The retry lease id.</param>
    /// <returns>A dispatch report containing the original metadata plus durable retry queue proof when applicable.</returns>
    public static EventDispatchExecutionReport CreateReport(
        EventDispatchExecutionReport report,
        string source,
        string durableRetryQueueId,
        string retryPersistenceId,
        string brokerErrorQueueId,
        string poisonQueueId,
        string retryCoordinationId,
        string retryLeaseId)
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
                durableRetryQueueId,
                retryPersistenceId,
                brokerErrorQueueId,
                poisonQueueId,
                retryCoordinationId,
                retryLeaseId));
    }

    /// <summary>
    /// Creates a metadata copy enriched with provider-reported durable retry queue proof when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch report metadata to copy.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported durable retry queue ownership.</param>
    /// <param name="durableRetryQueueId">The durable retry queue id.</param>
    /// <param name="retryPersistenceId">The retry persistence record or store id.</param>
    /// <param name="brokerErrorQueueId">The broker error queue id.</param>
    /// <param name="poisonQueueId">The poison queue id.</param>
    /// <param name="retryCoordinationId">The cross-node retry coordination id.</param>
    /// <param name="retryLeaseId">The retry lease id.</param>
    /// <returns>A case-insensitive metadata dictionary containing the original values plus durable retry queue proof when applicable.</returns>
    public static Dictionary<string, string> CreateMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string outcome,
        string source,
        string durableRetryQueueId,
        string retryPersistenceId,
        string brokerErrorQueueId,
        string poisonQueueId,
        string retryCoordinationId,
        string retryLeaseId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var result = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        TryApplyMetadata(
            result,
            outcome,
            source,
            durableRetryQueueId,
            retryPersistenceId,
            brokerErrorQueueId,
            poisonQueueId,
            retryCoordinationId,
            retryLeaseId);
        return result;
    }

    /// <summary>
    /// Applies provider-reported durable retry queue proof to an existing dispatch metadata dictionary when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to enrich.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported durable retry queue ownership.</param>
    /// <param name="durableRetryQueueId">The durable retry queue id.</param>
    /// <param name="retryPersistenceId">The retry persistence record or store id.</param>
    /// <param name="brokerErrorQueueId">The broker error queue id.</param>
    /// <param name="poisonQueueId">The poison queue id.</param>
    /// <param name="retryCoordinationId">The cross-node retry coordination id.</param>
    /// <param name="retryLeaseId">The retry lease id.</param>
    /// <returns><see langword="true" /> when durable retry queue proof was applied; otherwise, <see langword="false" />.</returns>
    public static bool TryApplyMetadata(
        IDictionary<string, string> metadata,
        string outcome,
        string source,
        string durableRetryQueueId,
        string retryPersistenceId,
        string brokerErrorQueueId,
        string poisonQueueId,
        string retryCoordinationId,
        string retryLeaseId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (!string.Equals(outcome, EventDispatchExecutionOutcomes.RetryScheduled, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedSource = RequireValue(source, nameof(source));
        var normalizedDurableRetryQueueId = RequireValue(durableRetryQueueId, nameof(durableRetryQueueId));
        var normalizedRetryPersistenceId = RequireValue(retryPersistenceId, nameof(retryPersistenceId));
        var normalizedBrokerErrorQueueId = RequireValue(brokerErrorQueueId, nameof(brokerErrorQueueId));
        var normalizedPoisonQueueId = RequireValue(poisonQueueId, nameof(poisonQueueId));
        var normalizedRetryCoordinationId = RequireValue(retryCoordinationId, nameof(retryCoordinationId));
        var normalizedRetryLeaseId = RequireValue(retryLeaseId, nameof(retryLeaseId));

        metadata[EventDispatchRuntimeMetadataKeys.DurableRetryQueue] = "provider-reported";
        metadata[EventDispatchRuntimeMetadataKeys.DurableRetryQueueSource] = normalizedSource;
        metadata[EventDispatchRuntimeMetadataKeys.DurableRetryQueueId] = normalizedDurableRetryQueueId;
        metadata[EventDispatchRuntimeMetadataKeys.RetryDurability] = "durable";
        metadata[EventDispatchRuntimeMetadataKeys.RetryScope] = "cross-node";
        metadata[EventDispatchRuntimeMetadataKeys.RetryPersistence] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.RetryPersistenceId] = normalizedRetryPersistenceId;
        metadata[EventDispatchRuntimeMetadataKeys.BrokerErrorQueue] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.BrokerErrorQueueId] = normalizedBrokerErrorQueueId;
        metadata[EventDispatchRuntimeMetadataKeys.PoisonQueueOwnership] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.PoisonQueueId] = normalizedPoisonQueueId;
        metadata[EventDispatchRuntimeMetadataKeys.CrossNodeRetryCoordination] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.RetryCoordinationId] = normalizedRetryCoordinationId;
        metadata[EventDispatchRuntimeMetadataKeys.RetryLease] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.RetryLeaseId] = normalizedRetryLeaseId;

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata contains provider-reported durable retry queue proof.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to inspect.</param>
    /// <returns><see langword="true" /> when complete durable retry queue proof is present; otherwise, <see langword="false" />.</returns>
    public static bool IsDurableRetryQueueProven(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return HasValue(metadata, EventDispatchRuntimeMetadataKeys.DurableRetryQueue, "provider-reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.DurableRetryQueueId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.RetryDurability, "durable") &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.RetryPersistence, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.RetryPersistenceId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.BrokerErrorQueue, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.BrokerErrorQueueId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.PoisonQueueOwnership, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.PoisonQueueId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.CrossNodeRetryCoordination, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.RetryCoordinationId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.RetryLease, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.RetryLeaseId);
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
