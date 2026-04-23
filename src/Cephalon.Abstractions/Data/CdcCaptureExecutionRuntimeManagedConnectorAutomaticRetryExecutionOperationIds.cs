namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable management-operation identifiers used by managed-connector automatic background retry execution answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionOperationIds
{
    /// <summary>
    /// No managed-connector operation is currently associated with automatic background retry execution.
    /// </summary>
    public const string None = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.None;

    /// <summary>
    /// Automatic background retry execution currently targets a future connector reconcile operation.
    /// </summary>
    public const string Reconcile = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Reconcile;

    /// <summary>
    /// Automatic background retry execution currently targets a connector pause operation.
    /// </summary>
    public const string Pause = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Pause;

    /// <summary>
    /// Automatic background retry execution currently targets a connector resume operation.
    /// </summary>
    public const string Resume = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Resume;

    /// <summary>
    /// Automatic background retry execution currently targets a connector restart operation.
    /// </summary>
    public const string Restart = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Restart;

    /// <summary>
    /// Automatic background retry execution currently targets a connector delete operation.
    /// </summary>
    public const string Delete = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.Delete;
}
