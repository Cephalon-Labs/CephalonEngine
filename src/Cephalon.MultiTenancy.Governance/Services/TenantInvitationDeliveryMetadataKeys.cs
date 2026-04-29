namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable metadata keys written by tenant invitation delivery dispatch and status reconciliation.
/// </summary>
public static class TenantInvitationDeliveryMetadataKeys
{
    /// <summary>
    /// Metadata key containing the last tenant invitation delivery outcome.
    /// </summary>
    public const string LastDeliveryOutcome = "lastDeliveryOutcome";

    /// <summary>
    /// Metadata key containing the UTC timestamp when delivery dispatch was evaluated.
    /// </summary>
    public const string LastDeliveryDispatchedAtUtc = "lastDeliveryDispatchedAtUtc";

    /// <summary>
    /// Metadata key containing the delivery channel used by the last dispatch attempt.
    /// </summary>
    public const string LastDeliveryChannel = "lastDeliveryChannel";

    /// <summary>
    /// Metadata key containing the sender identifier used by the last dispatch attempt.
    /// </summary>
    public const string LastDeliverySenderId = "lastDeliverySenderId";

    /// <summary>
    /// Metadata key containing the provider message identifier returned by the sender.
    /// </summary>
    public const string LastDeliveryProviderMessageId = "lastDeliveryProviderMessageId";

    /// <summary>
    /// Metadata key containing the last reconciled delivery status.
    /// </summary>
    public const string LastDeliveryStatus = "lastDeliveryStatus";

    /// <summary>
    /// Metadata key containing the UTC timestamp when delivery status was observed.
    /// </summary>
    public const string LastDeliveryStatusObservedAtUtc = "lastDeliveryStatusObservedAtUtc";

    /// <summary>
    /// Metadata key containing the last delivery status reconciliation outcome.
    /// </summary>
    public const string LastDeliveryStatusReconciliationOutcome = "lastDeliveryStatusReconciliationOutcome";

    /// <summary>
    /// Metadata key containing the provider message identifier associated with the last delivery status observation.
    /// </summary>
    public const string LastDeliveryStatusProviderMessageId = "lastDeliveryStatusProviderMessageId";

    /// <summary>
    /// Metadata key containing the sender identifier associated with the last delivery status observation.
    /// </summary>
    public const string LastDeliveryStatusSenderId = "lastDeliveryStatusSenderId";

    /// <summary>
    /// Metadata key containing the delivery channel associated with the last delivery status observation.
    /// </summary>
    public const string LastDeliveryStatusChannel = "lastDeliveryStatusChannel";

    /// <summary>
    /// Metadata key containing the source that requested the last delivery dispatch.
    /// </summary>
    public const string LastDeliverySource = "lastDeliverySource";

    /// <summary>
    /// Metadata key containing the actor that requested the last delivery dispatch.
    /// </summary>
    public const string LastDeliveryActor = "lastDeliveryActor";

    /// <summary>
    /// Metadata key containing the correlation identifier for the last delivery dispatch.
    /// </summary>
    public const string LastDeliveryCorrelationId = "lastDeliveryCorrelationId";

    /// <summary>
    /// Metadata key containing the source that reported the last delivery status observation.
    /// </summary>
    public const string LastDeliveryStatusSource = "lastDeliveryStatusSource";

    /// <summary>
    /// Metadata key containing the actor that reported the last delivery status observation.
    /// </summary>
    public const string LastDeliveryStatusActor = "lastDeliveryStatusActor";

    /// <summary>
    /// Metadata key containing the correlation identifier for the last delivery status observation.
    /// </summary>
    public const string LastDeliveryStatusCorrelationId = "lastDeliveryStatusCorrelationId";

    /// <summary>
    /// Metadata key containing the provider or receiver reason for the last delivery status observation.
    /// </summary>
    public const string LastDeliveryStatusReason = "lastDeliveryStatusReason";

    /// <summary>
    /// Metadata key describing Cephalon ownership of the host-agnostic dispatch pipeline.
    /// </summary>
    public const string DeliveryDispatchOwnership = "deliveryDispatchOwnership";

    /// <summary>
    /// Metadata key describing who owns provider-specific external delivery.
    /// </summary>
    public const string ExternalDeliveryOwnership = "externalDeliveryOwnership";

    /// <summary>
    /// Metadata key describing Cephalon ownership of host-agnostic status reconciliation.
    /// </summary>
    public const string DeliveryStatusReconciliationOwnership = "deliveryStatusReconciliationOwnership";

    /// <summary>
    /// Metadata key describing who owns provider-specific external delivery status truth.
    /// </summary>
    public const string ExternalDeliveryStatusOwnership = "externalDeliveryStatusOwnership";

    /// <summary>
    /// Metadata key containing the delivery status observation identifier recorded by the observation store.
    /// </summary>
    public const string DeliveryStatusObservationId = "deliveryStatusObservationId";

    /// <summary>
    /// Metadata key describing whether the delivery status observation store recorded the observation.
    /// </summary>
    public const string DeliveryStatusObservationStoreOutcome = "deliveryStatusObservationStoreOutcome";

    /// <summary>
    /// Metadata key describing the delivery status observation store kind.
    /// </summary>
    public const string DeliveryStatusObservationStoreKind = "deliveryStatusObservationStoreKind";

    /// <summary>
    /// Metadata key describing whether the delivery status observation store is durable.
    /// </summary>
    public const string DeliveryStatusObservationStoreDurable = "deliveryStatusObservationStoreDurable";

    /// <summary>
    /// Metadata key describing Cephalon ownership of delivery status observation storage.
    /// </summary>
    public const string DeliveryStatusObservationStoreOwnership = "deliveryStatusObservationStoreOwnership";

    /// <summary>
    /// Metadata key describing the retention limit used by the delivery status observation store.
    /// </summary>
    public const string DeliveryStatusObservationStoreHistoryLimit = "deliveryStatusObservationStoreHistoryLimit";

    /// <summary>
    /// Metadata key containing the exception type observed when delivery status observation storage fails.
    /// </summary>
    public const string DeliveryStatusObservationStoreExceptionType = "deliveryStatusObservationStoreExceptionType";

    /// <summary>
    /// Metadata key describing Cephalon ownership of the invitation delivery retry queue.
    /// </summary>
    public const string DeliveryRetryQueueOwnership = "deliveryRetryQueueOwnership";

    /// <summary>
    /// Metadata key describing the invitation delivery retry queue storage kind.
    /// </summary>
    public const string DeliveryRetryQueueStoreKind = "deliveryRetryQueueStoreKind";

    /// <summary>
    /// Metadata key describing whether the invitation delivery retry queue is durable.
    /// </summary>
    public const string DeliveryRetryQueueStoreDurable = "deliveryRetryQueueStoreDurable";

    /// <summary>
    /// Metadata key containing the invitation delivery retry queue entry identifier.
    /// </summary>
    public const string DeliveryRetryQueueEntryId = "deliveryRetryQueueEntryId";

    /// <summary>
    /// Metadata key describing whether a sender failure was queued for retry.
    /// </summary>
    public const string DeliveryRetryQueueOutcome = "deliveryRetryQueueOutcome";

    /// <summary>
    /// Metadata key containing the number of pending invitation delivery retry entries.
    /// </summary>
    public const string DeliveryRetryQueuePendingCount = "deliveryRetryQueuePendingCount";

    /// <summary>
    /// Metadata key containing the total number of retained invitation delivery retry entries.
    /// </summary>
    public const string DeliveryRetryQueueEntryCount = "deliveryRetryQueueEntryCount";

    /// <summary>
    /// Metadata key containing the retry queue attempt number.
    /// </summary>
    public const string DeliveryRetryQueueAttempt = "deliveryRetryQueueAttempt";

    /// <summary>
    /// Metadata key containing the maximum attempts configured for retry queue entries.
    /// </summary>
    public const string DeliveryRetryQueueMaxAttempts = "deliveryRetryQueueMaxAttempts";

    /// <summary>
    /// Metadata key containing the retry delay in seconds.
    /// </summary>
    public const string DeliveryRetryQueueDelaySeconds = "deliveryRetryQueueDelaySeconds";

    /// <summary>
    /// Metadata key containing the UTC timestamp when the next retry attempt is due.
    /// </summary>
    public const string DeliveryRetryQueueNextAttemptAtUtc = "deliveryRetryQueueNextAttemptAtUtc";

    /// <summary>
    /// Metadata key containing the UTC timestamp of the latest retry attempt.
    /// </summary>
    public const string DeliveryRetryQueueLastAttemptAtUtc = "deliveryRetryQueueLastAttemptAtUtc";

    /// <summary>
    /// Metadata key containing the latest retry dispatch outcome.
    /// </summary>
    public const string DeliveryRetryQueueLastOutcome = "deliveryRetryQueueLastOutcome";

    /// <summary>
    /// Metadata key containing the latest retry dispatch reason.
    /// </summary>
    public const string DeliveryRetryQueueLastReason = "deliveryRetryQueueLastReason";

    /// <summary>
    /// Metadata key that marks a dispatch request created by the retry runner.
    /// </summary>
    public const string DeliveryRetryExecution = "deliveryRetryExecution";

    /// <summary>
    /// Metadata key describing whether retry execution coordination was enabled for the retry runner.
    /// </summary>
    public const string DeliveryRetryExecutionCoordination = "deliveryRetryExecutionCoordination";

    /// <summary>
    /// Metadata key describing Cephalon ownership of retry execution coordination.
    /// </summary>
    public const string DeliveryRetryExecutionCoordinationOwnership = "deliveryRetryExecutionCoordinationOwnership";

    /// <summary>
    /// Metadata key describing the retry execution coordination scope.
    /// </summary>
    public const string DeliveryRetryExecutionCoordinationScope = "deliveryRetryExecutionCoordinationScope";

    /// <summary>
    /// Metadata key describing the retry execution coordination mode.
    /// </summary>
    public const string DeliveryRetryExecutionCoordinationMode = "deliveryRetryExecutionCoordinationMode";

    /// <summary>
    /// Metadata key describing whether a coordinated retry execution is currently running.
    /// </summary>
    public const string DeliveryRetryExecutionCoordinationInProgress = "deliveryRetryExecutionCoordinationInProgress";

    /// <summary>
    /// Metadata key containing the number of attempts to enter retry execution coordination.
    /// </summary>
    public const string DeliveryRetryExecutionCoordinationAttemptCount = "deliveryRetryExecutionCoordinationAttemptCount";

    /// <summary>
    /// Metadata key containing the number of retry execution coordination attempts accepted for execution.
    /// </summary>
    public const string DeliveryRetryExecutionCoordinationAcceptedCount = "deliveryRetryExecutionCoordinationAcceptedCount";

    /// <summary>
    /// Metadata key containing the number of retry execution coordination attempts skipped because another pass was already running.
    /// </summary>
    public const string DeliveryRetryExecutionCoordinationSkippedCount = "deliveryRetryExecutionCoordinationSkippedCount";

    /// <summary>
    /// Metadata key containing the number of coordinated retry passes completed with a retry result.
    /// </summary>
    public const string DeliveryRetryExecutionCoordinationCompletedCount = "deliveryRetryExecutionCoordinationCompletedCount";

    /// <summary>
    /// Metadata key containing the number of coordinated retry passes that ended with an unhandled failure.
    /// </summary>
    public const string DeliveryRetryExecutionCoordinationFailedCount = "deliveryRetryExecutionCoordinationFailedCount";

    /// <summary>
    /// Metadata key containing the latest retry execution coordination outcome.
    /// </summary>
    public const string DeliveryRetryExecutionCoordinationLastOutcome = "deliveryRetryExecutionCoordinationLastOutcome";

    /// <summary>
    /// Metadata key that marks a dispatch request created by automatic background retry scheduling.
    /// </summary>
    public const string DeliveryRetryBackgroundScheduling = "deliveryRetryBackgroundScheduling";

    /// <summary>
    /// Metadata key describing Cephalon ownership of automatic background retry scheduling.
    /// </summary>
    public const string DeliveryRetryBackgroundOwnership = "deliveryRetryBackgroundOwnership";
}
