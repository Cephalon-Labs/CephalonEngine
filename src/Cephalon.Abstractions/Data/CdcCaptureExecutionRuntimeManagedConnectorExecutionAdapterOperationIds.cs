namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable operation identifiers used by managed-connector execution-adapter answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds
{
    /// <summary>
    /// No managed-connector operation currently applies.
    /// </summary>
    public const string None = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.None;

    /// <summary>
    /// Reconcile the connector's declared-versus-observed topology.
    /// </summary>
    public const string Reconcile = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Reconcile;

    /// <summary>
    /// Pause the managed connector.
    /// </summary>
    public const string Pause = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Pause;

    /// <summary>
    /// Resume the managed connector.
    /// </summary>
    public const string Resume = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Resume;

    /// <summary>
    /// Restart the managed connector.
    /// </summary>
    public const string Restart = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Restart;

    /// <summary>
    /// Delete the managed connector.
    /// </summary>
    public const string Delete = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.Delete;
}
