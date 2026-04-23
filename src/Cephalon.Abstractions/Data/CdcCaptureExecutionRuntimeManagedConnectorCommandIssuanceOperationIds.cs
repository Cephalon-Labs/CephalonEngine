namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable managed-connector operation identifiers used by command-issuance answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds
{
    /// <summary>
    /// No management operation is currently associated with command issuance.
    /// </summary>
    public const string None = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.None;

    /// <summary>
    /// The command issuance currently targets connector reconciliation.
    /// </summary>
    public const string Reconcile = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Reconcile;

    /// <summary>
    /// The command issuance currently targets connector pause.
    /// </summary>
    public const string Pause = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Pause;

    /// <summary>
    /// The command issuance currently targets connector resume.
    /// </summary>
    public const string Resume = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Resume;

    /// <summary>
    /// The command issuance currently targets connector restart.
    /// </summary>
    public const string Restart = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Restart;

    /// <summary>
    /// The command issuance currently targets connector deletion.
    /// </summary>
    public const string Delete = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Delete;
}
