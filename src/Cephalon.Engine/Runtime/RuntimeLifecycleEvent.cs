namespace Cephalon.Engine.Runtime;

/// <summary>
/// Captures one operator-facing lifecycle event in the runtime story timeline.
/// </summary>
/// <param name="OccurredAtUtc">The UTC timestamp when the event was recorded.</param>
/// <param name="Scope">The runtime surface that emitted the event.</param>
/// <param name="Phase">The lifecycle phase or story phase, such as <c>load</c>, <c>initialize</c>, <c>start</c>, <c>stop</c>, or <c>restart</c>.</param>
/// <param name="Outcome">The completion outcome for the event.</param>
/// <param name="RuntimeStatus">The runtime status visible when the event was recorded.</param>
/// <param name="SubjectId">The runtime, module, package, execution-graph, or hosted-execution identifier associated with the event when available.</param>
/// <param name="SubjectVersion">The version associated with the event subject when available.</param>
/// <param name="Message">The operator-facing narrative for the event.</param>
/// <param name="ExceptionType">The exception type captured for failed events when available.</param>
public sealed record RuntimeLifecycleEvent(
    DateTimeOffset OccurredAtUtc,
    RuntimeLifecycleEventScope Scope,
    string Phase,
    RuntimeLifecycleEventOutcome Outcome,
    RuntimeStatus RuntimeStatus,
    string? SubjectId,
    string? SubjectVersion,
    string Message,
    string? ExceptionType);
