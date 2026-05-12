namespace Cephalon.Eventing.Services;

/// <summary>
/// Builds provider-reported broker topology materialization proof metadata for successful dispatch reports.
/// </summary>
/// <remarks>
/// Cephalon publication routing proves logical channel selection, not broker provisioning. This helper records the
/// stronger broker topology claim only when a provider/runtime reports successful dispatch evidence with exchange,
/// queue, topic, partition, verification, and provider-owned topology proof ids.
/// </remarks>
public static class EventDispatchBrokerTopologyMetadata
{
    /// <summary>
    /// Creates a dispatch report copy enriched with provider-reported broker topology proof when the inputs support it.
    /// </summary>
    /// <param name="report">The successful dispatch report to copy.</param>
    /// <param name="source">The stable provider or runtime source that reported broker topology materialization.</param>
    /// <param name="exchangeProvisioningId">The exchange provisioning proof id.</param>
    /// <param name="queueProvisioningId">The queue provisioning proof id.</param>
    /// <param name="topicProvisioningId">The topic provisioning proof id.</param>
    /// <param name="partitionProvisioningId">The partition provisioning proof id.</param>
    /// <param name="topologyVerificationId">The topology verification proof id.</param>
    /// <param name="providerTopologyId">The provider-owned topology proof id.</param>
    /// <returns>A dispatch report containing the original metadata plus broker topology proof when applicable.</returns>
    public static EventDispatchExecutionReport CreateReport(
        EventDispatchExecutionReport report,
        string source,
        string exchangeProvisioningId,
        string queueProvisioningId,
        string topicProvisioningId,
        string partitionProvisioningId,
        string topologyVerificationId,
        string providerTopologyId)
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
                exchangeProvisioningId,
                queueProvisioningId,
                topicProvisioningId,
                partitionProvisioningId,
                topologyVerificationId,
                providerTopologyId));
    }

    /// <summary>
    /// Creates a metadata copy enriched with provider-reported broker topology proof when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch report metadata to copy.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported broker topology materialization.</param>
    /// <param name="exchangeProvisioningId">The exchange provisioning proof id.</param>
    /// <param name="queueProvisioningId">The queue provisioning proof id.</param>
    /// <param name="topicProvisioningId">The topic provisioning proof id.</param>
    /// <param name="partitionProvisioningId">The partition provisioning proof id.</param>
    /// <param name="topologyVerificationId">The topology verification proof id.</param>
    /// <param name="providerTopologyId">The provider-owned topology proof id.</param>
    /// <returns>A case-insensitive metadata dictionary containing the original values plus broker topology proof when applicable.</returns>
    public static Dictionary<string, string> CreateMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string outcome,
        string source,
        string exchangeProvisioningId,
        string queueProvisioningId,
        string topicProvisioningId,
        string partitionProvisioningId,
        string topologyVerificationId,
        string providerTopologyId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var result = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        TryApplyMetadata(
            result,
            outcome,
            source,
            exchangeProvisioningId,
            queueProvisioningId,
            topicProvisioningId,
            partitionProvisioningId,
            topologyVerificationId,
            providerTopologyId);
        return result;
    }

    /// <summary>
    /// Applies provider-reported broker topology proof to an existing dispatch metadata dictionary when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to enrich.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported broker topology materialization.</param>
    /// <param name="exchangeProvisioningId">The exchange provisioning proof id.</param>
    /// <param name="queueProvisioningId">The queue provisioning proof id.</param>
    /// <param name="topicProvisioningId">The topic provisioning proof id.</param>
    /// <param name="partitionProvisioningId">The partition provisioning proof id.</param>
    /// <param name="topologyVerificationId">The topology verification proof id.</param>
    /// <param name="providerTopologyId">The provider-owned topology proof id.</param>
    /// <returns><see langword="true" /> when broker topology proof was applied; otherwise, <see langword="false" />.</returns>
    public static bool TryApplyMetadata(
        IDictionary<string, string> metadata,
        string outcome,
        string source,
        string exchangeProvisioningId,
        string queueProvisioningId,
        string topicProvisioningId,
        string partitionProvisioningId,
        string topologyVerificationId,
        string providerTopologyId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (!string.Equals(outcome, EventDispatchExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedSource = RequireValue(source, nameof(source));
        var normalizedExchangeProvisioningId = RequireValue(exchangeProvisioningId, nameof(exchangeProvisioningId));
        var normalizedQueueProvisioningId = RequireValue(queueProvisioningId, nameof(queueProvisioningId));
        var normalizedTopicProvisioningId = RequireValue(topicProvisioningId, nameof(topicProvisioningId));
        var normalizedPartitionProvisioningId = RequireValue(partitionProvisioningId, nameof(partitionProvisioningId));
        var normalizedTopologyVerificationId = RequireValue(topologyVerificationId, nameof(topologyVerificationId));
        var normalizedProviderTopologyId = RequireValue(providerTopologyId, nameof(providerTopologyId));

        metadata[EventDispatchRuntimeMetadataKeys.BrokerTopologyMaterialization] = "provider-reported";
        metadata[EventDispatchRuntimeMetadataKeys.BrokerTopologyMaterializationSource] = normalizedSource;
        metadata[EventDispatchRuntimeMetadataKeys.ExchangeProvisioning] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.ExchangeProvisioningId] = normalizedExchangeProvisioningId;
        metadata[EventDispatchRuntimeMetadataKeys.QueueProvisioning] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.QueueProvisioningId] = normalizedQueueProvisioningId;
        metadata[EventDispatchRuntimeMetadataKeys.TopicProvisioning] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.TopicProvisioningId] = normalizedTopicProvisioningId;
        metadata[EventDispatchRuntimeMetadataKeys.PartitionProvisioning] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.PartitionProvisioningId] = normalizedPartitionProvisioningId;
        metadata[EventDispatchRuntimeMetadataKeys.TopologyVerification] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.TopologyVerificationId] = normalizedTopologyVerificationId;
        metadata[EventDispatchRuntimeMetadataKeys.ProviderOwnedTopology] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.ProviderTopologyId] = normalizedProviderTopologyId;

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata contains complete provider-reported broker topology proof.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to inspect.</param>
    /// <returns><see langword="true" /> when complete broker topology proof is present; otherwise, <see langword="false" />.</returns>
    public static bool IsTopologyMaterialized(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return HasValue(metadata, EventDispatchRuntimeMetadataKeys.BrokerTopologyMaterialization, "provider-reported") &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.ExchangeProvisioning, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.ExchangeProvisioningId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.QueueProvisioning, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.QueueProvisioningId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.TopicProvisioning, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.TopicProvisioningId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.PartitionProvisioning, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.PartitionProvisioningId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.TopologyVerification, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.TopologyVerificationId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.ProviderOwnedTopology, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.ProviderTopologyId);
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
