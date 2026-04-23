namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector command-issuance answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories
{
    /// <summary>
    /// The command issuance is not currently applicable because the runtime remains observe-only.
    /// </summary>
    public const string ObserveOnlyMode = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ObserveOnlyMode;

    /// <summary>
    /// The command issuance is blocked by remediation that still needs to clear first.
    /// </summary>
    public const string BlockingRemediation = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.BlockingRemediation;

    /// <summary>
    /// The command issuance is constrained by stale observation posture.
    /// </summary>
    public const string StaleObservation = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.StaleObservation;

    /// <summary>
    /// The command issuance is constrained by incomplete reporting coverage.
    /// </summary>
    public const string IncompleteReportingCoverage = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.IncompleteReportingCoverage;

    /// <summary>
    /// The command issuance is constrained by incomplete runtime truth.
    /// </summary>
    public const string RuntimeTruthIncomplete = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.RuntimeTruthIncomplete;

    /// <summary>
    /// The command issuance is constrained by governance that is currently out of policy.
    /// </summary>
    public const string GovernanceOutOfPolicy = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.GovernanceOutOfPolicy;

    /// <summary>
    /// The command issuance is constrained because control-plane ownership still remains outside Cephalon.
    /// </summary>
    public const string ControlPlaneOwnershipGap = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ControlPlaneOwnershipGap;

    /// <summary>
    /// The command issuance still reflects one or more shared write-path changes.
    /// </summary>
    public const string ChangePlanned = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ChangePlanned;

    /// <summary>
    /// The command issuance still reflects a lifecycle transition such as pause, resume, restart, or delete.
    /// </summary>
    public const string LifecycleChange = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.LifecycleChange;

    /// <summary>
    /// The command issuance still reflects a destructive write-path such as connector deletion.
    /// </summary>
    public const string DestructiveOperation = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.DestructiveOperation;

    /// <summary>
    /// The command issuance still reflects a higher-risk approval requirement.
    /// </summary>
    public const string ApprovalRequired = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ApprovalRequired;

    /// <summary>
    /// The command issuance is currently approval-ready on the shared issuance lane.
    /// </summary>
    public const string ApprovalReady = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ApprovalReady;

    /// <summary>
    /// The command issuance is currently approval-gated on the shared issuance lane.
    /// </summary>
    public const string ApprovalGated = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ApprovalGated;

    /// <summary>
    /// The command issuance currently remains operator-owned.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.OperatorOnly;

    /// <summary>
    /// The command issuance has been accepted onto a future shared issuance lane.
    /// </summary>
    public const string Accepted = "accepted";

    /// <summary>
    /// The command issuance has been rejected on the shared issuance lane.
    /// </summary>
    public const string Rejected = "rejected";

    /// <summary>
    /// The command issuance has been marked as issued on the shared issuance lane.
    /// </summary>
    public const string Issued = "issued";

    /// <summary>
    /// The command issuance does not currently require additional write-path changes.
    /// </summary>
    public const string NoExecutionNeeded = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.NoExecutionNeeded;
}
