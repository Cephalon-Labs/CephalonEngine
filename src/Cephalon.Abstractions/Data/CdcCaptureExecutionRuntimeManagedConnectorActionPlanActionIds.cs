namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable operator-facing action identifiers used by managed-connector action plans.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds
{
    /// <summary>
    /// No operator action is currently required for the execution runtime.
    /// </summary>
    public const string None = CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds.None;

    /// <summary>
    /// Keep the connector in observe-only mode and continue using shared runtime reporting truth.
    /// </summary>
    public const string KeepObserveOnly = CdcCaptureExecutionRuntimeManagedConnectorGovernanceActionIds.KeepObserveOnly;

    /// <summary>
    /// Resolve runtime remediation work before relying on deeper managed-connector follow-through.
    /// </summary>
    public const string ResolveRuntimeRemediation = "resolve-runtime-remediation";

    /// <summary>
    /// Complete the connector declaration so shared governance truth has the minimum required metadata.
    /// </summary>
    public const string CompleteGovernanceDeclaration = CdcCaptureExecutionRuntimeManagedConnectorGovernanceActionIds.CompleteGovernanceDeclaration;

    /// <summary>
    /// Defer control-plane ownership until Cephalon ships write-path connector management for the declared mode.
    /// </summary>
    public const string DeferControlPlane = CdcCaptureExecutionRuntimeManagedConnectorGovernanceActionIds.DeferControlPlane;

    /// <summary>
    /// Complete the declared task baseline before relying on desired-versus-observed drift posture.
    /// </summary>
    public const string CompleteTaskBaseline = CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds.CompleteTaskBaseline;

    /// <summary>
    /// Wait for the managed connector to report task topology before evaluating desired-versus-observed drift.
    /// </summary>
    public const string WaitForRuntimeReport = CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds.WaitForRuntimeReport;

    /// <summary>
    /// Investigate the reported desired-versus-observed managed-connector drift.
    /// </summary>
    public const string InvestigateDrift = CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds.InvestigateDrift;
}
