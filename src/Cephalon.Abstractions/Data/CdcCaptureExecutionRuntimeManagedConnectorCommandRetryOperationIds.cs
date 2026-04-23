namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable management-operation identifiers used by managed-connector command-retry answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandRetryOperationIds
{
    /// <summary>
    /// No managed-connector operation is currently associated with the retry posture.
    /// </summary>
    public const string None = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None;

    /// <summary>
    /// The retry posture currently targets a future connector reconcile operation.
    /// </summary>
    public const string Reconcile = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile;

    /// <summary>
    /// The retry posture currently targets a connector pause operation.
    /// </summary>
    public const string Pause = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Pause;

    /// <summary>
    /// The retry posture currently targets a connector resume operation.
    /// </summary>
    public const string Resume = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Resume;

    /// <summary>
    /// The retry posture currently targets a connector restart operation.
    /// </summary>
    public const string Restart = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Restart;

    /// <summary>
    /// The retry posture currently targets a connector delete operation.
    /// </summary>
    public const string Delete = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Delete;
}
