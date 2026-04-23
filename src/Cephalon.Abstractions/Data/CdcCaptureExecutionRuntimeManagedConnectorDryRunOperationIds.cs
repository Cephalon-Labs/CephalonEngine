namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable managed-connector operation identifiers used by connector-management dry-run answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds
{
    /// <summary>
    /// No management operation is currently intended for the execution runtime.
    /// </summary>
    public const string None = CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.None;

    /// <summary>
    /// Cephalon would reconcile the managed connector toward its declared baseline.
    /// </summary>
    public const string Reconcile = CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.Reconcile;

    /// <summary>
    /// Cephalon would pause the managed connector.
    /// </summary>
    public const string Pause = CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.Pause;

    /// <summary>
    /// Cephalon would resume the managed connector.
    /// </summary>
    public const string Resume = CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.Resume;

    /// <summary>
    /// Cephalon would restart the managed connector.
    /// </summary>
    public const string Restart = CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.Restart;

    /// <summary>
    /// Cephalon would delete the managed connector.
    /// </summary>
    public const string Delete = CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.Delete;
}
