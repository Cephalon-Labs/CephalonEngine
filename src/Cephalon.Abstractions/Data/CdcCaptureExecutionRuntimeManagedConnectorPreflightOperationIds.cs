namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable managed-connector management-operation identifiers used by connector-management preflight answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds
{
    /// <summary>
    /// No management operation is currently intended for the execution runtime.
    /// </summary>
    public const string None = CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.None;

    /// <summary>
    /// Cephalon would preflight reconcile-style apply-and-reconcile follow-through for the managed connector.
    /// </summary>
    public const string Reconcile = "reconcile";

    /// <summary>
    /// Cephalon would preflight pause follow-through for the managed connector.
    /// </summary>
    public const string Pause = "pause";

    /// <summary>
    /// Cephalon would preflight resume follow-through for the managed connector.
    /// </summary>
    public const string Resume = "resume";

    /// <summary>
    /// Cephalon would preflight restart follow-through for the managed connector.
    /// </summary>
    public const string Restart = "restart";

    /// <summary>
    /// Cephalon would preflight delete follow-through for the managed connector.
    /// </summary>
    public const string Delete = "delete";
}
