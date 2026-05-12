namespace Cephalon.Eventing.Services;

/// <summary>
/// Builds provider-reported partition ownership proof metadata for successful dispatch reports.
/// </summary>
/// <remarks>
/// Cephalon publication routing proves logical channel selection, not provider partition placement. This helper records
/// the stronger provider partition claim only when a provider/runtime reports successful dispatch evidence with
/// assignment, affinity, rebalancing, ordering, and provider-owned partitioning proof ids.
/// </remarks>
public static class EventDispatchProviderPartitionMetadata
{
    /// <summary>
    /// Creates a dispatch report copy enriched with provider-reported partition ownership proof when the inputs support it.
    /// </summary>
    /// <param name="report">The successful dispatch report to copy.</param>
    /// <param name="source">The stable provider or runtime source that reported provider partition ownership.</param>
    /// <param name="partitionAssignmentId">The provider partition assignment proof id.</param>
    /// <param name="partitionAffinityId">The provider partition affinity proof id.</param>
    /// <param name="partitionRebalancingId">The provider partition rebalancing proof id.</param>
    /// <param name="partitionOrderingGuaranteeId">The provider partition ordering guarantee proof id.</param>
    /// <param name="providerPartitioningId">The provider-owned partitioning proof id.</param>
    /// <returns>A dispatch report containing the original metadata plus provider partition proof when applicable.</returns>
    public static EventDispatchExecutionReport CreateReport(
        EventDispatchExecutionReport report,
        string source,
        string partitionAssignmentId,
        string partitionAffinityId,
        string partitionRebalancingId,
        string partitionOrderingGuaranteeId,
        string providerPartitioningId)
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
                partitionAssignmentId,
                partitionAffinityId,
                partitionRebalancingId,
                partitionOrderingGuaranteeId,
                providerPartitioningId));
    }

    /// <summary>
    /// Creates a metadata copy enriched with provider-reported partition ownership proof when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch report metadata to copy.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported provider partition ownership.</param>
    /// <param name="partitionAssignmentId">The provider partition assignment proof id.</param>
    /// <param name="partitionAffinityId">The provider partition affinity proof id.</param>
    /// <param name="partitionRebalancingId">The provider partition rebalancing proof id.</param>
    /// <param name="partitionOrderingGuaranteeId">The provider partition ordering guarantee proof id.</param>
    /// <param name="providerPartitioningId">The provider-owned partitioning proof id.</param>
    /// <returns>A case-insensitive metadata dictionary containing the original values plus provider partition proof when applicable.</returns>
    public static Dictionary<string, string> CreateMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string outcome,
        string source,
        string partitionAssignmentId,
        string partitionAffinityId,
        string partitionRebalancingId,
        string partitionOrderingGuaranteeId,
        string providerPartitioningId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var result = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        TryApplyMetadata(
            result,
            outcome,
            source,
            partitionAssignmentId,
            partitionAffinityId,
            partitionRebalancingId,
            partitionOrderingGuaranteeId,
            providerPartitioningId);
        return result;
    }

    /// <summary>
    /// Applies provider-reported partition ownership proof to an existing dispatch metadata dictionary when the inputs support it.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to enrich.</param>
    /// <param name="outcome">The dispatch report outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported provider partition ownership.</param>
    /// <param name="partitionAssignmentId">The provider partition assignment proof id.</param>
    /// <param name="partitionAffinityId">The provider partition affinity proof id.</param>
    /// <param name="partitionRebalancingId">The provider partition rebalancing proof id.</param>
    /// <param name="partitionOrderingGuaranteeId">The provider partition ordering guarantee proof id.</param>
    /// <param name="providerPartitioningId">The provider-owned partitioning proof id.</param>
    /// <returns><see langword="true" /> when provider partition proof was applied; otherwise, <see langword="false" />.</returns>
    public static bool TryApplyMetadata(
        IDictionary<string, string> metadata,
        string outcome,
        string source,
        string partitionAssignmentId,
        string partitionAffinityId,
        string partitionRebalancingId,
        string partitionOrderingGuaranteeId,
        string providerPartitioningId)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (!string.Equals(outcome, EventDispatchExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedSource = RequireValue(source, nameof(source));
        var normalizedPartitionAssignmentId = RequireValue(partitionAssignmentId, nameof(partitionAssignmentId));
        var normalizedPartitionAffinityId = RequireValue(partitionAffinityId, nameof(partitionAffinityId));
        var normalizedPartitionRebalancingId = RequireValue(partitionRebalancingId, nameof(partitionRebalancingId));
        var normalizedPartitionOrderingGuaranteeId = RequireValue(partitionOrderingGuaranteeId, nameof(partitionOrderingGuaranteeId));
        var normalizedProviderPartitioningId = RequireValue(providerPartitioningId, nameof(providerPartitioningId));

        metadata[EventDispatchRuntimeMetadataKeys.ProviderPartitionOwnership] = "provider-reported";
        metadata[EventDispatchRuntimeMetadataKeys.ProviderPartitionOwnershipSource] = normalizedSource;
        metadata[EventDispatchRuntimeMetadataKeys.PartitionAssignment] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.PartitionAssignmentId] = normalizedPartitionAssignmentId;
        metadata[EventDispatchRuntimeMetadataKeys.PartitionAffinity] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.PartitionAffinityId] = normalizedPartitionAffinityId;
        metadata[EventDispatchRuntimeMetadataKeys.PartitionRebalancing] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.PartitionRebalancingId] = normalizedPartitionRebalancingId;
        metadata[EventDispatchRuntimeMetadataKeys.PartitionOrderingGuarantee] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.PartitionOrderingGuaranteeId] = normalizedPartitionOrderingGuaranteeId;
        metadata[EventDispatchRuntimeMetadataKeys.ProviderOwnedPartitioning] = "reported";
        metadata[EventDispatchRuntimeMetadataKeys.ProviderPartitioningId] = normalizedProviderPartitioningId;

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata contains complete provider-reported partition ownership proof.
    /// </summary>
    /// <param name="metadata">The dispatch metadata dictionary to inspect.</param>
    /// <returns><see langword="true" /> when complete provider partition proof is present; otherwise, <see langword="false" />.</returns>
    public static bool IsPartitionOwnershipProven(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return HasValue(metadata, EventDispatchRuntimeMetadataKeys.ProviderPartitionOwnership, "provider-reported") &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.PartitionAssignment, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.PartitionAssignmentId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.PartitionAffinity, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.PartitionAffinityId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.PartitionRebalancing, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.PartitionRebalancingId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.PartitionOrderingGuarantee, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.PartitionOrderingGuaranteeId) &&
            HasValue(metadata, EventDispatchRuntimeMetadataKeys.ProviderOwnedPartitioning, "reported") &&
            HasNonEmpty(metadata, EventDispatchRuntimeMetadataKeys.ProviderPartitioningId);
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
