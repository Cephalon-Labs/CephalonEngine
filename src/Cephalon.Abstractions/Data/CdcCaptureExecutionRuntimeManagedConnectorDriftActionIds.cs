namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable operator-facing action identifiers used by managed-connector drift answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds
{
    /// <summary>
    /// No operator action is currently required for the execution runtime.
    /// </summary>
    public const string None = "none";

    /// <summary>
    /// Complete the declared task baseline before relying on desired-versus-observed drift posture.
    /// </summary>
    public const string CompleteTaskBaseline = "complete-task-baseline";

    /// <summary>
    /// Wait for the managed connector to report task topology before evaluating desired-versus-observed drift.
    /// </summary>
    public const string WaitForRuntimeReport = "wait-for-runtime-report";

    /// <summary>
    /// Investigate the reported desired-versus-observed managed-connector drift.
    /// </summary>
    public const string InvestigateDrift = "investigate-drift";
}
