namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the operator-facing runtime state recorded for one event-dispatch remediation command.
/// </summary>
/// <param name="CommandId">The stable remediation command identifier.</param>
/// <param name="OutboxId">The outbox identifier that owned the staged event targeted by the command.</param>
/// <param name="MessageId">The staged event message identifier targeted by the command.</param>
/// <param name="ChannelId">The event channel identifier associated with the targeted staged event.</param>
/// <param name="OperationId">The remediation operation identifier requested by the operator.</param>
/// <param name="Outcome">The stable command outcome identifier.</param>
/// <param name="DispatchOutcome">The dispatch observation outcome produced by the command when it was accepted.</param>
/// <param name="ObservedAtUtc">The UTC timestamp when the command was evaluated by the runtime.</param>
/// <param name="Error">The operator-facing error summary when the command was rejected.</param>
/// <param name="Metadata">The operator-facing metadata captured with the command result.</param>
public sealed record EventDispatchRemediationRuntimeState(
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
