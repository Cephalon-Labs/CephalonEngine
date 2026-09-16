namespace Cephalon.Abstractions.Coordination;

/// <summary>Describes a completed local attempt without exposing provider error text.</summary>
/// <param name="Number">The one-based attempt number.</param>
/// <param name="StartedAtUtc">The invocation timestamp.</param>
/// <param name="CompletedAtUtc">The observed completion or uncertainty timestamp.</param>
/// <param name="Outcome">The confirmed outcome, or uncertainty.</param>
public sealed record ReconciliationAttempt(int Number, DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc, ReconciliationEffectOutcome Outcome);
