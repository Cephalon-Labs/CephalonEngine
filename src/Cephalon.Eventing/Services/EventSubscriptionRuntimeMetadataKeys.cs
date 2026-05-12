namespace Cephalon.Eventing.Services;

/// <summary>
/// Defines stable metadata keys used by the event-subscriptions runtime surface.
/// </summary>
/// <remarks>
/// These keys appear in the <c>event-subscriptions</c> technology runtime surface so operators
/// and companion packs can distinguish descriptor-only, application-managed, hosted-execution-linked,
/// and runtime-bound subscription paths without parsing provider-specific metadata.
/// </remarks>
public static class EventSubscriptionRuntimeMetadataKeys
{
    /// <summary>
    /// Identifies the logical event channel consumed by the declared subscription.
    /// </summary>
    public const string ChannelId = "channelId";

    /// <summary>
    /// Identifies the logical handler or consumer declared for the subscription.
    /// </summary>
    public const string HandlerId = "handlerId";

    /// <summary>
    /// Identifies the declared delivery mode for the subscription.
    /// </summary>
    public const string DeliveryMode = "deliveryMode";

    /// <summary>
    /// Identifies who owns the dispatch path feeding subscription execution.
    /// </summary>
    public const string DispatchRuntime = "dispatchRuntime";

    /// <summary>
    /// Identifies whether an inbox is available for the subscription's channel.
    /// </summary>
    public const string Inbox = "inbox";

    /// <summary>
    /// Identifies who owns the inbox linkage for the subscription's channel.
    /// </summary>
    public const string InboxLink = "inboxLink";

    /// <summary>
    /// Identifies whether runtime observations have been reported for the subscription.
    /// </summary>
    public const string RuntimeState = "runtimeState";

    /// <summary>
    /// Identifies the subscription execution posture, such as application-managed or runtime-bound.
    /// </summary>
    public const string SubscriptionRuntime = "subscriptionRuntime";

    /// <summary>
    /// Identifies the execution-readiness state derived from managed bindings, hosted execution links, or runtime observations.
    /// </summary>
    public const string ExecutionReadiness = "executionReadiness";

    /// <summary>
    /// Identifies whether an execution path is currently bound, linked, or observed.
    /// </summary>
    public const string ExecutionPath = "executionPath";

    /// <summary>
    /// Identifies the comma-separated reasons that explain the execution-readiness state.
    /// </summary>
    public const string ExecutionReadinessReasons = "executionReadinessReasons";

    /// <summary>
    /// Identifies the managed execution-runtime identifier bound to the subscription.
    /// </summary>
    public const string ExecutionRuntimeId = "executionRuntimeId";

    /// <summary>
    /// Identifies who owns the real subscription execution path.
    /// </summary>
    public const string ExecutionOwnership = "executionOwnership";

    /// <summary>
    /// Identifies the execution mode used by the managed subscription binding.
    /// </summary>
    public const string ExecutionMode = "executionMode";

    /// <summary>
    /// Identifies the comma-separated metadata keys contributed by the managed execution binding.
    /// </summary>
    public const string BindingMetadataKeys = "bindingMetadataKeys";

    /// <summary>
    /// Prefix for individual managed execution-binding metadata entries.
    /// </summary>
    public const string BindingMetadataPrefix = "binding.";

    /// <summary>
    /// Identifies the linked inbox identifiers that can observe the subscription channel.
    /// </summary>
    public const string InboxIds = "inboxIds";

    /// <summary>
    /// Identifies all hosted executions linked to the declared subscription.
    /// </summary>
    public const string HostedExecutionIds = "hostedExecutionIds";

    /// <summary>
    /// Identifies the single hosted execution linked to the declared subscription when exactly one exists.
    /// </summary>
    public const string HostedExecutionId = "hostedExecutionId";

    /// <summary>
    /// Identifies the execution graph linked to the subscription's hosted execution.
    /// </summary>
    public const string ExecutionGraphId = "executionGraphId";

    /// <summary>
    /// Identifies the latest reported subscription execution outcome.
    /// </summary>
    public const string LastOutcome = "lastOutcome";

    /// <summary>
    /// Identifies whether a provider or runtime reports ownership of inbound broker consumption.
    /// </summary>
    public const string BrokerInboundConsumption = "brokerInboundConsumption";

    /// <summary>
    /// Identifies the provider or runtime source that reported inbound broker consumption.
    /// </summary>
    public const string BrokerInboundConsumptionSource = "brokerInboundConsumptionSource";

    /// <summary>
    /// Identifies whether a provider-owned broker consumer loop was reported.
    /// </summary>
    public const string BrokerConsumerLoop = "brokerConsumerLoop";

    /// <summary>
    /// Identifies the provider consumer-loop proof id reported for broker inbound consumption.
    /// </summary>
    public const string BrokerConsumerLoopId = "brokerConsumerLoopId";

    /// <summary>
    /// Identifies whether inbound acknowledgement proof was reported for broker consumption.
    /// </summary>
    public const string InboundAcknowledgement = "inboundAcknowledgement";

    /// <summary>
    /// Identifies the inbound acknowledgement proof id reported for broker consumption.
    /// </summary>
    public const string InboundAcknowledgementId = "inboundAcknowledgementId";

    /// <summary>
    /// Identifies whether a consumer lease or ownership token was reported for broker consumption.
    /// </summary>
    public const string ConsumerLease = "consumerLease";

    /// <summary>
    /// Identifies the consumer lease or ownership-token proof id reported for broker consumption.
    /// </summary>
    public const string ConsumerLeaseId = "consumerLeaseId";

    /// <summary>
    /// Identifies the provider retry policy reported for inbound broker consumption.
    /// </summary>
    public const string InboundRetryPolicy = "inboundRetryPolicy";

    /// <summary>
    /// Identifies the poison-message handling posture reported for inbound broker consumption.
    /// </summary>
    public const string PoisonMessageHandling = "poisonMessageHandling";

    /// <summary>
    /// Identifies whether a consumer offset checkpoint was reported for broker consumption.
    /// </summary>
    public const string ConsumerOffsetCheckpoint = "consumerOffsetCheckpoint";

    /// <summary>
    /// Identifies the consumer offset-checkpoint proof id reported for broker consumption.
    /// </summary>
    public const string ConsumerOffsetCheckpointId = "consumerOffsetCheckpointId";

    /// <summary>
    /// Identifies whether message deduplication was proven by completed execution or provider-owned idempotency.
    /// </summary>
    public const string MessageDeduplication = "messageDeduplication";

    /// <summary>
    /// Identifies whether a provider or runtime reports ownership of subscription idempotency.
    /// </summary>
    public const string ProviderIdempotency = "providerIdempotency";

    /// <summary>
    /// Identifies the provider or runtime source that reported subscription idempotency proof.
    /// </summary>
    public const string ProviderIdempotencySource = "providerIdempotencySource";

    /// <summary>
    /// Identifies the provider idempotency key used to prove subscription duplicate suppression.
    /// </summary>
    public const string ProviderIdempotencyKey = "providerIdempotencyKey";

    /// <summary>
    /// Identifies whether broker deduplication proof was reported for subscription processing.
    /// </summary>
    public const string BrokerDeduplication = "brokerDeduplication";

    /// <summary>
    /// Identifies the broker deduplication proof id reported for subscription processing.
    /// </summary>
    public const string BrokerDeduplicationId = "brokerDeduplicationId";

    /// <summary>
    /// Identifies whether exactly-once subscription processing proof was reported.
    /// </summary>
    public const string ExactlyOnceDelivery = "exactlyOnceDelivery";

    /// <summary>
    /// Identifies the exactly-once subscription processing proof id.
    /// </summary>
    public const string ExactlyOnceDeliveryProofId = "exactlyOnceDeliveryProofId";

    /// <summary>
    /// Identifies whether durable inbox command ownership proof was reported for subscription processing.
    /// </summary>
    public const string DurableInboxCommandOwnership = "durableInboxCommandOwnership";

    /// <summary>
    /// Identifies the durable inbox command proof id reported for subscription processing.
    /// </summary>
    public const string DurableInboxCommandId = "durableInboxCommandId";

    /// <summary>
    /// Identifies whether generic inbox command ownership proof was reported for subscription processing.
    /// </summary>
    public const string GenericInboxCommandOwnership = "genericInboxCommandOwnership";

    /// <summary>
    /// Identifies the generic inbox command proof id reported for subscription processing.
    /// </summary>
    public const string GenericInboxCommandId = "genericInboxCommandId";

    /// <summary>
    /// Identifies whether cross-node idempotency lease proof was reported for subscription processing.
    /// </summary>
    public const string CrossNodeIdempotencyLease = "crossNodeIdempotencyLease";

    /// <summary>
    /// Identifies the cross-node idempotency lease proof id reported for subscription processing.
    /// </summary>
    public const string CrossNodeIdempotencyLeaseId = "crossNodeIdempotencyLeaseId";

    /// <summary>
    /// Identifies whether a provider or runtime reports ownership of subscription concurrency controls.
    /// </summary>
    public const string SubscriptionConcurrency = "subscriptionConcurrency";

    /// <summary>
    /// Identifies the provider or runtime source that reported subscription concurrency proof.
    /// </summary>
    public const string SubscriptionConcurrencySource = "subscriptionConcurrencySource";

    /// <summary>
    /// Identifies the reported per-subscription concurrency limit.
    /// </summary>
    public const string PerSubscriptionConcurrencyLimit = "perSubscriptionConcurrencyLimit";

    /// <summary>
    /// Identifies whether parallel handler execution is reported for a subscription.
    /// </summary>
    public const string ParallelHandlerExecution = "parallelHandlerExecution";

    /// <summary>
    /// Identifies whether consumer prefetch is reported for a subscription.
    /// </summary>
    public const string ConsumerPrefetch = "consumerPrefetch";

    /// <summary>
    /// Identifies the reported consumer prefetch count.
    /// </summary>
    public const string ConsumerPrefetchCount = "consumerPrefetchCount";

    /// <summary>
    /// Identifies whether backpressure handling is reported for a subscription.
    /// </summary>
    public const string Backpressure = "backpressure";

    /// <summary>
    /// Identifies the reported backpressure strategy.
    /// </summary>
    public const string BackpressureStrategy = "backpressureStrategy";

    /// <summary>
    /// Identifies whether provider-owned concurrency coordination is reported for a subscription.
    /// </summary>
    public const string ProviderConcurrency = "providerConcurrency";

    /// <summary>
    /// Identifies the provider concurrency proof id.
    /// </summary>
    public const string ProviderConcurrencyId = "providerConcurrencyId";

    /// <summary>
    /// Identifies whether work-stealing coordination is reported for a subscription.
    /// </summary>
    public const string WorkStealing = "workStealing";

    /// <summary>
    /// Identifies the work-stealing proof id.
    /// </summary>
    public const string WorkStealingId = "workStealingId";

    /// <summary>
    /// Identifies whether distributed work sharing is reported for a subscription.
    /// </summary>
    public const string DistributedWorkSharing = "distributedWorkSharing";

    /// <summary>
    /// Identifies the distributed work-sharing proof id.
    /// </summary>
    public const string DistributedWorkSharingId = "distributedWorkSharingId";

    /// <summary>
    /// Identifies whether a provider or runtime reports ownership of subscription ordering controls.
    /// </summary>
    public const string SubscriptionOrdering = "subscriptionOrdering";

    /// <summary>
    /// Identifies the provider or runtime source that reported subscription ordering proof.
    /// </summary>
    public const string SubscriptionOrderingSource = "subscriptionOrderingSource";

    /// <summary>
    /// Identifies whether handler ordering guarantee proof was reported for a subscription.
    /// </summary>
    public const string HandlerOrderingGuarantee = "handlerOrderingGuarantee";

    /// <summary>
    /// Identifies the handler ordering guarantee proof id.
    /// </summary>
    public const string HandlerOrderingGuaranteeId = "handlerOrderingGuaranteeId";

    /// <summary>
    /// Identifies whether local fan-out ordering proof was reported for a subscription.
    /// </summary>
    public const string LocalFanOutOrdering = "localFanOutOrdering";

    /// <summary>
    /// Identifies the local fan-out ordering proof id.
    /// </summary>
    public const string LocalFanOutOrderingId = "localFanOutOrderingId";

    /// <summary>
    /// Identifies whether per-key ordering proof was reported for a subscription.
    /// </summary>
    public const string PerKeyOrdering = "perKeyOrdering";

    /// <summary>
    /// Identifies the key shape used to prove per-key ordering.
    /// </summary>
    public const string PerKeyOrderingKey = "perKeyOrderingKey";

    /// <summary>
    /// Identifies whether partition ordering proof was reported for a subscription.
    /// </summary>
    public const string PartitionOrdering = "partitionOrdering";

    /// <summary>
    /// Identifies the partition ordering proof id.
    /// </summary>
    public const string PartitionOrderingId = "partitionOrderingId";

    /// <summary>
    /// Identifies whether causal ordering proof was reported for a subscription.
    /// </summary>
    public const string CausalOrdering = "causalOrdering";

    /// <summary>
    /// Identifies the causal ordering proof id.
    /// </summary>
    public const string CausalOrderingId = "causalOrderingId";

    /// <summary>
    /// Identifies whether replay ordering proof was reported for a subscription.
    /// </summary>
    public const string ReplayOrdering = "replayOrdering";

    /// <summary>
    /// Identifies the replay ordering cursor proof id.
    /// </summary>
    public const string ReplayOrderingCursorId = "replayOrderingCursorId";

    /// <summary>
    /// Identifies whether cross-node ordering proof was reported for a subscription.
    /// </summary>
    public const string CrossNodeOrdering = "crossNodeOrdering";

    /// <summary>
    /// Identifies the cross-node ordering proof id.
    /// </summary>
    public const string CrossNodeOrderingId = "crossNodeOrderingId";

    /// <summary>
    /// Identifies whether provider-owned ordering coordination was reported for a subscription.
    /// </summary>
    public const string ProviderOrdering = "providerOrdering";

    /// <summary>
    /// Identifies the provider ordering proof id.
    /// </summary>
    public const string ProviderOrderingId = "providerOrderingId";

    /// <summary>
    /// Identifies whether a provider or runtime reports ownership of saga/process-manager state.
    /// </summary>
    public const string ProcessManagerState = "processManagerState";

    /// <summary>
    /// Identifies the provider or runtime source that reported saga/process-manager state proof.
    /// </summary>
    public const string ProcessManagerStateSource = "processManagerStateSource";

    /// <summary>
    /// Identifies whether saga state persistence proof was reported.
    /// </summary>
    public const string SagaStatePersistence = "sagaStatePersistence";

    /// <summary>
    /// Identifies the saga state persistence proof id.
    /// </summary>
    public const string SagaStatePersistenceId = "sagaStatePersistenceId";

    /// <summary>
    /// Identifies whether saga correlation proof was reported.
    /// </summary>
    public const string SagaCorrelation = "sagaCorrelation";

    /// <summary>
    /// Identifies the saga correlation proof id.
    /// </summary>
    public const string SagaCorrelationId = "sagaCorrelationId";

    /// <summary>
    /// Identifies whether saga timeout scheduling proof was reported.
    /// </summary>
    public const string SagaTimeouts = "sagaTimeouts";

    /// <summary>
    /// Identifies the saga timeout scheduler proof id.
    /// </summary>
    public const string SagaTimeoutSchedulerId = "sagaTimeoutSchedulerId";

    /// <summary>
    /// Identifies whether compensation workflow proof was reported.
    /// </summary>
    public const string CompensationWorkflow = "compensationWorkflow";

    /// <summary>
    /// Identifies the compensation workflow proof id.
    /// </summary>
    public const string CompensationWorkflowId = "compensationWorkflowId";

    /// <summary>
    /// Identifies whether process-manager concurrency proof was reported.
    /// </summary>
    public const string ProcessManagerConcurrency = "processManagerConcurrency";

    /// <summary>
    /// Identifies the process-manager concurrency proof id.
    /// </summary>
    public const string ProcessManagerConcurrencyId = "processManagerConcurrencyId";

    /// <summary>
    /// Identifies whether process-manager recovery proof was reported.
    /// </summary>
    public const string ProcessManagerRecovery = "processManagerRecovery";

    /// <summary>
    /// Identifies the process-manager recovery proof id.
    /// </summary>
    public const string ProcessManagerRecoveryId = "processManagerRecoveryId";

    /// <summary>
    /// Identifies whether provider-owned process-manager coordination was reported.
    /// </summary>
    public const string ProviderProcessManager = "providerProcessManager";

    /// <summary>
    /// Identifies the provider process-manager proof id.
    /// </summary>
    public const string ProviderProcessManagerId = "providerProcessManagerId";

    /// <summary>
    /// Identifies whether the latest runtime observation says a retry is pending.
    /// </summary>
    public const string RetryPending = "retryPending";

    /// <summary>
    /// Identifies whether the subscription runtime extracted Cephalon context before executing the consumer.
    /// </summary>
    public const string ConsumerContextExtraction = "consumerContextExtraction";

    /// <summary>
    /// Identifies the source used for consumer-side Cephalon context extraction.
    /// </summary>
    public const string ConsumerContextExtractionSource = "consumerContextExtractionSource";

    /// <summary>
    /// Identifies the number of Cephalon context headers extracted before executing the consumer.
    /// </summary>
    public const string ConsumerContextHeaderCount = "consumerContextHeaderCount";

    /// <summary>
    /// Identifies the comma-separated Cephalon context header names extracted before executing the consumer.
    /// </summary>
    public const string ConsumerContextHeaderNames = "consumerContextHeaderNames";

    /// <summary>
    /// Prefix for individual runtime-observation metadata entries.
    /// </summary>
    public const string ReportedMetadataPrefix = "reported.";
}
