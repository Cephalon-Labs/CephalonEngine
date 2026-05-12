namespace Cephalon.Eventing.Services;

/// <summary>
/// Builds provider-reported scheduled and delayed delivery proof metadata for successful dispatch reports.
/// </summary>
/// <remarks>
/// Cephalon's process-local publication scheduler proves only bounded delayed acceptance. This helper records a
/// stronger provider-scheduler claim only when a provider/runtime reports successful dispatch evidence with durable
/// scheduled delivery, provider delay queue, broker scheduling, cross-node coordination, and recovery proof ids.
/// </remarks>
public static class EventDispatchScheduledDeliveryMetadata
{
    /// <summary>
    /// Creates a dispatch report copy enriched with provider-reported scheduled delivery proof when the inputs support it.
    /// </summary>
    /// <param name="report">The successful dispatch report to copy.</param>
    /// <param name="source">The stable provider or runtime source that reported scheduled delivery ownership.</param>
    /// <param name="durableScheduledDeliveryId">The durable scheduled delivery proof id.</param>
    /// <param name="providerDelayQueueId">The provider-owned delay queue proof id.</param>
    /// <param name="brokerScheduledDeliveryId">The broker-native scheduled delivery proof id.</param>
    /// <param name="scheduleCoordinationId">The cross-node schedule coordination proof id.</param>
    /// <param name="scheduleRecoveryId">The scheduled-delivery recovery proof id.</param>
    /// <returns>A dispatch report containing the original metadata plus scheduled delivery proof when applicable.</returns>
    public static EventDispatchExecutionReport CreateReport(
        EventDispatchExecutionReport report,
        string source,
        string durableScheduledDeliveryId,
        string providerDelayQueueId,
        string brokerScheduledDeliveryId,
        string scheduleCoordinationId,
        string scheduleRecoveryId)
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
                durableScheduledDeliveryId,
                providerDelayQueueId,
                brokerScheduledDeliveryId,
                scheduleCoordinationId,
                scheduleRecoveryId));
    }

    /// <summary>
    /// Creates a metadata copy enriched with provider-reported scheduled delivery proof when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch report metadata to copy.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported scheduled delivery ownership.</param>
    /// <param name="durableScheduledDeliveryId">The durable scheduled delivery proof id.</param>
    /// <param name="providerDelayQueueId">The provider-owned delay queue proof id.</param>
    /// <param name="brokerScheduledDeliveryId">The broker-native scheduled delivery proof id.</param>
    /// <param name="scheduleCoordinationId">The cross-node schedule coordination proof id.</param>
    /// <param name="scheduleRecoveryId">The scheduled-delivery recovery proof id.</param>
    /// <returns>A case-insensitive metadata dictionary containing the original values plus scheduled delivery proof when applicable.</returns>
    public static Dictionary<string, string> CreateMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string outcome,
        string source,
        string durableScheduledDeliveryId,
        string providerDelayQueueId,
        string brokerScheduledDeliveryId,
        string scheduleCoordinationId,
        string scheduleRecoveryId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var result = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        TryApplyMetadata(
            result,
            outcome,
            source,
            durableScheduledDeliveryId,
            providerDelayQueueId,
            brokerScheduledDeliveryId,
            scheduleCoordinationId,
            scheduleRecoveryId);
        return result;
    }

    /// <summary>
    /// Applies provider-reported scheduled delivery proof to an existing dispatch metadata dictionary when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to enrich.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported scheduled delivery ownership.</param>
    /// <param name="durableScheduledDeliveryId">The durable scheduled delivery proof id.</param>
    /// <param name="providerDelayQueueId">The provider-owned delay queue proof id.</param>
    /// <param name="brokerScheduledDeliveryId">The broker-native scheduled delivery proof id.</param>
    /// <param name="scheduleCoordinationId">The cross-node schedule coordination proof id.</param>
    /// <param name="scheduleRecoveryId">The scheduled-delivery recovery proof id.</param>
    /// <returns><see langword="true" /> when scheduled delivery proof was applied; otherwise, <see langword="false" />.</returns>
    public static bool TryApplyMetadata(
        IDictionary<string, string> metadata,
        string outcome,
        string source,
        string durableScheduledDeliveryId,
        string providerDelayQueueId,
        string brokerScheduledDeliveryId,
        string scheduleCoordinationId,
        string scheduleRecoveryId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (!string.Equals(outcome, EventDispatchExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedSource = RequireValue(source, nameof(source));
        var normalizedDurableScheduledDeliveryId = RequireValue(durableScheduledDeliveryId, nameof(durableScheduledDeliveryId));
        var normalizedProviderDelayQueueId = RequireValue(providerDelayQueueId, nameof(providerDelayQueueId));
        var normalizedBrokerScheduledDeliveryId = RequireValue(brokerScheduledDeliveryId, nameof(brokerScheduledDeliveryId));
        var normalizedScheduleCoordinationId = RequireValue(scheduleCoordinationId, nameof(scheduleCoordinationId));
        var normalizedScheduleRecoveryId = RequireValue(scheduleRecoveryId, nameof(scheduleRecoveryId));

        metadata[EventDispatchRuntimeMetadataKeys.ScheduledDeliveryOwnership] = "provider-reported";
        metadata[EventDispatchRuntimeMetadataKeys.ScheduledDeliveryOwnershipSource] = normalizedSource;
        metadata[EventDispatchRuntimeMetadataKeys.ScheduleDurability] = "durable";
        metadata[EventDispatchRuntimeMetadataKeys.ScheduleScope] = "cross-node";
        metadata[EventDispatchRuntimeMetadataKeys.DurableScheduledDelivery] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.DurableScheduledDeliveryId] = normalizedDurableScheduledDeliveryId;
        metadata[EventDispatchRuntimeMetadataKeys.ProviderDelayQueue] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.ProviderDelayQueueId] = normalizedProviderDelayQueueId;
        metadata[EventDispatchRuntimeMetadataKeys.BrokerScheduledDelivery] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.BrokerScheduledDeliveryId] = normalizedBrokerScheduledDeliveryId;
        metadata[EventDispatchRuntimeMetadataKeys.CrossNodeScheduleCoordination] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.ScheduleCoordinationId] = normalizedScheduleCoordinationId;
        metadata[EventDispatchRuntimeMetadataKeys.ScheduleRecovery] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.ScheduleRecoveryId] = normalizedScheduleRecoveryId;

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata contains complete provider-reported scheduled delivery proof.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to inspect.</param>
    /// <returns><see langword="true" /> when complete scheduled delivery proof is present; otherwise, <see langword="false" />.</returns>
    public static bool IsScheduledDeliveryProven(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return HasValue(metadata, EventDispatchRuntimeMetadataKeys.ScheduledDeliveryOwnership, "provider-reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.ScheduledDeliveryOwnershipSource) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.ScheduleDurability, "durable") &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.ScheduleScope, "cross-node") &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.DurableScheduledDelivery, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.DurableScheduledDeliveryId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.ProviderDelayQueue, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.ProviderDelayQueueId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.BrokerScheduledDelivery, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.BrokerScheduledDeliveryId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.CrossNodeScheduleCoordination, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.ScheduleCoordinationId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.ScheduleRecovery, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.ScheduleRecoveryId);
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
