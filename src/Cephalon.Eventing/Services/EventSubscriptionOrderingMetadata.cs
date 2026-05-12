namespace Cephalon.Eventing.Services;

/// <summary>
/// Builds provider-reported subscription ordering proof metadata for successful subscription reports.
/// </summary>
/// <remarks>
/// Declared subscriptions, direct in-process execution, middleware, hosted execution links, and provider bindings
/// do not automatically prove handler, fan-out, per-key, partition, causal, replay, or cross-node ordering.
/// This helper records that stronger proof only when a successful subscription observation supplies the
/// complete ordering proof set.
/// </remarks>
public static class EventSubscriptionOrderingMetadata
{
    /// <summary>
    /// Creates a subscription report copy enriched with provider-reported ordering proof when the inputs support it.
    /// </summary>
    /// <param name="report">The successful subscription execution report to copy.</param>
    /// <param name="source">The stable provider or runtime source that reported subscription ordering proof.</param>
    /// <param name="handlerOrderingGuaranteeId">The handler ordering guarantee proof id.</param>
    /// <param name="localFanOutOrderingId">The local fan-out ordering proof id.</param>
    /// <param name="perKeyOrderingKey">The key shape used to prove per-key ordering.</param>
    /// <param name="partitionOrderingId">The partition ordering proof id.</param>
    /// <param name="causalOrderingId">The causal ordering proof id.</param>
    /// <param name="replayOrderingCursorId">The replay ordering cursor proof id.</param>
    /// <param name="crossNodeOrderingId">The cross-node ordering proof id.</param>
    /// <param name="providerOrderingId">The provider ordering proof id.</param>
    /// <param name="crossNodeOrdering">A value indicating whether the provider reported cross-node ordering.</param>
    /// <returns>A subscription report containing the original metadata plus subscription ordering proof when applicable.</returns>
    public static EventSubscriptionExecutionReport CreateReport(
        EventSubscriptionExecutionReport report,
        string source,
        string handlerOrderingGuaranteeId,
        string localFanOutOrderingId,
        string perKeyOrderingKey,
        string partitionOrderingId,
        string causalOrderingId,
        string replayOrderingCursorId,
        string crossNodeOrderingId,
        string providerOrderingId,
        bool crossNodeOrdering = true)
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
                handlerOrderingGuaranteeId,
                localFanOutOrderingId,
                perKeyOrderingKey,
                partitionOrderingId,
                causalOrderingId,
                replayOrderingCursorId,
                crossNodeOrderingId,
                providerOrderingId,
                crossNodeOrdering));
    }

    /// <summary>
    /// Creates a metadata copy enriched with provider-reported ordering proof when the inputs support it.
    /// </summary>
    /// <param name="metadata">The subscription execution metadata to copy.</param>
    /// <param name="outcome">The subscription execution outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported subscription ordering proof.</param>
    /// <param name="handlerOrderingGuaranteeId">The handler ordering guarantee proof id.</param>
    /// <param name="localFanOutOrderingId">The local fan-out ordering proof id.</param>
    /// <param name="perKeyOrderingKey">The key shape used to prove per-key ordering.</param>
    /// <param name="partitionOrderingId">The partition ordering proof id.</param>
    /// <param name="causalOrderingId">The causal ordering proof id.</param>
    /// <param name="replayOrderingCursorId">The replay ordering cursor proof id.</param>
    /// <param name="crossNodeOrderingId">The cross-node ordering proof id.</param>
    /// <param name="providerOrderingId">The provider ordering proof id.</param>
    /// <param name="crossNodeOrdering">A value indicating whether the provider reported cross-node ordering.</param>
    /// <returns>A case-insensitive metadata dictionary containing the original values plus subscription ordering proof when applicable.</returns>
    public static Dictionary<string, string> CreateMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string outcome,
        string source,
        string handlerOrderingGuaranteeId,
        string localFanOutOrderingId,
        string perKeyOrderingKey,
        string partitionOrderingId,
        string causalOrderingId,
        string replayOrderingCursorId,
        string crossNodeOrderingId,
        string providerOrderingId,
        bool crossNodeOrdering = true)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var result = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        TryApplyMetadata(
            result,
            outcome,
            source,
            handlerOrderingGuaranteeId,
            localFanOutOrderingId,
            perKeyOrderingKey,
            partitionOrderingId,
            causalOrderingId,
            replayOrderingCursorId,
            crossNodeOrderingId,
            providerOrderingId,
            crossNodeOrdering);
        return result;
    }

    /// <summary>
    /// Applies provider-reported ordering proof to an existing subscription metadata dictionary when the inputs support it.
    /// </summary>
    /// <param name="metadata">The subscription metadata dictionary to enrich.</param>
    /// <param name="outcome">The subscription execution outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported subscription ordering proof.</param>
    /// <param name="handlerOrderingGuaranteeId">The handler ordering guarantee proof id.</param>
    /// <param name="localFanOutOrderingId">The local fan-out ordering proof id.</param>
    /// <param name="perKeyOrderingKey">The key shape used to prove per-key ordering.</param>
    /// <param name="partitionOrderingId">The partition ordering proof id.</param>
    /// <param name="causalOrderingId">The causal ordering proof id.</param>
    /// <param name="replayOrderingCursorId">The replay ordering cursor proof id.</param>
    /// <param name="crossNodeOrderingId">The cross-node ordering proof id.</param>
    /// <param name="providerOrderingId">The provider ordering proof id.</param>
    /// <param name="crossNodeOrdering">A value indicating whether the provider reported cross-node ordering.</param>
    /// <returns><see langword="true" /> when subscription ordering proof was applied; otherwise, <see langword="false" />.</returns>
    public static bool TryApplyMetadata(
        IDictionary<string, string> metadata,
        string outcome,
        string source,
        string handlerOrderingGuaranteeId,
        string localFanOutOrderingId,
        string perKeyOrderingKey,
        string partitionOrderingId,
        string causalOrderingId,
        string replayOrderingCursorId,
        string crossNodeOrderingId,
        string providerOrderingId,
        bool crossNodeOrdering = true)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (!string.Equals(outcome, EventSubscriptionExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedSource = RequireValue(source, nameof(source));
        var normalizedHandlerOrderingGuaranteeId = RequireValue(handlerOrderingGuaranteeId, nameof(handlerOrderingGuaranteeId));
        var normalizedLocalFanOutOrderingId = RequireValue(localFanOutOrderingId, nameof(localFanOutOrderingId));
        var normalizedPerKeyOrderingKey = RequireValue(perKeyOrderingKey, nameof(perKeyOrderingKey));
        var normalizedPartitionOrderingId = RequireValue(partitionOrderingId, nameof(partitionOrderingId));
        var normalizedCausalOrderingId = RequireValue(causalOrderingId, nameof(causalOrderingId));
        var normalizedReplayOrderingCursorId = RequireValue(replayOrderingCursorId, nameof(replayOrderingCursorId));
        var normalizedCrossNodeOrderingId = RequireValue(crossNodeOrderingId, nameof(crossNodeOrderingId));
        var normalizedProviderOrderingId = RequireValue(providerOrderingId, nameof(providerOrderingId));

        metadata[EventSubscriptionRuntimeMetadataKeys.SubscriptionOrdering] = "provider-reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.SubscriptionOrderingSource] = normalizedSource;
        metadata[EventSubscriptionRuntimeMetadataKeys.HandlerOrderingGuarantee] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.HandlerOrderingGuaranteeId] = normalizedHandlerOrderingGuaranteeId;
        metadata[EventSubscriptionRuntimeMetadataKeys.LocalFanOutOrdering] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.LocalFanOutOrderingId] = normalizedLocalFanOutOrderingId;
        metadata[EventSubscriptionRuntimeMetadataKeys.PerKeyOrdering] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.PerKeyOrderingKey] = normalizedPerKeyOrderingKey;
        metadata[EventSubscriptionRuntimeMetadataKeys.PartitionOrdering] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.PartitionOrderingId] = normalizedPartitionOrderingId;
        metadata[EventSubscriptionRuntimeMetadataKeys.CausalOrdering] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.CausalOrderingId] = normalizedCausalOrderingId;
        metadata[EventSubscriptionRuntimeMetadataKeys.ReplayOrdering] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.ReplayOrderingCursorId] = normalizedReplayOrderingCursorId;
        metadata[EventSubscriptionRuntimeMetadataKeys.CrossNodeOrdering] = crossNodeOrdering ? "reported" : "not-claimed";
        metadata[EventSubscriptionRuntimeMetadataKeys.CrossNodeOrderingId] = normalizedCrossNodeOrderingId;
        metadata[EventSubscriptionRuntimeMetadataKeys.ProviderOrdering] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.ProviderOrderingId] = normalizedProviderOrderingId;

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata contains complete provider-reported subscription ordering proof.
    /// </summary>
    /// <param name="metadata">The subscription metadata dictionary to inspect.</param>
    /// <returns><see langword="true" /> when complete subscription ordering proof is present; otherwise, <see langword="false" />.</returns>
    public static bool IsOrderingProven(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.SubscriptionOrdering, "provider-reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.HandlerOrderingGuarantee, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.LocalFanOutOrdering, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.PerKeyOrdering, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.PartitionOrdering, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.CausalOrdering, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ReplayOrdering, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.CrossNodeOrdering, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ProviderOrdering, "reported") &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.SubscriptionOrderingSource) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.HandlerOrderingGuaranteeId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.LocalFanOutOrderingId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.PerKeyOrderingKey) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.PartitionOrderingId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.CausalOrderingId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ReplayOrderingCursorId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.CrossNodeOrderingId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ProviderOrderingId);
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
