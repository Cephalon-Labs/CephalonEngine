namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable operator-facing action identifiers used by managed-connector governance answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorGovernanceActionIds
{
    /// <summary>
    /// No operator action is currently required for the execution runtime.
    /// </summary>
    public const string None = "none";

    /// <summary>
    /// Keep the connector in observe-only mode and continue using shared runtime reporting truth.
    /// </summary>
    public const string KeepObserveOnly = "keep-observe-only";

    /// <summary>
    /// Complete the connector declaration so shared governance truth has the minimum required metadata.
    /// </summary>
    public const string CompleteGovernanceDeclaration = "complete-governance-declaration";

    /// <summary>
    /// Defer control-plane ownership until Cephalon ships write-path connector management for the declared mode.
    /// </summary>
    public const string DeferControlPlane = "defer-control-plane";
}
