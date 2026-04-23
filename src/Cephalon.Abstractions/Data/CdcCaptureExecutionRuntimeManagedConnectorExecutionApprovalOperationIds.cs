namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable managed-connector operation identifiers used by connector-management execution-approval answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds
{
    /// <summary>
    /// No management operation is currently intended for the execution runtime.
    /// </summary>
    public const string None = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.None;

    /// <summary>
    /// Cephalon intends to reconcile the managed connector toward its declared baseline.
    /// </summary>
    public const string Reconcile = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Reconcile;

    /// <summary>
    /// Cephalon intends to pause the managed connector.
    /// </summary>
    public const string Pause = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Pause;

    /// <summary>
    /// Cephalon intends to resume the managed connector.
    /// </summary>
    public const string Resume = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Resume;

    /// <summary>
    /// Cephalon intends to restart the managed connector.
    /// </summary>
    public const string Restart = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Restart;

    /// <summary>
    /// Cephalon intends to delete the managed connector.
    /// </summary>
    public const string Delete = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Delete;
}
