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
