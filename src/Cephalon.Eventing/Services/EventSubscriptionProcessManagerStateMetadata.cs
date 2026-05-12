namespace Cephalon.Eventing.Services;

/// <summary>
/// Builds provider-reported saga/process-manager state proof metadata for successful subscription reports.
/// </summary>
/// <remarks>
/// Declared subscriptions, direct in-process execution, middleware, choreography handoff, outbox publication,
/// hosted execution links, and provider bindings do not automatically prove durable saga or process-manager
/// state. This helper records that stronger proof only when a successful subscription observation supplies
/// persistence, correlation, timeout, compensation, concurrency, recovery, and provider ownership evidence.
/// </remarks>
public static class EventSubscriptionProcessManagerStateMetadata
{
    /// <summary>
    /// Creates a subscription report copy enriched with provider-reported process-manager state proof when the inputs support it.
    /// </summary>
    /// <param name="report">The successful subscription execution report to copy.</param>
    /// <param name="source">The stable provider or runtime source that reported process-manager state proof.</param>
    /// <param name="sagaStatePersistenceId">The saga state persistence proof id.</param>
    /// <param name="sagaCorrelationId">The saga correlation proof id.</param>
    /// <param name="sagaTimeoutSchedulerId">The saga timeout scheduler proof id.</param>
    /// <param name="compensationWorkflowId">The compensation workflow proof id.</param>
    /// <param name="processManagerConcurrencyId">The process-manager concurrency proof id.</param>
    /// <param name="processManagerRecoveryId">The process-manager recovery proof id.</param>
    /// <param name="providerProcessManagerId">The provider process-manager proof id.</param>
    /// <param name="compensationWorkflow">A value indicating whether compensation workflow proof was reported.</param>
    /// <returns>A subscription report containing the original metadata plus process-manager state proof when applicable.</returns>
    public static EventSubscriptionExecutionReport CreateReport(
        EventSubscriptionExecutionReport report,
        string source,
        string sagaStatePersistenceId,
        string sagaCorrelationId,
        string sagaTimeoutSchedulerId,
        string compensationWorkflowId,
        string processManagerConcurrencyId,
        string processManagerRecoveryId,
        string providerProcessManagerId,
        bool compensationWorkflow = true)
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
                sagaStatePersistenceId,
                sagaCorrelationId,
                sagaTimeoutSchedulerId,
                compensationWorkflowId,
                processManagerConcurrencyId,
                processManagerRecoveryId,
                providerProcessManagerId,
                compensationWorkflow));
    }

    /// <summary>
    /// Creates a metadata copy enriched with provider-reported process-manager state proof when the inputs support it.
    /// </summary>
    /// <param name="metadata">The subscription execution metadata to copy.</param>
    /// <param name="outcome">The subscription execution outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported process-manager state proof.</param>
    /// <param name="sagaStatePersistenceId">The saga state persistence proof id.</param>
    /// <param name="sagaCorrelationId">The saga correlation proof id.</param>
    /// <param name="sagaTimeoutSchedulerId">The saga timeout scheduler proof id.</param>
    /// <param name="compensationWorkflowId">The compensation workflow proof id.</param>
    /// <param name="processManagerConcurrencyId">The process-manager concurrency proof id.</param>
    /// <param name="processManagerRecoveryId">The process-manager recovery proof id.</param>
    /// <param name="providerProcessManagerId">The provider process-manager proof id.</param>
    /// <param name="compensationWorkflow">A value indicating whether compensation workflow proof was reported.</param>
    /// <returns>A case-insensitive metadata dictionary containing the original values plus process-manager state proof when applicable.</returns>
    public static Dictionary<string, string> CreateMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string outcome,
        string source,
        string sagaStatePersistenceId,
        string sagaCorrelationId,
        string sagaTimeoutSchedulerId,
        string compensationWorkflowId,
        string processManagerConcurrencyId,
        string processManagerRecoveryId,
        string providerProcessManagerId,
        bool compensationWorkflow = true)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var result = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        TryApplyMetadata(
            result,
            outcome,
            source,
            sagaStatePersistenceId,
            sagaCorrelationId,
            sagaTimeoutSchedulerId,
            compensationWorkflowId,
            processManagerConcurrencyId,
            processManagerRecoveryId,
            providerProcessManagerId,
            compensationWorkflow);
        return result;
    }

    /// <summary>
    /// Applies provider-reported process-manager state proof to an existing subscription metadata dictionary when the inputs support it.
    /// </summary>
    /// <param name="metadata">The subscription metadata dictionary to enrich.</param>
    /// <param name="outcome">The subscription execution outcome associated with the metadata.</param>
    /// <param name="source">The stable provider or runtime source that reported process-manager state proof.</param>
    /// <param name="sagaStatePersistenceId">The saga state persistence proof id.</param>
    /// <param name="sagaCorrelationId">The saga correlation proof id.</param>
    /// <param name="sagaTimeoutSchedulerId">The saga timeout scheduler proof id.</param>
    /// <param name="compensationWorkflowId">The compensation workflow proof id.</param>
    /// <param name="processManagerConcurrencyId">The process-manager concurrency proof id.</param>
    /// <param name="processManagerRecoveryId">The process-manager recovery proof id.</param>
    /// <param name="providerProcessManagerId">The provider process-manager proof id.</param>
    /// <param name="compensationWorkflow">A value indicating whether compensation workflow proof was reported.</param>
    /// <returns><see langword="true" /> when process-manager state proof was applied; otherwise, <see langword="false" />.</returns>
    public static bool TryApplyMetadata(
        IDictionary<string, string> metadata,
        string outcome,
        string source,
        string sagaStatePersistenceId,
        string sagaCorrelationId,
        string sagaTimeoutSchedulerId,
        string compensationWorkflowId,
        string processManagerConcurrencyId,
        string processManagerRecoveryId,
        string providerProcessManagerId,
        bool compensationWorkflow = true)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (!string.Equals(outcome, EventSubscriptionExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedSource = RequireValue(source, nameof(source));
        var normalizedSagaStatePersistenceId = RequireValue(sagaStatePersistenceId, nameof(sagaStatePersistenceId));
        var normalizedSagaCorrelationId = RequireValue(sagaCorrelationId, nameof(sagaCorrelationId));
        var normalizedSagaTimeoutSchedulerId = RequireValue(sagaTimeoutSchedulerId, nameof(sagaTimeoutSchedulerId));
        var normalizedCompensationWorkflowId = RequireValue(compensationWorkflowId, nameof(compensationWorkflowId));
        var normalizedProcessManagerConcurrencyId = RequireValue(processManagerConcurrencyId, nameof(processManagerConcurrencyId));
        var normalizedProcessManagerRecoveryId = RequireValue(processManagerRecoveryId, nameof(processManagerRecoveryId));
        var normalizedProviderProcessManagerId = RequireValue(providerProcessManagerId, nameof(providerProcessManagerId));

        metadata[EventSubscriptionRuntimeMetadataKeys.ProcessManagerState] = "provider-reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.ProcessManagerStateSource] = normalizedSource;
        metadata[EventSubscriptionRuntimeMetadataKeys.SagaStatePersistence] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.SagaStatePersistenceId] = normalizedSagaStatePersistenceId;
        metadata[EventSubscriptionRuntimeMetadataKeys.SagaCorrelation] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.SagaCorrelationId] = normalizedSagaCorrelationId;
        metadata[EventSubscriptionRuntimeMetadataKeys.SagaTimeouts] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.SagaTimeoutSchedulerId] = normalizedSagaTimeoutSchedulerId;
        metadata[EventSubscriptionRuntimeMetadataKeys.CompensationWorkflow] = compensationWorkflow ? "reported" : "not-claimed";
        metadata[EventSubscriptionRuntimeMetadataKeys.CompensationWorkflowId] = normalizedCompensationWorkflowId;
        metadata[EventSubscriptionRuntimeMetadataKeys.ProcessManagerConcurrency] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.ProcessManagerConcurrencyId] = normalizedProcessManagerConcurrencyId;
        metadata[EventSubscriptionRuntimeMetadataKeys.ProcessManagerRecovery] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.ProcessManagerRecoveryId] = normalizedProcessManagerRecoveryId;
        metadata[EventSubscriptionRuntimeMetadataKeys.ProviderProcessManager] = "reported";
        metadata[EventSubscriptionRuntimeMetadataKeys.ProviderProcessManagerId] = normalizedProviderProcessManagerId;

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata contains complete provider-reported process-manager state proof.
    /// </summary>
    /// <param name="metadata">The subscription metadata dictionary to inspect.</param>
    /// <returns><see langword="true" /> when complete process-manager state proof is present; otherwise, <see langword="false" />.</returns>
    public static bool IsProcessManagerStateProven(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ProcessManagerState, "provider-reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.SagaStatePersistence, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.SagaCorrelation, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.SagaTimeouts, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.CompensationWorkflow, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ProcessManagerConcurrency, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ProcessManagerRecovery, "reported") &&
            HasMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ProviderProcessManager, "reported") &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ProcessManagerStateSource) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.SagaStatePersistenceId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.SagaCorrelationId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.SagaTimeoutSchedulerId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.CompensationWorkflowId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ProcessManagerConcurrencyId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ProcessManagerRecoveryId) &&
            HasMeaningfulMetadataValue(metadata, EventSubscriptionRuntimeMetadataKeys.ProviderProcessManagerId);
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
