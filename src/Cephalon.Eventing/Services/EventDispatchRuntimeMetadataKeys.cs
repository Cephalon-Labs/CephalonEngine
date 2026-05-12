namespace Cephalon.Eventing.Services;

/// <summary>
/// Defines stable metadata keys used by event-dispatch runtime observations.
/// </summary>
/// <remarks>
/// These keys appear in dispatch runtime reports and the derived event-dispatch runtime surfaces
/// so operators and dispatch stores can distinguish retryable failures from terminal failures
/// without parsing provider-specific metadata.
/// </remarks>
public static class EventDispatchRuntimeMetadataKeys
{
    /// <summary>
    /// Identifies the next UTC time when a retryable dispatch failure should become eligible again.
    /// </summary>
    public const string NextRetryAtUtc = "nextRetryAtUtc";

    /// <summary>
    /// Identifies the retry policy applied by the active dispatch runtime.
    /// </summary>
    public const string RetryPolicy = "retryPolicy";

    /// <summary>
    /// Identifies the maximum number of dispatch attempts allowed for one staged message.
    /// </summary>
    public const string RetryMaxAttempts = "retryMaxAttempts";

    /// <summary>
    /// Identifies the retry delay in seconds when the active dispatch runtime uses a delayed retry policy.
    /// </summary>
    public const string RetryDelaySeconds = "retryDelaySeconds";

    /// <summary>
    /// Identifies where retry eligibility is persisted.
    /// </summary>
    public const string RetryDurability = "retryDurability";

    /// <summary>
    /// Identifies who owns the retry policy.
    /// </summary>
    public const string RetryScope = "retryScope";

    /// <summary>
    /// Identifies whether a provider or runtime reported broker topology materialization ownership.
    /// </summary>
    public const string BrokerTopologyMaterialization = "brokerTopologyMaterialization";

    /// <summary>
    /// Identifies the provider or runtime source that reported broker topology materialization ownership.
    /// </summary>
    public const string BrokerTopologyMaterializationSource = "brokerTopologyMaterializationSource";

    /// <summary>
    /// Identifies whether broker exchange provisioning was reported.
    /// </summary>
    public const string ExchangeProvisioning = "exchangeProvisioning";

    /// <summary>
    /// Identifies the broker exchange provisioning proof id.
    /// </summary>
    public const string ExchangeProvisioningId = "exchangeProvisioningId";

    /// <summary>
    /// Identifies whether broker queue provisioning was reported.
    /// </summary>
    public const string QueueProvisioning = "queueProvisioning";

    /// <summary>
    /// Identifies the broker queue provisioning proof id.
    /// </summary>
    public const string QueueProvisioningId = "queueProvisioningId";

    /// <summary>
    /// Identifies whether broker topic provisioning was reported.
    /// </summary>
    public const string TopicProvisioning = "topicProvisioning";

    /// <summary>
    /// Identifies the broker topic provisioning proof id.
    /// </summary>
    public const string TopicProvisioningId = "topicProvisioningId";

    /// <summary>
    /// Identifies whether broker partition provisioning was reported.
    /// </summary>
    public const string PartitionProvisioning = "partitionProvisioning";

    /// <summary>
    /// Identifies the broker partition provisioning proof id.
    /// </summary>
    public const string PartitionProvisioningId = "partitionProvisioningId";

    /// <summary>
    /// Identifies whether provider topology verification was reported.
    /// </summary>
    public const string TopologyVerification = "topologyVerification";

    /// <summary>
    /// Identifies the provider topology verification proof id.
    /// </summary>
    public const string TopologyVerificationId = "topologyVerificationId";

    /// <summary>
    /// Identifies whether provider-owned broker topology proof was reported.
    /// </summary>
    public const string ProviderOwnedTopology = "providerOwnedTopology";

    /// <summary>
    /// Identifies the provider-owned broker topology proof id.
    /// </summary>
    public const string ProviderTopologyId = "providerTopologyId";

    /// <summary>
    /// Identifies whether a provider or runtime reported provider partition ownership.
    /// </summary>
    public const string ProviderPartitionOwnership = "providerPartitionOwnership";

    /// <summary>
    /// Identifies the provider or runtime source that reported provider partition ownership.
    /// </summary>
    public const string ProviderPartitionOwnershipSource = "providerPartitionOwnershipSource";

    /// <summary>
    /// Identifies whether provider partition assignment was reported.
    /// </summary>
    public const string PartitionAssignment = "partitionAssignment";

    /// <summary>
    /// Identifies the provider partition assignment proof id.
    /// </summary>
    public const string PartitionAssignmentId = "partitionAssignmentId";

    /// <summary>
    /// Identifies whether provider partition affinity was reported.
    /// </summary>
    public const string PartitionAffinity = "partitionAffinity";

    /// <summary>
    /// Identifies the provider partition affinity proof id.
    /// </summary>
    public const string PartitionAffinityId = "partitionAffinityId";

    /// <summary>
    /// Identifies whether provider partition rebalancing was reported.
    /// </summary>
    public const string PartitionRebalancing = "partitionRebalancing";

    /// <summary>
    /// Identifies the provider partition rebalancing proof id.
    /// </summary>
    public const string PartitionRebalancingId = "partitionRebalancingId";

    /// <summary>
    /// Identifies whether provider partition ordering guarantees were reported.
    /// </summary>
    public const string PartitionOrderingGuarantee = "partitionOrderingGuarantee";

    /// <summary>
    /// Identifies the provider partition ordering guarantee proof id.
    /// </summary>
    public const string PartitionOrderingGuaranteeId = "partitionOrderingGuaranteeId";

    /// <summary>
    /// Identifies whether provider-owned partitioning proof was reported.
    /// </summary>
    public const string ProviderOwnedPartitioning = "providerOwnedPartitioning";

    /// <summary>
    /// Identifies the provider-owned partitioning proof id.
    /// </summary>
    public const string ProviderPartitioningId = "providerPartitioningId";

    /// <summary>
    /// Identifies whether a provider or runtime reported durable retry queue ownership.
    /// </summary>
    public const string DurableRetryQueue = "durableRetryQueue";

    /// <summary>
    /// Identifies the provider or runtime source that reported durable retry queue ownership.
    /// </summary>
    public const string DurableRetryQueueSource = "durableRetryQueueSource";

    /// <summary>
    /// Identifies the durable retry queue id reported for the dispatch.
    /// </summary>
    public const string DurableRetryQueueId = "durableRetryQueueId";

    /// <summary>
    /// Identifies whether retry persistence was reported for the dispatch.
    /// </summary>
    public const string RetryPersistence = "retryPersistence";

    /// <summary>
    /// Identifies the retry persistence record or store id reported for the dispatch.
    /// </summary>
    public const string RetryPersistenceId = "retryPersistenceId";

    /// <summary>
    /// Identifies whether a broker error queue was reported for the dispatch.
    /// </summary>
    public const string BrokerErrorQueue = "brokerErrorQueue";

    /// <summary>
    /// Identifies the broker error queue id reported for the dispatch.
    /// </summary>
    public const string BrokerErrorQueueId = "brokerErrorQueueId";

    /// <summary>
    /// Identifies whether poison queue ownership was reported for the dispatch.
    /// </summary>
    public const string PoisonQueueOwnership = "poisonQueueOwnership";

    /// <summary>
    /// Identifies the poison queue id reported for the dispatch.
    /// </summary>
    public const string PoisonQueueId = "poisonQueueId";

    /// <summary>
    /// Identifies whether cross-node retry coordination was reported for the dispatch.
    /// </summary>
    public const string CrossNodeRetryCoordination = "crossNodeRetryCoordination";

    /// <summary>
    /// Identifies the cross-node retry coordination id reported for the dispatch.
    /// </summary>
    public const string RetryCoordinationId = "retryCoordinationId";

    /// <summary>
    /// Identifies whether a retry lease was reported for the dispatch.
    /// </summary>
    public const string RetryLease = "retryLease";

    /// <summary>
    /// Identifies the retry lease id reported for the dispatch.
    /// </summary>
    public const string RetryLeaseId = "retryLeaseId";

    /// <summary>
    /// Identifies the retry decision represented by the latest observation.
    /// </summary>
    public const string RetryOutcome = "retryOutcome";

    /// <summary>
    /// Identifies whether the retry budget was exhausted for the latest observation.
    /// </summary>
    public const string RetryExhausted = "retryExhausted";

    /// <summary>
    /// Identifies whether the latest failure should stop re-entering pending-dispatch reads.
    /// </summary>
    public const string TerminalFailure = "terminalFailure";

    /// <summary>
    /// Identifies the dead-letter decision represented by the latest operator observation.
    /// </summary>
    public const string DeadLetterOutcome = "deadLetterOutcome";

    /// <summary>
    /// Identifies the scope that owns the dead-letter decision.
    /// </summary>
    public const string DeadLetterScope = "deadLetterScope";

    /// <summary>
    /// Identifies where the dead-letter decision is persisted.
    /// </summary>
    public const string DeadLetterDurability = "deadLetterDurability";

    /// <summary>
    /// Identifies whether the dead-letter decision is owned by a broker-specific dead-letter queue.
    /// </summary>
    public const string BrokerDeadLetter = "brokerDeadLetter";

    /// <summary>
    /// Identifies the durable dispatch context propagation boundary proven by the latest runtime observation.
    /// </summary>
    public const string DurableDispatchContextPropagation = "durableDispatchContextPropagation";

    /// <summary>
    /// Identifies whether provider or broker headers carry the same context beyond Cephalon dispatch metadata.
    /// </summary>
    public const string ProviderBrokerContextHeaders = "providerBrokerContextHeaders";

    /// <summary>
    /// Identifies the provider-neutral projection used for provider or broker context headers.
    /// </summary>
    public const string ProviderBrokerContextHeaderProjection = "providerBrokerContextHeaderProjection";

    /// <summary>
    /// Identifies the number of Cephalon context headers projected toward the provider or broker boundary.
    /// </summary>
    public const string ProviderBrokerContextHeaderCount = "providerBrokerContextHeaderCount";

    /// <summary>
    /// Identifies the comma-separated Cephalon context header names projected toward the provider or broker boundary.
    /// </summary>
    public const string ProviderBrokerContextHeaderNames = "providerBrokerContextHeaderNames";

    /// <summary>
    /// Identifies whether the active provider-side dispatch store persisted the projected Cephalon context proof.
    /// </summary>
    public const string ProviderSideContextPersistence = "providerSideContextPersistence";

    /// <summary>
    /// Identifies the provider-side dispatch store that persisted the projected Cephalon context proof.
    /// </summary>
    public const string ProviderSideContextPersistenceSource = "providerSideContextPersistenceSource";

    /// <summary>
    /// Identifies the number of Cephalon context headers persisted by the provider-side dispatch store proof.
    /// </summary>
    public const string ProviderSideContextPersistenceHeaderCount = "providerSideContextPersistenceHeaderCount";

    /// <summary>
    /// Identifies the comma-separated Cephalon context header names persisted by the provider-side dispatch store proof.
    /// </summary>
    public const string ProviderSideContextPersistenceHeaderNames = "providerSideContextPersistenceHeaderNames";

    /// <summary>
    /// Identifies whether consumer-side extraction has been proven for the dispatched context.
    /// </summary>
    public const string ConsumerContextExtraction = "consumerContextExtraction";

    /// <summary>
    /// Identifies whether cross-node context handoff has been proven for the dispatched context.
    /// </summary>
    public const string CrossNodeContextHandoff = "crossNodeContextHandoff";

    /// <summary>
    /// Identifies the provider or runtime observation source that proved cross-node context handoff.
    /// </summary>
    public const string CrossNodeContextHandoffSource = "crossNodeContextHandoffSource";

    /// <summary>
    /// Identifies the producer-side node observed for a proven cross-node context handoff.
    /// </summary>
    public const string CrossNodeContextHandoffProducerNodeId = "crossNodeContextHandoffProducerNodeId";

    /// <summary>
    /// Identifies the consumer-side node observed for a proven cross-node context handoff.
    /// </summary>
    public const string CrossNodeContextHandoffConsumerNodeId = "crossNodeContextHandoffConsumerNodeId";

    /// <summary>
    /// Identifies the number of Cephalon context headers observed during a proven cross-node handoff.
    /// </summary>
    public const string CrossNodeContextHandoffHeaderCount = "crossNodeContextHandoffHeaderCount";

    /// <summary>
    /// Identifies the comma-separated Cephalon context header names observed during a proven cross-node handoff.
    /// </summary>
    public const string CrossNodeContextHandoffHeaderNames = "crossNodeContextHandoffHeaderNames";

    /// <summary>
    /// Identifies whether provider-reported downstream delivery completion has been proven for the dispatch.
    /// </summary>
    public const string DownstreamDeliveryCompletion = "downstreamDeliveryCompletion";

    /// <summary>
    /// Identifies the provider or runtime source that reported downstream delivery completion.
    /// </summary>
    public const string DownstreamDeliveryCompletionSource = "downstreamDeliveryCompletionSource";

    /// <summary>
    /// Identifies whether a provider delivery receipt was reported for the dispatch.
    /// </summary>
    public const string ProviderDeliveryReceipt = "providerDeliveryReceipt";

    /// <summary>
    /// Identifies the provider delivery receipt id reported for the dispatch.
    /// </summary>
    public const string ProviderDeliveryReceiptId = "providerDeliveryReceiptId";

    /// <summary>
    /// Identifies whether subscriber acknowledgement was reported for the dispatch.
    /// </summary>
    public const string SubscriberAcknowledgement = "subscriberAcknowledgement";

    /// <summary>
    /// Identifies the subscriber acknowledgement id reported for the dispatch.
    /// </summary>
    public const string SubscriberAcknowledgementId = "subscriberAcknowledgementId";

    /// <summary>
    /// Identifies whether destination commit proof was reported for the dispatch.
    /// </summary>
    public const string DestinationCommit = "destinationCommit";

    /// <summary>
    /// Identifies the destination commit id reported for the dispatch.
    /// </summary>
    public const string DestinationCommitId = "destinationCommitId";

    /// <summary>
    /// Identifies whether exactly-once delivery proof was reported for the dispatch.
    /// </summary>
    public const string ExactlyOnceDelivery = "exactlyOnceDelivery";

    /// <summary>
    /// Identifies the provider or runtime source that reported exactly-once delivery proof.
    /// </summary>
    public const string ExactlyOnceDeliverySource = "exactlyOnceDeliverySource";

    /// <summary>
    /// Identifies the provider exactly-once delivery proof id reported for the dispatch.
    /// </summary>
    public const string ExactlyOnceDeliveryProofId = "exactlyOnceDeliveryProofId";

    /// <summary>
    /// Identifies the provider exactly-once delivery strategy reported for the dispatch.
    /// </summary>
    public const string ExactlyOnceDeliveryStrategy = "exactlyOnceDeliveryStrategy";

    /// <summary>
    /// Identifies whether the latest dispatch runtime observation includes Cephalon context metadata.
    /// </summary>
    public const string DispatchContextMetadata = "dispatchContextMetadata";

    /// <summary>
    /// Identifies the number of context-capable headers present on the dispatch item used by the report.
    /// </summary>
    public const string DispatchContextHeaderCount = "dispatchContextHeaderCount";

    /// <summary>
    /// Identifies the number of context metadata entries carried from the dispatch item into the report.
    /// </summary>
    public const string DispatchContextMetadataCount = "dispatchContextMetadataCount";

    /// <summary>
    /// Gets a value indicating whether the supplied metadata describes a terminal failure.
    /// </summary>
    /// <param name="metadata">The dispatch observation metadata to inspect.</param>
    /// <returns>
    /// <see langword="true" /> when either <see cref="TerminalFailure" /> or
    /// <see cref="RetryExhausted" /> is set to <c>true</c>; otherwise, <see langword="false" />.
    /// </returns>
    public static bool IsTerminalFailure(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return IsTrue(metadata, TerminalFailure) || IsTrue(metadata, RetryExhausted);
    }

    private static bool IsTrue(IReadOnlyDictionary<string, string> metadata, string key) =>
        metadata.TryGetValue(key, out var value) &&
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}
