namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector command-envelope answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories
{
    /// <summary>
    /// The command envelope is not currently applicable because the runtime remains observe-only.
    /// </summary>
    public const string ObserveOnlyMode = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.ObserveOnlyMode;

    /// <summary>
    /// The command envelope is blocked by remediation that still needs to clear first.
    /// </summary>
    public const string BlockingRemediation = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.BlockingRemediation;

    /// <summary>
    /// The command envelope is constrained by stale observation posture.
    /// </summary>
    public const string StaleObservation = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.StaleObservation;

    /// <summary>
    /// The command envelope is constrained by incomplete reporting coverage.
    /// </summary>
    public const string IncompleteReportingCoverage = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.IncompleteReportingCoverage;

    /// <summary>
    /// The command envelope is constrained by incomplete runtime truth.
    /// </summary>
    public const string RuntimeTruthIncomplete = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.RuntimeTruthIncomplete;

    /// <summary>
    /// The command envelope is constrained by governance that is currently out of policy.
    /// </summary>
    public const string GovernanceOutOfPolicy = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.GovernanceOutOfPolicy;

    /// <summary>
    /// The command envelope is constrained because control-plane ownership still remains outside Cephalon.
    /// </summary>
    public const string ControlPlaneOwnershipGap = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.ControlPlaneOwnershipGap;

    /// <summary>
    /// The command envelope still reflects one or more shared write-path changes.
    /// </summary>
    public const string ChangePlanned = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.ChangePlanned;

    /// <summary>
    /// The command envelope still reflects a lifecycle transition such as pause, resume, restart, or delete.
    /// </summary>
    public const string LifecycleChange = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.LifecycleChange;

    /// <summary>
    /// The command envelope still reflects a destructive write-path such as connector deletion.
    /// </summary>
    public const string DestructiveOperation = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.DestructiveOperation;

    /// <summary>
    /// The command envelope still reflects a higher-risk approval requirement.
    /// </summary>
    public const string ApprovalRequired = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.ApprovalRequired;

    /// <summary>
    /// The command envelope is currently approval-ready on the shared execution lane.
    /// </summary>
    public const string ApprovalReady = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.ApprovalReady;

    /// <summary>
    /// The command envelope is currently approval-gated on the shared execution lane.
    /// </summary>
    public const string ApprovalGated = "approval-gated";

    /// <summary>
    /// The command envelope currently remains operator-owned.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.OperatorOnly;

    /// <summary>
    /// The command envelope is currently ready on the shared execution lane.
    /// </summary>
    public const string EngineReady = "engine-ready";

    /// <summary>
    /// The command envelope does not currently require additional write-path changes.
    /// </summary>
    public const string NoExecutionNeeded = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.NoExecutionNeeded;
}
