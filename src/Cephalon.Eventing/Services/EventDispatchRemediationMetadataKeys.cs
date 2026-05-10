namespace Cephalon.Eventing.Services;

/// <summary>
/// Defines stable metadata keys used by event-dispatch remediation command results.
/// </summary>
/// <remarks>
/// These keys appear in command-result records, remediation runtime surfaces, and rejected duplicate
/// command responses so operators can reason about command idempotency without parsing provider-specific
/// metadata.
/// </remarks>
public static class EventDispatchRemediationMetadataKeys
{
    /// <summary>
    /// Identifies the command idempotency policy enforced by the active remediation dispatcher.
    /// </summary>
    public const string CommandIdempotencyPolicy = "commandIdempotencyPolicy";

    /// <summary>
    /// Identifies how the active remediation dispatcher handles a duplicate command identifier.
    /// </summary>
    public const string DuplicateCommandPolicy = "duplicateCommandPolicy";

    /// <summary>
    /// Identifies the reservation policy enforced before dispatch-store mutation.
    /// </summary>
    public const string CommandReservationPolicy = "commandReservationPolicy";

    /// <summary>
    /// Identifies when the command identifier is reserved relative to dispatch-store mutation.
    /// </summary>
    public const string CommandReservationTiming = "commandReservationTiming";

    /// <summary>
    /// Identifies the current command-reservation state visible in command metadata.
    /// </summary>
    public const string CommandReservationState = "commandReservationState";

    /// <summary>
    /// Identifies how duplicate reservations are handled by the active command journal.
    /// </summary>
    public const string CommandReservationDuplicatePolicy = "commandReservationDuplicatePolicy";

    /// <summary>
    /// Identifies the command outcome used when a reservation exists before finalization.
    /// </summary>
    public const string CommandReservationInDoubtOutcome = "commandReservationInDoubtOutcome";

    /// <summary>
    /// Identifies the component that owns command reservations.
    /// </summary>
    public const string CommandReservationOwner = "commandReservationOwner";

    /// <summary>
    /// Identifies the operator actor that requested the remediation command.
    /// </summary>
    public const string OperatorActorId = "operatorActorId";

    /// <summary>
    /// Identifies the operator correlation identifier attached to the remediation command.
    /// </summary>
    public const string OperatorCorrelationId = "operatorCorrelationId";

    /// <summary>
    /// Identifies the operator-facing reason attached to the remediation command.
    /// </summary>
    public const string OperatorCommandReason = "operatorCommandReason";

    /// <summary>
    /// Identifies whether the current response describes a duplicate command request.
    /// </summary>
    public const string DuplicateCommand = "duplicateCommand";

    /// <summary>
    /// Identifies the outcome recorded for the first command that used the duplicate command identifier.
    /// </summary>
    public const string ExistingCommandOutcome = "existingCommandOutcome";

    /// <summary>
    /// Identifies the operation recorded for the first command that used the duplicate command identifier.
    /// </summary>
    public const string ExistingCommandOperationId = "existingCommandOperationId";

    /// <summary>
    /// Identifies the outbox recorded for the first command that used the duplicate command identifier.
    /// </summary>
    public const string ExistingCommandOutboxId = "existingCommandOutboxId";

    /// <summary>
    /// Identifies the UTC timestamp recorded for the first command that used the duplicate command identifier.
    /// </summary>
    public const string ExistingCommandObservedAtUtc = "existingCommandObservedAtUtc";
}
