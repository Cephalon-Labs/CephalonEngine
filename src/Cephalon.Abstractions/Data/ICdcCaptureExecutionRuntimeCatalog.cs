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

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector execution intent matches the requested state.
    /// </summary>
    /// <param name="executionIntentState">The stable managed-connector execution-intent state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that execution-intent state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorExecutionIntentState(string executionIntentState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector execution intent includes the requested category.
    /// </summary>
    /// <param name="executionIntentCategory">The stable managed-connector execution-intent category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that execution-intent category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorExecutionIntentCategory(string executionIntentCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector execution intent currently targets the requested operation.
    /// </summary>
    /// <param name="operationId">The stable managed-connector execution-intent operation identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that operation identifier.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorExecutionIntentOperationId(string operationId);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector execution-approval answer matches the requested state.
    /// </summary>
    /// <param name="executionApprovalState">The stable managed-connector execution-approval state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that execution-approval state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorExecutionApprovalState(string executionApprovalState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector execution-approval answer includes the requested category.
    /// </summary>
    /// <param name="executionApprovalCategory">The stable managed-connector execution-approval category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that execution-approval category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorExecutionApprovalCategory(string executionApprovalCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector execution-approval answer currently targets the requested operation.
    /// </summary>
    /// <param name="operationId">The stable managed-connector execution-approval operation identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that operation identifier.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorExecutionApprovalOperationId(string operationId);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector command envelope matches the requested state.
    /// </summary>
    /// <param name="commandState">The stable managed-connector command-envelope state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that command-envelope state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandEnvelopeState(string commandState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector command envelope includes the requested category.
    /// </summary>
    /// <param name="commandCategory">The stable managed-connector command-envelope category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that command-envelope category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandEnvelopeCategory(string commandCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector command envelope currently targets the requested operation.
    /// </summary>
    /// <param name="operationId">The stable managed-connector command-envelope operation identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that operation identifier.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandEnvelopeOperationId(string operationId);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector command issuance matches the requested state.
    /// </summary>
    /// <param name="issuanceState">The stable managed-connector command-issuance state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that command-issuance state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandIssuanceState(string issuanceState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector command issuance includes the requested category.
    /// </summary>
    /// <param name="issuanceCategory">The stable managed-connector command-issuance category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that command-issuance category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandIssuanceCategory(string issuanceCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector command issuance currently targets the requested operation.
    /// </summary>
    /// <param name="operationId">The stable managed-connector command-issuance operation identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that operation identifier.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandIssuanceOperationId(string operationId);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector execution adapter matches the requested state.
    /// </summary>
    /// <param name="executionAdapterState">The stable managed-connector execution-adapter state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that execution-adapter state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorExecutionAdapterState(string executionAdapterState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector execution adapter includes the requested category.
    /// </summary>
    /// <param name="executionAdapterCategory">The stable managed-connector execution-adapter category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that execution-adapter category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorExecutionAdapterCategory(string executionAdapterCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector execution adapter currently targets the requested operation.
    /// </summary>
    /// <param name="operationId">The stable managed-connector execution-adapter operation identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that operation identifier.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorExecutionAdapterOperationId(string operationId);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose latest managed-connector command-execution outcome matches the requested state.
    /// </summary>
    /// <param name="executionState">The stable managed-connector command-execution state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that execution state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandExecutionState(string executionState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose latest managed-connector command-execution outcome references the requested operation.
    /// </summary>
    /// <param name="operationId">The stable managed-connector operation identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that operation identifier.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandExecutionOperationId(string operationId);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector command-retry answer matches the requested state.
    /// </summary>
    /// <param name="retryState">The stable managed-connector command-retry state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that retry state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandRetryState(string retryState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector command-retry answer includes the requested category.
    /// </summary>
    /// <param name="retryCategory">The stable managed-connector command-retry category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that retry category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandRetryCategory(string retryCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector command-retry answer currently targets the requested operation.
    /// </summary>
    /// <param name="operationId">The stable managed-connector command-retry operation identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that operation identifier.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandRetryOperationId(string operationId);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector retry-execution policy answer matches the requested state.
    /// </summary>
    /// <param name="policyState">The stable managed-connector retry-execution policy state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that policy state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorRetryExecutionPolicyState(string policyState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector retry-execution policy answer includes the requested category.
    /// </summary>
    /// <param name="policyCategory">The stable managed-connector retry-execution policy category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that policy category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorRetryExecutionPolicyCategory(string policyCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector retry-execution policy answer currently targets the requested operation.
    /// </summary>
    /// <param name="operationId">The stable managed-connector retry-execution policy operation identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that operation identifier.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorRetryExecutionPolicyOperationId(string operationId);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current bounded managed-connector command-journal answer matches the requested state.
    /// </summary>
    /// <param name="journalState">The stable managed-connector command-journal state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that journal state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandJournalState(string journalState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current bounded managed-connector command-journal answer includes the requested category.
    /// </summary>
    /// <param name="journalCategory">The stable managed-connector command-journal category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that journal category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandJournalCategory(string journalCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector command-journal durability answer matches the requested state.
    /// </summary>
    /// <param name="durabilityState">The stable managed-connector command-journal durability state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that durability state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandJournalDurabilityState(string durabilityState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector command-journal durability answer includes the requested category.
    /// </summary>
    /// <param name="durabilityCategory">The stable managed-connector command-journal durability category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that durability category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandJournalDurabilityCategory(string durabilityCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector automatic background retry execution answer matches the requested state.
    /// </summary>
    /// <param name="automaticRetryState">The stable managed-connector automatic background retry execution state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that automatic background retry execution state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorAutomaticRetryExecutionState(string automaticRetryState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector automatic background retry execution answer includes the requested category.
    /// </summary>
    /// <param name="automaticRetryCategory">The stable managed-connector automatic background retry execution category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that automatic background retry execution category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorAutomaticRetryExecutionCategory(string automaticRetryCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector automatic background retry execution answer currently targets the requested operation.
    /// </summary>
    /// <param name="operationId">The stable managed-connector automatic background retry execution operation identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that operation identifier.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorAutomaticRetryExecutionOperationId(string operationId);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector automatic background retry coordination answer matches the requested state.
    /// </summary>
    /// <param name="coordinationState">The stable managed-connector automatic background retry coordination state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that coordination state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorAutomaticRetryCoordinationState(string coordinationState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector automatic background retry coordination answer includes the requested category.
    /// </summary>
    /// <param name="coordinationCategory">The stable managed-connector automatic background retry coordination category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that coordination category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorAutomaticRetryCoordinationCategory(string coordinationCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector automatic background retry coordination answer references the requested local coordination owner identifier.
    /// </summary>
    /// <param name="ownerId">The stable local coordination owner identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that owner identifier.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorAutomaticRetryCoordinationOwnerId(string ownerId);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector distributed retry lease answer matches the requested state.
    /// </summary>
    /// <param name="leaseState">The stable managed-connector distributed retry lease state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that distributed retry lease state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorDistributedRetryLeaseState(string leaseState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector distributed retry lease answer includes the requested category.
    /// </summary>
    /// <param name="leaseCategory">The stable managed-connector distributed retry lease category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that distributed retry lease category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorDistributedRetryLeaseCategory(string leaseCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector distributed retry lease answer references the requested local coordination owner identifier.
    /// </summary>
    /// <param name="ownerId">The stable local coordination owner identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that owner identifier.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorDistributedRetryLeaseOwnerId(string ownerId);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector cross-node idempotency-hardening answer matches the requested state.
    /// </summary>
    /// <param name="hardeningState">The stable cross-node idempotency-hardening state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that hardening state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCrossNodeIdempotencyHardeningState(string hardeningState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector cross-node idempotency-hardening answer includes the requested category.
    /// </summary>
    /// <param name="hardeningCategory">The stable cross-node idempotency-hardening category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that hardening category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCrossNodeIdempotencyHardeningCategory(string hardeningCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector cross-node idempotency-hardening answer references the requested coordination owner.
    /// </summary>
    /// <param name="ownerId">The stable coordination-owner identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that owner identifier.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCrossNodeIdempotencyHardeningOwnerId(string ownerId);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector cross-node idempotency-hardening answer references the requested retry fingerprint.
    /// </summary>
    /// <param name="retryFingerprint">The stable retry fingerprint to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that retry fingerprint.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCrossNodeIdempotencyHardeningRetryFingerprint(string retryFingerprint);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector distributed retry orchestration answer matches the requested state.
    /// </summary>
    /// <param name="orchestrationState">The stable managed-connector distributed retry orchestration state identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that orchestration state.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorDistributedRetryOrchestrationState(string orchestrationState);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector distributed retry orchestration answer includes the requested category.
    /// </summary>
    /// <param name="orchestrationCategory">The stable managed-connector distributed retry orchestration category identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that orchestration category.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorDistributedRetryOrchestrationCategory(string orchestrationCategory);

    /// <summary>
    /// Gets the CDC capture execution runtimes whose current managed-connector distributed retry orchestration answer references the requested local coordination owner identifier.
    /// </summary>
    /// <param name="ownerId">The stable local coordination owner identifier to filter by.</param>
    /// <returns>The matching execution-runtime descriptors, or an empty list when no runtime currently reports that owner identifier.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorDistributedRetryOrchestrationOwnerId(string ownerId);

    /// <summary>
    /// Gets the bounded managed-connector command-execution history currently recorded for one execution runtime.
    /// </summary>
    /// <param name="executionRuntimeId">The stable execution-runtime identifier to resolve.</param>
    /// <returns>The latest-first bounded command-execution history for the runtime, or an empty list when no outcome has been recorded yet.</returns>
    IReadOnlyList<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult> GetManagedConnectorCommandExecutionHistory(string executionRuntimeId);
}
