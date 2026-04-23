namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable managed-connector operation identifiers used by connector-management execution-intent answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds
{
    /// <summary>
    /// No management operation is currently intended for the execution runtime.
    /// </summary>
    public const string None = CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.None;

    /// <summary>
    /// Cephalon intends to reconcile the managed connector toward its declared baseline.
    /// </summary>
    public const string Reconcile = CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Reconcile;

    /// <summary>
    /// Cephalon intends to pause the managed connector.
    /// </summary>
    public const string Pause = CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Pause;

    /// <summary>
    /// Cephalon intends to resume the managed connector.
    /// </summary>
    public const string Resume = CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Resume;

    /// <summary>
    /// Cephalon intends to restart the managed connector.
    /// </summary>
    public const string Restart = CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Restart;

    /// <summary>
    /// Cephalon intends to delete the managed connector.
    /// </summary>
    public const string Delete = CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Delete;
}
