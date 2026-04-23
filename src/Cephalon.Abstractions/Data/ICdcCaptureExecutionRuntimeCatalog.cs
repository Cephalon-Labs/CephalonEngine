namespace Cephalon.Abstractions.Data;

/// <summary>
/// Exposes the configured CDC capture execution runtimes visible to the current runtime.
/// </summary>
public interface ICdcCaptureExecutionRuntimeCatalog
{
    /// <summary>
    /// Gets the configured CDC capture execution runtimes visible to the current runtime.
    /// </summary>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> Runtimes { get; }

    /// <summary>
    /// Gets one CDC capture execution runtime by its stable identifier.
    /// </summary>
    /// <param name="executionRuntimeId">The stable execution-runtime identifier to resolve.</param>
    /// <returns>The matching execution-runtime descriptor, or <see langword="null" /> when none exists.</returns>
    CdcCaptureExecutionRuntimeDescriptor? GetById(string executionRuntimeId);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current reporter-coordination story mentions the requested reporter.
    /// </summary>
    /// <param name="reporterId">The reporter identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when the reporter is not currently visible.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByReporterId(string reporterId);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current runtime story mentions the requested edge node.
    /// </summary>
    /// <param name="edgeNodeId">The edge-node identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when the edge node is not currently visible.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByEdgeNodeId(string edgeNodeId);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current reporter-coordination answer matches the requested state.
    /// </summary>
    /// <param name="coordinationState">The stable coordination-state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByReporterCoordinationState(string coordinationState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current reporter-coordination answer matches the requested degraded-reason identifier.
    /// </summary>
    /// <param name="degradedReason">The stable degraded-reason identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that degraded reason.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByReporterCoordinationIssueReason(string degradedReason);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current remediation posture matches the requested state.
    /// </summary>
    /// <param name="remediationState">The stable remediation-state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that remediation state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByRemediationState(string remediationState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current remediation posture includes the requested category.
    /// </summary>
    /// <param name="remediationCategory">The stable remediation-category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that remediation category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByRemediationCategory(string remediationCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector governance answer matches the requested state.
    /// </summary>
    /// <param name="governanceState">The stable governance-state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that governance state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorGovernanceState(string governanceState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector governance answer includes the requested category.
    /// </summary>
    /// <param name="governanceCategory">The stable governance-category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that governance category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorGovernanceCategory(string governanceCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector drift answer matches the requested state.
    /// </summary>
    /// <param name="driftState">The stable drift-state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that drift state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorDriftState(string driftState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector drift answer includes the requested category.
    /// </summary>
    /// <param name="driftCategory">The stable drift-category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that drift category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorDriftCategory(string driftCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector action plan matches the requested state.
    /// </summary>
    /// <param name="actionPlanState">The stable managed-connector action-plan state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that action-plan state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorActionPlanState(string actionPlanState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector action plan includes the requested action identifier.
    /// </summary>
    /// <param name="actionId">The stable managed-connector action identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that action identifier.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorActionId(string actionId);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector write-path readiness answer matches the requested state.
    /// </summary>
    /// <param name="readinessState">The stable managed-connector write-path readiness state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that readiness state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorWritePathReadinessState(string readinessState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector write-path readiness answer includes the requested category.
    /// </summary>
    /// <param name="readinessCategory">The stable managed-connector write-path readiness category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that readiness category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorWritePathReadinessCategory(string readinessCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector preflight answer matches the requested state.
    /// </summary>
    /// <param name="preflightState">The stable managed-connector preflight state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that preflight state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorPreflightState(string preflightState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector preflight answer includes the requested category.
    /// </summary>
    /// <param name="preflightCategory">The stable managed-connector preflight category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that preflight category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorPreflightCategory(string preflightCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector preflight answer currently targets the requested operation.
    /// </summary>
    /// <param name="operationId">The stable managed-connector operation identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that operation identifier.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorPreflightOperationId(string operationId);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector dry-run answer matches the requested state.
    /// </summary>
    /// <param name="dryRunState">The stable managed-connector dry-run state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that dry-run state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorDryRunState(string dryRunState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector dry-run answer includes the requested category.
    /// </summary>
    /// <param name="dryRunCategory">The stable managed-connector dry-run category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that dry-run category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorDryRunCategory(string dryRunCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector dry-run answer currently targets the requested operation.
    /// </summary>
    /// <param name="operationId">The stable managed-connector dry-run operation identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that operation identifier.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorDryRunOperationId(string operationId);
}
