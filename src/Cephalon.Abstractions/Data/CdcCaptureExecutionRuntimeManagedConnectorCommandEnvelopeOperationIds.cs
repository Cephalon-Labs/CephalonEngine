namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable managed-connector operation identifiers used by command-envelope answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds
{
    /// <summary>
    /// No management operation is currently associated with the command envelope.
    /// </summary>
    public const string None = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.None;

    /// <summary>
    /// The command envelope currently targets connector reconciliation.
    /// </summary>
    public const string Reconcile = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Reconcile;

    /// <summary>
    /// The command envelope currently targets connector pause.
    /// </summary>
    public const string Pause = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Pause;

    /// <summary>
    /// The command envelope currently targets connector resume.
    /// </summary>
    public const string Resume = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Resume;

    /// <summary>
    /// The command envelope currently targets connector restart.
    /// </summary>
    public const string Restart = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Restart;

    /// <summary>
    /// The command envelope currently targets connector deletion.
    /// </summary>
    public const string Delete = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Delete;
}
