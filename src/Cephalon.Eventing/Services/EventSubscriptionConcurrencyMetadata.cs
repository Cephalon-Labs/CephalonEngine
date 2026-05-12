using System.Globalization;

namespace Cephalon.Eventing.Services;

/// <summary>
/// Builds provider-reported subscription concurrency proof metadata for successful subscription reports.
/// </summary>
/// <remarks>
/// Declared subscriptions, direct in-process execution, middleware, hosted execution links, and provider bindings
/// do not automatically prove provider-owned concurrency, prefetch, backpressure, leasing, work stealing, or
/// distributed work sharing. This helper records that stronger proof only when a successful subscription observation
/// supplies the complete concurrency proof set.
/// </remarks>
public static class EventSubscriptionConcurrencyMetadata
{
    /// <summary>
    /// Creates a subscription report copy enriched with provider-reported concurrency proof when the inputs support it.
    /// </summary>
    /// <param name="report">The successful subscription execution report to copy.</param>
    /// <param name="source">The stable provider or runtime source that reported subscription concurrency proof.</param>
    /// <param name="perSubscriptionConcurrencyLimit">The reported positive per-subscription concurrency limit.</param>
    /// <param name="consumerPrefetchCount">The reported positive consumer prefetch count.</param>
    /// <param name="backpressureStrategy">The reported backpressure strategy.</param>
    /// <param name="providerConcurrencyId">The provider concurrency proof id.</param>
    /// <param name="consumerLeaseId">The consumer lease proof id.</param>
    /// <param name="workStealingId">The work-stealing proof id.</param>
    /// <param name="distributedWorkSharingId">The distributed work-sharing proof id.</param>
    /// <param name="parallelHandlerExecution">A value indicating whether the provider reported parallel handler execution.</param>
    /// <returns>A subscription report containing the original metadata plus subscription concurrency proof when applicable.</returns>
    public static EventSubscriptionExecutionReport CreateReport(
        EventSubscriptionExecutionReport report,
        string source,
        int perSubscriptionConcurrencyLimit,
        int consumerPrefetchCount,
        string backpressureStrategy,
        string providerConcurrencyId,
        string consumerLeaseId,
        string workStealingId,
        string distributedWorkSharingId,
        bool parallelHandlerExecution = true)
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
                perSubscriptionConcurrencyLimit,
                consumerPrefetchCount,
                backpressureStrategy,
                providerConcurrencyId,
                consumerLeaseId,
                workStealingId,
                distributedWorkSharingId,
                parallelHandlerExecution));
    }

    /// <summary>
    /// Creates a metadata copy enriched with provider-reported concurrency proof when the inputs support it.
    /// </summary>
    /// <param name="metadata">The subscription execution metadata to copy.</param>
    /// <param name="outcome">The subscription execution outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported subscription concurrency proof.</param>
    /// <param name="perSubscriptionConcurrencyLimit">The reported positive per-subscription concurrency limit.</param>
    /// <param name="consumerPrefetchCount">The reported positive consumer prefetch count.</param>
    /// <param name="backpressureStrategy">The reported backpressure strategy.</param>
    /// <param name="providerConcurrencyId">The provider concurrency proof id.</param>
    /// <param name="consumerLeaseId">The consumer lease proof id.</param>
    /// <param name="workStealingId">The work-stealing proof id.</param>
    /// <param name="distributedWorkSharingId">The distributed work-sharing proof id.</param>
    /// <param name="parallelHandlerExecution">A value indicating whether the provider reported parallel handler execution.</param>
    /// <returns>A case-insensitive metadata dictionary containing the original values plus subscription concurrency proof when applicable.</returns>
    public static Dictionary<string, string> CreateMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string outcome,
        string source,
        int perSubscriptionConcurrencyLimit,
        int consumerPrefetchCount,
        string backpressureStrategy,
        string providerConcurrencyId,
        string consumerLeaseId,
        string workStealingId,
        string distributedWorkSharingId,
        bool parallelHandlerExecution = true)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var result = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        TryApplyMetadata(
            result,
            outcome,
            source,
            perSubscriptionConcurrencyLimit,
            consumerPrefetchCount,
            backpressureStrategy,
            providerConcurrencyId,
            consumerLeaseId,
            workStealingId,
            distributedWorkSharingId,
            parallelHandlerExecution);
        return result;
    }

    /// <summary>
    /// Applies provider-reported concurrency proof to an existing subscription metadata dictionary when the inputs support it.
    /// </summary>
    /// <param name="metadata">The subscription metadata dictionary to enrich.</param>
    /// <param name="outcome">The subscription execution outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported subscription concurrency proof.</param>
    /// <param name="perSubscriptionConcurrencyLimit">The reported positive per-subscription concurrency limit.</param>
    /// <param name="consumerPrefetchCount">The reported positive consumer prefetch count.</param>
    /// <param name="backpressureStrategy">The reported backpressure strategy.</param>
    /// <param name="providerConcurrencyId">The provider concurrency proof id.</param>
    /// <param name="consumerLeaseId">The consumer lease proof id.</param>
    /// <param name="workStealingId">The work-stealing proof id.</param>
    /// <param name="distributedWorkSharingId">The distributed work-sharing proof id.</param>
    /// <param name="parallelHandlerExecution">A value indicating whether the provider reported parallel handler execution.</param>
    /// <returns><see langword="true" /> when subscription concurrency proof was applied; otherwise, <see langword="false" />.</returns>
    public static bool TryApplyMetadata(
        IDictionary<string, string> metadata,
        string outcome,
        string source,
        int perSubscriptionConcurrencyLimit,
        int consumerPrefetchCount,
        string backpressureStrategy,
        string providerConcurrencyId,
        string consumerLeaseId,
        string workStealingId,
        string distributedWorkSharingId,
        bool parallelHandlerExecution = true)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (!string.Equals(outcome, EventSubscriptionExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedSource = RequireValue(source, nameof(source));
        var normalizedBackpressureStrategy = RequireValue(backpressureStrategy, nameof(backpressureStrategy));
        var normalizedProviderConcurrencyId = RequireValue(providerConcurrencyId, nameof(providerConcurrencyId));
        var normalizedConsumerLeaseId = RequireValue(consumerLeaseId, nameof(consumerLeaseId));
        var normalizedWorkStealingId = RequireValue(workStealingId, nameof(workStealingId));
        var normalizedDistributedWorkSharingId = RequireValue(distributedWorkSharingId, nameof(distributedWorkSharingId));
        var normalizedConcurrencyLimit = RequirePositive(perSubscriptionConcurrencyLimit, nameof(perSubscriptionConcurrencyLimit));
        var normalizedPrefetchCount = RequirePositive(consumerPrefetchCount, nameof(consumerPrefetchCount));

        metadata[EventSubscriptionRuntimeMetadataKeys.SubscriptionConcurrency] = "provider-reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.SubscriptionConcurrencySource] = normalizedSource;
        metadata[EventSubscriptionRuntimeMetadataKeys.PerSubscriptionConcurrencyLimit] = normalizedConcurrencyLimit.ToString(CultureInfo.InvariantCulture);
        metadata[EventSubscriptionRuntimeMetadataKeys.ParallelHandlerExecution] = parallelHandlerExecution ? "reported" : "not-claimed";
        metadata[EventSubscriptionRuntimeMetadataKeys.ConsumerPrefetch] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.ConsumerPrefetchCount] = normalizedPrefetchCount.ToString(CultureInfo.InvariantCulture);
        metadata[EventSubscriptionRuntimeMetadataKeys.Backpressure] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.BackpressureStrategy] = normalizedBackpressureStrategy;
        metadata[EventSubscriptionRuntimeMetadataKeys.ProviderConcurrency] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.ProviderConcurrencyId] = normalizedProviderConcurrencyId;
        metadata[EventSubscriptionRuntimeMetadataKeys.ConsumerLease] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.ConsumerLeaseId] = normalizedConsumerLeaseId;
        metadata[EventSubscriptionRuntimeMetadataKeys.WorkStealing] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.WorkStealingId] = normalizedWorkStealingId;
        metadata[EventSubscriptionRuntimeMetadataKeys.DistributedWorkSharing] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.DistributedWorkSharingId] = normalizedDistributedWorkSharingId;

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata contains complete provider-reported subscription concurrency proof.
    /// </summary>
    /// <param name="metadata">The subscription metadata dictionary to inspect.</param>
    /// <returns><see langword="true" /> when complete subscription concurrency proof is present; otherwise, <see langword="false" />.</returns>
    public static bool IsConcurrencyProven(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.SubscriptionConcurrency, "provider-reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ParallelHandlerExecution, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ConsumerPrefetch, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.Backpressure, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ProviderConcurrency, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ConsumerLease, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.WorkStealing, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.DistributedWorkSharing, "reported") &&
            HasPositiveIntMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.PerSubscriptionConcurrencyLimit) &&
            HasPositiveIntMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ConsumerPrefetchCount) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.SubscriptionConcurrencySource) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.BackpressureStrategy) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ProviderConcurrencyId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ConsumerLeaseId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.WorkStealingId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.DistributedWorkSharingId);
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

    private static bool HasPositiveIntMetadataValue(
        IReadOnlyDictionary<string, string> metadata,
        string key)
    {
        return metadata.TryGetValue(key, out var candidate) &&
            int.TryParse(candidate, NumberStyles.None, CultureInfo.InvariantCulture, out var value) &&
            value > 0;
    }

    private static string RequireValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return value.Trim();
    }

    private static int RequirePositive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "A positive value is required.");
        }

        return value;
    }
}
