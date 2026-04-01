using Cephalon.Engine.Configuration;

namespace Cephalon.Engine.Runtime;

/// <summary>
/// Describes a runtime lifecycle failure in a way that can be surfaced through diagnostics,
/// status endpoints, and operator tooling.
/// </summary>
/// <param name="Phase">The lifecycle phase that failed, such as <c>initialize</c>, <c>start</c>, or <c>stop</c>.</param>
/// <param name="ModuleId">The module identifier that triggered the failure when available.</param>
/// <param name="ModuleVersion">The module version that was active when the failure occurred, if known.</param>
/// <param name="StatusBeforeFailure">The runtime status immediately before the failure was captured.</param>
/// <param name="ExceptionType">The fully qualified exception type that caused the failure.</param>
/// <param name="Message">The failure message surfaced to operators.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when the failure was captured.</param>
/// <param name="CanRestart">Whether the current policy allows a manual restart after this failure.</param>
/// <param name="StartupFailureBehavior">The startup failure behavior in effect when the failure occurred.</param>
/// <param name="StopFailureBehavior">The stop failure behavior in effect when the failure occurred.</param>
public sealed record RuntimeFailureInfo(
    string Phase,
    string? ModuleId,
    string? ModuleVersion,
    RuntimeStatus StatusBeforeFailure,
    string ExceptionType,
    string Message,
    DateTimeOffset OccurredAtUtc,
    bool CanRestart,
    StartupFailureBehavior StartupFailureBehavior,
    StopFailureBehavior StopFailureBehavior);
