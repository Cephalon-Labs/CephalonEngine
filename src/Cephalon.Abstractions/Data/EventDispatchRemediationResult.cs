namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the operator-facing result of one event-dispatch remediation command.
/// </summary>
/// <param name="CommandId">The stable command identifier.</param>
/// <param name="OutboxId">The outbox identifier that owns the staged event.</param>
/// <param name="MessageId">The staged event message identifier.</param>
/// <param name="ChannelId">The event channel identifier associated with the staged event.</param>
/// <param name="OperationId">The remediation operation identifier.</param>
/// <param name="Outcome">The stable command outcome identifier.</param>
/// <param name="DispatchOutcome">The dispatch observation outcome applied by the command when accepted.</param>
/// <param name="ObservedAtUtc">The UTC timestamp when the command was evaluated.</param>
/// <param name="Error">The operator-facing error summary when the command was rejected.</param>
/// <param name="Metadata">Optional operator-facing metadata captured with the result.</param>
public sealed record EventDispatchRemediationResult(
    string CommandId,
    string OutboxId,
    string MessageId,
    string ChannelId,
    string OperationId,
    string Outcome,
    string DispatchOutcome,
    DateTimeOffset ObservedAtUtc,
    string? Error,
    IReadOnlyDictionary<string, string> Metadata);
