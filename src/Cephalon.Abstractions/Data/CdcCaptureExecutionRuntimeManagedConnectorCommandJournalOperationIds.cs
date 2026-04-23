namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable management-operation identifiers used by managed-connector command-journal answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandJournalOperationIds
{
    /// <summary>
    /// No managed-connector operation is currently associated with the command journal.
    /// </summary>
    public const string None = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryOperationIds.None;

    /// <summary>
    /// The command journal currently targets a future connector reconcile operation.
    /// </summary>
    public const string Reconcile = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryOperationIds.Reconcile;

    /// <summary>
    /// The command journal currently targets a connector pause operation.
    /// </summary>
    public const string Pause = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryOperationIds.Pause;

    /// <summary>
    /// The command journal currently targets a connector resume operation.
    /// </summary>
    public const string Resume = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryOperationIds.Resume;

    /// <summary>
    /// The command journal currently targets a connector restart operation.
    /// </summary>
    public const string Restart = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryOperationIds.Restart;

    /// <summary>
    /// The command journal currently targets a connector delete operation.
    /// </summary>
    public const string Delete = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryOperationIds.Delete;
}
