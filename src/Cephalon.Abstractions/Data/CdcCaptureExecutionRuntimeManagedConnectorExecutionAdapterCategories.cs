namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector execution-adapter answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories
{
    /// <summary>
    /// The execution adapter is not currently applicable because the runtime remains observe-only.
    /// </summary>
    public const string ObserveOnlyMode = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ObserveOnlyMode;

    /// <summary>
    /// The execution adapter is blocked by remediation that still needs to clear first.
    /// </summary>
    public const string BlockingRemediation = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.BlockingRemediation;

    /// <summary>
    /// The execution adapter is constrained by stale observation posture.
    /// </summary>
    public const string StaleObservation = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.StaleObservation;

    /// <summary>
    /// The execution adapter is constrained by incomplete reporting coverage.
    /// </summary>
    public const string IncompleteReportingCoverage = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.IncompleteReportingCoverage;

    /// <summary>
    /// The execution adapter is constrained by incomplete runtime truth.
    /// </summary>
    public const string RuntimeTruthIncomplete = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.RuntimeTruthIncomplete;

    /// <summary>
    /// The execution adapter is constrained by governance that is currently out of policy.
    /// </summary>
    public const string GovernanceOutOfPolicy = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.GovernanceOutOfPolicy;

    /// <summary>
    /// The execution adapter is constrained because control-plane ownership still remains outside Cephalon.
    /// </summary>
    public const string ControlPlaneOwnershipGap = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ControlPlaneOwnershipGap;

    /// <summary>
    /// The execution adapter still reflects one or more shared write-path changes.
    /// </summary>
    public const string ChangePlanned = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ChangePlanned;

    /// <summary>
    /// The execution adapter still reflects a lifecycle transition such as pause, resume, restart, or delete.
    /// </summary>
    public const string LifecycleChange = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.LifecycleChange;

    /// <summary>
    /// The execution adapter still reflects a destructive write-path such as connector deletion.
    /// </summary>
    public const string DestructiveOperation = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.DestructiveOperation;

    /// <summary>
    /// The execution adapter still reflects a higher-risk approval requirement.
    /// </summary>
    public const string ApprovalRequired = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ApprovalRequired;

    /// <summary>
    /// The execution adapter is currently approval-ready on the shared execution lane.
    /// </summary>
    public const string ApprovalReady = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ApprovalReady;

    /// <summary>
    /// The execution adapter remains operator-owned.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.OperatorOnly;

    /// <summary>
    /// The execution adapter is currently ready to translate provider-facing commands.
    /// </summary>
    public const string AdapterReady = "adapter-ready";

    /// <summary>
    /// The execution adapter is currently unavailable for the active managed connector.
    /// </summary>
    public const string AdapterUnavailable = "adapter-unavailable";

    /// <summary>
    /// The current provider execution lane does not require an outbound provider command.
    /// </summary>
    public const string NoExecutionNeeded = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.NoExecutionNeeded;
}
