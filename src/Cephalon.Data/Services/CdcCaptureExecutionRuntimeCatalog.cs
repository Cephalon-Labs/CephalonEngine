using Cephalon.Data.Configuration;
using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal sealed class CdcCaptureExecutionRuntimeCatalog : ICdcCaptureExecutionRuntimeCatalog
{
    private const string ExecutionRuntimeMetadataPrefix = "executionRuntime.";
    private const int ManagedConnectorCommandRetryCooldownSeconds = 30;
    private const string ManagedConnectorManagementModeMetadataKey = "managedConnectorManagementMode";
    private const string ConnectClusterIdMetadataKey = "connectClusterId";
    private const string ConnectorClassMetadataKey = "connectorClass";
    private const string SourceProviderIdMetadataKey = "sourceProviderId";
    private const string ManagedConnectorDeclaredConnectClusterIdMetadataKey = "managedConnectorDeclaredConnectClusterId";
    private const string ManagedConnectorDeclaredConnectorClassMetadataKey = "managedConnectorDeclaredConnectorClass";
    private const string ManagedConnectorDeclaredSourceProviderIdMetadataKey = "managedConnectorDeclaredSourceProviderId";
    private const string ManagedConnectorReportedConnectClusterIdMetadataKey = "managedConnectorReportedConnectClusterId";
    private const string ManagedConnectorReportedConnectorClassMetadataKey = "managedConnectorReportedConnectorClass";
    private const string ManagedConnectorReportedSourceProviderIdMetadataKey = "managedConnectorReportedSourceProviderId";
    private const string ManagedConnectorExpectedTaskCountMetadataKey = "managedConnectorExpectedTaskCount";
    private const string ManagedConnectorDeclaredTaskIdsMetadataKey = "managedConnectorDeclaredTaskIds";
    private const string ManagedConnectorReportedTaskCountMetadataKey = "managedConnectorReportedTaskCount";
    private const string ManagedConnectorReportedTaskIdsMetadataKey = "managedConnectorReportedTaskIds";
    private const string ManagedConnectorActiveTaskIdsMetadataKey = "managedConnectorActiveTaskIds";
    private const string ManagedConnectorConnectorLifecycleStateMetadataKey = "managedConnectorConnectorLifecycleState";
    private const string ManagedConnectorTaskReconciliationStateMetadataKey = "managedConnectorTaskReconciliationState";
    private const string ManagedConnectorReconciliationStateMetadataKey = "managedConnectorReconciliationState";
    private const string ManagedConnectorReconciliationReasonMetadataKey = "managedConnectorReconciliationReason";
    private readonly Dictionary<string, CdcCaptureExecutionRuntimeDescriptor> index;
    private readonly ICdcCaptureCatalog captureCatalog;
    private readonly ICdcCaptureRuntimeStateCatalog? runtimeStateCatalog;
    private readonly ICdcCaptureExecutionRuntimeManagedConnectorExecutionAdapter[] executionAdapters;
    private readonly ManagedConnectorCommandExecutionHistoryStore? commandExecutionHistoryStore;
    private readonly TimeProvider timeProvider;
    private readonly bool managedConnectorAutomaticRetryEnabled;
    private readonly string? managedConnectorAutomaticRetryCoordinationOwnerId;

    public CdcCaptureExecutionRuntimeCatalog(
        CdcCaptureExecutionRuntimeDescriptorCatalog runtimeDescriptorCatalog,
        ICdcCaptureCatalog captureCatalog,
        ICdcCaptureRuntimeStateCatalog? runtimeStateCatalog = null,
        IEnumerable<ICdcCaptureExecutionRuntimeManagedConnectorExecutionAdapter>? executionAdapters = null,
        ManagedConnectorCommandExecutionHistoryStore? commandExecutionHistoryStore = null,
        DataRuntimeOptions? options = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(runtimeDescriptorCatalog);
        ArgumentNullException.ThrowIfNull(captureCatalog);

        this.captureCatalog = captureCatalog;
        this.runtimeStateCatalog = runtimeStateCatalog;
        this.executionAdapters = executionAdapters?.ToArray() ?? [];
        this.commandExecutionHistoryStore = commandExecutionHistoryStore;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        managedConnectorAutomaticRetryEnabled = options?.EnableManagedConnectorAutomaticRetryExecution ?? false;
        managedConnectorAutomaticRetryCoordinationOwnerId = string.IsNullOrWhiteSpace(options?.ManagedConnectorAutomaticRetryCoordinationOwnerId)
            ? null
            : options!.ManagedConnectorAutomaticRetryCoordinationOwnerId!.Trim();
        var runtimes = runtimeDescriptorCatalog.Runtimes;
        index = runtimes.ToDictionary(static runtime => runtime.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> Runtimes => index.Values
        .Select(Enrich)
        .OrderBy(static runtime => runtime.DisplayName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public CdcCaptureExecutionRuntimeDescriptor? GetById(string executionRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(executionRuntimeId))
        {
            return null;
        }

        return index.TryGetValue(executionRuntimeId.Trim(), out var runtime)
            ? Enrich(runtime)
            : null;
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByReporterId(string reporterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reporterId);
        var normalizedReporterId = reporterId.Trim();

        return FilterRuntimes(runtime => runtime.Summary.ReporterCoordination.ReporterParticipants.Any(
            participant => string.Equals(participant.ReporterId, normalizedReporterId, StringComparison.OrdinalIgnoreCase)));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByEdgeNodeId(string edgeNodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(edgeNodeId);
        var normalizedEdgeNodeId = edgeNodeId.Trim();

        return FilterRuntimes(runtime => runtime.Summary.ObservedEdgeNodeIds.Contains(normalizedEdgeNodeId, StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByReporterCoordinationState(string coordinationState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(coordinationState);
        var normalizedCoordinationState = coordinationState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.Summary.ReporterCoordination.State,
            normalizedCoordinationState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByReporterCoordinationIssueReason(string degradedReason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(degradedReason);
        var normalizedDegradedReason = degradedReason.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.Summary.ReporterCoordination.DegradedReason,
            normalizedDegradedReason,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByRemediationState(string remediationState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(remediationState);
        var normalizedRemediationState = remediationState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.Summary.Remediation.State,
            normalizedRemediationState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByRemediationCategory(string remediationCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(remediationCategory);
        var normalizedRemediationCategory = remediationCategory.Trim();

        return FilterRuntimes(runtime => runtime.Summary.Remediation.CategoryIds.Contains(
            normalizedRemediationCategory,
            StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorGovernanceState(string governanceState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(governanceState);
        var normalizedGovernanceState = governanceState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorGovernance.State,
            normalizedGovernanceState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorGovernanceCategory(string governanceCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(governanceCategory);
        var normalizedGovernanceCategory = governanceCategory.Trim();

        return FilterRuntimes(runtime => runtime.ManagedConnectorGovernance.CategoryIds.Contains(
            normalizedGovernanceCategory,
            StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorDriftState(string driftState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(driftState);
        var normalizedDriftState = driftState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorDrift.State,
            normalizedDriftState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorDriftCategory(string driftCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(driftCategory);
        var normalizedDriftCategory = driftCategory.Trim();

        return FilterRuntimes(runtime => runtime.ManagedConnectorDrift.CategoryIds.Contains(
            normalizedDriftCategory,
            StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorActionPlanState(string actionPlanState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionPlanState);
        var normalizedActionPlanState = actionPlanState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorActionPlan.State,
            normalizedActionPlanState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorActionId(string actionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        var normalizedActionId = actionId.Trim();

        return FilterRuntimes(runtime =>
            string.Equals(
                runtime.ManagedConnectorActionPlan.PrimaryActionId,
                normalizedActionId,
                StringComparison.OrdinalIgnoreCase) ||
            runtime.ManagedConnectorActionPlan.ActionIds.Contains(
                normalizedActionId,
                StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorWritePathReadinessState(string readinessState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(readinessState);
        var normalizedReadinessState = readinessState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorWritePathReadiness.State,
            normalizedReadinessState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorWritePathReadinessCategory(string readinessCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(readinessCategory);
        var normalizedReadinessCategory = readinessCategory.Trim();

        return FilterRuntimes(runtime => runtime.ManagedConnectorWritePathReadiness.CategoryIds.Contains(
            normalizedReadinessCategory,
            StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorPreflightState(string preflightState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(preflightState);
        var normalizedPreflightState = preflightState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorPreflight.State,
            normalizedPreflightState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorPreflightCategory(string preflightCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(preflightCategory);
        var normalizedPreflightCategory = preflightCategory.Trim();

        return FilterRuntimes(runtime => runtime.ManagedConnectorPreflight.CategoryIds.Contains(
            normalizedPreflightCategory,
            StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorPreflightOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorPreflight.OperationId,
            normalizedOperationId,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorDryRunState(string dryRunState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dryRunState);
        var normalizedDryRunState = dryRunState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorDryRun.State,
            normalizedDryRunState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorDryRunCategory(string dryRunCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dryRunCategory);
        var normalizedDryRunCategory = dryRunCategory.Trim();

        return FilterRuntimes(runtime => runtime.ManagedConnectorDryRun.CategoryIds.Contains(
            normalizedDryRunCategory,
            StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorDryRunOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorDryRun.OperationId,
            normalizedOperationId,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorExecutionIntentState(string executionIntentState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionIntentState);
        var normalizedExecutionIntentState = executionIntentState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorExecutionIntent.State,
            normalizedExecutionIntentState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorExecutionIntentCategory(string executionIntentCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionIntentCategory);
        var normalizedExecutionIntentCategory = executionIntentCategory.Trim();

        return FilterRuntimes(runtime => runtime.ManagedConnectorExecutionIntent.CategoryIds.Contains(
            normalizedExecutionIntentCategory,
            StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorExecutionIntentOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorExecutionIntent.OperationId,
            normalizedOperationId,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorExecutionApprovalState(string executionApprovalState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionApprovalState);
        var normalizedExecutionApprovalState = executionApprovalState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorExecutionApproval.State,
            normalizedExecutionApprovalState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorExecutionApprovalCategory(string executionApprovalCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionApprovalCategory);
        var normalizedExecutionApprovalCategory = executionApprovalCategory.Trim();

        return FilterRuntimes(runtime => runtime.ManagedConnectorExecutionApproval.CategoryIds.Contains(
            normalizedExecutionApprovalCategory,
            StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorExecutionApprovalOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorExecutionApproval.OperationId,
            normalizedOperationId,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandEnvelopeState(string commandState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandState);
        var normalizedCommandState = commandState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorCommandEnvelope.State,
            normalizedCommandState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandEnvelopeCategory(string commandCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandCategory);
        var normalizedCommandCategory = commandCategory.Trim();

        return FilterRuntimes(runtime => runtime.ManagedConnectorCommandEnvelope.CategoryIds.Contains(
            normalizedCommandCategory,
            StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandEnvelopeOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorCommandEnvelope.OperationId,
            normalizedOperationId,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandIssuanceState(string issuanceState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuanceState);
        var normalizedIssuanceState = issuanceState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorCommandIssuance.State,
            normalizedIssuanceState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandIssuanceCategory(string issuanceCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuanceCategory);
        var normalizedIssuanceCategory = issuanceCategory.Trim();

        return FilterRuntimes(runtime => runtime.ManagedConnectorCommandIssuance.CategoryIds.Contains(
            normalizedIssuanceCategory,
            StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandIssuanceOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorCommandIssuance.OperationId,
            normalizedOperationId,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorExecutionAdapterState(string executionAdapterState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionAdapterState);
        var normalizedExecutionAdapterState = executionAdapterState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorExecutionAdapter.State,
            normalizedExecutionAdapterState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorExecutionAdapterCategory(string executionAdapterCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionAdapterCategory);
        var normalizedExecutionAdapterCategory = executionAdapterCategory.Trim();

        return FilterRuntimes(runtime => runtime.ManagedConnectorExecutionAdapter.CategoryIds.Contains(
            normalizedExecutionAdapterCategory,
            StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorExecutionAdapterOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorExecutionAdapter.OperationId,
            normalizedOperationId,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandExecutionState(string executionState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionState);
        var normalizedExecutionState = executionState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorCommandExecution.State,
            normalizedExecutionState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandExecutionOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();

        return FilterRuntimes(runtime =>
            string.Equals(
                runtime.ManagedConnectorCommandExecution.RequestedOperationId,
                normalizedOperationId,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                runtime.ManagedConnectorCommandExecution.ResolvedOperationId,
                normalizedOperationId,
                StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandRetryState(string retryState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(retryState);
        var normalizedRetryState = retryState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorCommandRetry.State,
            normalizedRetryState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandRetryCategory(string retryCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(retryCategory);
        var normalizedRetryCategory = retryCategory.Trim();

        return FilterRuntimes(runtime => runtime.ManagedConnectorCommandRetry.CategoryIds.Contains(
            normalizedRetryCategory,
            StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandRetryOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorCommandRetry.OperationId,
            normalizedOperationId,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorRetryExecutionPolicyState(string policyState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyState);
        var normalizedPolicyState = policyState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorRetryExecutionPolicy.State,
            normalizedPolicyState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorRetryExecutionPolicyCategory(string policyCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyCategory);
        var normalizedPolicyCategory = policyCategory.Trim();

        return FilterRuntimes(runtime => runtime.ManagedConnectorRetryExecutionPolicy.CategoryIds.Contains(
            normalizedPolicyCategory,
            StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorRetryExecutionPolicyOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorRetryExecutionPolicy.OperationId,
            normalizedOperationId,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandJournalState(string journalState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalState);
        var normalizedJournalState = journalState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorCommandJournal.State,
            normalizedJournalState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandJournalCategory(string journalCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalCategory);
        var normalizedJournalCategory = journalCategory.Trim();

        return FilterRuntimes(runtime => runtime.ManagedConnectorCommandJournal.CategoryIds.Contains(
            normalizedJournalCategory,
            StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandJournalDurabilityState(string durabilityState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(durabilityState);
        var normalizedDurabilityState = durabilityState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorCommandJournalDurability.State,
            normalizedDurabilityState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorCommandJournalDurabilityCategory(string durabilityCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(durabilityCategory);
        var normalizedDurabilityCategory = durabilityCategory.Trim();

        return FilterRuntimes(runtime => runtime.ManagedConnectorCommandJournalDurability.CategoryIds.Contains(
            normalizedDurabilityCategory,
            StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorAutomaticRetryExecutionState(string automaticRetryState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automaticRetryState);
        var normalizedAutomaticRetryState = automaticRetryState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorAutomaticRetryExecution.State,
            normalizedAutomaticRetryState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorAutomaticRetryExecutionCategory(string automaticRetryCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automaticRetryCategory);
        var normalizedAutomaticRetryCategory = automaticRetryCategory.Trim();

        return FilterRuntimes(runtime => runtime.ManagedConnectorAutomaticRetryExecution.CategoryIds.Contains(
            normalizedAutomaticRetryCategory,
            StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorAutomaticRetryExecutionOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorAutomaticRetryExecution.OperationId,
            normalizedOperationId,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorAutomaticRetryCoordinationState(string coordinationState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(coordinationState);
        var normalizedCoordinationState = coordinationState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorAutomaticRetryCoordination.State,
            normalizedCoordinationState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorAutomaticRetryCoordinationCategory(string coordinationCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(coordinationCategory);
        var normalizedCoordinationCategory = coordinationCategory.Trim();

        return FilterRuntimes(runtime => runtime.ManagedConnectorAutomaticRetryCoordination.CategoryIds.Contains(
            normalizedCoordinationCategory,
            StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByManagedConnectorAutomaticRetryCoordinationOwnerId(string ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        var normalizedOwnerId = ownerId.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.ManagedConnectorAutomaticRetryCoordination.CoordinationOwnerId,
            normalizedOwnerId,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult> GetManagedConnectorCommandExecutionHistory(string executionRuntimeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);

        return commandExecutionHistoryStore?.GetHistory(executionRuntimeId.Trim()) ?? [];
    }

    private CdcCaptureExecutionRuntimeDescriptor[] FilterRuntimes(
        Func<CdcCaptureExecutionRuntimeDescriptor, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return index.Values
            .Select(Enrich)
            .Where(predicate)
            .OrderBy(static runtime => runtime.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private CdcCaptureExecutionRuntimeDescriptor Enrich(CdcCaptureExecutionRuntimeDescriptor runtime)
    {
        var captureIds = ResolveCaptureIds(runtime.Id);
        var matchingStates = runtimeStateCatalog?.GetByExecutionRuntimeId(runtime.Id) ?? [];
        var mergedMetadata = MergeRuntimeMetadata(runtime.Metadata, matchingStates);
        var summary = matchingStates.Count == 0
            ? CreateEmptySummary(runtime, captureIds)
            : CreateSummary(runtime, captureIds, matchingStates);
        var managedConnectorGovernance = CreateManagedConnectorGovernance(runtime.ExecutionTopology, mergedMetadata);
        var managedConnectorDrift = CreateManagedConnectorDrift(runtime.ExecutionTopology, mergedMetadata);
        var managedConnectorActionPlan = CreateManagedConnectorActionPlan(
            runtime.ExecutionTopology,
            summary.Remediation,
            managedConnectorGovernance,
            managedConnectorDrift);
        var managedConnectorWritePathReadiness = CreateManagedConnectorWritePathReadiness(
            runtime.ExecutionTopology,
            summary.ReportingCoverage,
            summary.Remediation,
            managedConnectorGovernance,
            managedConnectorDrift,
            managedConnectorActionPlan);
        var managedConnectorPreflight = CreateManagedConnectorPreflight(
            runtime.ExecutionTopology,
            summary.ReportingCoverage,
            summary.Remediation,
            managedConnectorGovernance,
            managedConnectorDrift,
            managedConnectorActionPlan,
            managedConnectorWritePathReadiness);
        var managedConnectorDryRun = CreateManagedConnectorDryRun(
            runtime.ExecutionTopology,
            summary.ReportingCoverage,
            summary.Remediation,
            managedConnectorGovernance,
            managedConnectorDrift,
            managedConnectorActionPlan,
            managedConnectorWritePathReadiness,
            managedConnectorPreflight);
        var managedConnectorExecutionIntent = CreateManagedConnectorExecutionIntent(
            runtime.ExecutionTopology,
            summary.ReportingCoverage,
            summary.Remediation,
            managedConnectorGovernance,
            managedConnectorDrift,
            managedConnectorActionPlan,
            managedConnectorWritePathReadiness,
            managedConnectorPreflight,
            managedConnectorDryRun);
        var managedConnectorExecutionApproval = CreateManagedConnectorExecutionApproval(
            runtime.ExecutionTopology,
            summary.ReportingCoverage,
            summary.Remediation,
            managedConnectorGovernance,
            managedConnectorDrift,
            managedConnectorActionPlan,
            managedConnectorWritePathReadiness,
            managedConnectorPreflight,
            managedConnectorDryRun,
            managedConnectorExecutionIntent);
        var managedConnectorMetadata = ResolveManagedConnectorMetadata(mergedMetadata);
        var managedConnectorCommandEnvelope = CreateManagedConnectorCommandEnvelope(
            runtime.Id,
            captureIds,
            runtime.ExecutionTopology,
            summary.ReportingCoverage,
            summary.Remediation,
            managedConnectorGovernance,
            managedConnectorDrift,
            managedConnectorActionPlan,
            managedConnectorWritePathReadiness,
            managedConnectorPreflight,
            managedConnectorDryRun,
            managedConnectorExecutionIntent,
            managedConnectorExecutionApproval,
            managedConnectorMetadata);
        var managedConnectorCommandIssuance = CreateManagedConnectorCommandIssuance(
            runtime.Id,
            captureIds,
            runtime.ExecutionTopology,
            summary.ReportingCoverage,
            summary.Remediation,
            managedConnectorGovernance,
            managedConnectorDrift,
            managedConnectorActionPlan,
            managedConnectorWritePathReadiness,
            managedConnectorPreflight,
            managedConnectorDryRun,
            managedConnectorExecutionIntent,
            managedConnectorExecutionApproval,
            managedConnectorCommandEnvelope);
        var provisionalRuntime = new CdcCaptureExecutionRuntimeDescriptor(
            id: runtime.Id,
            displayName: runtime.DisplayName,
            description: runtime.Description,
            metadata: mergedMetadata,
            cdcCaptureIds: captureIds,
            summary: summary)
        {
            ManagedConnectorGovernance = managedConnectorGovernance,
            ManagedConnectorDrift = managedConnectorDrift,
            ManagedConnectorActionPlan = managedConnectorActionPlan,
            ManagedConnectorWritePathReadiness = managedConnectorWritePathReadiness,
            ManagedConnectorPreflight = managedConnectorPreflight,
            ManagedConnectorDryRun = managedConnectorDryRun,
            ManagedConnectorExecutionIntent = managedConnectorExecutionIntent,
            ManagedConnectorExecutionApproval = managedConnectorExecutionApproval,
            ManagedConnectorCommandEnvelope = managedConnectorCommandEnvelope,
            ManagedConnectorCommandIssuance = managedConnectorCommandIssuance
        };
        var managedConnectorExecutionAdapter = CreateManagedConnectorExecutionAdapter(
            provisionalRuntime,
            summary.ReportingCoverage,
            summary.Remediation,
            managedConnectorGovernance,
            managedConnectorDrift,
            managedConnectorActionPlan,
            managedConnectorWritePathReadiness,
            managedConnectorPreflight,
            managedConnectorDryRun,
            managedConnectorExecutionIntent,
            managedConnectorExecutionApproval,
            managedConnectorCommandEnvelope,
            managedConnectorCommandIssuance,
            managedConnectorMetadata);
        var commandExecutionJournal = commandExecutionHistoryStore?.GetJournal(runtime.Id) ?? ManagedConnectorCommandExecutionJournal.Empty(runtime.Id);
        var commandJournalDurability = commandExecutionHistoryStore?.GetDurabilitySnapshot(runtime.Id) ??
                                       ManagedConnectorCommandExecutionJournalDurabilitySnapshot.Empty(runtime.Id);
        var commandExecutionHistory = commandExecutionJournal.Entries;
        var managedConnectorCommandExecution = CreateManagedConnectorCommandExecution(
            provisionalRuntime,
            managedConnectorExecutionAdapter,
            commandExecutionHistory.Count > 0 ? commandExecutionHistory[0] : null);
        var managedConnectorCommandRetry = CreateManagedConnectorCommandRetry(
            runtime.Id,
            captureIds,
            runtime.ExecutionTopology,
            summary.ReportingCoverage,
            summary.Remediation,
            managedConnectorGovernance,
            managedConnectorDrift,
            managedConnectorActionPlan,
            managedConnectorWritePathReadiness,
            managedConnectorPreflight,
            managedConnectorDryRun,
            managedConnectorExecutionIntent,
            managedConnectorExecutionApproval,
            managedConnectorCommandEnvelope,
            managedConnectorCommandIssuance,
            managedConnectorExecutionAdapter,
            managedConnectorCommandExecution,
            commandExecutionHistory,
            timeProvider.GetUtcNow());
        var managedConnectorRetryExecutionPolicy = CreateManagedConnectorRetryExecutionPolicy(
            runtime.Id,
            captureIds,
            runtime.ExecutionTopology,
            summary.ReportingCoverage,
            summary.Remediation,
            managedConnectorGovernance,
            managedConnectorDrift,
            managedConnectorActionPlan,
            managedConnectorWritePathReadiness,
            managedConnectorPreflight,
            managedConnectorDryRun,
            managedConnectorExecutionIntent,
            managedConnectorExecutionApproval,
            managedConnectorCommandEnvelope,
            managedConnectorCommandIssuance,
            managedConnectorExecutionAdapter,
            managedConnectorCommandExecution,
            managedConnectorCommandRetry,
            managedConnectorAutomaticRetryEnabled);
        var managedConnectorCommandJournal = CreateManagedConnectorCommandJournal(
            runtime.Id,
            captureIds,
            runtime.ExecutionTopology,
            summary.ReportingCoverage,
            summary.Remediation,
            managedConnectorGovernance,
            managedConnectorDrift,
            managedConnectorActionPlan,
            managedConnectorWritePathReadiness,
            managedConnectorPreflight,
            managedConnectorDryRun,
            managedConnectorExecutionIntent,
            managedConnectorExecutionApproval,
            managedConnectorCommandEnvelope,
            managedConnectorCommandIssuance,
            managedConnectorExecutionAdapter,
            managedConnectorCommandExecution,
            managedConnectorCommandRetry,
            managedConnectorRetryExecutionPolicy,
            commandExecutionJournal);
        var managedConnectorAutomaticRetryExecution = CreateManagedConnectorAutomaticRetryExecution(
            runtime.Id,
            captureIds,
            runtime.ExecutionTopology,
            summary.ReportingCoverage,
            summary.Remediation,
            managedConnectorGovernance,
            managedConnectorDrift,
            managedConnectorActionPlan,
            managedConnectorWritePathReadiness,
            managedConnectorPreflight,
            managedConnectorDryRun,
            managedConnectorExecutionIntent,
            managedConnectorExecutionApproval,
            managedConnectorCommandEnvelope,
            managedConnectorCommandIssuance,
            managedConnectorExecutionAdapter,
            managedConnectorCommandExecution,
            managedConnectorCommandRetry,
            managedConnectorRetryExecutionPolicy,
            managedConnectorCommandJournal,
            commandExecutionJournal,
            managedConnectorAutomaticRetryEnabled);
        var managedConnectorAutomaticRetryCoordination = CreateManagedConnectorAutomaticRetryCoordination(
            runtime.Id,
            captureIds,
            runtime.ExecutionOwnership,
            runtime.ExecutionTopology,
            managedConnectorRetryExecutionPolicy.ManagementMode ?? managedConnectorAutomaticRetryExecution.ManagementMode,
            summary,
            managedConnectorAutomaticRetryExecution,
            managedConnectorRetryExecutionPolicy,
            managedConnectorCommandJournal,
            managedConnectorAutomaticRetryCoordinationOwnerId);
        var managedConnectorCommandJournalDurability = CreateManagedConnectorCommandJournalDurability(
            runtime.Id,
            captureIds,
            runtime.ExecutionTopology,
            managedConnectorRetryExecutionPolicy.ManagementMode ?? managedConnectorAutomaticRetryExecution.ManagementMode,
            managedConnectorCommandJournal,
            managedConnectorAutomaticRetryExecution,
            managedConnectorAutomaticRetryCoordination,
            commandJournalDurability);

        return new CdcCaptureExecutionRuntimeDescriptor(
            id: runtime.Id,
            displayName: runtime.DisplayName,
            description: runtime.Description,
            metadata: mergedMetadata,
            cdcCaptureIds: captureIds,
            summary: summary)
        {
            ManagedConnectorGovernance = managedConnectorGovernance,
            ManagedConnectorDrift = managedConnectorDrift,
            ManagedConnectorActionPlan = managedConnectorActionPlan,
            ManagedConnectorWritePathReadiness = managedConnectorWritePathReadiness,
            ManagedConnectorPreflight = managedConnectorPreflight,
            ManagedConnectorDryRun = managedConnectorDryRun,
            ManagedConnectorExecutionIntent = managedConnectorExecutionIntent,
            ManagedConnectorExecutionApproval = managedConnectorExecutionApproval,
            ManagedConnectorCommandEnvelope = managedConnectorCommandEnvelope,
            ManagedConnectorCommandIssuance = managedConnectorCommandIssuance,
            ManagedConnectorExecutionAdapter = managedConnectorExecutionAdapter,
            ManagedConnectorCommandExecution = managedConnectorCommandExecution,
            ManagedConnectorCommandRetry = managedConnectorCommandRetry,
            ManagedConnectorRetryExecutionPolicy = managedConnectorRetryExecutionPolicy,
            ManagedConnectorCommandJournal = managedConnectorCommandJournal,
            ManagedConnectorCommandJournalDurability = managedConnectorCommandJournalDurability,
            ManagedConnectorAutomaticRetryExecution = managedConnectorAutomaticRetryExecution,
            ManagedConnectorAutomaticRetryCoordination = managedConnectorAutomaticRetryCoordination
        };
    }

    private static CdcCaptureExecutionRuntimeSummary CreateEmptySummary(
        CdcCaptureExecutionRuntimeDescriptor runtime,
        IReadOnlyList<string> captureIds)
    {
        var reporterCoordination = CreateReporterCoordination(runtime);
        var reportingCoverage = CreateReportingCoverage(captureIds, []);

        return CdcCaptureExecutionRuntimeSummary.Empty with
        {
            ReporterCoordination = reporterCoordination,
            ReportingCoverage = reportingCoverage,
            Remediation = CreateRemediation(reportingCoverage, [])
        };
    }

    private CdcCaptureExecutionRuntimeSummary CreateSummary(
        CdcCaptureExecutionRuntimeDescriptor runtime,
        IReadOnlyList<string> captureIds,
        IReadOnlyList<CdcCaptureRuntimeState> matchingStates)
    {
        var reportedCaptureIds = matchingStates
            .Where(static state => state.HasReports)
            .Select(static state => state.CdcCaptureId)
            .Where(static cdcCaptureId => !string.IsNullOrWhiteSpace(cdcCaptureId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static cdcCaptureId => cdcCaptureId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var latestState = matchingStates
            .OrderByDescending(static state => state.LastObservedAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(static state => state.CdcCaptureId, StringComparer.OrdinalIgnoreCase)
            .First();
        latestState.Metadata.TryGetValue("acknowledgement", out var lastAcknowledgement);
        var activeReporterId = ResolveActiveReporterId(matchingStates, timeProvider.GetUtcNow());
        var reportingCoverage = CreateReportingCoverage(captureIds, matchingStates);
        var remediation = CreateRemediation(reportingCoverage, matchingStates);

        return new CdcCaptureExecutionRuntimeSummary(
            ReportedCdcCaptureIds: reportedCaptureIds,
            LastCdcCaptureId: latestState.CdcCaptureId,
            LastOutcome: latestState.LastOutcome,
            LastObservedAtUtc: latestState.LastObservedAtUtc,
            LastReportId: latestState.LastReportId,
            LastChangeId: latestState.LastChangeId,
            LastCheckpoint: latestState.LastCheckpoint,
            StartedCount: matchingStates.Sum(static state => state.StartedCount),
            CapturedCount: matchingStates.Sum(static state => state.CapturedCount),
            IdleCount: matchingStates.Sum(static state => state.IdleCount),
            FailedCount: matchingStates.Sum(static state => state.FailedCount),
            TotalCapturedChangeCount: matchingStates.Sum(static state => state.TotalCapturedChangeCount),
            TotalProducedMessageCount: matchingStates.Sum(static state => state.TotalProducedMessageCount),
            LastAcknowledgement: string.IsNullOrWhiteSpace(lastAcknowledgement) ? null : lastAcknowledgement.Trim(),
            LastError: latestState.LastError,
            ObservationFreshness: AggregateObservationFreshness(matchingStates))
        {
            ReporterCoordination = latestState.ReporterCoordination,
            ReportingCoverage = reportingCoverage,
            Remediation = remediation,
            ReporterCoordinationRollup = CreateReporterCoordinationRollup(matchingStates),
            LastReporterId = latestState.LastReporterId,
            ActiveReporterId = activeReporterId,
            ReporterLeaseExpiresAtUtc = ResolveActiveReporterLeaseExpiry(matchingStates, activeReporterId),
            ObservedEdgeNodeIds = matchingStates
                .Select(static state => state.LastEdgeNodeId)
                .Where(static edgeNodeId => !string.IsNullOrWhiteSpace(edgeNodeId))
                .Select(static edgeNodeId => edgeNodeId!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static edgeNodeId => edgeNodeId, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            LastEdgeNodeId = latestState.LastEdgeNodeId
        };
    }

    private static CdcCaptureExecutionRuntimeRemediationStatus CreateRemediation(
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        IReadOnlyList<CdcCaptureRuntimeState> matchingStates)
    {
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(matchingStates);

        var unreportedCaptureIds = reportingCoverage.UnreportedCdcCaptureIds
            .OrderBy(static cdcCaptureId => cdcCaptureId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var staleCaptureIds = matchingStates
            .Where(static state => state.HasReports && state.IsObservationStale)
            .Select(static state => state.CdcCaptureId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static cdcCaptureId => cdcCaptureId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var failedCaptureIds = matchingStates
            .Where(static state => state.HasReports && state.IsFailed)
            .Select(static state => state.CdcCaptureId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static cdcCaptureId => cdcCaptureId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var reporterCoordinationIssueCaptureIds = matchingStates
            .Where(static state => state.HasReports && state.HasReporterCoordinationIssue)
            .Select(static state => state.CdcCaptureId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static cdcCaptureId => cdcCaptureId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var categories = new List<string>(capacity: 4);

        if (failedCaptureIds.Length > 0)
        {
            categories.Add(CdcCaptureExecutionRuntimeRemediationCategories.FailedCdcCaptures);
        }

        if (reporterCoordinationIssueCaptureIds.Length > 0)
        {
            categories.Add(CdcCaptureExecutionRuntimeRemediationCategories.ReporterCoordinationIssues);
        }

        if (staleCaptureIds.Length > 0)
        {
            categories.Add(CdcCaptureExecutionRuntimeRemediationCategories.StaleObservations);
        }

        if (unreportedCaptureIds.Length > 0)
        {
            categories.Add(CdcCaptureExecutionRuntimeRemediationCategories.UnreportedCdcCaptures);
        }

        if (categories.Count == 0)
        {
            return new CdcCaptureExecutionRuntimeRemediationStatus(
                CdcCaptureExecutionRuntimeRemediationStates.Ready,
                reportingCoverage.DeclaredCaptureCount == 0
                    ? "The execution runtime does not currently require remediation because it does not resolve to any CDC captures."
                    : "The execution runtime does not currently require remediation.");
        }

        var affectedCaptureIds = failedCaptureIds
            .Concat(reporterCoordinationIssueCaptureIds)
            .Concat(staleCaptureIds)
            .Concat(unreportedCaptureIds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static cdcCaptureId => cdcCaptureId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new CdcCaptureExecutionRuntimeRemediationStatus(
            failedCaptureIds.Length > 0
                ? CdcCaptureExecutionRuntimeRemediationStates.Blocked
                : CdcCaptureExecutionRuntimeRemediationStates.Attention,
            CreateRemediationDescription(
                failedCaptureIds,
                reporterCoordinationIssueCaptureIds,
                staleCaptureIds,
                unreportedCaptureIds))
        {
            CategoryIds = categories,
            AffectedCdcCaptureIds = affectedCaptureIds,
            UnreportedCdcCaptureIds = unreportedCaptureIds,
            StaleCdcCaptureIds = staleCaptureIds,
            FailedCdcCaptureIds = failedCaptureIds,
            ReporterCoordinationIssueCdcCaptureIds = reporterCoordinationIssueCaptureIds
        };
    }

    private static string CreateRemediationDescription(
        string[] failedCaptureIds,
        string[] reporterCoordinationIssueCaptureIds,
        string[] staleCaptureIds,
        string[] unreportedCaptureIds)
    {
        var messages = new List<string>(capacity: 4);

        if (failedCaptureIds.Length > 0)
        {
            messages.Add(failedCaptureIds.Length == 1
                ? $"CDC capture '{failedCaptureIds[0]}' currently reports a failed external runtime outcome."
                : $"{failedCaptureIds.Length} CDC captures currently report failed external runtime outcomes.");
        }

        if (reporterCoordinationIssueCaptureIds.Length > 0)
        {
            messages.Add(reporterCoordinationIssueCaptureIds.Length == 1
                ? $"CDC capture '{reporterCoordinationIssueCaptureIds[0]}' currently reports degraded reporter coordination."
                : $"{reporterCoordinationIssueCaptureIds.Length} CDC captures currently report degraded reporter coordination.");
        }

        if (staleCaptureIds.Length > 0)
        {
            messages.Add(staleCaptureIds.Length == 1
                ? $"CDC capture '{staleCaptureIds[0]}' currently reports a stale observation."
                : $"{staleCaptureIds.Length} CDC captures currently report stale observations.");
        }

        if (unreportedCaptureIds.Length > 0)
        {
            messages.Add(unreportedCaptureIds.Length == 1
                ? $"Declared CDC capture '{unreportedCaptureIds[0]}' has not reported runtime state yet."
                : $"{unreportedCaptureIds.Length} declared CDC captures have not reported runtime state yet.");
        }

        return string.Join(" ", messages);
    }

    private static Dictionary<string, string> MergeRuntimeMetadata(
        IReadOnlyDictionary<string, string> runtimeMetadata,
        IReadOnlyList<CdcCaptureRuntimeState> matchingStates)
    {
        var merged = new Dictionary<string, string>(runtimeMetadata, StringComparer.OrdinalIgnoreCase);
        if (matchingStates.Count == 0)
        {
            return merged;
        }

        var latestState = matchingStates
            .OrderByDescending(static state => state.LastObservedAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(static state => state.CdcCaptureId, StringComparer.OrdinalIgnoreCase)
            .First();

        foreach (var pair in latestState.Metadata)
        {
            if (!pair.Key.StartsWith(ExecutionRuntimeMetadataPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var normalizedKey = pair.Key[ExecutionRuntimeMetadataPrefix.Length..].Trim();
            if (string.IsNullOrWhiteSpace(normalizedKey))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(pair.Value))
            {
                merged.Remove(normalizedKey);
                continue;
            }

            merged[normalizedKey] = pair.Value.Trim();
        }

        return merged;
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus CreateManagedConnectorGovernance(
        string executionTopology,
        IReadOnlyDictionary<string, string> metadata)
    {
        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase))
        {
            return new CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus(
                CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.NotApplicable,
                "The execution runtime does not currently represent a managed connector.")
            {
                RecommendedActionId = CdcCaptureExecutionRuntimeManagedConnectorGovernanceActionIds.None
            };
        }

        var snapshot = ResolveManagedConnectorMetadata(metadata);
        var categories = new List<string>(capacity: 5);

        if (string.IsNullOrWhiteSpace(snapshot.ManagementMode))
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.MissingManagementMode);
        }

        if (string.IsNullOrWhiteSpace(snapshot.DeclaredConnectClusterId))
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.MissingConnectClusterId);
        }

        if (string.IsNullOrWhiteSpace(snapshot.DeclaredConnectorClass))
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.MissingConnectorClass);
        }

        if (string.IsNullOrWhiteSpace(snapshot.DeclaredSourceProviderId))
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.MissingSourceProviderId);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.ManagementMode) &&
            !string.Equals(snapshot.ManagementMode, CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.ObserveOnly, StringComparison.OrdinalIgnoreCase))
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.FutureControlPlaneMode);
        }

        if (categories.Any(category => category.StartsWith("missing-", StringComparison.OrdinalIgnoreCase)))
        {
            return new CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus(
                CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.OutOfPolicy,
                CreateManagedConnectorOutOfPolicyDescription(categories, snapshot.ManagementMode))
            {
                CategoryIds = categories,
                ManagementMode = snapshot.ManagementMode,
                ConnectClusterId = snapshot.DeclaredConnectClusterId,
                ConnectorClass = snapshot.DeclaredConnectorClass,
                SourceProviderId = snapshot.DeclaredSourceProviderId,
                ExpectedTaskCount = snapshot.ExpectedTaskCount,
                ReportedTaskCount = snapshot.ReportedTaskCount,
                DeclaredTaskIds = snapshot.DeclaredTaskIds,
                ReportedTaskIds = snapshot.ReportedTaskIds,
                ActiveTaskIds = snapshot.ActiveTaskIds,
                ConnectorLifecycleState = snapshot.ConnectorLifecycleState,
                TaskReconciliationState = snapshot.TaskReconciliationState,
                ReconciliationState = snapshot.ReconciliationState,
                ReconciliationReason = snapshot.ReconciliationReason,
                RecommendedActionId = CdcCaptureExecutionRuntimeManagedConnectorGovernanceActionIds.CompleteGovernanceDeclaration
            };
        }

        if (!string.IsNullOrWhiteSpace(snapshot.ManagementMode) &&
            !string.Equals(snapshot.ManagementMode, CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.ObserveOnly, StringComparison.OrdinalIgnoreCase))
        {
            return new CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus(
                CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.FutureControlPlane,
                $"The managed connector declares management mode '{snapshot.ManagementMode}', but Cephalon currently exposes governance truth only and does not own connector write actions yet.")
            {
                CategoryIds = [CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.FutureControlPlaneMode],
                ManagementMode = snapshot.ManagementMode,
                ConnectClusterId = snapshot.DeclaredConnectClusterId,
                ConnectorClass = snapshot.DeclaredConnectorClass,
                SourceProviderId = snapshot.DeclaredSourceProviderId,
                ExpectedTaskCount = snapshot.ExpectedTaskCount,
                ReportedTaskCount = snapshot.ReportedTaskCount,
                DeclaredTaskIds = snapshot.DeclaredTaskIds,
                ReportedTaskIds = snapshot.ReportedTaskIds,
                ActiveTaskIds = snapshot.ActiveTaskIds,
                ConnectorLifecycleState = snapshot.ConnectorLifecycleState,
                TaskReconciliationState = snapshot.TaskReconciliationState,
                ReconciliationState = snapshot.ReconciliationState,
                ReconciliationReason = snapshot.ReconciliationReason,
                RecommendedActionId = CdcCaptureExecutionRuntimeManagedConnectorGovernanceActionIds.DeferControlPlane
            };
        }

        return new CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus(
            CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.ObserveOnly,
            CreateManagedConnectorObserveOnlyDescription(snapshot.ReconciliationState, snapshot.ReconciliationReason))
        {
            ManagementMode = snapshot.ManagementMode,
            ConnectClusterId = snapshot.DeclaredConnectClusterId,
            ConnectorClass = snapshot.DeclaredConnectorClass,
            SourceProviderId = snapshot.DeclaredSourceProviderId,
            ExpectedTaskCount = snapshot.ExpectedTaskCount,
            ReportedTaskCount = snapshot.ReportedTaskCount,
            DeclaredTaskIds = snapshot.DeclaredTaskIds,
            ReportedTaskIds = snapshot.ReportedTaskIds,
            ActiveTaskIds = snapshot.ActiveTaskIds,
            ConnectorLifecycleState = snapshot.ConnectorLifecycleState,
            TaskReconciliationState = snapshot.TaskReconciliationState,
            ReconciliationState = snapshot.ReconciliationState,
            ReconciliationReason = snapshot.ReconciliationReason,
            RecommendedActionId = CdcCaptureExecutionRuntimeManagedConnectorGovernanceActionIds.KeepObserveOnly
        };
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorDriftStatus CreateManagedConnectorDrift(
        string executionTopology,
        IReadOnlyDictionary<string, string> metadata)
    {
        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase))
        {
            return new CdcCaptureExecutionRuntimeManagedConnectorDriftStatus(
                CdcCaptureExecutionRuntimeManagedConnectorDriftStates.NotApplicable,
                "The execution runtime does not currently represent a managed connector.")
            {
                RecommendedActionId = CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds.None
            };
        }

        var snapshot = ResolveManagedConnectorMetadata(metadata);
        var hasTaskBaseline = snapshot.DeclaredTaskIds.Length > 0 || snapshot.ExpectedTaskCount.HasValue;
        if (!hasTaskBaseline)
        {
            return new CdcCaptureExecutionRuntimeManagedConnectorDriftStatus(
                CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown,
                "The managed connector does not yet declare task ids or an expected task count, so desired-versus-observed drift cannot be evaluated.")
            {
                CategoryIds = [CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.MissingTaskBaseline],
                ManagementMode = snapshot.ManagementMode,
                DeclaredConnectClusterId = snapshot.DeclaredConnectClusterId,
                ReportedConnectClusterId = snapshot.ReportedConnectClusterId,
                DeclaredConnectorClass = snapshot.DeclaredConnectorClass,
                ReportedConnectorClass = snapshot.ReportedConnectorClass,
                DeclaredSourceProviderId = snapshot.DeclaredSourceProviderId,
                ReportedSourceProviderId = snapshot.ReportedSourceProviderId,
                ExpectedTaskCount = snapshot.ExpectedTaskCount,
                ReportedTaskCount = snapshot.ReportedTaskCount,
                DeclaredTaskIds = snapshot.DeclaredTaskIds,
                ReportedTaskIds = snapshot.ReportedTaskIds,
                ActiveTaskIds = snapshot.ActiveTaskIds,
                ConnectorLifecycleState = snapshot.ConnectorLifecycleState,
                TaskReconciliationState = snapshot.TaskReconciliationState,
                ReconciliationState = snapshot.ReconciliationState,
                ReconciliationReason = snapshot.ReconciliationReason,
                RecommendedActionId = CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds.CompleteTaskBaseline
            };
        }

        var hasReportedTaskTopology = snapshot.ReportedTaskIds.Length > 0 || snapshot.ReportedTaskCount.HasValue;
        if (!hasReportedTaskTopology)
        {
            return new CdcCaptureExecutionRuntimeManagedConnectorDriftStatus(
                CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown,
                "The managed connector has not yet reported task ids or a reported task count, so desired-versus-observed drift cannot be evaluated.")
            {
                CategoryIds = [CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ReportedTaskTopologyUnavailable],
                ManagementMode = snapshot.ManagementMode,
                DeclaredConnectClusterId = snapshot.DeclaredConnectClusterId,
                ReportedConnectClusterId = snapshot.ReportedConnectClusterId,
                DeclaredConnectorClass = snapshot.DeclaredConnectorClass,
                ReportedConnectorClass = snapshot.ReportedConnectorClass,
                DeclaredSourceProviderId = snapshot.DeclaredSourceProviderId,
                ReportedSourceProviderId = snapshot.ReportedSourceProviderId,
                ExpectedTaskCount = snapshot.ExpectedTaskCount,
                ReportedTaskCount = snapshot.ReportedTaskCount,
                DeclaredTaskIds = snapshot.DeclaredTaskIds,
                ReportedTaskIds = snapshot.ReportedTaskIds,
                ActiveTaskIds = snapshot.ActiveTaskIds,
                ConnectorLifecycleState = snapshot.ConnectorLifecycleState,
                TaskReconciliationState = snapshot.TaskReconciliationState,
                ReconciliationState = snapshot.ReconciliationState,
                ReconciliationReason = snapshot.ReconciliationReason,
                RecommendedActionId = CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds.WaitForRuntimeReport
            };
        }

        if (snapshot.DeclaredTaskIds.Length > 0 && snapshot.ReportedTaskIds.Length == 0)
        {
            return new CdcCaptureExecutionRuntimeManagedConnectorDriftStatus(
                CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown,
                "The managed connector reports task counts, but not the task identities needed to compare declared task ids.")
            {
                CategoryIds = [CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ReportedTaskIdentityUnavailable],
                ManagementMode = snapshot.ManagementMode,
                DeclaredConnectClusterId = snapshot.DeclaredConnectClusterId,
                ReportedConnectClusterId = snapshot.ReportedConnectClusterId,
                DeclaredConnectorClass = snapshot.DeclaredConnectorClass,
                ReportedConnectorClass = snapshot.ReportedConnectorClass,
                DeclaredSourceProviderId = snapshot.DeclaredSourceProviderId,
                ReportedSourceProviderId = snapshot.ReportedSourceProviderId,
                ExpectedTaskCount = snapshot.ExpectedTaskCount,
                ReportedTaskCount = snapshot.ReportedTaskCount,
                DeclaredTaskIds = snapshot.DeclaredTaskIds,
                ReportedTaskIds = snapshot.ReportedTaskIds,
                ActiveTaskIds = snapshot.ActiveTaskIds,
                ConnectorLifecycleState = snapshot.ConnectorLifecycleState,
                TaskReconciliationState = snapshot.TaskReconciliationState,
                ReconciliationState = snapshot.ReconciliationState,
                ReconciliationReason = snapshot.ReconciliationReason,
                RecommendedActionId = CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds.WaitForRuntimeReport
            };
        }

        var categories = new List<string>(capacity: 6);
        if (snapshot.ExpectedTaskCount.HasValue &&
            snapshot.ReportedTaskCount.HasValue &&
            snapshot.ExpectedTaskCount.Value != snapshot.ReportedTaskCount.Value)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.TaskCountMismatch);
        }

        string[] missingDeclaredTaskIds = [];
        string[] unexpectedReportedTaskIds = [];
        if (snapshot.DeclaredTaskIds.Length > 0 && snapshot.ReportedTaskIds.Length > 0)
        {
            missingDeclaredTaskIds = snapshot.DeclaredTaskIds
                .Except(snapshot.ReportedTaskIds, StringComparer.OrdinalIgnoreCase)
                .OrderBy(static taskId => taskId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            unexpectedReportedTaskIds = snapshot.ReportedTaskIds
                .Except(snapshot.DeclaredTaskIds, StringComparer.OrdinalIgnoreCase)
                .OrderBy(static taskId => taskId, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (missingDeclaredTaskIds.Length > 0)
            {
                categories.Add(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.MissingDeclaredTaskReports);
            }

            if (unexpectedReportedTaskIds.Length > 0)
            {
                categories.Add(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.UnexpectedReportedTasks);
            }
        }

        if (!string.IsNullOrWhiteSpace(snapshot.DeclaredConnectClusterId) &&
            !string.IsNullOrWhiteSpace(snapshot.ReportedConnectClusterId) &&
            !string.Equals(snapshot.DeclaredConnectClusterId, snapshot.ReportedConnectClusterId, StringComparison.OrdinalIgnoreCase))
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ConnectClusterMismatch);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.DeclaredConnectorClass) &&
            !string.IsNullOrWhiteSpace(snapshot.ReportedConnectorClass) &&
            !string.Equals(snapshot.DeclaredConnectorClass, snapshot.ReportedConnectorClass, StringComparison.OrdinalIgnoreCase))
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ConnectorClassMismatch);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.DeclaredSourceProviderId) &&
            !string.IsNullOrWhiteSpace(snapshot.ReportedSourceProviderId) &&
            !string.Equals(snapshot.DeclaredSourceProviderId, snapshot.ReportedSourceProviderId, StringComparison.OrdinalIgnoreCase))
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.SourceProviderMismatch);
        }

        if (categories.Count == 0)
        {
            return new CdcCaptureExecutionRuntimeManagedConnectorDriftStatus(
                CdcCaptureExecutionRuntimeManagedConnectorDriftStates.InSync,
                CreateManagedConnectorInSyncDescription(snapshot.ReconciliationState))
            {
                ManagementMode = snapshot.ManagementMode,
                DeclaredConnectClusterId = snapshot.DeclaredConnectClusterId,
                ReportedConnectClusterId = snapshot.ReportedConnectClusterId,
                DeclaredConnectorClass = snapshot.DeclaredConnectorClass,
                ReportedConnectorClass = snapshot.ReportedConnectorClass,
                DeclaredSourceProviderId = snapshot.DeclaredSourceProviderId,
                ReportedSourceProviderId = snapshot.ReportedSourceProviderId,
                ExpectedTaskCount = snapshot.ExpectedTaskCount,
                ReportedTaskCount = snapshot.ReportedTaskCount,
                DeclaredTaskIds = snapshot.DeclaredTaskIds,
                ReportedTaskIds = snapshot.ReportedTaskIds,
                ActiveTaskIds = snapshot.ActiveTaskIds,
                ConnectorLifecycleState = snapshot.ConnectorLifecycleState,
                TaskReconciliationState = snapshot.TaskReconciliationState,
                ReconciliationState = snapshot.ReconciliationState,
                ReconciliationReason = snapshot.ReconciliationReason,
                RecommendedActionId = CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds.None
            };
        }

        return new CdcCaptureExecutionRuntimeManagedConnectorDriftStatus(
            CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Drifted,
            CreateManagedConnectorDriftDescription(
                categories,
                snapshot,
                missingDeclaredTaskIds,
                unexpectedReportedTaskIds))
        {
            CategoryIds = categories,
            ManagementMode = snapshot.ManagementMode,
            DeclaredConnectClusterId = snapshot.DeclaredConnectClusterId,
            ReportedConnectClusterId = snapshot.ReportedConnectClusterId,
            DeclaredConnectorClass = snapshot.DeclaredConnectorClass,
            ReportedConnectorClass = snapshot.ReportedConnectorClass,
            DeclaredSourceProviderId = snapshot.DeclaredSourceProviderId,
            ReportedSourceProviderId = snapshot.ReportedSourceProviderId,
            ExpectedTaskCount = snapshot.ExpectedTaskCount,
            ReportedTaskCount = snapshot.ReportedTaskCount,
            DeclaredTaskIds = snapshot.DeclaredTaskIds,
            ReportedTaskIds = snapshot.ReportedTaskIds,
            ActiveTaskIds = snapshot.ActiveTaskIds,
            MissingDeclaredTaskIds = missingDeclaredTaskIds,
            UnexpectedReportedTaskIds = unexpectedReportedTaskIds,
            ConnectorLifecycleState = snapshot.ConnectorLifecycleState,
            TaskReconciliationState = snapshot.TaskReconciliationState,
            ReconciliationState = snapshot.ReconciliationState,
            ReconciliationReason = snapshot.ReconciliationReason,
            RecommendedActionId = CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds.InvestigateDrift
        };
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus CreateManagedConnectorActionPlan(
        string executionTopology,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift)
    {
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase))
        {
            return new CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus(
                CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.NotApplicable,
                "The execution runtime does not currently represent a managed connector.")
            {
                RemediationState = remediation.State,
                GovernanceState = governance.State,
                DriftState = drift.State
            };
        }

        var hasRuntimeRemediationAttention =
            remediation.CategoryIds.Contains(CdcCaptureExecutionRuntimeRemediationCategories.StaleObservations, StringComparer.OrdinalIgnoreCase) ||
            remediation.CategoryIds.Contains(CdcCaptureExecutionRuntimeRemediationCategories.ReporterCoordinationIssues, StringComparer.OrdinalIgnoreCase);
        var isDriftBaselineIncomplete = drift.CategoryIds.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.MissingTaskBaseline,
            StringComparer.OrdinalIgnoreCase);
        var isWaitingForRuntimeTruth =
            drift.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ReportedTaskTopologyUnavailable, StringComparer.OrdinalIgnoreCase) ||
            drift.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ReportedTaskIdentityUnavailable, StringComparer.OrdinalIgnoreCase);

        var categories = CreateManagedConnectorActionPlanCategories(
            remediation,
            governance,
            drift,
            hasRuntimeRemediationAttention,
            isDriftBaselineIncomplete,
            isWaitingForRuntimeTruth);

        if (remediation.IsBlocked)
        {
            return CreateManagedConnectorActionPlanStatus(
                CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Blocked,
                CreateManagedConnectorBlockedActionPlanDescription(remediation.Description),
                categories,
                CreateManagedConnectorActionIds(
                    CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.ResolveRuntimeRemediation,
                    remediation,
                    governance,
                    drift,
                    hasRuntimeRemediationAttention,
                    isDriftBaselineIncomplete,
                    isWaitingForRuntimeTruth),
                remediation,
                governance,
                drift);
        }

        if (hasRuntimeRemediationAttention)
        {
            return CreateManagedConnectorActionPlanStatus(
                CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.ActionRequired,
                CreateManagedConnectorRuntimeRemediationActionPlanDescription(remediation.Description),
                categories,
                CreateManagedConnectorActionIds(
                    CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.ResolveRuntimeRemediation,
                    remediation,
                    governance,
                    drift,
                    hasRuntimeRemediationAttention,
                    isDriftBaselineIncomplete,
                    isWaitingForRuntimeTruth),
                remediation,
                governance,
                drift);
        }

        if (governance.IsOutOfPolicy)
        {
            return CreateManagedConnectorActionPlanStatus(
                CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.ActionRequired,
                CreateManagedConnectorGovernanceActionPlanDescription(governance.Description),
                categories,
                CreateManagedConnectorActionIds(
                    CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.CompleteGovernanceDeclaration,
                    remediation,
                    governance,
                    drift,
                    hasRuntimeRemediationAttention,
                    isDriftBaselineIncomplete,
                    isWaitingForRuntimeTruth),
                remediation,
                governance,
                drift);
        }

        if (isDriftBaselineIncomplete)
        {
            return CreateManagedConnectorActionPlanStatus(
                CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.ActionRequired,
                CreateManagedConnectorTaskBaselineActionPlanDescription(drift.Description),
                categories,
                CreateManagedConnectorActionIds(
                    CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.CompleteTaskBaseline,
                    remediation,
                    governance,
                    drift,
                    hasRuntimeRemediationAttention,
                    isDriftBaselineIncomplete,
                    isWaitingForRuntimeTruth),
                remediation,
                governance,
                drift);
        }

        if (isWaitingForRuntimeTruth)
        {
            return CreateManagedConnectorActionPlanStatus(
                CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Waiting,
                CreateManagedConnectorWaitForRuntimeTruthActionPlanDescription(drift.Description),
                categories,
                CreateManagedConnectorActionIds(
                    CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.WaitForRuntimeReport,
                    remediation,
                    governance,
                    drift,
                    hasRuntimeRemediationAttention,
                    isDriftBaselineIncomplete,
                    isWaitingForRuntimeTruth),
                remediation,
                governance,
                drift);
        }

        if (drift.IsDrifted)
        {
            return CreateManagedConnectorActionPlanStatus(
                CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.ActionRequired,
                CreateManagedConnectorDriftActionPlanDescription(drift.Description),
                categories,
                CreateManagedConnectorActionIds(
                    CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.InvestigateDrift,
                    remediation,
                    governance,
                    drift,
                    hasRuntimeRemediationAttention,
                    isDriftBaselineIncomplete,
                    isWaitingForRuntimeTruth),
                remediation,
                governance,
                drift);
        }

        if (governance.RequiresControlPlaneSupport)
        {
            return CreateManagedConnectorActionPlanStatus(
                CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Observe,
                CreateManagedConnectorDeferredControlPlaneActionPlanDescription(governance.Description),
                categories,
                CreateManagedConnectorActionIds(
                    CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.DeferControlPlane,
                    remediation,
                    governance,
                    drift,
                    hasRuntimeRemediationAttention,
                    isDriftBaselineIncomplete,
                    isWaitingForRuntimeTruth),
                remediation,
                governance,
                drift);
        }

        return CreateManagedConnectorActionPlanStatus(
            CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Observe,
            CreateManagedConnectorObserveActionPlanDescription(governance.Description),
            categories,
            CreateManagedConnectorActionIds(
                CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.KeepObserveOnly,
                remediation,
                governance,
                drift,
                hasRuntimeRemediationAttention,
                isDriftBaselineIncomplete,
                isWaitingForRuntimeTruth),
            remediation,
            governance,
            drift);
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus CreateManagedConnectorWritePathReadiness(
        string executionTopology,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan)
    {
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(actionPlan);

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase))
        {
            return new CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus(
                CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotApplicable,
                "The execution runtime does not currently represent a managed connector.")
            {
                ReportingCoverageState = reportingCoverage.State,
                RemediationState = remediation.State,
                GovernanceState = governance.State,
                DriftState = drift.State,
                ActionPlanState = actionPlan.State,
                PrimaryActionId = actionPlan.PrimaryActionId
            };
        }

        var hasRuntimeRemediationAttention =
            remediation.CategoryIds.Contains(CdcCaptureExecutionRuntimeRemediationCategories.StaleObservations, StringComparer.OrdinalIgnoreCase) ||
            remediation.CategoryIds.Contains(CdcCaptureExecutionRuntimeRemediationCategories.ReporterCoordinationIssues, StringComparer.OrdinalIgnoreCase);
        var hasIncompleteReportingCoverage = !reportingCoverage.HasFullCoverage;
        var isRuntimeTruthIncomplete =
            actionPlan.IsWaiting ||
            string.Equals(drift.State, CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown, StringComparison.OrdinalIgnoreCase);
        var isObserveOnlyMode = string.Equals(governance.ManagementMode, "observe-only", StringComparison.OrdinalIgnoreCase);
        var isWritePathRequested = !string.IsNullOrWhiteSpace(governance.ManagementMode) && !isObserveOnlyMode;

        var categories = CreateManagedConnectorWritePathReadinessCategories(
            reportingCoverage,
            remediation,
            governance,
            drift,
            hasRuntimeRemediationAttention,
            hasIncompleteReportingCoverage,
            isRuntimeTruthIncomplete,
            isObserveOnlyMode,
            isWritePathRequested);

        if (remediation.IsBlocked)
        {
            return CreateManagedConnectorWritePathReadinessStatus(
                CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Blocked,
                CreateManagedConnectorBlockedWritePathReadinessDescription(remediation.Description),
                categories,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan);
        }

        if (hasRuntimeRemediationAttention)
        {
            return CreateManagedConnectorWritePathReadinessStatus(
                CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotReady,
                CreateManagedConnectorRuntimeRemediationWritePathReadinessDescription(remediation.Description),
                categories,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan);
        }

        if (hasIncompleteReportingCoverage)
        {
            return CreateManagedConnectorWritePathReadinessStatus(
                CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotReady,
                CreateManagedConnectorReportingCoverageWritePathReadinessDescription(reportingCoverage.Description),
                categories,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan);
        }

        if (governance.IsOutOfPolicy)
        {
            return CreateManagedConnectorWritePathReadinessStatus(
                CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotReady,
                CreateManagedConnectorGovernanceWritePathReadinessDescription(governance.Description),
                categories,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan);
        }

        if (isRuntimeTruthIncomplete)
        {
            return CreateManagedConnectorWritePathReadinessStatus(
                CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotReady,
                CreateManagedConnectorRuntimeTruthWritePathReadinessDescription(
                    actionPlan.Description,
                    drift.Description),
                categories,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan);
        }

        if (drift.IsDrifted)
        {
            return CreateManagedConnectorWritePathReadinessStatus(
                CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotReady,
                CreateManagedConnectorDriftWritePathReadinessDescription(drift.Description),
                categories,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan);
        }

        if (isWritePathRequested)
        {
            return CreateManagedConnectorWritePathReadinessStatus(
                CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Ready,
                CreateManagedConnectorReadyWritePathReadinessDescription(governance.Description),
                categories,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan);
        }

        return CreateManagedConnectorWritePathReadinessStatus(
            CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Deferred,
            CreateManagedConnectorDeferredWritePathReadinessDescription(governance.Description),
            categories,
            governance,
            reportingCoverage,
            remediation,
            drift,
            actionPlan);
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus CreateManagedConnectorPreflight(
        string executionTopology,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness)
    {
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(actionPlan);
        ArgumentNullException.ThrowIfNull(writePathReadiness);

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase))
        {
            return new CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus(
                CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotApplicable,
                "The execution runtime does not currently represent a managed connector.")
            {
                OperationId = CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.None,
                ReportingCoverageState = reportingCoverage.State,
                RemediationState = remediation.State,
                GovernanceState = governance.State,
                DriftState = drift.State,
                ActionPlanState = actionPlan.State,
                WritePathReadinessState = writePathReadiness.State,
                PrimaryActionId = actionPlan.PrimaryActionId
            };
        }

        var hasRuntimeRemediationAttention =
            remediation.CategoryIds.Contains(CdcCaptureExecutionRuntimeRemediationCategories.StaleObservations, StringComparer.OrdinalIgnoreCase) ||
            remediation.CategoryIds.Contains(CdcCaptureExecutionRuntimeRemediationCategories.ReporterCoordinationIssues, StringComparer.OrdinalIgnoreCase);
        var hasIncompleteReportingCoverage = !reportingCoverage.HasFullCoverage;
        var isRuntimeTruthIncomplete =
            actionPlan.IsWaiting ||
            string.Equals(drift.State, CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown, StringComparison.OrdinalIgnoreCase);
        var isObserveOnlyMode = string.Equals(governance.ManagementMode, "observe-only", StringComparison.OrdinalIgnoreCase);
        var isWritePathRequested = !string.IsNullOrWhiteSpace(governance.ManagementMode) && !isObserveOnlyMode;
        var operationId = isWritePathRequested
            ? CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.Reconcile
            : CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.None;

        var categories = CreateManagedConnectorPreflightCategories(
            remediation,
            governance,
            drift,
            hasRuntimeRemediationAttention,
            hasIncompleteReportingCoverage,
            isRuntimeTruthIncomplete,
            isObserveOnlyMode,
            isWritePathRequested,
            writePathReadiness.IsReady);

        if (remediation.IsBlocked)
        {
            return CreateManagedConnectorPreflightStatus(
                CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.Blocked,
                CreateManagedConnectorBlockedPreflightDescription(remediation.Description, operationId),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness);
        }

        if (hasRuntimeRemediationAttention)
        {
            return CreateManagedConnectorPreflightStatus(
                CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotReady,
                CreateManagedConnectorRuntimeRemediationPreflightDescription(remediation.Description, operationId),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness);
        }

        if (hasIncompleteReportingCoverage)
        {
            return CreateManagedConnectorPreflightStatus(
                CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotReady,
                CreateManagedConnectorReportingCoveragePreflightDescription(reportingCoverage.Description, operationId),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness);
        }

        if (governance.IsOutOfPolicy)
        {
            return CreateManagedConnectorPreflightStatus(
                CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotReady,
                CreateManagedConnectorGovernancePreflightDescription(governance.Description, operationId),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness);
        }

        if (isRuntimeTruthIncomplete)
        {
            return CreateManagedConnectorPreflightStatus(
                CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotReady,
                CreateManagedConnectorRuntimeTruthPreflightDescription(
                    actionPlan.Description,
                    drift.Description,
                    operationId),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness);
        }

        if (drift.IsDrifted)
        {
            return CreateManagedConnectorPreflightStatus(
                CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotReady,
                CreateManagedConnectorDriftPreflightDescription(drift.Description, operationId),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness);
        }

        if (isWritePathRequested)
        {
            return CreateManagedConnectorPreflightStatus(
                CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.Ready,
                CreateManagedConnectorReadyPreflightDescription(governance.Description, operationId),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness);
        }

        return CreateManagedConnectorPreflightStatus(
            CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.Deferred,
            CreateManagedConnectorDeferredPreflightDescription(governance.Description),
            categories,
            operationId,
            governance,
            reportingCoverage,
            remediation,
            drift,
            actionPlan,
            writePathReadiness);
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus CreateManagedConnectorDryRun(
        string executionTopology,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight)
    {
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(actionPlan);
        ArgumentNullException.ThrowIfNull(writePathReadiness);
        ArgumentNullException.ThrowIfNull(preflight);

        var operationId = ResolveManagedConnectorDryRunOperationId(governance.ManagementMode);
        var wouldApplyChanges = WouldManagedConnectorDryRunApplyChanges(operationId, drift, reportingCoverage);
        var potentialChangeCount = CountManagedConnectorDryRunPotentialChanges(operationId, drift, reportingCoverage, wouldApplyChanges);
        var categories = CreateManagedConnectorDryRunCategories(
            reportingCoverage,
            remediation,
            governance,
            drift,
            actionPlan,
            preflight,
            operationId,
            wouldApplyChanges);

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase))
        {
            return CreateManagedConnectorDryRunStatus(
                CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NotApplicable,
                "The execution runtime does not currently represent a managed connector.",
                categories,
                CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.None,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                0,
                wouldApplyChanges: false);
        }

        if (preflight.IsDeferred)
        {
            return CreateManagedConnectorDryRunStatus(
                CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Deferred,
                CreateManagedConnectorDeferredDryRunDescription(governance.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                0,
                wouldApplyChanges: false);
        }

        if (preflight.RequiresAttention)
        {
            return CreateManagedConnectorDryRunStatus(
                CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Blocked,
                CreateManagedConnectorBlockedDryRunDescription(
                    preflight.Description,
                    operationId,
                    wouldApplyChanges,
                    drift,
                    reportingCoverage),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                potentialChangeCount,
                wouldApplyChanges);
        }

        if (wouldApplyChanges)
        {
            return CreateManagedConnectorDryRunStatus(
                CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.WouldChange,
                CreateManagedConnectorWouldChangeDryRunDescription(
                    operationId,
                    drift,
                    reportingCoverage),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                potentialChangeCount,
                wouldApplyChanges: true);
        }

        return CreateManagedConnectorDryRunStatus(
            CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NoOp,
            CreateManagedConnectorNoOpDryRunDescription(
                operationId,
                drift,
                reportingCoverage),
            categories,
            operationId,
            governance,
            reportingCoverage,
            remediation,
            drift,
            actionPlan,
            writePathReadiness,
            preflight,
            0,
            wouldApplyChanges: false);
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus CreateManagedConnectorExecutionIntent(
        string executionTopology,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun)
    {
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(actionPlan);
        ArgumentNullException.ThrowIfNull(writePathReadiness);
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(dryRun);

        var operationId = string.IsNullOrWhiteSpace(dryRun.OperationId)
            ? CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.None
            : dryRun.OperationId;
        var categories = CreateManagedConnectorExecutionIntentCategories(
            reportingCoverage,
            remediation,
            governance,
            drift,
            actionPlan,
            writePathReadiness,
            preflight,
            dryRun,
            operationId);
        var confidenceSourceId = ResolveManagedConnectorExecutionIntentConfidenceSource(
            dryRun,
            preflight,
            writePathReadiness,
            actionPlan);

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase))
        {
            return CreateManagedConnectorExecutionIntentStatus(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.NotApplicable,
                "The execution runtime does not currently represent a managed connector.",
                categories,
                CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.None,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                confidenceSourceId);
        }

        if (dryRun.IsDeferred || preflight.IsDeferred)
        {
            return CreateManagedConnectorExecutionIntentStatus(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.Deferred,
                CreateManagedConnectorDeferredExecutionIntentDescription(governance.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                confidenceSourceId);
        }

        if (RequiresManagedConnectorOperatorOwnedExecutionIntent(governance, dryRun, operationId))
        {
            return CreateManagedConnectorExecutionIntentStatus(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.OperatorAction,
                CreateManagedConnectorOperatorActionExecutionIntentDescription(
                    operationId,
                    governance.Description,
                    dryRun.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                confidenceSourceId);
        }

        if (preflight.RequiresAttention)
        {
            return CreateManagedConnectorExecutionIntentStatus(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.Blocked,
                CreateManagedConnectorBlockedExecutionIntentDescription(
                    operationId,
                    dryRun.WouldApplyChanges,
                    preflight.Description,
                    dryRun.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                confidenceSourceId);
        }

        if (dryRun.IsWouldChange)
        {
            return CreateManagedConnectorExecutionIntentStatus(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.RequiresApproval,
                CreateManagedConnectorApprovalRequiredExecutionIntentDescription(
                    operationId,
                    dryRun.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                confidenceSourceId);
        }

        return CreateManagedConnectorExecutionIntentStatus(
            CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.ReadyToExecute,
            CreateManagedConnectorReadyExecutionIntentDescription(
                operationId,
                dryRun.Description),
            categories,
            operationId,
            governance,
            reportingCoverage,
            remediation,
            drift,
            actionPlan,
            writePathReadiness,
            preflight,
            dryRun,
            confidenceSourceId);
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus CreateManagedConnectorExecutionIntentStatus(
        string state,
        string description,
        IReadOnlyList<string> categoryIds,
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        string confidenceSourceId)
    {
        return new CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus(state, description)
        {
            CategoryIds = categoryIds,
            OperationId = operationId,
            ManagementMode = governance.ManagementMode,
            ReportingCoverageState = reportingCoverage.State,
            RemediationState = remediation.State,
            GovernanceState = governance.State,
            DriftState = drift.State,
            ActionPlanState = actionPlan.State,
            WritePathReadinessState = writePathReadiness.State,
            PreflightState = preflight.State,
            DryRunState = dryRun.State,
            PrimaryActionId = actionPlan.PrimaryActionId,
            ConfidenceSourceId = confidenceSourceId,
            PotentialChangeCount = dryRun.PotentialChangeCount,
            WouldApplyChanges = dryRun.WouldApplyChanges
        };
    }

    private static string[] CreateManagedConnectorExecutionIntentCategories(
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        string operationId)
    {
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(actionPlan);
        ArgumentNullException.ThrowIfNull(writePathReadiness);
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(dryRun);

        var categories = new List<string>(capacity: 12);
        static void AddCategory(List<string> values, string category)
        {
            if (!values.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(category);
            }
        }

        if (remediation.IsBlocked)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.BlockingRemediation);
        }
        else if (dryRun.CategoryIds.Contains(
                     CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.RuntimeRemediation,
                     StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.RuntimeRemediation);
        }

        if (!reportingCoverage.HasFullCoverage ||
            dryRun.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.IncompleteReportingCoverage,
                StringComparer.OrdinalIgnoreCase) ||
            preflight.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.IncompleteReportingCoverage,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.IncompleteReportingCoverage);
        }

        if (governance.IsOutOfPolicy)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.GovernanceOutOfPolicy);
        }

        if (actionPlan.IsWaiting ||
            string.Equals(drift.State, CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown, StringComparison.OrdinalIgnoreCase) ||
            dryRun.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.RuntimeTruthIncomplete,
                StringComparer.OrdinalIgnoreCase) ||
            preflight.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.RuntimeTruthIncomplete,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.RuntimeTruthIncomplete);
        }

        if (dryRun.IsDeferred || preflight.IsDeferred)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.ObserveOnlyMode);
        }

        var isOperatorOwnedExecutionIntent = RequiresManagedConnectorOperatorOwnedExecutionIntent(governance, dryRun, operationId);

        if (isOperatorOwnedExecutionIntent)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.FutureControlPlane);
        }

        if (isOperatorOwnedExecutionIntent)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.OperatorOnly);
        }

        if (dryRun.WouldApplyChanges)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.ChangePlanned);
        }
        else if (dryRun.IsNoOp)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.NoExecutionNeeded);
        }

        if (preflight.IsReady && !isOperatorOwnedExecutionIntent)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.EngineExecutionCandidate);
        }

        if (dryRun.IsWouldChange && !isOperatorOwnedExecutionIntent && preflight.IsReady)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.ApprovalRequired);
        }

        if (WouldManagedConnectorDryRunRequireLifecycleChange(operationId, dryRun.WouldApplyChanges))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.LifecycleChange);
        }

        return [.. categories];
    }

    private static string ResolveManagedConnectorExecutionIntentConfidenceSource(
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan)
    {
        ArgumentNullException.ThrowIfNull(dryRun);
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(writePathReadiness);
        ArgumentNullException.ThrowIfNull(actionPlan);

        if (dryRun.IsNoOp || dryRun.IsWouldChange || dryRun.IsBlocked || dryRun.IsDeferred)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources.DryRun;
        }

        if (preflight.IsReady || preflight.RequiresAttention || preflight.IsDeferred)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources.Preflight;
        }

        if (writePathReadiness.IsReady || writePathReadiness.RequiresAttention || writePathReadiness.IsDeferred)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources.WritePathReadiness;
        }

        if (actionPlan.AppliesToManagedConnector)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources.ActionPlan;
        }

        return CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources.Unknown;
    }

    private static bool RequiresManagedConnectorOperatorOwnedExecutionIntent(
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        string operationId)
    {
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(dryRun);

        return governance.RequiresControlPlaneSupport &&
               dryRun.WouldApplyChanges &&
               string.Equals(
                   operationId,
                   CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.Reconcile,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateManagedConnectorDeferredExecutionIntentDescription(string? governanceDescription)
    {
        return AppendManagedConnectorExecutionIntentDetail(
            "Cephalon does not currently intend to execute managed-connector follow-through while the runtime remains observe-only on the shared surface.",
            governanceDescription);
    }

    private static string CreateManagedConnectorOperatorActionExecutionIntentDescription(
        string operationId,
        string? governanceDescription,
        string? dryRunDescription)
    {
        var detail = governanceDescription;
        if (!string.IsNullOrWhiteSpace(dryRunDescription))
        {
            detail = string.IsNullOrWhiteSpace(detail)
                ? dryRunDescription
                : $"{detail.Trim()} {dryRunDescription.Trim()}";
        }

        return AppendManagedConnectorExecutionIntentDetail(
            $"Cephalon currently intends to {CreateManagedConnectorExecutionIntentOperationLabel(operationId)}, but the next step remains operator-owned because the runtime declares a future control-plane mode that Cephalon does not yet execute.",
            detail);
    }

    private static string CreateManagedConnectorBlockedExecutionIntentDescription(
        string operationId,
        bool wouldApplyChanges,
        string? preflightDescription,
        string? dryRunDescription)
    {
        var summary = wouldApplyChanges
            ? $"Cephalon tentatively intends to {CreateManagedConnectorExecutionIntentOperationLabel(operationId)}, but execution intent remains blocked until shared preconditions clear."
            : $"Cephalon cannot form a trustworthy execution intent for {CreateManagedConnectorExecutionIntentOperationLabel(operationId)} until shared preconditions clear.";
        var detail = string.IsNullOrWhiteSpace(preflightDescription)
            ? dryRunDescription
            : preflightDescription;

        if (wouldApplyChanges && !string.IsNullOrWhiteSpace(dryRunDescription) && !string.IsNullOrWhiteSpace(detail) && !string.Equals(detail, dryRunDescription, StringComparison.Ordinal))
        {
            detail = $"{detail.Trim()} {dryRunDescription.Trim()}";
        }

        return AppendManagedConnectorExecutionIntentDetail(summary, detail);
    }

    private static string CreateManagedConnectorApprovalRequiredExecutionIntentDescription(
        string operationId,
        string? dryRunDescription)
    {
        return AppendManagedConnectorExecutionIntentDetail(
            $"Cephalon currently intends to {CreateManagedConnectorExecutionIntentOperationLabel(operationId)}, and the shared dry-run truth suggests that operation would still change managed-connector posture. Future engine execution would require an approval gate first.",
            dryRunDescription);
    }

    private static string CreateManagedConnectorReadyExecutionIntentDescription(
        string operationId,
        string? dryRunDescription)
    {
        return AppendManagedConnectorExecutionIntentDetail(
            $"Cephalon currently intends to {CreateManagedConnectorExecutionIntentOperationLabel(operationId)}, and the shared dry-run truth suggests no additional managed-connector changes would be required. Actual write-path execution remains later follow-through.",
            dryRunDescription);
    }

    private static string AppendManagedConnectorExecutionIntentDetail(
        string summary,
        string? detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        return string.IsNullOrWhiteSpace(detail)
            ? summary.Trim()
            : $"{summary.Trim()} {detail.Trim()}";
    }

    private static string CreateManagedConnectorExecutionIntentOperationLabel(string operationId)
    {
        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.None, StringComparison.OrdinalIgnoreCase))
        {
            return "managed-connector follow-through";
        }

        return CreateManagedConnectorDryRunOperationLabel(operationId);
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus CreateManagedConnectorExecutionApproval(
        string executionTopology,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent)
    {
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(actionPlan);
        ArgumentNullException.ThrowIfNull(writePathReadiness);
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(dryRun);
        ArgumentNullException.ThrowIfNull(executionIntent);

        var operationId = string.IsNullOrWhiteSpace(executionIntent.OperationId)
            ? CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.None
            : executionIntent.OperationId;
        var categories = CreateManagedConnectorExecutionApprovalCategories(
            reportingCoverage,
            remediation,
            governance,
            drift,
            actionPlan,
            writePathReadiness,
            preflight,
            dryRun,
            executionIntent,
            operationId);
        var sourceId = ResolveManagedConnectorExecutionApprovalSource(
            remediation,
            governance,
            dryRun,
            executionIntent,
            categories);

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase))
        {
            return CreateManagedConnectorExecutionApprovalStatus(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.NotApplicable,
                "The execution runtime does not currently represent a managed connector.",
                categories,
                CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.None,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                sourceId);
        }

        if (executionIntent.IsDeferred)
        {
            return CreateManagedConnectorExecutionApprovalStatus(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.NotApplicable,
                CreateManagedConnectorNotApplicableExecutionApprovalDescription(governance.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                sourceId);
        }

        if (executionIntent.IsOperatorOnly || governance.IsOutOfPolicy)
        {
            return CreateManagedConnectorExecutionApprovalStatus(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.PolicyBlocked,
                CreateManagedConnectorPolicyBlockedExecutionApprovalDescription(
                    operationId,
                    governance.Description,
                    executionIntent.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                sourceId);
        }

        if (executionIntent.IsBlocked)
        {
            return CreateManagedConnectorExecutionApprovalStatus(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.AutoBlocked,
                CreateManagedConnectorAutoBlockedExecutionApprovalDescription(
                    operationId,
                    remediation.Description,
                    executionIntent.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                sourceId);
        }

        if (executionIntent.IsApprovalRequired)
        {
            var requiresElevatedApproval = RequiresManagedConnectorElevatedExecutionApproval(drift, operationId);
            var state = requiresElevatedApproval
                ? CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.ApprovalRequired
                : CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.ApprovalReady;
            var description = requiresElevatedApproval
                ? CreateManagedConnectorApprovalRequiredExecutionApprovalDescription(
                    operationId,
                    dryRun.Description,
                    drift.Description)
                : CreateManagedConnectorApprovalReadyExecutionApprovalDescription(
                    operationId,
                    dryRun.Description);

            return CreateManagedConnectorExecutionApprovalStatus(
                state,
                description,
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                sourceId);
        }

        if (executionIntent.IsReadyToExecute)
        {
            return CreateManagedConnectorExecutionApprovalStatus(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.AutoEligible,
                CreateManagedConnectorAutoEligibleExecutionApprovalDescription(
                    operationId,
                    dryRun.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                sourceId);
        }

        return CreateManagedConnectorExecutionApprovalStatus(
            CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.AutoBlocked,
            CreateManagedConnectorAutoBlockedExecutionApprovalDescription(
                operationId,
                remediation.Description,
                executionIntent.Description),
            categories,
            operationId,
            governance,
            reportingCoverage,
            remediation,
            drift,
            actionPlan,
            writePathReadiness,
            preflight,
            dryRun,
            executionIntent,
            sourceId);
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus CreateManagedConnectorExecutionApprovalStatus(
        string state,
        string description,
        IReadOnlyList<string> categoryIds,
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        string sourceId)
    {
        var requiresExplicitApproval =
            string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.ApprovalRequired, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.ApprovalReady, StringComparison.OrdinalIgnoreCase);

        return new CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus(state, description)
        {
            CategoryIds = categoryIds,
            OperationId = operationId,
            ManagementMode = governance.ManagementMode,
            ReportingCoverageState = reportingCoverage.State,
            RemediationState = remediation.State,
            GovernanceState = governance.State,
            DriftState = drift.State,
            ActionPlanState = actionPlan.State,
            WritePathReadinessState = writePathReadiness.State,
            PreflightState = preflight.State,
            DryRunState = dryRun.State,
            ExecutionIntentState = executionIntent.State,
            PrimaryActionId = actionPlan.PrimaryActionId,
            SourceId = sourceId,
            ExecutionIntentConfidenceSourceId = executionIntent.ConfidenceSourceId,
            PotentialChangeCount = dryRun.PotentialChangeCount,
            WouldApplyChanges = dryRun.WouldApplyChanges,
            RequiresExplicitApproval = requiresExplicitApproval
        };
    }

    private static string[] CreateManagedConnectorExecutionApprovalCategories(
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        string operationId)
    {
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(actionPlan);
        ArgumentNullException.ThrowIfNull(writePathReadiness);
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(dryRun);
        ArgumentNullException.ThrowIfNull(executionIntent);

        var categories = new List<string>(capacity: 14);
        static void AddCategory(List<string> values, string category)
        {
            if (!values.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(category);
            }
        }

        if (executionIntent.IsDeferred)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.ObserveOnlyMode);
        }

        if (remediation.IsBlocked)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.BlockingRemediation);
        }

        if (remediation.CategoryIds.Contains(CdcCaptureExecutionRuntimeRemediationCategories.StaleObservations, StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.StaleObservation);
        }

        if (!reportingCoverage.HasFullCoverage ||
            executionIntent.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.IncompleteReportingCoverage,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.IncompleteReportingCoverage);
        }

        if (actionPlan.IsWaiting ||
            executionIntent.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.RuntimeTruthIncomplete,
                StringComparer.OrdinalIgnoreCase) ||
            preflight.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.RuntimeTruthIncomplete,
                StringComparer.OrdinalIgnoreCase) ||
            writePathReadiness.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.RuntimeTruthIncomplete,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.RuntimeTruthIncomplete);
        }

        if (governance.IsOutOfPolicy)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.GovernanceOutOfPolicy);
        }

        if (executionIntent.IsOperatorOnly)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.ControlPlaneOwnershipGap);
        }

        if (drift.IsDrifted)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.DriftRisk);
        }

        if (WouldManagedConnectorDryRunRequireLifecycleChange(operationId, dryRun.WouldApplyChanges))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.LifecycleChange);
        }

        if (IsManagedConnectorExecutionApprovalDestructiveOperation(operationId) && dryRun.WouldApplyChanges)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.DestructiveOperation);
        }

        if (executionIntent.IsApprovalRequired)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.ApprovalRequired);
            if (!RequiresManagedConnectorElevatedExecutionApproval(drift, operationId))
            {
                AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.ApprovalReady);
            }
        }

        if (executionIntent.IsReadyToExecute)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.AutoEligible);
        }

        if (dryRun.IsNoOp ||
            executionIntent.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories.NoExecutionNeeded,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.NoExecutionNeeded);
        }

        return [.. categories];
    }

    private static string ResolveManagedConnectorExecutionApprovalSource(
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        IReadOnlyList<string> categories)
    {
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(dryRun);
        ArgumentNullException.ThrowIfNull(executionIntent);
        ArgumentNullException.ThrowIfNull(categories);

        if (remediation.IsBlocked ||
            categories.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.StaleObservation,
                StringComparer.OrdinalIgnoreCase))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources.Remediation;
        }

        if (governance.IsOutOfPolicy || executionIntent.IsOperatorOnly || executionIntent.IsDeferred)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources.Governance;
        }

        if (executionIntent.IsApprovalRequired || executionIntent.IsReadyToExecute || executionIntent.IsBlocked)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources.ExecutionIntent;
        }

        if (dryRun.AppliesToManagedConnector)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources.DryRun;
        }

        return CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources.Unknown;
    }

    private static bool RequiresManagedConnectorElevatedExecutionApproval(
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        string operationId)
    {
        ArgumentNullException.ThrowIfNull(drift);

        return IsManagedConnectorExecutionApprovalDestructiveOperation(operationId) || drift.IsDrifted;
    }

    private static bool IsManagedConnectorExecutionApprovalDestructiveOperation(string operationId)
    {
        return string.Equals(
            operationId,
            CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.Delete,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateManagedConnectorNotApplicableExecutionApprovalDescription(string? governanceDescription)
    {
        return AppendManagedConnectorExecutionApprovalDetail(
            "Cephalon does not currently require managed-connector execution approval while the runtime remains observe-only on the shared surface.",
            governanceDescription);
    }

    private static string CreateManagedConnectorPolicyBlockedExecutionApprovalDescription(
        string operationId,
        string? governanceDescription,
        string? executionIntentDescription)
    {
        var detail = governanceDescription;
        if (!string.IsNullOrWhiteSpace(executionIntentDescription))
        {
            detail = string.IsNullOrWhiteSpace(detail)
                ? executionIntentDescription
                : $"{detail.Trim()} {executionIntentDescription.Trim()}";
        }

        return AppendManagedConnectorExecutionApprovalDetail(
            $"Cephalon cannot currently advance execution approval for {CreateManagedConnectorExecutionIntentOperationLabel(operationId)} because governance or control-plane policy still blocks the shared execution lane.",
            detail);
    }

    private static string CreateManagedConnectorAutoBlockedExecutionApprovalDescription(
        string operationId,
        string? remediationDescription,
        string? executionIntentDescription)
    {
        var detail = remediationDescription;
        if (!string.IsNullOrWhiteSpace(executionIntentDescription))
        {
            detail = string.IsNullOrWhiteSpace(detail)
                ? executionIntentDescription
                : $"{detail.Trim()} {executionIntentDescription.Trim()}";
        }

        return AppendManagedConnectorExecutionApprovalDetail(
            $"Cephalon cannot currently trust execution approval for {CreateManagedConnectorExecutionIntentOperationLabel(operationId)} until shared runtime truth and remediation posture clear.",
            detail);
    }

    private static string CreateManagedConnectorApprovalRequiredExecutionApprovalDescription(
        string operationId,
        string? dryRunDescription,
        string? driftDescription)
    {
        var detail = string.IsNullOrWhiteSpace(driftDescription)
            ? dryRunDescription
            : string.IsNullOrWhiteSpace(dryRunDescription)
                ? driftDescription
                : $"{driftDescription.Trim()} {dryRunDescription.Trim()}";

        return AppendManagedConnectorExecutionApprovalDetail(
            $"Cephalon currently treats {CreateManagedConnectorExecutionIntentOperationLabel(operationId)} as a higher-risk follow-through that would still require an elevated explicit approval gate before future engine execution.",
            detail);
    }

    private static string CreateManagedConnectorApprovalReadyExecutionApprovalDescription(
        string operationId,
        string? dryRunDescription)
    {
        return AppendManagedConnectorExecutionApprovalDetail(
            $"Cephalon currently has enough shared runtime truth to prepare an approval workflow for {CreateManagedConnectorExecutionIntentOperationLabel(operationId)} before future engine execution.",
            dryRunDescription);
    }

    private static string CreateManagedConnectorAutoEligibleExecutionApprovalDescription(
        string operationId,
        string? dryRunDescription)
    {
        return AppendManagedConnectorExecutionApprovalDetail(
            $"Cephalon currently treats {CreateManagedConnectorExecutionIntentOperationLabel(operationId)} as auto-eligible on the shared execution lane because no additional approval workflow is required.",
            dryRunDescription);
    }

    private static string AppendManagedConnectorExecutionApprovalDetail(
        string summary,
        string? detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        return string.IsNullOrWhiteSpace(detail)
            ? summary.Trim()
            : $"{summary.Trim()} {detail.Trim()}";
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus CreateManagedConnectorCommandEnvelope(
        string executionRuntimeId,
        IReadOnlyList<string> cdcCaptureIds,
        string executionTopology,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        ManagedConnectorMetadataSnapshot metadataSnapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);
        ArgumentNullException.ThrowIfNull(cdcCaptureIds);
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(actionPlan);
        ArgumentNullException.ThrowIfNull(writePathReadiness);
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(dryRun);
        ArgumentNullException.ThrowIfNull(executionIntent);
        ArgumentNullException.ThrowIfNull(executionApproval);
        ArgumentNullException.ThrowIfNull(metadataSnapshot);

        var operationId = ResolveManagedConnectorCommandEnvelopeOperationId(
            executionApproval.OperationId,
            executionIntent.OperationId,
            dryRun.OperationId);
        var categories = CreateManagedConnectorCommandEnvelopeCategories(
            executionTopology,
            reportingCoverage,
            remediation,
            governance,
            drift,
            actionPlan,
            writePathReadiness,
            preflight,
            dryRun,
            executionIntent,
            executionApproval,
            operationId);
        var sourceId = ResolveManagedConnectorCommandEnvelopeSource(
            executionApproval,
            executionIntent,
            dryRun);

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase))
        {
            return CreateManagedConnectorCommandEnvelopeStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.NotApplicable,
                "The execution runtime does not currently represent a managed connector.",
                categories,
                CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.None,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                metadataSnapshot,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds);
        }

        if (executionIntent.IsDeferred ||
            string.Equals(
                executionApproval.State,
                CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.NotApplicable,
                StringComparison.OrdinalIgnoreCase))
        {
            return CreateManagedConnectorCommandEnvelopeStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.NotApplicable,
                CreateManagedConnectorNotApplicableCommandEnvelopeDescription(
                    executionIntent.Description,
                    governance.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                metadataSnapshot,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds);
        }

        if (executionIntent.IsOperatorOnly)
        {
            return CreateManagedConnectorCommandEnvelopeStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.OperatorOnly,
                CreateManagedConnectorOperatorOnlyCommandEnvelopeDescription(
                    operationId,
                    executionIntent.Description,
                    executionApproval.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                metadataSnapshot,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds);
        }

        if (executionApproval.IsAutoBlocked || executionApproval.IsPolicyBlocked)
        {
            return CreateManagedConnectorCommandEnvelopeStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.Blocked,
                CreateManagedConnectorBlockedCommandEnvelopeDescription(
                    operationId,
                    executionApproval.Description,
                    executionIntent.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                metadataSnapshot,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds);
        }

        if (executionApproval.IsApprovalRequired || executionApproval.IsApprovalReady)
        {
            return CreateManagedConnectorCommandEnvelopeStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.ApprovalGated,
                CreateManagedConnectorApprovalGatedCommandEnvelopeDescription(
                    operationId,
                    executionApproval,
                    dryRun.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                metadataSnapshot,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds);
        }

        if (executionApproval.IsAutoEligible)
        {
            return CreateManagedConnectorCommandEnvelopeStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.EngineReady,
                CreateManagedConnectorEngineReadyCommandEnvelopeDescription(
                    operationId,
                    executionApproval.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                metadataSnapshot,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds);
        }

        return CreateManagedConnectorCommandEnvelopeStatus(
            CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.Blocked,
            CreateManagedConnectorBlockedCommandEnvelopeDescription(
                operationId,
                executionApproval.Description,
                executionIntent.Description),
            categories,
            operationId,
            governance,
            reportingCoverage,
            remediation,
            drift,
            actionPlan,
            writePathReadiness,
            preflight,
            dryRun,
            executionIntent,
            executionApproval,
            metadataSnapshot,
            sourceId,
            executionRuntimeId,
            cdcCaptureIds);
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus CreateManagedConnectorCommandEnvelopeStatus(
        string state,
        string description,
        IReadOnlyList<string> categoryIds,
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        ManagedConnectorMetadataSnapshot metadataSnapshot,
        string sourceId,
        string executionRuntimeId,
        IReadOnlyList<string> cdcCaptureIds)
    {
        var normalizedOperationId = string.IsNullOrWhiteSpace(operationId)
            ? CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.None
            : operationId.Trim();
        var connectClusterId = ResolveManagedConnectorCommandTargetValue(
            metadataSnapshot.DeclaredConnectClusterId,
            metadataSnapshot.ReportedConnectClusterId);
        var connectorClass = ResolveManagedConnectorCommandTargetValue(
            metadataSnapshot.DeclaredConnectorClass,
            metadataSnapshot.ReportedConnectorClass);
        var sourceProviderId = ResolveManagedConnectorCommandTargetValue(
            metadataSnapshot.DeclaredSourceProviderId,
            metadataSnapshot.ReportedSourceProviderId);
        var wouldApplyChanges = executionApproval.WouldApplyChanges || executionIntent.WouldApplyChanges || dryRun.WouldApplyChanges;
        var potentialChangeCount = Math.Max(
            executionApproval.PotentialChangeCount,
            Math.Max(
                executionIntent.PotentialChangeCount,
                dryRun.PotentialChangeCount));
        var isDestructiveOperation = IsManagedConnectorCommandEnvelopeDestructiveOperation(normalizedOperationId);

        return new CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus(state, description)
        {
            CategoryIds = categoryIds,
            OperationId = normalizedOperationId,
            ManagementMode = governance.ManagementMode,
            ReportingCoverageState = reportingCoverage.State,
            RemediationState = remediation.State,
            GovernanceState = governance.State,
            DriftState = drift.State,
            ActionPlanState = actionPlan.State,
            WritePathReadinessState = writePathReadiness.State,
            PreflightState = preflight.State,
            DryRunState = dryRun.State,
            ExecutionIntentState = executionIntent.State,
            ExecutionApprovalState = executionApproval.State,
            PrimaryActionId = actionPlan.PrimaryActionId,
            SourceId = sourceId,
            ExecutionIntentConfidenceSourceId = executionIntent.ConfidenceSourceId,
            ExecutionApprovalSourceId = executionApproval.SourceId,
            PotentialChangeCount = potentialChangeCount,
            WouldApplyChanges = wouldApplyChanges,
            RequiresExplicitApproval = executionApproval.RequiresExplicitApproval,
            IsDestructiveOperation = isDestructiveOperation,
            ExecutionRuntimeId = executionRuntimeId,
            CdcCaptureIds = cdcCaptureIds,
            ConnectClusterId = connectClusterId,
            ConnectorClass = connectorClass,
            SourceProviderId = sourceProviderId,
            CommandFingerprint = CreateManagedConnectorCommandEnvelopeFingerprint(
                state,
                normalizedOperationId,
                sourceId,
                governance.ManagementMode,
                executionRuntimeId,
                cdcCaptureIds,
                connectClusterId,
                connectorClass,
                sourceProviderId,
                executionApproval.State,
                executionIntent.State,
                dryRun.State,
                reportingCoverage.State,
                executionApproval.RequiresExplicitApproval,
                wouldApplyChanges,
                isDestructiveOperation)
        };
    }

    private static string[] CreateManagedConnectorCommandEnvelopeCategories(
        string executionTopology,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        string operationId)
    {
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(actionPlan);
        ArgumentNullException.ThrowIfNull(writePathReadiness);
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(dryRun);
        ArgumentNullException.ThrowIfNull(executionIntent);
        ArgumentNullException.ThrowIfNull(executionApproval);

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var categories = new List<string>(capacity: 16);
        static void AddCategory(List<string> values, string category)
        {
            if (!values.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(category);
            }
        }

        if (executionIntent.IsDeferred)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ObserveOnlyMode);
        }

        if (remediation.IsBlocked ||
            executionApproval.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.BlockingRemediation,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.BlockingRemediation);
        }

        if (executionApproval.CategoryIds.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.StaleObservation,
            StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.StaleObservation);
        }

        if (!reportingCoverage.HasFullCoverage ||
            executionApproval.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.IncompleteReportingCoverage,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.IncompleteReportingCoverage);
        }

        if (actionPlan.IsWaiting ||
            executionApproval.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.RuntimeTruthIncomplete,
                StringComparer.OrdinalIgnoreCase) ||
            preflight.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.RuntimeTruthIncomplete,
                StringComparer.OrdinalIgnoreCase) ||
            writePathReadiness.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.RuntimeTruthIncomplete,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.RuntimeTruthIncomplete);
        }

        if (governance.IsOutOfPolicy)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.GovernanceOutOfPolicy);
        }

        if (executionIntent.IsOperatorOnly)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ControlPlaneOwnershipGap);
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.OperatorOnly);
        }

        if (executionIntent.WouldApplyChanges || dryRun.WouldApplyChanges)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ChangePlanned);
        }

        if (dryRun.IsNoOp ||
            executionApproval.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.NoExecutionNeeded,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.NoExecutionNeeded);
        }

        if (WouldManagedConnectorDryRunRequireLifecycleChange(operationId, dryRun.WouldApplyChanges))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.LifecycleChange);
        }

        if (IsManagedConnectorCommandEnvelopeDestructiveOperation(operationId) &&
            (dryRun.WouldApplyChanges || executionIntent.WouldApplyChanges))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.DestructiveOperation);
        }

        if (executionApproval.IsApprovalRequired)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ApprovalRequired);
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ApprovalGated);
        }
        else if (executionApproval.IsApprovalReady)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ApprovalReady);
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.ApprovalGated);
        }

        if (executionApproval.IsAutoEligible)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.EngineReady);
        }

        return [.. categories];
    }

    private static string ResolveManagedConnectorCommandEnvelopeSource(
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun)
    {
        ArgumentNullException.ThrowIfNull(executionApproval);
        ArgumentNullException.ThrowIfNull(executionIntent);
        ArgumentNullException.ThrowIfNull(dryRun);

        if (executionApproval.AppliesToManagedConnector)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources.ExecutionApproval;
        }

        if (executionIntent.AppliesToManagedConnector)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources.ExecutionIntent;
        }

        if (dryRun.AppliesToManagedConnector)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources.DryRun;
        }

        return CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources.Unknown;
    }

    private static string ResolveManagedConnectorCommandEnvelopeOperationId(
        string? executionApprovalOperationId,
        string? executionIntentOperationId,
        string? dryRunOperationId)
    {
        if (!string.IsNullOrWhiteSpace(executionApprovalOperationId))
        {
            return executionApprovalOperationId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(executionIntentOperationId))
        {
            return executionIntentOperationId.Trim();
        }

        return string.IsNullOrWhiteSpace(dryRunOperationId)
            ? CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.None
            : dryRunOperationId.Trim();
    }

    private static bool IsManagedConnectorCommandEnvelopeDestructiveOperation(string operationId)
    {
        return string.Equals(
            operationId,
            CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.Delete,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveManagedConnectorCommandTargetValue(
        string? declaredValue,
        string? reportedValue)
    {
        if (!string.IsNullOrWhiteSpace(declaredValue))
        {
            return declaredValue.Trim();
        }

        return string.IsNullOrWhiteSpace(reportedValue)
            ? null
            : reportedValue.Trim();
    }

    private static string CreateManagedConnectorCommandEnvelopeFingerprint(
        string state,
        string operationId,
        string sourceId,
        string? managementMode,
        string executionRuntimeId,
        IReadOnlyList<string> cdcCaptureIds,
        string? connectClusterId,
        string? connectorClass,
        string? sourceProviderId,
        string executionApprovalState,
        string executionIntentState,
        string dryRunState,
        string reportingCoverageState,
        bool requiresExplicitApproval,
        bool wouldApplyChanges,
        bool isDestructiveOperation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);
        ArgumentNullException.ThrowIfNull(cdcCaptureIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(executionApprovalState);
        ArgumentException.ThrowIfNullOrWhiteSpace(executionIntentState);
        ArgumentException.ThrowIfNullOrWhiteSpace(dryRunState);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportingCoverageState);

        return string.Join(
            "|",
            [
                "cephalon-managed-connector-command-envelope/v1",
                $"state={state.Trim()}",
                $"operation={operationId.Trim()}",
                $"source={sourceId.Trim()}",
                $"managementMode={NormalizeManagedConnectorFingerprintSegment(managementMode)}",
                $"runtime={executionRuntimeId.Trim()}",
                $"captures={string.Join(",", cdcCaptureIds)}",
                $"cluster={NormalizeManagedConnectorFingerprintSegment(connectClusterId)}",
                $"connectorClass={NormalizeManagedConnectorFingerprintSegment(connectorClass)}",
                $"sourceProvider={NormalizeManagedConnectorFingerprintSegment(sourceProviderId)}",
                $"executionApproval={executionApprovalState.Trim()}",
                $"executionIntent={executionIntentState.Trim()}",
                $"dryRun={dryRunState.Trim()}",
                $"reportingCoverage={reportingCoverageState.Trim()}",
                $"requiresExplicitApproval={requiresExplicitApproval.ToString().ToLowerInvariant()}",
                $"wouldApplyChanges={wouldApplyChanges.ToString().ToLowerInvariant()}",
                $"destructive={isDestructiveOperation.ToString().ToLowerInvariant()}"
            ]);
    }

    private static string NormalizeManagedConnectorFingerprintSegment(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "none"
            : value.Trim();
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus CreateManagedConnectorCommandIssuance(
        string executionRuntimeId,
        IReadOnlyList<string> cdcCaptureIds,
        string executionTopology,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus commandEnvelope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);
        ArgumentNullException.ThrowIfNull(cdcCaptureIds);
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(actionPlan);
        ArgumentNullException.ThrowIfNull(writePathReadiness);
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(dryRun);
        ArgumentNullException.ThrowIfNull(executionIntent);
        ArgumentNullException.ThrowIfNull(executionApproval);
        ArgumentNullException.ThrowIfNull(commandEnvelope);

        var operationId = ResolveManagedConnectorCommandIssuanceOperationId(
            commandEnvelope.OperationId,
            executionApproval.OperationId,
            executionIntent.OperationId);
        var categories = CreateManagedConnectorCommandIssuanceCategories(
            executionTopology,
            reportingCoverage,
            remediation,
            governance,
            drift,
            actionPlan,
            writePathReadiness,
            preflight,
            dryRun,
            executionIntent,
            executionApproval,
            commandEnvelope,
            operationId);
        var sourceId = ResolveManagedConnectorCommandIssuanceSource(
            commandEnvelope,
            executionApproval,
            executionIntent);

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase) ||
            executionIntent.IsDeferred ||
            string.Equals(
                executionApproval.State,
                CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.NotApplicable,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                commandEnvelope.State,
                CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.NotApplicable,
                StringComparison.OrdinalIgnoreCase))
        {
            return CreateManagedConnectorCommandIssuanceStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.NotApplicable,
                CreateManagedConnectorNotApplicableCommandIssuanceDescription(
                    commandEnvelope.Description,
                    governance.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds);
        }

        if (commandEnvelope.IsOperatorOnly)
        {
            return CreateManagedConnectorCommandIssuanceStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.OperatorOnly,
                CreateManagedConnectorOperatorOnlyCommandIssuanceDescription(
                    operationId,
                    commandEnvelope.Description,
                    executionApproval.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds);
        }

        if (commandEnvelope.IsBlocked)
        {
            return CreateManagedConnectorCommandIssuanceStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Blocked,
                CreateManagedConnectorBlockedCommandIssuanceDescription(
                    operationId,
                    commandEnvelope.Description,
                    executionApproval.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds);
        }

        if (commandEnvelope.IsApprovalGated)
        {
            return CreateManagedConnectorCommandIssuanceStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Accepted,
                CreateManagedConnectorAcceptedCommandIssuanceDescription(
                    operationId,
                    executionApproval,
                    dryRun.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds);
        }

        if (ShouldRejectManagedConnectorCommandIssuance(commandEnvelope, dryRun, executionApproval))
        {
            return CreateManagedConnectorCommandIssuanceStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Rejected,
                CreateManagedConnectorRejectedCommandIssuanceDescription(
                    operationId,
                    commandEnvelope.Description,
                    dryRun.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds);
        }

        if (commandEnvelope.IsEngineReady)
        {
            return CreateManagedConnectorCommandIssuanceStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Issued,
                CreateManagedConnectorIssuedCommandIssuanceDescription(
                    operationId,
                    commandEnvelope.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds);
        }

        return CreateManagedConnectorCommandIssuanceStatus(
            CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Blocked,
            CreateManagedConnectorBlockedCommandIssuanceDescription(
                operationId,
                commandEnvelope.Description,
                executionApproval.Description),
            categories,
            operationId,
            governance,
            reportingCoverage,
            remediation,
            drift,
            actionPlan,
            writePathReadiness,
            preflight,
            dryRun,
            executionIntent,
            executionApproval,
            commandEnvelope,
            sourceId,
            executionRuntimeId,
            cdcCaptureIds);
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus CreateManagedConnectorCommandIssuanceStatus(
        string state,
        string description,
        IReadOnlyList<string> categoryIds,
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus commandEnvelope,
        string sourceId,
        string executionRuntimeId,
        IReadOnlyList<string> cdcCaptureIds)
    {
        var normalizedOperationId = string.IsNullOrWhiteSpace(operationId)
            ? CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.None
            : operationId.Trim();

        return new CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus(state, description)
        {
            CategoryIds = categoryIds,
            OperationId = normalizedOperationId,
            ManagementMode = governance.ManagementMode,
            ReportingCoverageState = reportingCoverage.State,
            RemediationState = remediation.State,
            GovernanceState = governance.State,
            DriftState = drift.State,
            ActionPlanState = actionPlan.State,
            WritePathReadinessState = writePathReadiness.State,
            PreflightState = preflight.State,
            DryRunState = dryRun.State,
            ExecutionIntentState = executionIntent.State,
            ExecutionApprovalState = executionApproval.State,
            CommandEnvelopeState = commandEnvelope.State,
            PrimaryActionId = actionPlan.PrimaryActionId,
            SourceId = sourceId,
            CommandEnvelopeSourceId = commandEnvelope.SourceId,
            ExecutionIntentConfidenceSourceId = executionIntent.ConfidenceSourceId,
            ExecutionApprovalSourceId = executionApproval.SourceId,
            PotentialChangeCount = commandEnvelope.PotentialChangeCount,
            WouldApplyChanges = commandEnvelope.WouldApplyChanges,
            RequiresExplicitApproval = commandEnvelope.RequiresExplicitApproval,
            IsDestructiveOperation = commandEnvelope.IsDestructiveOperation,
            ExecutionRuntimeId = executionRuntimeId,
            CdcCaptureIds = cdcCaptureIds,
            ConnectClusterId = commandEnvelope.ConnectClusterId,
            ConnectorClass = commandEnvelope.ConnectorClass,
            SourceProviderId = commandEnvelope.SourceProviderId,
            CommandFingerprint = commandEnvelope.CommandFingerprint,
            IssuanceFingerprint = CreateManagedConnectorCommandIssuanceFingerprint(
                state,
                normalizedOperationId,
                sourceId,
                commandEnvelope.State,
                commandEnvelope.CommandFingerprint,
                executionApproval.State,
                executionIntent.State,
                dryRun.State,
                commandEnvelope.RequiresExplicitApproval,
                commandEnvelope.WouldApplyChanges,
                commandEnvelope.IsDestructiveOperation)
        };
    }

    private static string[] CreateManagedConnectorCommandIssuanceCategories(
        string executionTopology,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus commandEnvelope,
        string operationId)
    {
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(actionPlan);
        ArgumentNullException.ThrowIfNull(writePathReadiness);
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(dryRun);
        ArgumentNullException.ThrowIfNull(executionIntent);
        ArgumentNullException.ThrowIfNull(executionApproval);
        ArgumentNullException.ThrowIfNull(commandEnvelope);

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var categories = new List<string>(capacity: 18);
        static void AddCategory(List<string> values, string category)
        {
            if (!values.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(category);
            }
        }

        if (executionIntent.IsDeferred ||
            string.Equals(
                commandEnvelope.State,
                CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.NotApplicable,
                StringComparison.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ObserveOnlyMode);
        }

        if (remediation.IsBlocked ||
            commandEnvelope.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.BlockingRemediation,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.BlockingRemediation);
        }

        if (commandEnvelope.CategoryIds.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.StaleObservation,
            StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.StaleObservation);
        }

        if (!reportingCoverage.HasFullCoverage ||
            commandEnvelope.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.IncompleteReportingCoverage,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.IncompleteReportingCoverage);
        }

        if (actionPlan.IsWaiting ||
            commandEnvelope.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.RuntimeTruthIncomplete,
                StringComparer.OrdinalIgnoreCase) ||
            preflight.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.RuntimeTruthIncomplete,
                StringComparer.OrdinalIgnoreCase) ||
            writePathReadiness.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.RuntimeTruthIncomplete,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.RuntimeTruthIncomplete);
        }

        if (governance.IsOutOfPolicy)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.GovernanceOutOfPolicy);
        }

        if (commandEnvelope.IsOperatorOnly)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ControlPlaneOwnershipGap);
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.OperatorOnly);
        }

        if (commandEnvelope.WouldApplyChanges || dryRun.WouldApplyChanges)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ChangePlanned);
        }

        if (WouldManagedConnectorDryRunRequireLifecycleChange(operationId, dryRun.WouldApplyChanges))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.LifecycleChange);
        }

        if (commandEnvelope.IsDestructiveOperation)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.DestructiveOperation);
        }

        if (executionApproval.IsApprovalRequired)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ApprovalRequired);
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ApprovalGated);
        }
        else if (executionApproval.IsApprovalReady)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ApprovalReady);
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.ApprovalGated);
        }

        if (ShouldRejectManagedConnectorCommandIssuance(commandEnvelope, dryRun, executionApproval))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.Rejected);
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.NoExecutionNeeded);
        }
        else if (commandEnvelope.IsApprovalGated)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.Accepted);
        }
        else if (commandEnvelope.IsEngineReady)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.Issued);
        }

        return [.. categories];
    }

    private static string ResolveManagedConnectorCommandIssuanceSource(
        CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus commandEnvelope,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent)
    {
        ArgumentNullException.ThrowIfNull(commandEnvelope);
        ArgumentNullException.ThrowIfNull(executionApproval);
        ArgumentNullException.ThrowIfNull(executionIntent);

        if (commandEnvelope.AppliesToManagedConnector ||
            string.Equals(
                commandEnvelope.State,
                CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.NotApplicable,
                StringComparison.OrdinalIgnoreCase))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources.CommandEnvelope;
        }

        if (executionApproval.AppliesToManagedConnector)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources.ExecutionApproval;
        }

        if (executionIntent.AppliesToManagedConnector)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources.ExecutionIntent;
        }

        return CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources.Unknown;
    }

    private static string ResolveManagedConnectorCommandIssuanceOperationId(
        string? commandEnvelopeOperationId,
        string? executionApprovalOperationId,
        string? executionIntentOperationId)
    {
        if (!string.IsNullOrWhiteSpace(commandEnvelopeOperationId))
        {
            return commandEnvelopeOperationId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(executionApprovalOperationId))
        {
            return executionApprovalOperationId.Trim();
        }

        return string.IsNullOrWhiteSpace(executionIntentOperationId)
            ? CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.None
            : executionIntentOperationId.Trim();
    }

    private static bool ShouldRejectManagedConnectorCommandIssuance(
        CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus commandEnvelope,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval)
    {
        ArgumentNullException.ThrowIfNull(commandEnvelope);
        ArgumentNullException.ThrowIfNull(dryRun);
        ArgumentNullException.ThrowIfNull(executionApproval);

        return commandEnvelope.IsEngineReady &&
               (!commandEnvelope.HasCommandTarget ||
                !commandEnvelope.WouldApplyChanges ||
                dryRun.IsNoOp ||
                commandEnvelope.CategoryIds.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories.NoExecutionNeeded,
                    StringComparer.OrdinalIgnoreCase) ||
                executionApproval.CategoryIds.Contains(
                    CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories.NoExecutionNeeded,
                    StringComparer.OrdinalIgnoreCase));
    }

    private static string CreateManagedConnectorCommandIssuanceFingerprint(
        string state,
        string operationId,
        string sourceId,
        string commandEnvelopeState,
        string commandFingerprint,
        string executionApprovalState,
        string executionIntentState,
        string dryRunState,
        bool requiresExplicitApproval,
        bool wouldApplyChanges,
        bool isDestructiveOperation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandEnvelopeState);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandFingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(executionApprovalState);
        ArgumentException.ThrowIfNullOrWhiteSpace(executionIntentState);
        ArgumentException.ThrowIfNullOrWhiteSpace(dryRunState);

        return string.Join(
            "|",
            [
                "cephalon-managed-connector-command-issuance/v1",
                $"state={state.Trim()}",
                $"operation={operationId.Trim()}",
                $"source={sourceId.Trim()}",
                $"commandEnvelope={commandEnvelopeState.Trim()}",
                $"commandFingerprint={commandFingerprint.Trim()}",
                $"executionApproval={executionApprovalState.Trim()}",
                $"executionIntent={executionIntentState.Trim()}",
                $"dryRun={dryRunState.Trim()}",
                $"requiresExplicitApproval={requiresExplicitApproval.ToString().ToLowerInvariant()}",
                $"wouldApplyChanges={wouldApplyChanges.ToString().ToLowerInvariant()}",
                $"destructive={isDestructiveOperation.ToString().ToLowerInvariant()}"
            ]);
    }

    private CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus CreateManagedConnectorExecutionAdapter(
        CdcCaptureExecutionRuntimeDescriptor runtime,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus commandEnvelope,
        CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus commandIssuance,
        ManagedConnectorMetadataSnapshot metadataSnapshot)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(actionPlan);
        ArgumentNullException.ThrowIfNull(writePathReadiness);
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(dryRun);
        ArgumentNullException.ThrowIfNull(executionIntent);
        ArgumentNullException.ThrowIfNull(executionApproval);
        ArgumentNullException.ThrowIfNull(commandEnvelope);
        ArgumentNullException.ThrowIfNull(commandIssuance);
        ArgumentNullException.ThrowIfNull(metadataSnapshot);

        var candidateExecutionAdapter = ResolveManagedConnectorExecutionAdapter(runtime);
        var adapterId = candidateExecutionAdapter?.AdapterId ?? CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.None;
        var operationId = ResolveManagedConnectorExecutionAdapterOperationId(
            commandIssuance.OperationId,
            commandEnvelope.OperationId,
            executionApproval.OperationId,
            executionIntent.OperationId);
        var sourceId = ResolveManagedConnectorExecutionAdapterSource(
            commandIssuance,
            commandEnvelope,
            executionApproval,
            executionIntent);
        string state;
        string description;

        if (!string.Equals(runtime.ExecutionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                commandIssuance.State,
                CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.NotApplicable,
                StringComparison.OrdinalIgnoreCase))
        {
            state = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.NotApplicable;
            description = CreateManagedConnectorNotApplicableExecutionAdapterDescription(
                commandIssuance.Description,
                governance.Description);
        }
        else if (commandIssuance.IsOperatorOnly)
        {
            state = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.OperatorOnly;
            description = CreateManagedConnectorOperatorOnlyExecutionAdapterDescription(
                operationId,
                adapterId,
                commandIssuance.Description,
                executionApproval.Description);
        }
        else if (commandIssuance.IsBlocked)
        {
            state = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Blocked;
            description = CreateManagedConnectorBlockedExecutionAdapterDescription(
                operationId,
                adapterId,
                commandIssuance.Description,
                executionApproval.Description);
        }
        else if (candidateExecutionAdapter is null)
        {
            state = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Unavailable;
            description = CreateManagedConnectorUnavailableExecutionAdapterDescription(
                operationId,
                commandIssuance.Description);
        }
        else
        {
            state = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Ready;
            description = CreateManagedConnectorReadyExecutionAdapterDescription(
                operationId,
                adapterId,
                commandIssuance);
        }

        var categories = CreateManagedConnectorExecutionAdapterCategories(
            runtime.ExecutionTopology,
            state,
            reportingCoverage,
            remediation,
            governance,
            drift,
            actionPlan,
            writePathReadiness,
            preflight,
            dryRun,
            executionIntent,
            executionApproval,
            commandEnvelope,
            commandIssuance,
            operationId);

        return CreateManagedConnectorExecutionAdapterStatus(
            state,
            description,
            categories,
            operationId,
            governance,
            reportingCoverage,
            remediation,
            drift,
            actionPlan,
            writePathReadiness,
            preflight,
            dryRun,
            executionIntent,
            executionApproval,
            commandEnvelope,
            commandIssuance,
            sourceId,
            adapterId,
            runtime.Id,
            runtime.CdcCaptureIds,
            metadataSnapshot);
    }

    private ICdcCaptureExecutionRuntimeManagedConnectorExecutionAdapter? ResolveManagedConnectorExecutionAdapter(
        CdcCaptureExecutionRuntimeDescriptor runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        foreach (var executionAdapter in executionAdapters)
        {
            if (executionAdapter.CanHandle(runtime))
            {
                return executionAdapter;
            }
        }

        return null;
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult CreateManagedConnectorCommandExecution(
        CdcCaptureExecutionRuntimeDescriptor runtime,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult? latestCommandExecution)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(executionAdapter);

        if (latestCommandExecution is not null)
        {
            return latestCommandExecution;
        }

        var connectorId = runtime.Metadata.TryGetValue("connectorId", out var configuredConnectorId) &&
                          !string.IsNullOrWhiteSpace(configuredConnectorId)
            ? configuredConnectorId.Trim()
            : runtime.Id;
        var providerId = runtime.Metadata.TryGetValue("provider", out var configuredProviderId) &&
                         !string.IsNullOrWhiteSpace(configuredProviderId)
            ? configuredProviderId.Trim()
            : null;

        return new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult(
            executionAdapter.AppliesToManagedConnector
                ? CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded
                : CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NotApplicable,
            executionAdapter.AppliesToManagedConnector
                ? CreateManagedConnectorUnrecordedCommandExecutionDescription(executionAdapter)
                : "The execution runtime does not currently participate in a managed-connector command-execution lane.")
        {
            ExecutionRuntimeId = runtime.Id,
            RequestedOperationId = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None,
            ResolvedOperationId = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None,
            ExecutionAdapterState = executionAdapter.State,
            CommandIssuanceState = executionAdapter.CommandIssuanceState,
            CommandEnvelopeState = executionAdapter.CommandEnvelopeState,
            AdapterId = executionAdapter.AdapterId,
            ProviderId = providerId,
            ConnectClusterId = executionAdapter.ConnectClusterId,
            ConnectorId = connectorId,
            ConnectorClass = executionAdapter.ConnectorClass,
            SourceProviderId = executionAdapter.SourceProviderId,
            ManagementMode = executionAdapter.ManagementMode,
            SourceId = executionAdapter.SourceId,
            InvocationSourceId = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.None,
            CommandFingerprint = executionAdapter.CommandFingerprint,
            IssuanceFingerprint = executionAdapter.IssuanceFingerprint,
            AdapterFingerprint = executionAdapter.AdapterFingerprint,
            ExecutionFingerprint = string.Empty,
            RequiresExplicitApproval = executionAdapter.RequiresExplicitApproval,
            IsDestructiveOperation = executionAdapter.IsDestructiveOperation,
            WouldApplyChanges = executionAdapter.WouldApplyChanges
        };
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus CreateManagedConnectorCommandRetry(
        string executionRuntimeId,
        IReadOnlyList<string> cdcCaptureIds,
        string executionTopology,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus commandEnvelope,
        CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus commandIssuance,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult commandExecution,
        IReadOnlyList<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult> commandExecutionHistory,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);
        ArgumentNullException.ThrowIfNull(cdcCaptureIds);
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(actionPlan);
        ArgumentNullException.ThrowIfNull(writePathReadiness);
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(dryRun);
        ArgumentNullException.ThrowIfNull(executionIntent);
        ArgumentNullException.ThrowIfNull(executionApproval);
        ArgumentNullException.ThrowIfNull(commandEnvelope);
        ArgumentNullException.ThrowIfNull(commandIssuance);
        ArgumentNullException.ThrowIfNull(executionAdapter);
        ArgumentNullException.ThrowIfNull(commandExecution);
        ArgumentNullException.ThrowIfNull(commandExecutionHistory);

        var operationId = ResolveManagedConnectorCommandRetryOperationId(
            executionAdapter.OperationId,
            commandExecution.RequestedOperationId,
            commandExecution.ResolvedOperationId,
            commandIssuance.OperationId);
        var latestRecordedCommand = commandExecutionHistory.Count > 0
            ? commandExecutionHistory[0]
            : null;
        var latestMatchingCommand = commandExecutionHistory.FirstOrDefault(
            entry => MatchesManagedConnectorCommandRetryFingerprint(
                operationId,
                executionAdapter,
                entry));
        var hasMatchingCommandFingerprint = latestMatchingCommand is not null &&
                                           string.Equals(
                                               latestMatchingCommand.CommandFingerprint,
                                               executionAdapter.CommandFingerprint,
                                               StringComparison.OrdinalIgnoreCase);
        var hasMatchingIssuanceFingerprint = latestMatchingCommand is not null &&
                                            string.Equals(
                                                latestMatchingCommand.IssuanceFingerprint,
                                                executionAdapter.IssuanceFingerprint,
                                                StringComparison.OrdinalIgnoreCase);
        var hasMatchingAdapterFingerprint = latestMatchingCommand is not null &&
                                          string.Equals(
                                              latestMatchingCommand.AdapterFingerprint,
                                              executionAdapter.AdapterFingerprint,
                                              StringComparison.OrdinalIgnoreCase);
        var hasMatchingRetryFingerprint = latestMatchingCommand is not null &&
                                         hasMatchingCommandFingerprint &&
                                         hasMatchingIssuanceFingerprint &&
                                         hasMatchingAdapterFingerprint;
        var latestMatchingApprovalApplied = latestMatchingCommand?.ApprovalApplied ?? false;
        var latestMatchingDestructiveAllowanceApplied = latestMatchingCommand?.DestructiveAllowanceApplied ?? false;
        var canReuseApprovalFromMatchingHistory =
            !executionAdapter.RequiresExplicitApproval ||
            latestMatchingApprovalApplied;
        var canReuseDestructiveAllowanceFromMatchingHistory =
            !executionAdapter.IsDestructiveOperation ||
            latestMatchingDestructiveAllowanceApplied;
        var latestCommandState = latestRecordedCommand?.State ?? commandExecution.State;
        var latestCommandSourceId = latestRecordedCommand?.SourceId ?? commandExecution.SourceId;
        var latestExecutionFingerprint = latestRecordedCommand?.ExecutionFingerprint ?? commandExecution.ExecutionFingerprint;
        var latestAttemptId = latestRecordedCommand?.AttemptId ?? string.Empty;
        var latestRecordedAtUtc = latestRecordedCommand?.RecordedAtUtc;
        var cooldownUntilUtc = ResolveManagedConnectorCommandRetryCooldownUntilUtc(latestMatchingCommand);
        var categories = CreateManagedConnectorCommandRetryCategories(
            executionTopology,
            reportingCoverage,
            remediation,
            governance,
            actionPlan,
            writePathReadiness,
            preflight,
            executionApproval,
            commandIssuance,
            executionAdapter,
            latestRecordedCommand,
            latestMatchingCommand,
            operationId,
            now);
        var sourceId = ResolveManagedConnectorCommandRetrySource(latestMatchingCommand, latestRecordedCommand, executionAdapter);
        var retryFingerprint = CreateManagedConnectorCommandRetryFingerprint(
            operationId,
            executionRuntimeId,
            executionAdapter.CommandFingerprint,
            executionAdapter.IssuanceFingerprint,
            executionAdapter.AdapterFingerprint,
            executionAdapter.RequiresExplicitApproval,
            executionAdapter.IsDestructiveOperation,
            executionAdapter.WouldApplyChanges);

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase) ||
            executionIntent.IsDeferred ||
            !executionAdapter.AppliesToManagedConnector)
        {
            return CreateManagedConnectorCommandRetryStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotApplicable,
                CreateManagedConnectorNotApplicableCommandRetryDescription(
                    executionAdapter.Description,
                    governance.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                commandIssuance,
                executionAdapter,
                latestCommandState,
                sourceId,
                latestCommandSourceId,
                executionRuntimeId,
                cdcCaptureIds,
                latestAttemptId,
                latestRecordedAtUtc,
                cooldownUntilUtc,
                latestExecutionFingerprint,
                retryFingerprint,
                hasMatchingRetryFingerprint,
                hasMatchingCommandFingerprint,
                hasMatchingIssuanceFingerprint,
                hasMatchingAdapterFingerprint,
                latestMatchingApprovalApplied,
                latestMatchingDestructiveAllowanceApplied,
                canReuseApprovalFromMatchingHistory,
                canReuseDestructiveAllowanceFromMatchingHistory);
        }

        if (executionAdapter.IsOperatorOnly || commandExecution.IsOperatorOnly)
        {
            return CreateManagedConnectorCommandRetryStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.OperatorOnly,
                CreateManagedConnectorOperatorOnlyCommandRetryDescription(
                    operationId,
                    executionAdapter.Description,
                    latestMatchingCommand?.Description ?? latestRecordedCommand?.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                commandIssuance,
                executionAdapter,
                latestCommandState,
                sourceId,
                latestCommandSourceId,
                executionRuntimeId,
                cdcCaptureIds,
                latestAttemptId,
                latestRecordedAtUtc,
                cooldownUntilUtc,
                latestExecutionFingerprint,
                retryFingerprint,
                hasMatchingRetryFingerprint,
                hasMatchingCommandFingerprint,
                hasMatchingIssuanceFingerprint,
                hasMatchingAdapterFingerprint,
                latestMatchingApprovalApplied,
                latestMatchingDestructiveAllowanceApplied,
                canReuseApprovalFromMatchingHistory,
                canReuseDestructiveAllowanceFromMatchingHistory);
        }

        if (commandIssuance.IsRejected ||
            !executionAdapter.WouldApplyChanges ||
            commandExecution.IsNoOp ||
            (latestMatchingCommand?.IsNoOp ?? false))
        {
            return CreateManagedConnectorCommandRetryStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotNeeded,
                CreateManagedConnectorNotNeededCommandRetryDescription(
                    operationId,
                    executionAdapter,
                    latestMatchingCommand,
                    latestRecordedCommand),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                commandIssuance,
                executionAdapter,
                latestCommandState,
                sourceId,
                latestCommandSourceId,
                executionRuntimeId,
                cdcCaptureIds,
                latestAttemptId,
                latestRecordedAtUtc,
                cooldownUntilUtc,
                latestExecutionFingerprint,
                retryFingerprint,
                hasMatchingRetryFingerprint,
                hasMatchingCommandFingerprint,
                hasMatchingIssuanceFingerprint,
                hasMatchingAdapterFingerprint,
                latestMatchingApprovalApplied,
                latestMatchingDestructiveAllowanceApplied,
                canReuseApprovalFromMatchingHistory,
                canReuseDestructiveAllowanceFromMatchingHistory);
        }

        if (latestMatchingCommand is null)
        {
            return CreateManagedConnectorCommandRetryStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotNeeded,
                CreateManagedConnectorNoMatchingHistoryCommandRetryDescription(
                    operationId,
                    latestRecordedCommand is null,
                    executionAdapter.Description,
                    latestRecordedCommand?.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                commandIssuance,
                executionAdapter,
                latestCommandState,
                sourceId,
                latestCommandSourceId,
                executionRuntimeId,
                cdcCaptureIds,
                latestAttemptId,
                latestRecordedAtUtc,
                cooldownUntilUtc,
                latestExecutionFingerprint,
                retryFingerprint,
                hasMatchingRetryFingerprint,
                hasMatchingCommandFingerprint,
                hasMatchingIssuanceFingerprint,
                hasMatchingAdapterFingerprint,
                latestMatchingApprovalApplied,
                latestMatchingDestructiveAllowanceApplied,
                canReuseApprovalFromMatchingHistory,
                canReuseDestructiveAllowanceFromMatchingHistory);
        }

        if (cooldownUntilUtc.HasValue &&
            cooldownUntilUtc.Value > now)
        {
            return CreateManagedConnectorCommandRetryStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.Cooldown,
                CreateManagedConnectorCooldownCommandRetryDescription(
                    operationId,
                    latestMatchingCommand,
                    cooldownUntilUtc.Value),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                commandIssuance,
                executionAdapter,
                latestCommandState,
                sourceId,
                latestCommandSourceId,
                executionRuntimeId,
                cdcCaptureIds,
                latestAttemptId,
                latestRecordedAtUtc,
                cooldownUntilUtc,
                latestExecutionFingerprint,
                retryFingerprint,
                hasMatchingRetryFingerprint,
                hasMatchingCommandFingerprint,
                hasMatchingIssuanceFingerprint,
                hasMatchingAdapterFingerprint,
                latestMatchingApprovalApplied,
                latestMatchingDestructiveAllowanceApplied,
                canReuseApprovalFromMatchingHistory,
                canReuseDestructiveAllowanceFromMatchingHistory);
        }

        if (latestMatchingCommand.IsAdapted)
        {
            return CreateManagedConnectorCommandRetryStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.Duplicate,
                CreateManagedConnectorDuplicateCommandRetryDescription(
                    operationId,
                    latestMatchingCommand,
                    executionAdapter.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                commandIssuance,
                executionAdapter,
                latestCommandState,
                sourceId,
                latestCommandSourceId,
                executionRuntimeId,
                cdcCaptureIds,
                latestAttemptId,
                latestRecordedAtUtc,
                cooldownUntilUtc,
                latestExecutionFingerprint,
                retryFingerprint,
                hasMatchingRetryFingerprint,
                hasMatchingCommandFingerprint,
                hasMatchingIssuanceFingerprint,
                hasMatchingAdapterFingerprint,
                latestMatchingApprovalApplied,
                latestMatchingDestructiveAllowanceApplied,
                canReuseApprovalFromMatchingHistory,
                canReuseDestructiveAllowanceFromMatchingHistory);
        }

        if (executionAdapter.IsBlocked ||
            executionAdapter.IsUnavailable ||
            ((executionApproval.IsApprovalRequired ||
              executionApproval.IsApprovalReady ||
              executionAdapter.RequiresExplicitApproval ||
              executionAdapter.IsDestructiveOperation) &&
             (!canReuseApprovalFromMatchingHistory || !canReuseDestructiveAllowanceFromMatchingHistory)))
        {
            return CreateManagedConnectorCommandRetryStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.RetryBlocked,
                CreateManagedConnectorRetryBlockedDescription(
                    operationId,
                    latestMatchingCommand,
                    executionAdapter,
                    executionApproval),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                commandIssuance,
                executionAdapter,
                latestCommandState,
                sourceId,
                latestCommandSourceId,
                executionRuntimeId,
                cdcCaptureIds,
                latestAttemptId,
                latestRecordedAtUtc,
                cooldownUntilUtc,
                latestExecutionFingerprint,
                retryFingerprint,
                hasMatchingRetryFingerprint,
                hasMatchingCommandFingerprint,
                hasMatchingIssuanceFingerprint,
                hasMatchingAdapterFingerprint,
                latestMatchingApprovalApplied,
                latestMatchingDestructiveAllowanceApplied,
                canReuseApprovalFromMatchingHistory,
                canReuseDestructiveAllowanceFromMatchingHistory);
        }

        if (latestMatchingCommand.IsFailed ||
            latestMatchingCommand.IsUnavailable ||
            latestMatchingCommand.IsBlocked)
        {
            return CreateManagedConnectorCommandRetryStatus(
                CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.RetryEligible,
                CreateManagedConnectorRetryEligibleDescription(
                    operationId,
                    latestMatchingCommand,
                    executionAdapter.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                commandIssuance,
                executionAdapter,
                latestCommandState,
                sourceId,
                latestCommandSourceId,
                executionRuntimeId,
                cdcCaptureIds,
                latestAttemptId,
                latestRecordedAtUtc,
                cooldownUntilUtc,
                latestExecutionFingerprint,
                retryFingerprint,
                hasMatchingRetryFingerprint,
                hasMatchingCommandFingerprint,
                hasMatchingIssuanceFingerprint,
                hasMatchingAdapterFingerprint,
                latestMatchingApprovalApplied,
                latestMatchingDestructiveAllowanceApplied,
                canReuseApprovalFromMatchingHistory,
                canReuseDestructiveAllowanceFromMatchingHistory);
        }

        return CreateManagedConnectorCommandRetryStatus(
            CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotNeeded,
            CreateManagedConnectorNotNeededCommandRetryDescription(
                operationId,
                executionAdapter,
                latestMatchingCommand,
                latestRecordedCommand),
            categories,
            operationId,
            governance,
            reportingCoverage,
            remediation,
            drift,
            actionPlan,
            writePathReadiness,
            preflight,
            dryRun,
            executionIntent,
            executionApproval,
            commandEnvelope,
            commandIssuance,
            executionAdapter,
            latestCommandState,
            sourceId,
            latestCommandSourceId,
            executionRuntimeId,
            cdcCaptureIds,
            latestAttemptId,
            latestRecordedAtUtc,
            cooldownUntilUtc,
            latestExecutionFingerprint,
            retryFingerprint,
            hasMatchingRetryFingerprint,
            hasMatchingCommandFingerprint,
            hasMatchingIssuanceFingerprint,
            hasMatchingAdapterFingerprint,
            latestMatchingApprovalApplied,
            latestMatchingDestructiveAllowanceApplied,
            canReuseApprovalFromMatchingHistory,
            canReuseDestructiveAllowanceFromMatchingHistory);
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus CreateManagedConnectorCommandRetryStatus(
        string state,
        string description,
        IReadOnlyList<string> categoryIds,
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus commandEnvelope,
        CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus commandIssuance,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter,
        string latestCommandExecutionState,
        string sourceId,
        string latestCommandExecutionSourceId,
        string executionRuntimeId,
        IReadOnlyList<string> cdcCaptureIds,
        string latestAttemptId,
        DateTimeOffset? latestRecordedAtUtc,
        DateTimeOffset? cooldownUntilUtc,
        string latestExecutionFingerprint,
        string retryFingerprint,
        bool hasMatchingRetryFingerprint,
        bool hasMatchingCommandFingerprint,
        bool hasMatchingIssuanceFingerprint,
        bool hasMatchingAdapterFingerprint,
        bool latestMatchingApprovalApplied,
        bool latestMatchingDestructiveAllowanceApplied,
        bool canReuseApprovalFromMatchingHistory,
        bool canReuseDestructiveAllowanceFromMatchingHistory)
    {
        var normalizedOperationId = string.IsNullOrWhiteSpace(operationId)
            ? CdcCaptureExecutionRuntimeManagedConnectorCommandRetryOperationIds.None
            : operationId.Trim();

        return new CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus(state, description)
        {
            CategoryIds = categoryIds,
            OperationId = normalizedOperationId,
            ManagementMode = governance.ManagementMode,
            ReportingCoverageState = reportingCoverage.State,
            RemediationState = remediation.State,
            GovernanceState = governance.State,
            DriftState = drift.State,
            ActionPlanState = actionPlan.State,
            WritePathReadinessState = writePathReadiness.State,
            PreflightState = preflight.State,
            DryRunState = dryRun.State,
            ExecutionIntentState = executionIntent.State,
            ExecutionApprovalState = executionApproval.State,
            CommandEnvelopeState = commandEnvelope.State,
            CommandIssuanceState = commandIssuance.State,
            ExecutionAdapterState = executionAdapter.State,
            LatestCommandExecutionState = string.IsNullOrWhiteSpace(latestCommandExecutionState)
                ? CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded
                : latestCommandExecutionState.Trim(),
            PrimaryActionId = actionPlan.PrimaryActionId,
            SourceId = sourceId,
            ExecutionAdapterSourceId = executionAdapter.SourceId,
            LatestCommandExecutionSourceId = string.IsNullOrWhiteSpace(latestCommandExecutionSourceId)
                ? CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.Unknown
                : latestCommandExecutionSourceId.Trim(),
            ExecutionRuntimeId = executionRuntimeId,
            CdcCaptureIds = cdcCaptureIds,
            ConnectClusterId = executionAdapter.ConnectClusterId,
            ConnectorClass = executionAdapter.ConnectorClass,
            SourceProviderId = executionAdapter.SourceProviderId,
            PotentialChangeCount = executionAdapter.PotentialChangeCount,
            WouldApplyChanges = executionAdapter.WouldApplyChanges,
            RequiresExplicitApproval = executionAdapter.RequiresExplicitApproval,
            LatestMatchingApprovalApplied = latestMatchingApprovalApplied,
            IsDestructiveOperation = executionAdapter.IsDestructiveOperation,
            LatestMatchingDestructiveAllowanceApplied = latestMatchingDestructiveAllowanceApplied,
            CanReuseApprovalFromMatchingHistory = canReuseApprovalFromMatchingHistory,
            CanReuseDestructiveAllowanceFromMatchingHistory = canReuseDestructiveAllowanceFromMatchingHistory,
            CommandFingerprint = executionAdapter.CommandFingerprint,
            IssuanceFingerprint = executionAdapter.IssuanceFingerprint,
            AdapterFingerprint = executionAdapter.AdapterFingerprint,
            RetryFingerprint = retryFingerprint,
            LatestExecutionFingerprint = latestExecutionFingerprint,
            LatestAttemptId = latestAttemptId,
            LatestRecordedAtUtc = latestRecordedAtUtc,
            CooldownUntilUtc = cooldownUntilUtc,
            HasMatchingRetryFingerprint = hasMatchingRetryFingerprint,
            HasMatchingCommandFingerprint = hasMatchingCommandFingerprint,
            HasMatchingIssuanceFingerprint = hasMatchingIssuanceFingerprint,
            HasMatchingAdapterFingerprint = hasMatchingAdapterFingerprint
        };
    }

    private static string[] CreateManagedConnectorCommandRetryCategories(
        string executionTopology,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus commandIssuance,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult? latestRecordedCommand,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult? latestMatchingCommand,
        string operationId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(actionPlan);
        ArgumentNullException.ThrowIfNull(writePathReadiness);
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(executionApproval);
        ArgumentNullException.ThrowIfNull(commandIssuance);
        ArgumentNullException.ThrowIfNull(executionAdapter);

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var categories = new List<string>(capacity: 24);
        static void AddCategory(List<string> values, string category)
        {
            if (!values.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(category);
            }
        }

        if (!executionAdapter.AppliesToManagedConnector)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.ObserveOnlyMode);
        }

        if (remediation.IsBlocked ||
            executionAdapter.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.BlockingRemediation,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.BlockingRemediation);
        }

        if (executionAdapter.CategoryIds.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.StaleObservation,
            StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.StaleObservation);
        }

        if (!reportingCoverage.HasFullCoverage ||
            executionAdapter.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.IncompleteReportingCoverage,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.IncompleteReportingCoverage);
        }

        if (actionPlan.IsWaiting ||
            executionAdapter.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.RuntimeTruthIncomplete,
                StringComparer.OrdinalIgnoreCase) ||
            preflight.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.RuntimeTruthIncomplete,
                StringComparer.OrdinalIgnoreCase) ||
            writePathReadiness.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.RuntimeTruthIncomplete,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.RuntimeTruthIncomplete);
        }

        if (governance.IsOutOfPolicy)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.GovernanceOutOfPolicy);
        }

        if (executionAdapter.IsOperatorOnly)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.ControlPlaneOwnershipGap);
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.OperatorOnly);
        }

        if (executionAdapter.WouldApplyChanges)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.ChangePlanned);
        }

        if (WouldManagedConnectorDryRunRequireLifecycleChange(operationId, executionAdapter.WouldApplyChanges))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.LifecycleChange);
        }

        if (executionAdapter.IsDestructiveOperation)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.DestructiveOperation);
        }

        if (executionApproval.IsApprovalRequired)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.ApprovalRequired);
        }
        else if (executionApproval.IsApprovalReady)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.ApprovalReady);
        }

        if (commandIssuance.IsRejected || !executionAdapter.WouldApplyChanges)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.NoExecutionNeeded);
        }

        if (latestRecordedCommand is null)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.NoRecordedCommand);
        }
        else if (latestMatchingCommand is null)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.NoMatchingCommandHistory);
        }
        else
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.MatchingCommandFingerprint);
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.MatchingIssuanceFingerprint);
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.MatchingAdapterFingerprint);

            var cooldownUntilUtc = ResolveManagedConnectorCommandRetryCooldownUntilUtc(latestMatchingCommand);
            if (cooldownUntilUtc.HasValue &&
                cooldownUntilUtc.Value > now)
            {
                AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.CooldownActive);
            }

            if (latestMatchingCommand.IsAdapted)
            {
                AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.DuplicateCommand);
                AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.LatestExecutionAdapted);
            }
            else if (latestMatchingCommand.IsFailed)
            {
                AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.LatestExecutionFailed);
                if (!executionAdapter.IsBlocked &&
                    !executionAdapter.IsUnavailable &&
                    !executionApproval.IsApprovalRequired &&
                    !executionApproval.IsApprovalReady)
                {
                    AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.RetryEligible);
                }
            }
            else if (latestMatchingCommand.IsUnavailable)
            {
                AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.LatestExecutionUnavailable);
                if (!executionAdapter.IsBlocked &&
                    !executionAdapter.IsUnavailable &&
                    !executionApproval.IsApprovalRequired &&
                    !executionApproval.IsApprovalReady)
                {
                    AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.RetryEligible);
                }
            }
            else if (latestMatchingCommand.IsBlocked)
            {
                AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.LatestExecutionBlocked);
                if (!executionAdapter.IsBlocked &&
                    !executionAdapter.IsUnavailable &&
                    !executionApproval.IsApprovalRequired &&
                    !executionApproval.IsApprovalReady &&
                    !executionAdapter.RequiresExplicitApproval)
                {
                    AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.RetryEligible);
                }
            }
        }

        return [.. categories];
    }

    private static string ResolveManagedConnectorCommandRetrySource(
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult? latestMatchingCommand,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult? latestRecordedCommand,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter)
    {
        ArgumentNullException.ThrowIfNull(executionAdapter);

        if (latestMatchingCommand is not null)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandRetrySources.CommandExecutionHistory;
        }

        if (latestRecordedCommand is not null)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandRetrySources.CommandExecution;
        }

        return executionAdapter.AppliesToManagedConnector
            ? CdcCaptureExecutionRuntimeManagedConnectorCommandRetrySources.ExecutionAdapter
            : CdcCaptureExecutionRuntimeManagedConnectorCommandRetrySources.Unknown;
    }

    private static string ResolveManagedConnectorCommandRetryOperationId(
        string? executionAdapterOperationId,
        string? requestedOperationId,
        string? resolvedOperationId,
        string? commandIssuanceOperationId)
    {
        if (!string.IsNullOrWhiteSpace(executionAdapterOperationId))
        {
            return executionAdapterOperationId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(requestedOperationId))
        {
            return requestedOperationId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(resolvedOperationId))
        {
            return resolvedOperationId.Trim();
        }

        return string.IsNullOrWhiteSpace(commandIssuanceOperationId)
            ? CdcCaptureExecutionRuntimeManagedConnectorCommandRetryOperationIds.None
            : commandIssuanceOperationId.Trim();
    }

    private static bool MatchesManagedConnectorCommandRetryFingerprint(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult entry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentNullException.ThrowIfNull(executionAdapter);
        ArgumentNullException.ThrowIfNull(entry);

        var matchesOperation = string.Equals(entry.RequestedOperationId, operationId, StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(entry.ResolvedOperationId, operationId, StringComparison.OrdinalIgnoreCase);
        return matchesOperation &&
               string.Equals(entry.CommandFingerprint, executionAdapter.CommandFingerprint, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(entry.IssuanceFingerprint, executionAdapter.IssuanceFingerprint, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(entry.AdapterFingerprint, executionAdapter.AdapterFingerprint, StringComparison.OrdinalIgnoreCase);
    }

    private static DateTimeOffset? ResolveManagedConnectorCommandRetryCooldownUntilUtc(
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult? latestMatchingCommand)
    {
        if (latestMatchingCommand?.RecordedAtUtc is not DateTimeOffset recordedAtUtc ||
            !ShouldManagedConnectorCommandRetryUseCooldown(latestMatchingCommand.State))
        {
            return null;
        }

        return recordedAtUtc.AddSeconds(ManagedConnectorCommandRetryCooldownSeconds);
    }

    private static bool ShouldManagedConnectorCommandRetryUseCooldown(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return false;
        }

        return string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unavailable, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Failed, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateManagedConnectorCommandRetryFingerprint(
        string operationId,
        string executionRuntimeId,
        string commandFingerprint,
        string issuanceFingerprint,
        string adapterFingerprint,
        bool requiresExplicitApproval,
        bool isDestructiveOperation,
        bool wouldApplyChanges)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);

        return string.Join(
            "|",
            [
                "cephalon-managed-connector-command-retry/v1",
                $"operation={operationId.Trim()}",
                $"runtime={executionRuntimeId.Trim()}",
                $"commandFingerprint={NormalizeManagedConnectorFingerprintSegment(commandFingerprint)}",
                $"issuanceFingerprint={NormalizeManagedConnectorFingerprintSegment(issuanceFingerprint)}",
                $"adapterFingerprint={NormalizeManagedConnectorFingerprintSegment(adapterFingerprint)}",
                $"requiresExplicitApproval={requiresExplicitApproval.ToString().ToLowerInvariant()}",
                $"destructive={isDestructiveOperation.ToString().ToLowerInvariant()}",
                $"wouldApplyChanges={wouldApplyChanges.ToString().ToLowerInvariant()}"
            ]);
    }

    private static string CreateManagedConnectorNotApplicableCommandRetryDescription(
        string? executionAdapterDescription,
        string? governanceDescription)
    {
        return AppendManagedConnectorCommandEnvelopeDetail(
            "Cephalon does not currently expose managed-connector retry posture while the shared runtime remains observe-only.",
            CombineManagedConnectorCommandEnvelopeDetail(executionAdapterDescription, governanceDescription));
    }

    private static string CreateManagedConnectorNotNeededCommandRetryDescription(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult? latestMatchingCommand,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult? latestRecordedCommand)
    {
        ArgumentNullException.ThrowIfNull(executionAdapter);

        if (latestMatchingCommand?.IsNoOp == true ||
            latestRecordedCommand?.IsNoOp == true ||
            !executionAdapter.WouldApplyChanges)
        {
            return AppendManagedConnectorCommandEnvelopeDetail(
                $"Cephalon does not currently recommend retrying {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} because the shared runtime truth indicates no additional provider command is needed.",
                CombineManagedConnectorCommandEnvelopeDetail(
                    latestMatchingCommand?.Description ?? latestRecordedCommand?.Description,
                    executionAdapter.Description));
        }

        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon does not currently recommend retrying {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)}.",
            CombineManagedConnectorCommandEnvelopeDetail(
                latestMatchingCommand?.Description ?? latestRecordedCommand?.Description,
                executionAdapter.Description));
    }

    private static string CreateManagedConnectorNoMatchingHistoryCommandRetryDescription(
        string operationId,
        bool hasNoRecordedHistory,
        string? executionAdapterDescription,
        string? latestRecordedDescription)
    {
        var summary = hasNoRecordedHistory
            ? $"Cephalon does not currently recommend retrying {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} because no matching command-execution outcome has been recorded yet. Use the shared command lane for the first attempt."
            : $"Cephalon does not currently recommend retrying {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} because the current command posture does not match any recorded command outcome.";

        return AppendManagedConnectorCommandEnvelopeDetail(
            summary,
            CombineManagedConnectorCommandEnvelopeDetail(latestRecordedDescription, executionAdapterDescription));
    }

    private static string CreateManagedConnectorCooldownCommandRetryDescription(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult latestMatchingCommand,
        DateTimeOffset cooldownUntilUtc)
    {
        ArgumentNullException.ThrowIfNull(latestMatchingCommand);

        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon recently recorded matching {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} command attempt '{NormalizeManagedConnectorFingerprintSegment(latestMatchingCommand.AttemptId)}'. Wait until '{cooldownUntilUtc:O}' before reconsidering a retry.",
            latestMatchingCommand.Description);
    }

    private static string CreateManagedConnectorDuplicateCommandRetryDescription(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult latestMatchingCommand,
        string? executionAdapterDescription)
    {
        ArgumentNullException.ThrowIfNull(latestMatchingCommand);

        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon already recorded matching {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} command attempt '{NormalizeManagedConnectorFingerprintSegment(latestMatchingCommand.AttemptId)}'. Replaying it now would be duplicative until runtime truth changes.",
            CombineManagedConnectorCommandEnvelopeDetail(latestMatchingCommand.Description, executionAdapterDescription));
    }

    private static string CreateManagedConnectorRetryBlockedDescription(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult latestMatchingCommand,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval)
    {
        ArgumentNullException.ThrowIfNull(latestMatchingCommand);
        ArgumentNullException.ThrowIfNull(executionAdapter);
        ArgumentNullException.ThrowIfNull(executionApproval);

        var summary = executionApproval.IsApprovalRequired
            ? $"Cephalon recorded a prior {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} outcome, but a higher-risk approval gate still blocks a safe retry."
            : executionApproval.IsApprovalReady || executionAdapter.RequiresExplicitApproval
                ? $"Cephalon recorded a prior {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} outcome, but approval must still clear before a safe retry."
                : executionAdapter.IsUnavailable
                    ? $"Cephalon recorded a prior {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} outcome, but no provider execution adapter is currently available for a safe retry."
                    : $"Cephalon recorded a prior {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} outcome, but the current shared runtime truth still blocks a safe retry.";

        return AppendManagedConnectorCommandEnvelopeDetail(
            summary,
            CombineManagedConnectorCommandEnvelopeDetail(latestMatchingCommand.Description, executionAdapter.Description));
    }

    private static string CreateManagedConnectorRetryEligibleDescription(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult latestMatchingCommand,
        string? executionAdapterDescription)
    {
        ArgumentNullException.ThrowIfNull(latestMatchingCommand);

        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon recorded a prior {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} outcome and the current shared runtime truth now allows one safe retry.",
            CombineManagedConnectorCommandEnvelopeDetail(latestMatchingCommand.Description, executionAdapterDescription));
    }

    private static string CreateManagedConnectorOperatorOnlyCommandRetryDescription(
        string operationId,
        string? executionAdapterDescription,
        string? latestCommandDescription)
    {
        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon can track command history for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)}, but retry remains operator-owned until a later managed control-plane slice ships.",
            CombineManagedConnectorCommandEnvelopeDetail(latestCommandDescription, executionAdapterDescription));
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus CreateManagedConnectorRetryExecutionPolicy(
        string executionRuntimeId,
        IReadOnlyList<string> cdcCaptureIds,
        string executionTopology,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus commandEnvelope,
        CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus commandIssuance,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult commandExecution,
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry,
        bool automaticRetryEnabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);
        ArgumentNullException.ThrowIfNull(cdcCaptureIds);
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(actionPlan);
        ArgumentNullException.ThrowIfNull(writePathReadiness);
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(dryRun);
        ArgumentNullException.ThrowIfNull(executionIntent);
        ArgumentNullException.ThrowIfNull(executionApproval);
        ArgumentNullException.ThrowIfNull(commandEnvelope);
        ArgumentNullException.ThrowIfNull(commandIssuance);
        ArgumentNullException.ThrowIfNull(executionAdapter);
        ArgumentNullException.ThrowIfNull(commandExecution);
        ArgumentNullException.ThrowIfNull(commandRetry);

        var operationId = string.IsNullOrWhiteSpace(commandRetry.OperationId)
            ? CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.None
            : commandRetry.OperationId.Trim();
        var categories = CreateManagedConnectorRetryExecutionPolicyCategories(
            executionTopology,
            commandRetry,
            executionApproval,
            executionAdapter,
            automaticRetryEnabled);
        var sourceId = ResolveManagedConnectorRetryExecutionPolicySource(
            commandRetry,
            executionApproval,
            executionAdapter);

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase) ||
            executionIntent.IsDeferred ||
            !commandRetry.AppliesToManagedConnector)
        {
            return CreateManagedConnectorRetryExecutionPolicyStatus(
                CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable,
                CreateManagedConnectorNotApplicableRetryExecutionPolicyDescription(
                    commandRetry.Description,
                    executionAdapter.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                commandIssuance,
                executionAdapter,
                commandExecution,
                commandRetry,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds,
                automaticRetryEnabled);
        }

        if (commandRetry.IsOperatorOnly || executionAdapter.IsOperatorOnly)
        {
            return CreateManagedConnectorRetryExecutionPolicyStatus(
                CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.OperatorOnly,
                CreateManagedConnectorOperatorOnlyRetryExecutionPolicyDescription(
                    operationId,
                    executionAdapter.Description,
                    commandRetry.Description),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                commandIssuance,
                executionAdapter,
                commandExecution,
                commandRetry,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds,
                automaticRetryEnabled);
        }

        if (commandRetry.IsNotNeeded || commandRetry.IsDuplicate)
        {
            return CreateManagedConnectorRetryExecutionPolicyStatus(
                CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotNeeded,
                CreateManagedConnectorNotNeededRetryExecutionPolicyDescription(
                    operationId,
                    commandRetry),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                commandIssuance,
                executionAdapter,
                commandExecution,
                commandRetry,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds,
                automaticRetryEnabled);
        }

        if (commandRetry.IsCooldown)
        {
            return CreateManagedConnectorRetryExecutionPolicyStatus(
                CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.Cooldown,
                CreateManagedConnectorCooldownRetryExecutionPolicyDescription(
                    operationId,
                    commandRetry),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                commandIssuance,
                executionAdapter,
                commandExecution,
                commandRetry,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds,
                automaticRetryEnabled);
        }

        if (commandRetry.IsRetryBlocked &&
            (executionApproval.IsApprovalRequired ||
             executionApproval.IsApprovalReady ||
             commandRetry.RequiresExplicitApproval))
        {
            return CreateManagedConnectorRetryExecutionPolicyStatus(
                CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.ManualApproval,
                CreateManagedConnectorManualApprovalRetryExecutionPolicyDescription(
                    operationId,
                    executionApproval,
                    commandRetry),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                commandIssuance,
                executionAdapter,
                commandExecution,
                commandRetry,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds,
                automaticRetryEnabled);
        }

        if (commandRetry.IsRetryBlocked)
        {
            return CreateManagedConnectorRetryExecutionPolicyStatus(
                CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.PolicyBlocked,
                CreateManagedConnectorPolicyBlockedRetryExecutionPolicyDescription(
                    operationId,
                    executionAdapter,
                    commandRetry),
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                commandIssuance,
                executionAdapter,
                commandExecution,
                commandRetry,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds,
                automaticRetryEnabled);
        }

        if (commandRetry.IsRetryEligible)
        {
            var policyState = automaticRetryEnabled
                ? CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.RetryReady
                : CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.BackgroundRetryDisabled;
            var description = automaticRetryEnabled
                ? CreateManagedConnectorRetryReadyDescription(
                    operationId,
                    commandRetry)
                : CreateManagedConnectorBackgroundRetryDisabledDescription(
                    operationId,
                    commandRetry);

            return CreateManagedConnectorRetryExecutionPolicyStatus(
                policyState,
                description,
                categories,
                operationId,
                governance,
                reportingCoverage,
                remediation,
                drift,
                actionPlan,
                writePathReadiness,
                preflight,
                dryRun,
                executionIntent,
                executionApproval,
                commandEnvelope,
                commandIssuance,
                executionAdapter,
                commandExecution,
                commandRetry,
                sourceId,
                executionRuntimeId,
                cdcCaptureIds,
                automaticRetryEnabled);
        }

        return CreateManagedConnectorRetryExecutionPolicyStatus(
            CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotNeeded,
            CreateManagedConnectorNotNeededRetryExecutionPolicyDescription(
                operationId,
                commandRetry),
            categories,
            operationId,
            governance,
            reportingCoverage,
            remediation,
            drift,
            actionPlan,
            writePathReadiness,
            preflight,
            dryRun,
            executionIntent,
            executionApproval,
            commandEnvelope,
            commandIssuance,
            executionAdapter,
            commandExecution,
            commandRetry,
            sourceId,
            executionRuntimeId,
            cdcCaptureIds,
            automaticRetryEnabled);
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus CreateManagedConnectorRetryExecutionPolicyStatus(
        string state,
        string description,
        IReadOnlyList<string> categoryIds,
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus commandEnvelope,
        CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus commandIssuance,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult commandExecution,
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry,
        string sourceId,
        string executionRuntimeId,
        IReadOnlyList<string> cdcCaptureIds,
        bool automaticRetryEnabled)
    {
        var normalizedOperationId = string.IsNullOrWhiteSpace(operationId)
            ? CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyOperationIds.None
            : operationId.Trim();

        return new CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus(state, description)
        {
            CategoryIds = categoryIds,
            OperationId = normalizedOperationId,
            ManagementMode = governance.ManagementMode,
            ReportingCoverageState = reportingCoverage.State,
            RemediationState = remediation.State,
            GovernanceState = governance.State,
            DriftState = drift.State,
            ActionPlanState = actionPlan.State,
            WritePathReadinessState = writePathReadiness.State,
            PreflightState = preflight.State,
            DryRunState = dryRun.State,
            ExecutionIntentState = executionIntent.State,
            ExecutionApprovalState = executionApproval.State,
            CommandEnvelopeState = commandEnvelope.State,
            CommandIssuanceState = commandIssuance.State,
            ExecutionAdapterState = executionAdapter.State,
            CommandRetryState = commandRetry.State,
            LatestCommandExecutionState = string.IsNullOrWhiteSpace(commandRetry.LatestCommandExecutionState)
                ? commandExecution.State
                : commandRetry.LatestCommandExecutionState.Trim(),
            PrimaryActionId = actionPlan.PrimaryActionId,
            SourceId = sourceId,
            CommandRetrySourceId = commandRetry.SourceId,
            ExecutionApprovalSourceId = executionApproval.SourceId,
            ExecutionAdapterSourceId = executionAdapter.SourceId,
            ExecutionRuntimeId = executionRuntimeId,
            CdcCaptureIds = cdcCaptureIds,
            ConnectClusterId = commandRetry.ConnectClusterId ?? executionAdapter.ConnectClusterId,
            ConnectorClass = commandRetry.ConnectorClass ?? executionAdapter.ConnectorClass,
            SourceProviderId = commandRetry.SourceProviderId ?? executionAdapter.SourceProviderId,
            PotentialChangeCount = commandRetry.PotentialChangeCount,
            WouldApplyChanges = commandRetry.WouldApplyChanges,
            RequiresExplicitApproval = commandRetry.RequiresExplicitApproval,
            LatestMatchingApprovalApplied = commandRetry.LatestMatchingApprovalApplied,
            IsDestructiveOperation = commandRetry.IsDestructiveOperation,
            LatestMatchingDestructiveAllowanceApplied = commandRetry.LatestMatchingDestructiveAllowanceApplied,
            CanReuseApprovalFromMatchingHistory = commandRetry.CanReuseApprovalFromMatchingHistory,
            CanReuseDestructiveAllowanceFromMatchingHistory = commandRetry.CanReuseDestructiveAllowanceFromMatchingHistory,
            IsAutomaticRetryEnabled = automaticRetryEnabled,
            CommandFingerprint = commandRetry.CommandFingerprint,
            IssuanceFingerprint = commandRetry.IssuanceFingerprint,
            AdapterFingerprint = commandRetry.AdapterFingerprint,
            RetryFingerprint = commandRetry.RetryFingerprint,
            LatestExecutionFingerprint = commandRetry.LatestExecutionFingerprint,
            LatestAttemptId = commandRetry.LatestAttemptId,
            LatestRecordedAtUtc = commandRetry.LatestRecordedAtUtc,
            CooldownUntilUtc = commandRetry.CooldownUntilUtc,
            HasMatchingRetryFingerprint = commandRetry.HasMatchingRetryFingerprint,
            HasMatchingCommandFingerprint = commandRetry.HasMatchingCommandFingerprint,
            HasMatchingIssuanceFingerprint = commandRetry.HasMatchingIssuanceFingerprint,
            HasMatchingAdapterFingerprint = commandRetry.HasMatchingAdapterFingerprint
        };
    }

    private static string[] CreateManagedConnectorRetryExecutionPolicyCategories(
        string executionTopology,
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter,
        bool automaticRetryEnabled)
    {
        ArgumentNullException.ThrowIfNull(commandRetry);
        ArgumentNullException.ThrowIfNull(executionApproval);
        ArgumentNullException.ThrowIfNull(executionAdapter);

        var categories = new List<string>();

        static void AddCategory(List<string> values, string category)
        {
            if (!values.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(category);
            }
        }

        static void AddMirroredCategory(
            List<string> values,
            CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetryStatus,
            string retryCategory,
            string policyCategory)
        {
            if (commandRetryStatus.CategoryIds.Contains(retryCategory, StringComparer.OrdinalIgnoreCase))
            {
                AddCategory(values, policyCategory);
            }
        }

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        AddMirroredCategory(categories, commandRetry, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.ObserveOnlyMode, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ObserveOnlyMode);
        AddMirroredCategory(categories, commandRetry, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.BlockingRemediation, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.BlockingRemediation);
        AddMirroredCategory(categories, commandRetry, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.StaleObservation, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.StaleObservation);
        AddMirroredCategory(categories, commandRetry, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.IncompleteReportingCoverage, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.IncompleteReportingCoverage);
        AddMirroredCategory(categories, commandRetry, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.RuntimeTruthIncomplete, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.RuntimeTruthIncomplete);
        AddMirroredCategory(categories, commandRetry, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.GovernanceOutOfPolicy, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.GovernanceOutOfPolicy);
        AddMirroredCategory(categories, commandRetry, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.ControlPlaneOwnershipGap, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ControlPlaneOwnershipGap);
        AddMirroredCategory(categories, commandRetry, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.ChangePlanned, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ChangePlanned);
        AddMirroredCategory(categories, commandRetry, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.LifecycleChange, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.LifecycleChange);
        AddMirroredCategory(categories, commandRetry, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.DestructiveOperation, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.DestructiveOperation);
        AddMirroredCategory(categories, commandRetry, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.NoExecutionNeeded, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.NoExecutionNeeded);
        AddMirroredCategory(categories, commandRetry, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.CooldownActive, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.CooldownActive);
        AddMirroredCategory(categories, commandRetry, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.DuplicateCommand, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.DuplicateCommand);
        AddMirroredCategory(categories, commandRetry, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.OperatorOnly, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.OperatorOnly);
        AddMirroredCategory(categories, commandRetry, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.LatestExecutionBlocked, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ProviderExecutionBlocked);
        AddMirroredCategory(categories, commandRetry, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.LatestExecutionUnavailable, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ProviderExecutionUnavailable);
        AddMirroredCategory(categories, commandRetry, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.LatestExecutionFailed, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ProviderExecutionFailed);

        if (executionApproval.IsApprovalRequired)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ManualApprovalRequired);
        }
        else if (executionApproval.IsApprovalReady || commandRetry.RequiresExplicitApproval)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ManualApprovalReady);
        }

        if (commandRetry.IsRetryEligible)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.RetryCandidate);

            if (!automaticRetryEnabled)
            {
                AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.AutomaticRetryDisabled);
            }
        }

        if (executionAdapter.IsOperatorOnly || commandRetry.IsOperatorOnly)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ControlPlaneOwnershipGap);
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.OperatorOnly);
        }

        return [.. categories];
    }

    private static string ResolveManagedConnectorRetryExecutionPolicySource(
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter)
    {
        ArgumentNullException.ThrowIfNull(commandRetry);
        ArgumentNullException.ThrowIfNull(executionApproval);
        ArgumentNullException.ThrowIfNull(executionAdapter);

        if (commandRetry.IsRetryBlocked &&
            (executionApproval.IsApprovalRequired ||
             executionApproval.IsApprovalReady ||
             commandRetry.RequiresExplicitApproval))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicySources.ExecutionApproval;
        }

        if (commandRetry.IsOperatorOnly || executionAdapter.IsOperatorOnly)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicySources.ExecutionAdapter;
        }

        return commandRetry.AppliesToManagedConnector
            ? CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicySources.CommandRetry
            : CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicySources.Unknown;
    }

    private static string CreateManagedConnectorNotApplicableRetryExecutionPolicyDescription(
        string? commandRetryDescription,
        string? executionAdapterDescription)
    {
        return AppendManagedConnectorCommandEnvelopeDetail(
            "Cephalon does not currently expose automatic retry-execution policy while the shared runtime remains observe-only.",
            CombineManagedConnectorCommandEnvelopeDetail(commandRetryDescription, executionAdapterDescription));
    }

    private static string CreateManagedConnectorNotNeededRetryExecutionPolicyDescription(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry)
    {
        ArgumentNullException.ThrowIfNull(commandRetry);

        var summary = commandRetry.IsDuplicate
            ? $"Cephalon does not currently schedule automatic {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} retry because replaying the matching command would be duplicative until runtime truth changes."
            : $"Cephalon does not currently schedule automatic {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} retry because the shared runtime truth indicates no further retry action is needed.";

        return AppendManagedConnectorCommandEnvelopeDetail(summary, commandRetry.Description);
    }

    private static string CreateManagedConnectorCooldownRetryExecutionPolicyDescription(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry)
    {
        ArgumentNullException.ThrowIfNull(commandRetry);

        var summary = commandRetry.CooldownUntilUtc.HasValue
            ? $"Cephalon identified a matching {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} retry candidate, but retry-execution policy is waiting until '{commandRetry.CooldownUntilUtc.Value:O}' before reconsidering it."
            : $"Cephalon identified a matching {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} retry candidate, but retry-execution policy is still within its cooldown window.";

        return AppendManagedConnectorCommandEnvelopeDetail(summary, commandRetry.Description);
    }

    private static string CreateManagedConnectorManualApprovalRetryExecutionPolicyDescription(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry)
    {
        ArgumentNullException.ThrowIfNull(executionApproval);
        ArgumentNullException.ThrowIfNull(commandRetry);

        var summary = executionApproval.IsApprovalRequired
            ? $"Cephalon identified a retry candidate for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)}, but a higher-risk manual approval gate still blocks automatic retry."
            : $"Cephalon identified a retry candidate for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)}, but manual approval must clear before retry-execution policy can act on it.";

        return AppendManagedConnectorCommandEnvelopeDetail(
            summary,
            CombineManagedConnectorCommandEnvelopeDetail(executionApproval.Description, commandRetry.Description));
    }

    private static string CreateManagedConnectorPolicyBlockedRetryExecutionPolicyDescription(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter,
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry)
    {
        ArgumentNullException.ThrowIfNull(executionAdapter);
        ArgumentNullException.ThrowIfNull(commandRetry);

        var summary = executionAdapter.IsUnavailable
            ? $"Cephalon identified a retry candidate for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)}, but no provider execution adapter is currently available."
            : executionAdapter.IsBlocked
                ? $"Cephalon identified a retry candidate for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)}, but shared runtime truth still blocks a safe retry."
                : $"Cephalon identified a retry candidate for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)}, but current retry-execution policy still blocks automatic retry.";

        return AppendManagedConnectorCommandEnvelopeDetail(
            summary,
            CombineManagedConnectorCommandEnvelopeDetail(executionAdapter.Description, commandRetry.Description));
    }

    private static string CreateManagedConnectorBackgroundRetryDisabledDescription(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry)
    {
        ArgumentNullException.ThrowIfNull(commandRetry);

        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon identified one safe {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} retry candidate, but automatic background retry remains disabled in the current engine policy baseline.",
            commandRetry.Description);
    }

    private static string CreateManagedConnectorRetryReadyDescription(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry)
    {
        ArgumentNullException.ThrowIfNull(commandRetry);

        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon identified one safe {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} retry candidate and the current engine policy allows automatic retry.",
            commandRetry.Description);
    }

    private static string CreateManagedConnectorOperatorOnlyRetryExecutionPolicyDescription(
        string operationId,
        string? executionAdapterDescription,
        string? commandRetryDescription)
    {
        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon can evaluate retry policy for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)}, but execution remains operator-owned until a later managed control-plane slice ships.",
            CombineManagedConnectorCommandEnvelopeDetail(commandRetryDescription, executionAdapterDescription));
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus CreateManagedConnectorCommandJournal(
        string executionRuntimeId,
        IReadOnlyList<string> cdcCaptureIds,
        string executionTopology,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus commandEnvelope,
        CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus commandIssuance,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult commandExecution,
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry,
        CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus retryExecutionPolicy,
        ManagedConnectorCommandExecutionJournal commandJournal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);
        ArgumentNullException.ThrowIfNull(cdcCaptureIds);
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(actionPlan);
        ArgumentNullException.ThrowIfNull(writePathReadiness);
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(dryRun);
        ArgumentNullException.ThrowIfNull(executionIntent);
        ArgumentNullException.ThrowIfNull(executionApproval);
        ArgumentNullException.ThrowIfNull(commandEnvelope);
        ArgumentNullException.ThrowIfNull(commandIssuance);
        ArgumentNullException.ThrowIfNull(executionAdapter);
        ArgumentNullException.ThrowIfNull(commandExecution);
        ArgumentNullException.ThrowIfNull(commandRetry);
        ArgumentNullException.ThrowIfNull(retryExecutionPolicy);
        ArgumentNullException.ThrowIfNull(commandJournal);

        var state = ResolveManagedConnectorCommandJournalState(
            executionTopology,
            commandExecution,
            commandRetry,
            retryExecutionPolicy,
            commandJournal);
        var categories = CreateManagedConnectorCommandJournalCategories(
            executionTopology,
            reportingCoverage,
            governance,
            commandExecution,
            commandRetry,
            retryExecutionPolicy,
            commandJournal);
        var operationId = ResolveManagedConnectorCommandJournalOperationId(
            retryExecutionPolicy.OperationId,
            commandRetry.OperationId,
            commandExecution.RequestedOperationId,
            commandExecution.ResolvedOperationId);
        var sourceId = ResolveManagedConnectorCommandJournalSource(
            state,
            commandRetry,
            retryExecutionPolicy);
        var latestEntry = commandJournal.LatestEntry;
        var oldestRetainedEntry = commandJournal.OldestRetainedEntry;
        var description = state switch
        {
            CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable =>
                CreateManagedConnectorNotApplicableCommandJournalDescription(
                    commandRetry.Description,
                    retryExecutionPolicy.Description),
            CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Empty =>
                CreateManagedConnectorEmptyCommandJournalDescription(
                    commandJournal,
                    commandExecution.Description,
                    retryExecutionPolicy.Description),
            CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Truncated =>
                CreateManagedConnectorTruncatedCommandJournalDescription(
                    operationId,
                    commandJournal,
                    latestEntry?.Description,
                    retryExecutionPolicy.Description),
            CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.CooldownActive =>
                CreateManagedConnectorCooldownActiveCommandJournalDescription(
                    operationId,
                    commandJournal,
                    commandRetry,
                    retryExecutionPolicy),
            CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.DuplicateEvidencePresent =>
                CreateManagedConnectorDuplicateEvidenceCommandJournalDescription(
                    operationId,
                    commandJournal,
                    commandRetry,
                    retryExecutionPolicy),
            CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.InsufficientForAutomation =>
                CreateManagedConnectorInsufficientAutomationCommandJournalDescription(
                    operationId,
                    retryExecutionPolicy,
                    commandRetry),
            _ => CreateManagedConnectorBoundedCommandJournalDescription(
                operationId,
                commandJournal,
                commandExecution.Description,
                commandRetry.Description)
        };

        return CreateManagedConnectorCommandJournalStatus(
            state,
            description,
            categories,
            operationId,
            governance,
            reportingCoverage,
            remediation,
            drift,
            actionPlan,
            writePathReadiness,
            preflight,
            dryRun,
            executionIntent,
            executionApproval,
            commandEnvelope,
            commandIssuance,
            executionAdapter,
            commandExecution,
            commandRetry,
            retryExecutionPolicy,
            sourceId,
            executionRuntimeId,
            cdcCaptureIds,
            commandJournal,
            latestEntry,
            oldestRetainedEntry);
    }

    private static string ResolveManagedConnectorCommandJournalState(
        string executionTopology,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult commandExecution,
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry,
        CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus retryExecutionPolicy,
        ManagedConnectorCommandExecutionJournal commandJournal)
    {
        ArgumentNullException.ThrowIfNull(commandExecution);
        ArgumentNullException.ThrowIfNull(commandRetry);
        ArgumentNullException.ThrowIfNull(retryExecutionPolicy);
        ArgumentNullException.ThrowIfNull(commandJournal);

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(commandExecution.State, CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NotApplicable, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(commandRetry.State, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotApplicable, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(retryExecutionPolicy.State, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable, StringComparison.OrdinalIgnoreCase))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable;
        }

        if (!commandJournal.HasRecordedEntries)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Empty;
        }

        if (commandJournal.IsTruncated)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Truncated;
        }

        if (commandRetry.IsCooldown || retryExecutionPolicy.IsCooldown)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.CooldownActive;
        }

        if (commandRetry.IsDuplicate ||
            retryExecutionPolicy.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.DuplicateCommand,
                StringComparer.OrdinalIgnoreCase))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.DuplicateEvidencePresent;
        }

        if (retryExecutionPolicy.IsOperatorOnly ||
            retryExecutionPolicy.IsPolicyBlocked ||
            retryExecutionPolicy.RequiresManualApproval ||
            retryExecutionPolicy.IsBackgroundRetryDisabled)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.InsufficientForAutomation;
        }

        return CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Bounded;
    }

    private static string[] CreateManagedConnectorCommandJournalCategories(
        string executionTopology,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult commandExecution,
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry,
        CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus retryExecutionPolicy,
        ManagedConnectorCommandExecutionJournal commandJournal)
    {
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(commandExecution);
        ArgumentNullException.ThrowIfNull(commandRetry);
        ArgumentNullException.ThrowIfNull(retryExecutionPolicy);
        ArgumentNullException.ThrowIfNull(commandJournal);

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        if (string.Equals(commandExecution.State, CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NotApplicable, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(commandRetry.State, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotApplicable, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(retryExecutionPolicy.State, CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable, StringComparison.OrdinalIgnoreCase))
        {
            return [CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.ObserveOnlyMode];
        }

        var categories = new List<string>(capacity: 16);

        static void AddCategory(List<string> values, string category)
        {
            if (!values.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(category);
            }
        }

        if (!commandJournal.HasRecordedEntries)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.NoRecordedCommand);
            return [.. categories];
        }

        AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.BoundedRetention);

        if (commandJournal.IsTruncated)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.HistoryTruncated);
        }

        if (!reportingCoverage.HasFullCoverage ||
            retryExecutionPolicy.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.RuntimeTruthIncomplete,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.RuntimeTruthIncomplete);
        }

        if (governance.IsOutOfPolicy ||
            retryExecutionPolicy.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.GovernanceOutOfPolicy,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.GovernanceOutOfPolicy);
        }

        if (commandRetry.HasMatchingCommandFingerprint || retryExecutionPolicy.HasMatchingCommandFingerprint)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.MatchingCommandFingerprint);
        }

        if (commandRetry.HasCooldownWindow || retryExecutionPolicy.HasCooldownWindow)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.CooldownActive);
        }

        if (commandRetry.IsDuplicate ||
            retryExecutionPolicy.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.DuplicateCommand,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.DuplicateCommand);
        }

        if (commandRetry.IsOperatorOnly || retryExecutionPolicy.IsOperatorOnly)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.ControlPlaneOwnershipGap);
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.OperatorOnly);
        }

        if (retryExecutionPolicy.IsPolicyBlocked)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.PolicyBlocked);
        }

        if (retryExecutionPolicy.RequiresManualApproval)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.ManualApprovalRequired);
        }

        if (retryExecutionPolicy.IsBackgroundRetryDisabled)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.AutomaticRetryDisabled);
        }

        if (!commandExecution.WouldApplyChanges ||
            retryExecutionPolicy.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.NoExecutionNeeded,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.NoExecutionNeeded);
        }

        if (commandExecution.IsBlocked)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.LatestExecutionBlocked);
        }
        else if (commandExecution.IsUnavailable)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.LatestExecutionUnavailable);
        }
        else if (commandExecution.IsFailed)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.LatestExecutionFailed);
        }
        else if (commandExecution.IsAdapted)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.LatestExecutionAdapted);
        }

        return [.. categories];
    }

    private static string ResolveManagedConnectorCommandJournalSource(
        string state,
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry,
        CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus retryExecutionPolicy)
    {
        ArgumentNullException.ThrowIfNull(commandRetry);
        ArgumentNullException.ThrowIfNull(retryExecutionPolicy);

        return state switch
        {
            CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.CooldownActive or
            CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.DuplicateEvidencePresent =>
                CdcCaptureExecutionRuntimeManagedConnectorCommandJournalSources.CommandRetry,
            CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.InsufficientForAutomation =>
                CdcCaptureExecutionRuntimeManagedConnectorCommandJournalSources.RetryExecutionPolicy,
            CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable =>
                CdcCaptureExecutionRuntimeManagedConnectorCommandJournalSources.Unknown,
            _ => CdcCaptureExecutionRuntimeManagedConnectorCommandJournalSources.CommandExecutionHistory
        };
    }

    private static string ResolveManagedConnectorCommandJournalOperationId(
        string? retryExecutionPolicyOperationId,
        string? commandRetryOperationId,
        string? requestedOperationId,
        string? resolvedOperationId)
    {
        if (!string.IsNullOrWhiteSpace(retryExecutionPolicyOperationId))
        {
            return retryExecutionPolicyOperationId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(commandRetryOperationId))
        {
            return commandRetryOperationId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(requestedOperationId))
        {
            return requestedOperationId.Trim();
        }

        return string.IsNullOrWhiteSpace(resolvedOperationId)
            ? CdcCaptureExecutionRuntimeManagedConnectorCommandJournalOperationIds.None
            : resolvedOperationId.Trim();
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus CreateManagedConnectorCommandJournalStatus(
        string state,
        string description,
        IReadOnlyList<string> categoryIds,
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus commandEnvelope,
        CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus commandIssuance,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult commandExecution,
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry,
        CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus retryExecutionPolicy,
        string sourceId,
        string executionRuntimeId,
        IReadOnlyList<string> cdcCaptureIds,
        ManagedConnectorCommandExecutionJournal commandJournal,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult? latestEntry,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult? oldestRetainedEntry)
    {
        var normalizedOperationId = string.IsNullOrWhiteSpace(operationId)
            ? CdcCaptureExecutionRuntimeManagedConnectorCommandJournalOperationIds.None
            : operationId.Trim();
        var commandFingerprint = !string.IsNullOrWhiteSpace(retryExecutionPolicy.CommandFingerprint)
            ? retryExecutionPolicy.CommandFingerprint
            : commandRetry.CommandFingerprint;
        var retryFingerprint = !string.IsNullOrWhiteSpace(retryExecutionPolicy.RetryFingerprint)
            ? retryExecutionPolicy.RetryFingerprint
            : commandRetry.RetryFingerprint;

        return new CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus(state, description)
        {
            CategoryIds = categoryIds,
            OperationId = normalizedOperationId,
            ManagementMode = retryExecutionPolicy.ManagementMode ?? commandRetry.ManagementMode ?? commandExecution.ManagementMode,
            ReportingCoverageState = reportingCoverage.State,
            RemediationState = remediation.State,
            GovernanceState = governance.State,
            DriftState = drift.State,
            ActionPlanState = actionPlan.State,
            WritePathReadinessState = writePathReadiness.State,
            PreflightState = preflight.State,
            DryRunState = dryRun.State,
            ExecutionIntentState = executionIntent.State,
            ExecutionApprovalState = executionApproval.State,
            CommandEnvelopeState = commandEnvelope.State,
            CommandIssuanceState = commandIssuance.State,
            ExecutionAdapterState = executionAdapter.State,
            LatestCommandExecutionState = commandExecution.State,
            CommandRetryState = commandRetry.State,
            RetryExecutionPolicyState = retryExecutionPolicy.State,
            PrimaryActionId = actionPlan.PrimaryActionId,
            SourceId = sourceId,
            CommandRetrySourceId = commandRetry.SourceId,
            RetryExecutionPolicySourceId = retryExecutionPolicy.SourceId,
            ExecutionRuntimeId = executionRuntimeId,
            CdcCaptureIds = cdcCaptureIds,
            ConnectClusterId = retryExecutionPolicy.ConnectClusterId ?? commandRetry.ConnectClusterId ?? commandExecution.ConnectClusterId,
            ConnectorClass = retryExecutionPolicy.ConnectorClass ?? commandRetry.ConnectorClass ?? commandExecution.ConnectorClass,
            SourceProviderId = retryExecutionPolicy.SourceProviderId ?? commandRetry.SourceProviderId ?? commandExecution.SourceProviderId,
            TotalRecordedEntryCount = commandJournal.TotalRecordedEntryCount,
            RetainedEntryCount = commandJournal.RetainedEntryCount,
            MaximumRetainedEntryCount = commandJournal.MaximumRetainedEntryCount,
            PotentialChangeCount = Math.Max(retryExecutionPolicy.PotentialChangeCount, commandRetry.PotentialChangeCount),
            WouldApplyChanges = retryExecutionPolicy.WouldApplyChanges || commandRetry.WouldApplyChanges || commandExecution.WouldApplyChanges,
            RequiresExplicitApproval = retryExecutionPolicy.RequiresExplicitApproval || commandRetry.RequiresExplicitApproval || commandExecution.RequiresExplicitApproval,
            IsDestructiveOperation = retryExecutionPolicy.IsDestructiveOperation || commandRetry.IsDestructiveOperation || commandExecution.IsDestructiveOperation,
            IsAutomaticRetryEnabled = retryExecutionPolicy.IsAutomaticRetryEnabled,
            CommandFingerprint = commandFingerprint,
            RetryFingerprint = retryFingerprint,
            LatestExecutionFingerprint = latestEntry?.ExecutionFingerprint ?? commandRetry.LatestExecutionFingerprint,
            LatestAttemptId = latestEntry?.AttemptId ?? commandRetry.LatestAttemptId,
            OldestRetainedAttemptId = oldestRetainedEntry?.AttemptId ?? string.Empty,
            LatestRecordedAtUtc = latestEntry?.RecordedAtUtc ?? commandRetry.LatestRecordedAtUtc,
            OldestRetainedRecordedAtUtc = oldestRetainedEntry?.RecordedAtUtc,
            CooldownUntilUtc = retryExecutionPolicy.CooldownUntilUtc ?? commandRetry.CooldownUntilUtc,
            HasMatchingRetryFingerprint = commandRetry.HasMatchingRetryFingerprint || retryExecutionPolicy.HasMatchingRetryFingerprint,
            HasMatchingCommandFingerprint = commandRetry.HasMatchingCommandFingerprint || retryExecutionPolicy.HasMatchingCommandFingerprint
        };
    }

    private static string CreateManagedConnectorNotApplicableCommandJournalDescription(
        string? commandRetryDescription,
        string? retryExecutionPolicyDescription)
    {
        return AppendManagedConnectorCommandEnvelopeDetail(
            "Cephalon does not currently expose a managed-connector command journal while the shared runtime remains observe-only.",
            CombineManagedConnectorCommandEnvelopeDetail(commandRetryDescription, retryExecutionPolicyDescription));
    }

    private static string CreateManagedConnectorEmptyCommandJournalDescription(
        ManagedConnectorCommandExecutionJournal commandJournal,
        string? commandExecutionDescription,
        string? retryExecutionPolicyDescription)
    {
        ArgumentNullException.ThrowIfNull(commandJournal);

        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon has not yet recorded any managed-connector command outcomes for this runtime. The bounded journal is empty and can retain up to {commandJournal.MaximumRetainedEntryCount} recent entries once commands are recorded.",
            CombineManagedConnectorCommandEnvelopeDetail(commandExecutionDescription, retryExecutionPolicyDescription));
    }

    private static string CreateManagedConnectorTruncatedCommandJournalDescription(
        string operationId,
        ManagedConnectorCommandExecutionJournal commandJournal,
        string? latestCommandDescription,
        string? retryExecutionPolicyDescription)
    {
        ArgumentNullException.ThrowIfNull(commandJournal);

        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon retains only the newest {commandJournal.RetainedEntryCount} of {commandJournal.TotalRecordedEntryCount} recorded {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} command outcomes. Older entries have been truncated from the bounded journal.",
            CombineManagedConnectorCommandEnvelopeDetail(latestCommandDescription, retryExecutionPolicyDescription));
    }

    private static string CreateManagedConnectorCooldownActiveCommandJournalDescription(
        string operationId,
        ManagedConnectorCommandExecutionJournal commandJournal,
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry,
        CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus retryExecutionPolicy)
    {
        ArgumentNullException.ThrowIfNull(commandJournal);
        ArgumentNullException.ThrowIfNull(commandRetry);
        ArgumentNullException.ThrowIfNull(retryExecutionPolicy);

        var cooldownUntilUtc = retryExecutionPolicy.CooldownUntilUtc ?? commandRetry.CooldownUntilUtc;
        var summary = cooldownUntilUtc.HasValue
            ? $"Cephalon retains bounded {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} command evidence, and matching recent history remains inside the cooldown window until '{cooldownUntilUtc.Value:O}'."
            : $"Cephalon retains bounded {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} command evidence, and matching recent history still remains inside an active cooldown window.";

        return AppendManagedConnectorCommandEnvelopeDetail(
            summary,
            CombineManagedConnectorCommandEnvelopeDetail(commandRetry.Description, retryExecutionPolicy.Description));
    }

    private static string CreateManagedConnectorDuplicateEvidenceCommandJournalDescription(
        string operationId,
        ManagedConnectorCommandExecutionJournal commandJournal,
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry,
        CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus retryExecutionPolicy)
    {
        ArgumentNullException.ThrowIfNull(commandJournal);
        ArgumentNullException.ThrowIfNull(commandRetry);
        ArgumentNullException.ThrowIfNull(retryExecutionPolicy);

        var latestAttemptId = NormalizeManagedConnectorFingerprintSegment(commandJournal.LatestEntry?.AttemptId);

        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon retains bounded {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} command history showing matching prior attempt '{latestAttemptId}'. Replaying it now would be duplicative until shared runtime truth changes.",
            CombineManagedConnectorCommandEnvelopeDetail(commandRetry.Description, retryExecutionPolicy.Description));
    }

    private static string CreateManagedConnectorInsufficientAutomationCommandJournalDescription(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus retryExecutionPolicy,
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry)
    {
        ArgumentNullException.ThrowIfNull(retryExecutionPolicy);
        ArgumentNullException.ThrowIfNull(commandRetry);

        string summary;
        if (retryExecutionPolicy.IsOperatorOnly)
        {
            summary = $"Cephalon retains bounded {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} command history, but execution remains operator-owned until a later managed control-plane slice ships.";
        }
        else if (retryExecutionPolicy.RequiresManualApproval)
        {
            summary = $"Cephalon retains bounded {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} command history, but manual approval must clear before automation should continue.";
        }
        else if (retryExecutionPolicy.IsBackgroundRetryDisabled)
        {
            summary = $"Cephalon retains bounded {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} command history, but automatic background retry remains disabled in the current engine policy baseline.";
        }
        else
        {
            summary = $"Cephalon retains bounded {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} command history, but current shared runtime truth still leaves the journal insufficient for automation.";
        }

        return AppendManagedConnectorCommandEnvelopeDetail(
            summary,
            CombineManagedConnectorCommandEnvelopeDetail(retryExecutionPolicy.Description, commandRetry.Description));
    }

    private static string CreateManagedConnectorBoundedCommandJournalDescription(
        string operationId,
        ManagedConnectorCommandExecutionJournal commandJournal,
        string? commandExecutionDescription,
        string? commandRetryDescription)
    {
        ArgumentNullException.ThrowIfNull(commandJournal);

        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon retains {commandJournal.RetainedEntryCount} recent {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} command outcome(s) in the bounded journal, and the retained evidence remains sufficient for operator-facing retry and idempotency answers.",
            CombineManagedConnectorCommandEnvelopeDetail(commandExecutionDescription, commandRetryDescription));
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStatus CreateManagedConnectorAutomaticRetryExecution(
        string executionRuntimeId,
        IReadOnlyList<string> cdcCaptureIds,
        string executionTopology,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus commandEnvelope,
        CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus commandIssuance,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult commandExecution,
        CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus commandRetry,
        CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus retryExecutionPolicy,
        CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus commandJournal,
        ManagedConnectorCommandExecutionJournal commandExecutionHistoryJournal,
        bool automaticRetryEnabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);
        ArgumentNullException.ThrowIfNull(cdcCaptureIds);
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(actionPlan);
        ArgumentNullException.ThrowIfNull(writePathReadiness);
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(dryRun);
        ArgumentNullException.ThrowIfNull(executionIntent);
        ArgumentNullException.ThrowIfNull(executionApproval);
        ArgumentNullException.ThrowIfNull(commandEnvelope);
        ArgumentNullException.ThrowIfNull(commandIssuance);
        ArgumentNullException.ThrowIfNull(executionAdapter);
        ArgumentNullException.ThrowIfNull(commandExecution);
        ArgumentNullException.ThrowIfNull(commandRetry);
        ArgumentNullException.ThrowIfNull(retryExecutionPolicy);
        ArgumentNullException.ThrowIfNull(commandJournal);
        ArgumentNullException.ThrowIfNull(commandExecutionHistoryJournal);

        var operationId = string.IsNullOrWhiteSpace(retryExecutionPolicy.OperationId)
            ? string.IsNullOrWhiteSpace(commandJournal.OperationId)
                ? string.IsNullOrWhiteSpace(commandRetry.OperationId)
                    ? CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionOperationIds.None
                    : commandRetry.OperationId.Trim()
                : commandJournal.OperationId.Trim()
            : retryExecutionPolicy.OperationId.Trim();
        var commandFingerprint = !string.IsNullOrWhiteSpace(retryExecutionPolicy.CommandFingerprint)
            ? retryExecutionPolicy.CommandFingerprint
            : commandJournal.CommandFingerprint;
        var expectedIssuanceFingerprint = !string.IsNullOrWhiteSpace(retryExecutionPolicy.IssuanceFingerprint)
            ? retryExecutionPolicy.IssuanceFingerprint
            : commandExecution.IssuanceFingerprint;
        var expectedAdapterFingerprint = !string.IsNullOrWhiteSpace(retryExecutionPolicy.AdapterFingerprint)
            ? retryExecutionPolicy.AdapterFingerprint
            : commandExecution.AdapterFingerprint;
        var retryFingerprint = !string.IsNullOrWhiteSpace(retryExecutionPolicy.RetryFingerprint)
            ? retryExecutionPolicy.RetryFingerprint
            : commandJournal.RetryFingerprint;
        var latestAutomaticExecution = commandExecutionHistoryJournal.Entries.FirstOrDefault(static entry => entry.IsAutomaticRetryInvocation);
        var latestMatchingAutomaticExecution = commandExecutionHistoryJournal.Entries.FirstOrDefault(entry =>
            entry.IsAutomaticRetryInvocation &&
            (string.Equals(entry.RequestedOperationId, operationId, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(entry.ResolvedOperationId, operationId, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(commandFingerprint) ||
             string.Equals(entry.CommandFingerprint, commandFingerprint, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(expectedIssuanceFingerprint) ||
             string.Equals(entry.IssuanceFingerprint, expectedIssuanceFingerprint, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(expectedAdapterFingerprint) ||
             string.Equals(entry.AdapterFingerprint, expectedAdapterFingerprint, StringComparison.OrdinalIgnoreCase)));

        var state =
            !string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase) ||
            executionIntent.IsDeferred ||
            !retryExecutionPolicy.AppliesToManagedConnector
                ? CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.NotApplicable
                : latestMatchingAutomaticExecution is not null
                    ? CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.Completed
                    : !automaticRetryEnabled || retryExecutionPolicy.IsBackgroundRetryDisabled
                        ? CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.Disabled
                        : retryExecutionPolicy.CanExecuteRetryThroughPolicy
                            ? CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.Eligible
                            : CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.Blocked;

        var categories = CreateManagedConnectorAutomaticRetryExecutionCategories(
            executionTopology,
            retryExecutionPolicy,
            commandJournal,
            latestAutomaticExecution,
            latestMatchingAutomaticExecution,
            automaticRetryEnabled);
        var sourceId =
            latestMatchingAutomaticExecution is not null || latestAutomaticExecution is not null
                ? CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionSources.AutomaticRetryHistory
                : retryExecutionPolicy.AppliesToManagedConnector
                    ? CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionSources.RetryExecutionPolicy
                    : commandJournal.AppliesToManagedConnector
                        ? CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionSources.CommandJournal
                        : CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionSources.Unknown;
        var description = state switch
        {
            CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.NotApplicable =>
                AppendManagedConnectorCommandEnvelopeDetail(
                    "Cephalon does not currently expose automatic background retry execution while the shared runtime remains observe-only.",
                    CombineManagedConnectorCommandEnvelopeDetail(retryExecutionPolicy.Description, commandJournal.Description)),
            CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.Disabled =>
                !automaticRetryEnabled
                    ? AppendManagedConnectorCommandEnvelopeDetail(
                        $"Cephalon identified shared managed-connector retry posture for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)}, but automatic background retry is disabled by the current data runtime options.",
                        CombineManagedConnectorCommandEnvelopeDetail(retryExecutionPolicy.Description, commandJournal.Description))
                    : AppendManagedConnectorCommandEnvelopeDetail(
                        $"Cephalon identified a safe {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} retry candidate, but automatic background retry remains disabled in the current shared retry-execution policy baseline.",
                        CombineManagedConnectorCommandEnvelopeDetail(retryExecutionPolicy.Description, commandJournal.Description)),
            CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.Eligible =>
                AppendManagedConnectorCommandEnvelopeDetail(
                    $"Cephalon can now run one automatic background retry for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} because the shared retry-execution policy is retry-ready and the bounded journal does not yet contain a matching automatic attempt.",
                    CombineManagedConnectorCommandEnvelopeDetail(retryExecutionPolicy.Description, commandJournal.Description)),
            CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.Completed =>
                CreateManagedConnectorCompletedAutomaticRetryExecutionDescription(
                    operationId,
                    latestMatchingAutomaticExecution ?? latestAutomaticExecution,
                    retryExecutionPolicy.Description,
                    commandJournal.Description),
            _ => CreateManagedConnectorBlockedAutomaticRetryExecutionDescription(
                operationId,
                retryExecutionPolicy,
                commandJournal)
        };

        return new CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStatus(state, description)
        {
            CategoryIds = categories,
            OperationId = string.IsNullOrWhiteSpace(operationId)
                ? CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionOperationIds.None
                : operationId,
            ManagementMode = retryExecutionPolicy.ManagementMode ?? commandJournal.ManagementMode ?? commandExecution.ManagementMode,
            ReportingCoverageState = reportingCoverage.State,
            RemediationState = remediation.State,
            GovernanceState = governance.State,
            DriftState = drift.State,
            ActionPlanState = actionPlan.State,
            WritePathReadinessState = writePathReadiness.State,
            PreflightState = preflight.State,
            DryRunState = dryRun.State,
            ExecutionIntentState = executionIntent.State,
            ExecutionApprovalState = executionApproval.State,
            CommandEnvelopeState = commandEnvelope.State,
            CommandIssuanceState = commandIssuance.State,
            ExecutionAdapterState = executionAdapter.State,
            LatestCommandExecutionState = commandExecution.State,
            CommandRetryState = commandRetry.State,
            RetryExecutionPolicyState = retryExecutionPolicy.State,
            CommandJournalState = commandJournal.State,
            PrimaryActionId = actionPlan.PrimaryActionId,
            SourceId = sourceId,
            RetryExecutionPolicySourceId = retryExecutionPolicy.SourceId,
            CommandJournalSourceId = commandJournal.SourceId,
            LatestCommandExecutionInvocationSourceId = commandExecution.InvocationSourceId,
            ExecutionRuntimeId = executionRuntimeId,
            CdcCaptureIds = cdcCaptureIds,
            ConnectClusterId = retryExecutionPolicy.ConnectClusterId ?? commandJournal.ConnectClusterId ?? commandExecution.ConnectClusterId,
            ConnectorClass = retryExecutionPolicy.ConnectorClass ?? commandJournal.ConnectorClass ?? commandExecution.ConnectorClass,
            SourceProviderId = retryExecutionPolicy.SourceProviderId ?? commandJournal.SourceProviderId ?? commandExecution.SourceProviderId,
            PotentialChangeCount = Math.Max(retryExecutionPolicy.PotentialChangeCount, commandJournal.PotentialChangeCount),
            WouldApplyChanges = retryExecutionPolicy.WouldApplyChanges || commandJournal.WouldApplyChanges || commandExecution.WouldApplyChanges,
            RequiresExplicitApproval = retryExecutionPolicy.RequiresExplicitApproval || commandJournal.RequiresExplicitApproval || commandExecution.RequiresExplicitApproval,
            LatestMatchingApprovalApplied = retryExecutionPolicy.LatestMatchingApprovalApplied,
            IsDestructiveOperation = retryExecutionPolicy.IsDestructiveOperation || commandJournal.IsDestructiveOperation || commandExecution.IsDestructiveOperation,
            LatestMatchingDestructiveAllowanceApplied = retryExecutionPolicy.LatestMatchingDestructiveAllowanceApplied,
            CanReuseApprovalFromMatchingHistory = retryExecutionPolicy.CanReuseApprovalFromMatchingHistory,
            CanReuseDestructiveAllowanceFromMatchingHistory = retryExecutionPolicy.CanReuseDestructiveAllowanceFromMatchingHistory,
            IsAutomaticRetryEnabled = automaticRetryEnabled,
            CommandFingerprint = commandFingerprint,
            RetryFingerprint = retryFingerprint,
            LatestExecutionFingerprint = latestMatchingAutomaticExecution?.ExecutionFingerprint ?? latestAutomaticExecution?.ExecutionFingerprint ?? commandExecution.ExecutionFingerprint,
            LatestAttemptId = latestMatchingAutomaticExecution?.AttemptId ?? latestAutomaticExecution?.AttemptId ?? string.Empty,
            LatestRecordedAtUtc = latestMatchingAutomaticExecution?.RecordedAtUtc ?? latestAutomaticExecution?.RecordedAtUtc,
            CooldownUntilUtc = retryExecutionPolicy.CooldownUntilUtc ?? commandJournal.CooldownUntilUtc,
            HasAutomaticRetryAttempt = latestAutomaticExecution is not null,
            HasMatchingAutomaticRetryAttempt = latestMatchingAutomaticExecution is not null,
            LatestAutomaticRetryState = latestAutomaticExecution?.State ?? CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded,
            LatestAutomaticRetryAttemptId = latestAutomaticExecution?.AttemptId ?? string.Empty,
            LatestAutomaticRetryRecordedAtUtc = latestAutomaticExecution?.RecordedAtUtc,
            LatestAutomaticRetryExecutionFingerprint = latestAutomaticExecution?.ExecutionFingerprint ?? string.Empty
        };
    }

    private static string[] CreateManagedConnectorAutomaticRetryExecutionCategories(
        string executionTopology,
        CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus retryExecutionPolicy,
        CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus commandJournal,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult? latestAutomaticExecution,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult? latestMatchingAutomaticExecution,
        bool automaticRetryEnabled)
    {
        ArgumentNullException.ThrowIfNull(retryExecutionPolicy);
        ArgumentNullException.ThrowIfNull(commandJournal);

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var categories = new List<string>();

        static void AddCategory(List<string> values, string category)
        {
            if (!values.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(category);
            }
        }

        if (commandJournal.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.ObserveOnlyMode,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.ObserveOnlyMode);
        }

        if (!automaticRetryEnabled)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.FeatureDisabled);
        }

        if (retryExecutionPolicy.IsBackgroundRetryDisabled)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.PolicyDisabled);
        }

        if (retryExecutionPolicy.CanExecuteRetryThroughPolicy)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.RetryReady);
        }

        if (retryExecutionPolicy.IsCooldown || string.Equals(commandJournal.State, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.CooldownActive, StringComparison.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.CooldownActive);
        }

        if (retryExecutionPolicy.RequiresManualApproval)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.ManualApprovalRequired);
        }

        if (retryExecutionPolicy.IsPolicyBlocked)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.PolicyBlocked);
        }

        if (retryExecutionPolicy.IsOperatorOnly)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.OperatorOnly);
        }

        if (retryExecutionPolicy.IsNotNeeded)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.NoExecutionNeeded);
        }

        if (latestAutomaticExecution is not null)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.AutomaticAttemptRecorded);
        }

        if (latestMatchingAutomaticExecution is not null)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.MatchingAutomaticAttempt);
        }

        switch (latestMatchingAutomaticExecution?.State)
        {
            case CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NoOp:
                AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.LatestExecutionNoOp);
                break;
            case CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted:
                AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.LatestExecutionAdapted);
                break;
            case CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked:
                AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.LatestExecutionBlocked);
                break;
            case CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unavailable:
                AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.LatestExecutionUnavailable);
                break;
            case CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Failed:
                AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.LatestExecutionFailed);
                break;
        }

        return [.. categories];
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStatus CreateManagedConnectorAutomaticRetryCoordination(
        string executionRuntimeId,
        IReadOnlyList<string> cdcCaptureIds,
        string executionOwnership,
        string executionTopology,
        string? managementMode,
        CdcCaptureExecutionRuntimeSummary summary,
        CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStatus automaticRetryExecution,
        CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus retryExecutionPolicy,
        CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus commandJournal,
        string? coordinationOwnerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);
        ArgumentNullException.ThrowIfNull(cdcCaptureIds);
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(automaticRetryExecution);
        ArgumentNullException.ThrowIfNull(retryExecutionPolicy);
        ArgumentNullException.ThrowIfNull(commandJournal);

        var normalizedExecutionOwnership = string.IsNullOrWhiteSpace(executionOwnership)
            ? "runtime-managed"
            : executionOwnership.Trim();
        var normalizedExecutionTopology = string.IsNullOrWhiteSpace(executionTopology)
            ? "not-configured"
            : executionTopology.Trim();
        var normalizedCoordinationOwnerId = string.IsNullOrWhiteSpace(coordinationOwnerId)
            ? null
            : coordinationOwnerId.Trim();
        var reporterCoordination = summary.ReporterCoordination;
        var hasCoordinationOwner = !string.IsNullOrWhiteSpace(normalizedCoordinationOwnerId);
        var hasActiveReporterLease =
            !string.IsNullOrWhiteSpace(summary.ActiveReporterId) &&
            summary.ReporterLeaseExpiresAtUtc.HasValue;
        var usesReporterLeaseCoordination =
            string.Equals(normalizedExecutionOwnership, "external-managed", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                reporterCoordination.State,
                CdcCaptureReporterCoordinationStates.NotConfigured,
                StringComparison.OrdinalIgnoreCase);

        var state =
            !string.Equals(normalizedExecutionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase) ||
            !automaticRetryExecution.AppliesToManagedConnector
                ? CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.NotApplicable
                : retryExecutionPolicy.IsOperatorOnly
                    ? CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.OperatorOnly
                    : string.Equals(
                            reporterCoordination.State,
                            CdcCaptureReporterCoordinationStates.Conflicted,
                            StringComparison.OrdinalIgnoreCase) ||
                      reporterCoordination.HasMultipleActiveReporters ||
                      string.Equals(
                            reporterCoordination.DegradedReason,
                            CdcCaptureReporterCoordinationIssueReasons.MultipleActiveReporters,
                            StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(
                            reporterCoordination.DegradedReason,
                            CdcCaptureReporterCoordinationIssueReasons.RejectedReporterConflict,
                            StringComparison.OrdinalIgnoreCase)
                        ? CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.Conflicted
                        : usesReporterLeaseCoordination
                            ? !hasActiveReporterLease ||
                              string.Equals(
                                  reporterCoordination.State,
                                  CdcCaptureReporterCoordinationStates.LeaseExpired,
                                  StringComparison.OrdinalIgnoreCase) ||
                              reporterCoordination.RequiresTakeover
                                ? CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.LeaseMissing
                                : !hasCoordinationOwner
                                    ? CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.Uncoordinated
                                    : string.Equals(
                                            normalizedCoordinationOwnerId,
                                            summary.ActiveReporterId,
                                            StringComparison.OrdinalIgnoreCase)
                                        ? CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.LeaseHeld
                                        : CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.Uncoordinated
                            : CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.SingleNode;

        var categories = CreateManagedConnectorAutomaticRetryCoordinationCategories(
            state,
            usesReporterLeaseCoordination,
            hasCoordinationOwner,
            hasActiveReporterLease,
            normalizedCoordinationOwnerId,
            summary.ActiveReporterId,
            retryExecutionPolicy);
        var description = CreateManagedConnectorAutomaticRetryCoordinationDescription(
            state,
            normalizedCoordinationOwnerId,
            summary.ActiveReporterId,
            reporterCoordination,
            automaticRetryExecution,
            retryExecutionPolicy);
        var sourceId = ResolveManagedConnectorAutomaticRetryCoordinationSourceId(
            state,
            hasCoordinationOwner);

        return new CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStatus(state, description)
        {
            CategoryIds = categories,
            ExecutionRuntimeId = executionRuntimeId,
            CdcCaptureIds = cdcCaptureIds,
            ExecutionOwnership = normalizedExecutionOwnership,
            ExecutionTopology = normalizedExecutionTopology,
            ManagementMode = managementMode,
            CoordinationOwnerId = automaticRetryExecution.AppliesToManagedConnector ? normalizedCoordinationOwnerId : null,
            ActiveReporterId = summary.ActiveReporterId,
            ActiveReporterLeaseExpiresAtUtc = summary.ReporterLeaseExpiresAtUtc,
            ReporterCoordinationState = reporterCoordination.State,
            ReporterCoordinationIssueReason = reporterCoordination.DegradedReason,
            ReporterTakeoverState = reporterCoordination.TakeoverState,
            AutomaticRetryExecutionState = automaticRetryExecution.State,
            RetryExecutionPolicyState = retryExecutionPolicy.State,
            CommandJournalState = commandJournal.State,
            SourceId = sourceId,
            ObservedEdgeNodeIds = summary.ObservedEdgeNodeIds
        };
    }

    private static string[] CreateManagedConnectorAutomaticRetryCoordinationCategories(
        string state,
        bool usesReporterLeaseCoordination,
        bool hasCoordinationOwner,
        bool hasActiveReporterLease,
        string? coordinationOwnerId,
        string? activeReporterId,
        CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus retryExecutionPolicy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentNullException.ThrowIfNull(retryExecutionPolicy);

        if (string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.NotApplicable, StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var categories = new List<string>();

        static void AddCategory(List<string> values, string category)
        {
            if (!values.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(category);
            }
        }

        AddCategory(
            categories,
            usesReporterLeaseCoordination
                ? CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationCategories.LeaseCoordinatedRuntime
                : CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationCategories.SingleNodeRuntime);

        AddCategory(
            categories,
            hasCoordinationOwner
                ? CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationCategories.CoordinationOwnerConfigured
                : CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationCategories.CoordinationOwnerMissing);

        if (retryExecutionPolicy.IsOperatorOnly)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationCategories.OperatorOnly);
        }

        if (hasActiveReporterLease)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationCategories.ActiveReporterVisible);
        }
        else if (usesReporterLeaseCoordination)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationCategories.LeaseMissing);
        }

        if (string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.Conflicted, StringComparison.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationCategories.ReporterConflict);
        }

        if (!string.IsNullOrWhiteSpace(coordinationOwnerId) &&
            !string.IsNullOrWhiteSpace(activeReporterId))
        {
            AddCategory(
                categories,
                string.Equals(coordinationOwnerId, activeReporterId, StringComparison.OrdinalIgnoreCase)
                    ? CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationCategories.OwnerMatch
                    : CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationCategories.OwnerMismatch);
        }

        if (string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.LeaseHeld, StringComparison.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationCategories.ActiveLeaseHeld);
        }

        return [.. categories];
    }

    private static string CreateManagedConnectorAutomaticRetryCoordinationDescription(
        string state,
        string? coordinationOwnerId,
        string? activeReporterId,
        CdcCaptureReporterCoordinationStatus reporterCoordination,
        CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStatus automaticRetryExecution,
        CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus retryExecutionPolicy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentNullException.ThrowIfNull(reporterCoordination);
        ArgumentNullException.ThrowIfNull(automaticRetryExecution);
        ArgumentNullException.ThrowIfNull(retryExecutionPolicy);

        var detail = string.Equals(
            state,
            CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.OperatorOnly,
            StringComparison.OrdinalIgnoreCase)
            ? CombineManagedConnectorCommandEnvelopeDetail(retryExecutionPolicy.Description, automaticRetryExecution.Description)
            : CombineManagedConnectorCommandEnvelopeDetail(reporterCoordination.Description, automaticRetryExecution.Description);

        return state switch
        {
            CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.NotApplicable =>
                AppendManagedConnectorCommandEnvelopeDetail(
                    "Cephalon does not currently require automatic background retry coordination for this execution runtime.",
                    detail),
            CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.SingleNode =>
                AppendManagedConnectorCommandEnvelopeDetail(
                    "Cephalon can evaluate automatic background retry on the current node without reporter-lease coordination.",
                    detail),
            CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.LeaseHeld =>
                AppendManagedConnectorCommandEnvelopeDetail(
                    $"Reporter '{NormalizeManagedConnectorFingerprintSegment(activeReporterId)}' currently holds the active reporter lease for this execution runtime, so the current node can evaluate automatic background retry safely.",
                    detail),
            CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.LeaseMissing =>
                AppendManagedConnectorCommandEnvelopeDetail(
                    "Cephalon cannot safely run automatic background retry on the current node because no active reporter lease is currently visible for the execution runtime.",
                    detail),
            CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.Conflicted =>
                AppendManagedConnectorCommandEnvelopeDetail(
                    "Cephalon cannot safely run automatic background retry while reporter coordination remains conflicted for the execution runtime.",
                    detail),
            CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.OperatorOnly =>
                AppendManagedConnectorCommandEnvelopeDetail(
                    "Cephalon can observe automatic retry posture for this managed connector, but control-plane ownership still remains operator-owned outside Cephalon.",
                    detail),
            _ => string.IsNullOrWhiteSpace(coordinationOwnerId)
                ? AppendManagedConnectorCommandEnvelopeDetail(
                    "Cephalon identified automatic retry posture for this managed connector, but the current host did not declare a local coordination owner id, so background retry remains uncoordinated on this node.",
                    detail)
                : AppendManagedConnectorCommandEnvelopeDetail(
                    $"Reporter '{NormalizeManagedConnectorFingerprintSegment(activeReporterId)}' currently holds the active reporter lease, but the current host declares coordination owner '{NormalizeManagedConnectorFingerprintSegment(coordinationOwnerId)}', so automatic background retry remains uncoordinated on this node.",
                    detail)
        };
    }

    private static string ResolveManagedConnectorAutomaticRetryCoordinationSourceId(
        string state,
        bool hasCoordinationOwner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);

        if (string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.NotApplicable, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.OperatorOnly, StringComparison.OrdinalIgnoreCase))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationSources.AutomaticRetryExecution;
        }

        if (string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.SingleNode, StringComparison.OrdinalIgnoreCase))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationSources.ExecutionOwnership;
        }

        if (string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.Conflicted, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.LeaseMissing, StringComparison.OrdinalIgnoreCase))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationSources.ReporterCoordination;
        }

        if (string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.LeaseHeld, StringComparison.OrdinalIgnoreCase) ||
            (string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.Uncoordinated, StringComparison.OrdinalIgnoreCase) && hasCoordinationOwner))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationSources.CoordinationOwner;
        }

        return CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationSources.Unknown;
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStatus CreateManagedConnectorCommandJournalDurability(
        string executionRuntimeId,
        IReadOnlyList<string> cdcCaptureIds,
        string executionTopology,
        string? managementMode,
        CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus commandJournal,
        CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStatus automaticRetryExecution,
        CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStatus automaticRetryCoordination,
        ManagedConnectorCommandExecutionJournalDurabilitySnapshot durabilitySnapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);
        ArgumentNullException.ThrowIfNull(cdcCaptureIds);
        ArgumentNullException.ThrowIfNull(commandJournal);
        ArgumentNullException.ThrowIfNull(automaticRetryExecution);
        ArgumentNullException.ThrowIfNull(automaticRetryCoordination);

        var normalizedExecutionTopology = string.IsNullOrWhiteSpace(executionTopology)
            ? "not-configured"
            : executionTopology.Trim();
        var state = ResolveManagedConnectorCommandJournalDurabilityState(
            normalizedExecutionTopology,
            commandJournal,
            durabilitySnapshot);
        var categories = CreateManagedConnectorCommandJournalDurabilityCategories(
            state,
            commandJournal,
            automaticRetryExecution,
            automaticRetryCoordination,
            durabilitySnapshot);
        var description = CreateManagedConnectorCommandJournalDurabilityDescription(
            state,
            commandJournal,
            automaticRetryExecution,
            automaticRetryCoordination,
            durabilitySnapshot);
        var sourceId = ResolveManagedConnectorCommandJournalDurabilitySourceId(
            state,
            durabilitySnapshot);

        return new CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStatus(state, description)
        {
            CategoryIds = categories,
            ExecutionRuntimeId = executionRuntimeId,
            CdcCaptureIds = cdcCaptureIds,
            ExecutionTopology = normalizedExecutionTopology,
            ManagementMode = managementMode,
            CommandJournalState = commandJournal.State,
            AutomaticRetryExecutionState = automaticRetryExecution.State,
            AutomaticRetryCoordinationState = automaticRetryCoordination.State,
            SourceId = sourceId,
            PersistencePath = durabilitySnapshot.PersistencePath,
            LastRecoveredAtUtc = durabilitySnapshot.LastRecoveredAtUtc,
            LastPersistedAtUtc = durabilitySnapshot.LastPersistedAtUtc,
            LastRecoveryError = durabilitySnapshot.LastRecoveryError,
            LastPersistenceError = durabilitySnapshot.LastPersistenceError,
            TotalRecordedEntryCount = commandJournal.TotalRecordedEntryCount,
            RetainedEntryCount = commandJournal.RetainedEntryCount,
            MaximumRetainedEntryCount = commandJournal.MaximumRetainedEntryCount,
            HasDurableStoreConfigured = durabilitySnapshot.HasDurableStoreConfigured,
            HasPersistedSnapshot = durabilitySnapshot.HasPersistedSnapshot,
            HasPersistedRecordedHistory = durabilitySnapshot.HasPersistedRecordedHistory,
            HasRecoveredPersistedHistory = durabilitySnapshot.HasRecoveredPersistedHistory
        };
    }

    private static string ResolveManagedConnectorCommandJournalDurabilityState(
        string executionTopology,
        CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus commandJournal,
        ManagedConnectorCommandExecutionJournalDurabilitySnapshot durabilitySnapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionTopology);
        ArgumentNullException.ThrowIfNull(commandJournal);

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase) ||
            !commandJournal.AppliesToManagedConnector)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.NotApplicable;
        }

        if (durabilitySnapshot.HasRecoveryError)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.RecoveryFailed;
        }

        if (durabilitySnapshot.HasPersistenceError)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.PersistenceFailed;
        }

        if (!durabilitySnapshot.HasDurableStoreConfigured)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.InMemoryOnly;
        }

        return durabilitySnapshot.HasRecoveredPersistedHistory
            ? CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.Recovered
            : CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.Persisted;
    }

    private static string[] CreateManagedConnectorCommandJournalDurabilityCategories(
        string state,
        CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus commandJournal,
        CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStatus automaticRetryExecution,
        CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStatus automaticRetryCoordination,
        ManagedConnectorCommandExecutionJournalDurabilitySnapshot durabilitySnapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentNullException.ThrowIfNull(commandJournal);
        ArgumentNullException.ThrowIfNull(automaticRetryExecution);
        ArgumentNullException.ThrowIfNull(automaticRetryCoordination);

        if (string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.NotApplicable, StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var categories = new List<string>();

        static void AddCategory(List<string> values, string category)
        {
            if (!values.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(category);
            }
        }

        if (durabilitySnapshot.HasDurableStoreConfigured)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories.DurableStoreConfigured);
        }
        else
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories.InMemoryOnly);
        }

        if (durabilitySnapshot.HasPersistedSnapshot)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories.PersistedSnapshotAvailable);
        }

        if (durabilitySnapshot.HasPersistedRecordedHistory)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories.PersistedRecordedHistory);
        }

        if (durabilitySnapshot.HasRecoveredPersistedHistory)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories.RecoveredHistory);
        }

        if (durabilitySnapshot.HasRecoveryError)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories.RecoveryError);
        }

        if (durabilitySnapshot.HasPersistenceError)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories.PersistenceError);
        }

        if (durabilitySnapshot.HasDurableStoreConfigured &&
            !durabilitySnapshot.HasRecoveryError &&
            !durabilitySnapshot.HasPersistenceError)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories.PersistenceHealthy);
        }

        if (commandJournal.HasRecordedCommandHistory)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories.RecordedCommandHistory);
        }

        if (commandJournal.HasTruncatedHistory)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories.TruncatedHistory);
        }

        if (automaticRetryExecution.IsEligible)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories.AutomaticRetryEligible);
        }

        if (automaticRetryCoordination.CanExecuteOnCurrentNode)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories.CoordinationReady);
        }

        return [.. categories];
    }

    private static string CreateManagedConnectorCommandJournalDurabilityDescription(
        string state,
        CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus commandJournal,
        CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStatus automaticRetryExecution,
        CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStatus automaticRetryCoordination,
        ManagedConnectorCommandExecutionJournalDurabilitySnapshot durabilitySnapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentNullException.ThrowIfNull(commandJournal);
        ArgumentNullException.ThrowIfNull(automaticRetryExecution);
        ArgumentNullException.ThrowIfNull(automaticRetryCoordination);

        var detail = CombineManagedConnectorCommandEnvelopeDetail(
            commandJournal.Description,
            automaticRetryCoordination.CanExecuteOnCurrentNode
                ? automaticRetryCoordination.Description
                : automaticRetryExecution.Description);

        return state switch
        {
            CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.NotApplicable =>
                AppendManagedConnectorCommandEnvelopeDetail(
                    "Cephalon does not currently expose durable managed-connector command-journal posture for this execution runtime.",
                    detail),
            CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.InMemoryOnly =>
                AppendManagedConnectorCommandEnvelopeDetail(
                    "Cephalon currently keeps the bounded managed-connector command journal in memory only, so retry evidence would not survive process restart on this host.",
                    detail),
            CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.Recovered =>
                AppendManagedConnectorCommandEnvelopeDetail(
                    "Cephalon recovered the bounded managed-connector command journal from the configured durable store after startup, so retained retry evidence now survives host restart.",
                    detail),
            CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.RecoveryFailed =>
                AppendManagedConnectorCommandEnvelopeDetail(
                    $"Cephalon could not recover the configured durable managed-connector command journal after startup: {NormalizeManagedConnectorFingerprintSegment(durabilitySnapshot.LastRecoveryError)}.",
                    detail),
            CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.PersistenceFailed =>
                AppendManagedConnectorCommandEnvelopeDetail(
                    $"Cephalon could not persist the latest bounded managed-connector command journal snapshot: {NormalizeManagedConnectorFingerprintSegment(durabilitySnapshot.LastPersistenceError)}.",
                    detail),
            _ => durabilitySnapshot.HasPersistedRecordedHistory
                ? AppendManagedConnectorCommandEnvelopeDetail(
                    "Cephalon now persists bounded managed-connector command-journal history for this execution runtime, so retained retry evidence survives process restart on the current host.",
                    detail)
                : AppendManagedConnectorCommandEnvelopeDetail(
                    "Cephalon configured a durable managed-connector command-journal store for this execution runtime and is ready to persist retained retry evidence as soon as command history is recorded.",
                    detail)
        };
    }

    private static string ResolveManagedConnectorCommandJournalDurabilitySourceId(
        string state,
        ManagedConnectorCommandExecutionJournalDurabilitySnapshot durabilitySnapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);

        if (string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.NotApplicable, StringComparison.OrdinalIgnoreCase))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilitySources.Unknown;
        }

        if (string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.InMemoryOnly, StringComparison.OrdinalIgnoreCase))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilitySources.InMemoryHistoryStore;
        }

        if (string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.RecoveryFailed, StringComparison.OrdinalIgnoreCase))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilitySources.RecoveryError;
        }

        if (string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.PersistenceFailed, StringComparison.OrdinalIgnoreCase))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilitySources.PersistenceError;
        }

        return durabilitySnapshot.HasRecoveredPersistedHistory
            ? CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilitySources.RecoveredDurableJournalStore
            : CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilitySources.DurableJournalStore;
    }

    private static string CreateManagedConnectorBlockedAutomaticRetryExecutionDescription(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus retryExecutionPolicy,
        CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus commandJournal)
    {
        ArgumentNullException.ThrowIfNull(retryExecutionPolicy);
        ArgumentNullException.ThrowIfNull(commandJournal);

        string summary;
        if (retryExecutionPolicy.IsCooldown)
        {
            summary = retryExecutionPolicy.CooldownUntilUtc.HasValue
                ? $"Cephalon is holding automatic background retry for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} until the cooldown window ends at '{retryExecutionPolicy.CooldownUntilUtc.Value:O}'."
                : $"Cephalon is holding automatic background retry for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} while the current cooldown window remains active.";
        }
        else if (retryExecutionPolicy.RequiresManualApproval)
        {
            summary = $"Cephalon can evaluate automatic background retry for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)}, but manual approval must clear before it should continue.";
        }
        else if (retryExecutionPolicy.IsOperatorOnly)
        {
            summary = $"Cephalon can observe retry posture for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)}, but automatic background retry remains operator-owned until a later managed control-plane slice ships.";
        }
        else if (retryExecutionPolicy.IsNotNeeded)
        {
            summary = $"Cephalon does not currently need an automatic background retry for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} because the shared runtime truth already converged.";
        }
        else
        {
            summary = $"Cephalon is not currently able to continue automatic background retry for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} because the shared runtime truth still blocks it.";
        }

        return AppendManagedConnectorCommandEnvelopeDetail(
            summary,
            CombineManagedConnectorCommandEnvelopeDetail(retryExecutionPolicy.Description, commandJournal.Description));
    }

    private static string CreateManagedConnectorCompletedAutomaticRetryExecutionDescription(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult? latestAutomaticExecution,
        string? retryExecutionPolicyDescription,
        string? commandJournalDescription)
    {
        string summary;
        if (latestAutomaticExecution is null)
        {
            summary = $"Cephalon already recorded one automatic background retry attempt for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} on the shared command lane.";
        }
        else
        {
            var attemptId = NormalizeManagedConnectorFingerprintSegment(latestAutomaticExecution.AttemptId);
            summary = latestAutomaticExecution.State switch
            {
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NoOp =>
                    $"Cephalon already ran one automatic background retry for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} and recorded attempt '{attemptId}' as a no-op because no provider command is needed right now.",
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted =>
                    $"Cephalon already ran one automatic background retry for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} and translated provider command shape during attempt '{attemptId}'.",
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked =>
                    $"Cephalon already ran one automatic background retry for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} but attempt '{attemptId}' remained blocked by the current provider translation truth.",
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unavailable =>
                    $"Cephalon already ran one automatic background retry for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} but attempt '{attemptId}' could not resolve a provider execution adapter.",
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Failed =>
                    $"Cephalon already ran one automatic background retry for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} but attempt '{attemptId}' failed while translating the provider command.",
                _ =>
                    $"Cephalon already recorded one automatic background retry attempt '{attemptId}' for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} on the shared command lane."
            };
        }

        return AppendManagedConnectorCommandEnvelopeDetail(
            summary,
            CombineManagedConnectorCommandEnvelopeDetail(latestAutomaticExecution?.Description, retryExecutionPolicyDescription, commandJournalDescription));
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus CreateManagedConnectorExecutionAdapterStatus(
        string state,
        string description,
        IReadOnlyList<string> categoryIds,
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus commandEnvelope,
        CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus commandIssuance,
        string sourceId,
        string adapterId,
        string executionRuntimeId,
        IReadOnlyList<string> cdcCaptureIds,
        ManagedConnectorMetadataSnapshot metadataSnapshot)
    {
        var normalizedOperationId = string.IsNullOrWhiteSpace(operationId)
            ? CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None
            : operationId.Trim();
        var normalizedAdapterId = string.IsNullOrWhiteSpace(adapterId)
            ? CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.None
            : adapterId.Trim();
        var connectClusterId = commandIssuance.ConnectClusterId ?? ResolveManagedConnectorCommandTargetValue(
            metadataSnapshot.DeclaredConnectClusterId,
            metadataSnapshot.ReportedConnectClusterId);
        var connectorClass = commandIssuance.ConnectorClass ?? ResolveManagedConnectorCommandTargetValue(
            metadataSnapshot.DeclaredConnectorClass,
            metadataSnapshot.ReportedConnectorClass);
        var sourceProviderId = commandIssuance.SourceProviderId ?? ResolveManagedConnectorCommandTargetValue(
            metadataSnapshot.DeclaredSourceProviderId,
            metadataSnapshot.ReportedSourceProviderId);

        return new CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus(state, description)
        {
            CategoryIds = categoryIds,
            OperationId = normalizedOperationId,
            ManagementMode = governance.ManagementMode,
            ReportingCoverageState = reportingCoverage.State,
            RemediationState = remediation.State,
            GovernanceState = governance.State,
            DriftState = drift.State,
            ActionPlanState = actionPlan.State,
            WritePathReadinessState = writePathReadiness.State,
            PreflightState = preflight.State,
            DryRunState = dryRun.State,
            ExecutionIntentState = executionIntent.State,
            ExecutionApprovalState = executionApproval.State,
            CommandEnvelopeState = commandEnvelope.State,
            CommandIssuanceState = commandIssuance.State,
            PrimaryActionId = actionPlan.PrimaryActionId,
            SourceId = sourceId,
            CommandIssuanceSourceId = commandIssuance.SourceId,
            CommandEnvelopeSourceId = commandEnvelope.SourceId,
            ExecutionIntentConfidenceSourceId = executionIntent.ConfidenceSourceId,
            ExecutionApprovalSourceId = executionApproval.SourceId,
            AdapterId = normalizedAdapterId,
            PotentialChangeCount = commandIssuance.PotentialChangeCount,
            WouldApplyChanges = commandIssuance.WouldApplyChanges,
            RequiresExplicitApproval = commandIssuance.RequiresExplicitApproval,
            IsDestructiveOperation = commandIssuance.IsDestructiveOperation,
            ExecutionRuntimeId = executionRuntimeId,
            CdcCaptureIds = cdcCaptureIds,
            ConnectClusterId = connectClusterId,
            ConnectorClass = connectorClass,
            SourceProviderId = sourceProviderId,
            CommandFingerprint = commandIssuance.CommandFingerprint,
            IssuanceFingerprint = commandIssuance.IssuanceFingerprint,
            AdapterFingerprint = CreateManagedConnectorExecutionAdapterFingerprint(
                state,
                normalizedOperationId,
                sourceId,
                normalizedAdapterId,
                commandIssuance.State,
                commandEnvelope.State,
                executionApproval.State,
                executionIntent.State,
                commandIssuance.CommandFingerprint,
                commandIssuance.IssuanceFingerprint,
                commandIssuance.RequiresExplicitApproval,
                commandIssuance.WouldApplyChanges,
                commandIssuance.IsDestructiveOperation)
        };
    }

    private static string[] CreateManagedConnectorExecutionAdapterCategories(
        string executionTopology,
        string state,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus dryRun,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus commandEnvelope,
        CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus commandIssuance,
        string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(actionPlan);
        ArgumentNullException.ThrowIfNull(writePathReadiness);
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(dryRun);
        ArgumentNullException.ThrowIfNull(executionIntent);
        ArgumentNullException.ThrowIfNull(executionApproval);
        ArgumentNullException.ThrowIfNull(commandEnvelope);
        ArgumentNullException.ThrowIfNull(commandIssuance);

        if (!string.Equals(executionTopology, "managed-connector", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var categories = new List<string>(capacity: 18);
        static void AddCategory(List<string> values, string category)
        {
            if (!values.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(category);
            }
        }

        if (!commandIssuance.AppliesToManagedConnector || executionIntent.IsDeferred)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ObserveOnlyMode);
        }

        if (remediation.IsBlocked ||
            commandIssuance.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.BlockingRemediation,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.BlockingRemediation);
        }

        if (commandIssuance.CategoryIds.Contains(
            CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.StaleObservation,
            StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.StaleObservation);
        }

        if (!reportingCoverage.HasFullCoverage ||
            commandIssuance.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.IncompleteReportingCoverage,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.IncompleteReportingCoverage);
        }

        if (actionPlan.IsWaiting ||
            commandIssuance.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.RuntimeTruthIncomplete,
                StringComparer.OrdinalIgnoreCase) ||
            preflight.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.RuntimeTruthIncomplete,
                StringComparer.OrdinalIgnoreCase) ||
            writePathReadiness.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.RuntimeTruthIncomplete,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.RuntimeTruthIncomplete);
        }

        if (governance.IsOutOfPolicy)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.GovernanceOutOfPolicy);
        }

        if (commandIssuance.IsOperatorOnly)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ControlPlaneOwnershipGap);
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.OperatorOnly);
        }

        if (commandIssuance.WouldApplyChanges || dryRun.WouldApplyChanges)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ChangePlanned);
        }

        if (WouldManagedConnectorDryRunRequireLifecycleChange(operationId, commandIssuance.WouldApplyChanges || dryRun.WouldApplyChanges))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.LifecycleChange);
        }

        if (commandIssuance.IsDestructiveOperation)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.DestructiveOperation);
        }

        if (executionApproval.IsApprovalRequired)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ApprovalRequired);
        }
        else if (executionApproval.IsApprovalReady)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ApprovalReady);
        }

        if (commandIssuance.IsRejected ||
            commandIssuance.CategoryIds.Contains(
                CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories.NoExecutionNeeded,
                StringComparer.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.NoExecutionNeeded);
        }

        if (string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Ready, StringComparison.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.AdapterReady);
        }
        else if (string.Equals(state, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Unavailable, StringComparison.OrdinalIgnoreCase))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.AdapterUnavailable);
        }

        return [.. categories];
    }

    private static string ResolveManagedConnectorExecutionAdapterSource(
        CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus commandIssuance,
        CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus commandEnvelope,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus executionIntent)
    {
        ArgumentNullException.ThrowIfNull(commandIssuance);
        ArgumentNullException.ThrowIfNull(commandEnvelope);
        ArgumentNullException.ThrowIfNull(executionApproval);
        ArgumentNullException.ThrowIfNull(executionIntent);

        if (commandIssuance.AppliesToManagedConnector ||
            string.Equals(
                commandIssuance.State,
                CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.NotApplicable,
                StringComparison.OrdinalIgnoreCase))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.CommandIssuance;
        }

        if (commandEnvelope.AppliesToManagedConnector)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.CommandEnvelope;
        }

        if (executionApproval.AppliesToManagedConnector)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.ExecutionApproval;
        }

        if (executionIntent.AppliesToManagedConnector)
        {
            return CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.ExecutionIntent;
        }

        return CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.Unknown;
    }

    private static string ResolveManagedConnectorExecutionAdapterOperationId(
        string? commandIssuanceOperationId,
        string? commandEnvelopeOperationId,
        string? executionApprovalOperationId,
        string? executionIntentOperationId)
    {
        if (!string.IsNullOrWhiteSpace(commandIssuanceOperationId))
        {
            return commandIssuanceOperationId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(commandEnvelopeOperationId))
        {
            return commandEnvelopeOperationId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(executionApprovalOperationId))
        {
            return executionApprovalOperationId.Trim();
        }

        return string.IsNullOrWhiteSpace(executionIntentOperationId)
            ? CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None
            : executionIntentOperationId.Trim();
    }

    private static string CreateManagedConnectorExecutionAdapterFingerprint(
        string state,
        string operationId,
        string sourceId,
        string adapterId,
        string commandIssuanceState,
        string commandEnvelopeState,
        string executionApprovalState,
        string executionIntentState,
        string commandFingerprint,
        string issuanceFingerprint,
        bool requiresExplicitApproval,
        bool wouldApplyChanges,
        bool isDestructiveOperation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterId);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandIssuanceState);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandEnvelopeState);
        ArgumentException.ThrowIfNullOrWhiteSpace(executionApprovalState);
        ArgumentException.ThrowIfNullOrWhiteSpace(executionIntentState);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandFingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuanceFingerprint);

        return string.Join(
            "|",
            [
                "cephalon-managed-connector-execution-adapter/v1",
                $"state={state.Trim()}",
                $"operation={operationId.Trim()}",
                $"source={sourceId.Trim()}",
                $"adapter={adapterId.Trim()}",
                $"commandIssuance={commandIssuanceState.Trim()}",
                $"commandEnvelope={commandEnvelopeState.Trim()}",
                $"executionApproval={executionApprovalState.Trim()}",
                $"executionIntent={executionIntentState.Trim()}",
                $"commandFingerprint={commandFingerprint.Trim()}",
                $"issuanceFingerprint={issuanceFingerprint.Trim()}",
                $"requiresExplicitApproval={requiresExplicitApproval.ToString().ToLowerInvariant()}",
                $"wouldApplyChanges={wouldApplyChanges.ToString().ToLowerInvariant()}",
                $"destructive={isDestructiveOperation.ToString().ToLowerInvariant()}"
            ]);
    }

    private static string CreateManagedConnectorNotApplicableExecutionAdapterDescription(
        string? issuanceDescription,
        string? governanceDescription)
    {
        return AppendManagedConnectorCommandEnvelopeDetail(
            "Cephalon does not currently route provider execution adapters while the shared runtime remains observe-only.",
            string.IsNullOrWhiteSpace(issuanceDescription)
                ? governanceDescription
                : issuanceDescription);
    }

    private static string CreateManagedConnectorOperatorOnlyExecutionAdapterDescription(
        string operationId,
        string adapterId,
        string? issuanceDescription,
        string? executionApprovalDescription)
    {
        var detail = string.IsNullOrWhiteSpace(issuanceDescription)
            ? executionApprovalDescription
            : issuanceDescription;

        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon can identify provider execution adapter '{NormalizeManagedConnectorFingerprintSegment(adapterId)}', but {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} still remains operator-owned on the shared runtime surface.",
            detail);
    }

    private static string CreateManagedConnectorBlockedExecutionAdapterDescription(
        string operationId,
        string adapterId,
        string? issuanceDescription,
        string? executionApprovalDescription)
    {
        var detail = string.IsNullOrWhiteSpace(issuanceDescription)
            ? executionApprovalDescription
            : issuanceDescription;

        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon cannot yet trust provider execution adapter '{NormalizeManagedConnectorFingerprintSegment(adapterId)}' for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)}.",
            detail);
    }

    private static string CreateManagedConnectorUnavailableExecutionAdapterDescription(
        string operationId,
        string? issuanceDescription)
    {
        return AppendManagedConnectorCommandEnvelopeDetail(
            $"The managed connector is otherwise ready for {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)}, but no matching provider execution adapter is currently registered.",
            issuanceDescription);
    }

    private static string CreateManagedConnectorReadyExecutionAdapterDescription(
        string operationId,
        string adapterId,
        CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus commandIssuance)
    {
        ArgumentNullException.ThrowIfNull(commandIssuance);

        if (commandIssuance.IsRejected || !commandIssuance.WouldApplyChanges)
        {
            return AppendManagedConnectorCommandEnvelopeDetail(
                $"Cephalon can route {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} through provider execution adapter '{NormalizeManagedConnectorFingerprintSegment(adapterId)}', but the current shared runtime truth indicates no outbound provider command is needed.",
                commandIssuance.Description);
        }

        if (commandIssuance.IsAccepted)
        {
            return AppendManagedConnectorCommandEnvelopeDetail(
                $"Cephalon can route {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} through provider execution adapter '{NormalizeManagedConnectorFingerprintSegment(adapterId)}' once the required approval gates are satisfied.",
                commandIssuance.Description);
        }

        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon can route {CreateManagedConnectorExecutionAdapterOperationLabel(operationId)} through provider execution adapter '{NormalizeManagedConnectorFingerprintSegment(adapterId)}'. Provider completion remains later work, while shared command-execution outcome history is now available on this runtime surface.",
            commandIssuance.Description);
    }

    private static string CreateManagedConnectorUnrecordedCommandExecutionDescription(
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter)
    {
        ArgumentNullException.ThrowIfNull(executionAdapter);

        if (!executionAdapter.AppliesToManagedConnector)
        {
            return "The execution runtime does not currently participate in a managed-connector command-execution lane.";
        }

        if (executionAdapter.HasAdaptableCommand)
        {
            return AppendManagedConnectorCommandEnvelopeDetail(
                $"No managed-connector command-execution outcome has been recorded yet. Current execution-adapter posture is '{executionAdapter.State}' for {CreateManagedConnectorExecutionAdapterOperationLabel(executionAdapter.OperationId)}.",
                executionAdapter.Description);
        }

        return AppendManagedConnectorCommandEnvelopeDetail(
            $"No managed-connector command-execution outcome has been recorded yet. Current execution-adapter posture is '{executionAdapter.State}'.",
            executionAdapter.Description);
    }

    private static string CreateManagedConnectorExecutionAdapterOperationLabel(string operationId)
    {
        if (string.Equals(
            operationId,
            CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile,
            StringComparison.OrdinalIgnoreCase))
        {
            return "future reconcile follow-through";
        }

        if (string.Equals(
            operationId,
            CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Pause,
            StringComparison.OrdinalIgnoreCase))
        {
            return "future pause follow-through";
        }

        if (string.Equals(
            operationId,
            CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Resume,
            StringComparison.OrdinalIgnoreCase))
        {
            return "future resume follow-through";
        }

        if (string.Equals(
            operationId,
            CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Restart,
            StringComparison.OrdinalIgnoreCase))
        {
            return "future restart follow-through";
        }

        if (string.Equals(
            operationId,
            CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Delete,
            StringComparison.OrdinalIgnoreCase))
        {
            return "future delete follow-through";
        }

        return "future managed-connector follow-through";
    }

    private static string CreateManagedConnectorNotApplicableCommandIssuanceDescription(
        string? commandEnvelopeDescription,
        string? governanceDescription)
    {
        return AppendManagedConnectorCommandEnvelopeDetail(
            "Cephalon does not currently prepare a managed-connector command issuance while the shared runtime remains observe-only.",
            CombineManagedConnectorCommandEnvelopeDetail(commandEnvelopeDescription, governanceDescription));
    }

    private static string CreateManagedConnectorOperatorOnlyCommandIssuanceDescription(
        string operationId,
        string? commandEnvelopeDescription,
        string? executionApprovalDescription)
    {
        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon can describe command issuance for {CreateManagedConnectorCommandEnvelopeOperationLabel(operationId)}, but the write-path still remains operator-owned until a later managed control-plane slice ships.",
            CombineManagedConnectorCommandEnvelopeDetail(commandEnvelopeDescription, executionApprovalDescription));
    }

    private static string CreateManagedConnectorBlockedCommandIssuanceDescription(
        string operationId,
        string? commandEnvelopeDescription,
        string? executionApprovalDescription)
    {
        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon cannot currently trust shared command issuance for {CreateManagedConnectorCommandEnvelopeOperationLabel(operationId)} until shared runtime truth, governance, and remediation blockers clear.",
            CombineManagedConnectorCommandEnvelopeDetail(commandEnvelopeDescription, executionApprovalDescription));
    }

    private static string CreateManagedConnectorAcceptedCommandIssuanceDescription(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        string? dryRunDescription)
    {
        ArgumentNullException.ThrowIfNull(executionApproval);

        var summary = executionApproval.IsApprovalRequired
            ? $"Cephalon accepts {CreateManagedConnectorCommandEnvelopeOperationLabel(operationId)} onto a future shared issuance lane, but an elevated explicit approval gate must still clear before later execution handoff."
            : $"Cephalon accepts {CreateManagedConnectorCommandEnvelopeOperationLabel(operationId)} onto a future shared issuance lane, but approval still has to clear before later execution handoff.";

        return AppendManagedConnectorCommandEnvelopeDetail(
            summary,
            CombineManagedConnectorCommandEnvelopeDetail(executionApproval.Description, dryRunDescription));
    }

    private static string CreateManagedConnectorRejectedCommandIssuanceDescription(
        string operationId,
        string? commandEnvelopeDescription,
        string? dryRunDescription)
    {
        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon rejects shared command issuance for {CreateManagedConnectorCommandEnvelopeOperationLabel(operationId)} because current runtime truth indicates no additional managed-connector changes are needed.",
            CombineManagedConnectorCommandEnvelopeDetail(commandEnvelopeDescription, dryRunDescription));
    }

    private static string CreateManagedConnectorIssuedCommandIssuanceDescription(
        string operationId,
        string? commandEnvelopeDescription)
    {
        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon marks {CreateManagedConnectorCommandEnvelopeOperationLabel(operationId)} as issued on the shared issuance lane so a later provider-execution slice can consume the canonical command safely.",
            commandEnvelopeDescription);
    }

    private static string CreateManagedConnectorNotApplicableCommandEnvelopeDescription(
        string? executionIntentDescription,
        string? governanceDescription)
    {
        return AppendManagedConnectorCommandEnvelopeDetail(
            "Cephalon does not currently prepare a managed-connector command envelope while the shared runtime remains observe-only.",
            CombineManagedConnectorCommandEnvelopeDetail(executionIntentDescription, governanceDescription));
    }

    private static string CreateManagedConnectorOperatorOnlyCommandEnvelopeDescription(
        string operationId,
        string? executionIntentDescription,
        string? executionApprovalDescription)
    {
        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon can describe a canonical command envelope for {CreateManagedConnectorCommandEnvelopeOperationLabel(operationId)}, but execution remains operator-owned until a later managed control-plane slice ships.",
            CombineManagedConnectorCommandEnvelopeDetail(executionIntentDescription, executionApprovalDescription));
    }

    private static string CreateManagedConnectorBlockedCommandEnvelopeDescription(
        string operationId,
        string? executionApprovalDescription,
        string? executionIntentDescription)
    {
        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon cannot currently trust a runnable command envelope for {CreateManagedConnectorCommandEnvelopeOperationLabel(operationId)} until shared runtime truth, governance, and remediation blockers clear.",
            CombineManagedConnectorCommandEnvelopeDetail(executionApprovalDescription, executionIntentDescription));
    }

    private static string CreateManagedConnectorApprovalGatedCommandEnvelopeDescription(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus executionApproval,
        string? dryRunDescription)
    {
        ArgumentNullException.ThrowIfNull(executionApproval);

        var summary = executionApproval.IsApprovalRequired
            ? $"Cephalon can now describe a canonical command envelope for {CreateManagedConnectorCommandEnvelopeOperationLabel(operationId)}, but that follow-through still needs an elevated explicit approval gate before future engine execution."
            : $"Cephalon can now describe a canonical command envelope for {CreateManagedConnectorCommandEnvelopeOperationLabel(operationId)}, but approval still has to clear before future engine execution can use it.";

        return AppendManagedConnectorCommandEnvelopeDetail(
            summary,
            CombineManagedConnectorCommandEnvelopeDetail(executionApproval.Description, dryRunDescription));
    }

    private static string CreateManagedConnectorEngineReadyCommandEnvelopeDescription(
        string operationId,
        string? executionApprovalDescription)
    {
        return AppendManagedConnectorCommandEnvelopeDetail(
            $"Cephalon can now describe a canonical command envelope for {CreateManagedConnectorCommandEnvelopeOperationLabel(operationId)} on the shared execution lane. Actual write-path execution remains a later follow-through slice.",
            executionApprovalDescription);
    }

    private static string CreateManagedConnectorCommandEnvelopeOperationLabel(string operationId)
    {
        if (string.IsNullOrWhiteSpace(operationId) ||
            string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.None, StringComparison.OrdinalIgnoreCase))
        {
            return "managed-connector follow-through";
        }

        return CreateManagedConnectorExecutionIntentOperationLabel(operationId);
    }

    private static string? CombineManagedConnectorCommandEnvelopeDetail(
        string? primary,
        string? secondary)
    {
        var normalizedPrimary = string.IsNullOrWhiteSpace(primary) ? null : primary.Trim();
        var normalizedSecondary = string.IsNullOrWhiteSpace(secondary) ? null : secondary.Trim();

        if (normalizedPrimary is null)
        {
            return normalizedSecondary;
        }

        if (normalizedSecondary is null ||
            string.Equals(normalizedPrimary, normalizedSecondary, StringComparison.Ordinal))
        {
            return normalizedPrimary;
        }

        return $"{normalizedPrimary} {normalizedSecondary}";
    }

    private static string? CombineManagedConnectorCommandEnvelopeDetail(
        string? primary,
        string? secondary,
        string? tertiary)
    {
        return CombineManagedConnectorCommandEnvelopeDetail(
            CombineManagedConnectorCommandEnvelopeDetail(primary, secondary),
            tertiary);
    }

    private static string AppendManagedConnectorCommandEnvelopeDetail(
        string summary,
        string? detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        return string.IsNullOrWhiteSpace(detail)
            ? summary.Trim()
            : $"{summary.Trim()} {detail.Trim()}";
    }

    private static ManagedConnectorMetadataSnapshot ResolveManagedConnectorMetadata(
        IReadOnlyDictionary<string, string> metadata)
    {
        return new ManagedConnectorMetadataSnapshot(
            ManagementMode: ResolveMetadata(metadata, ManagedConnectorManagementModeMetadataKey, "debeziumManagementMode"),
            DeclaredConnectClusterId: ResolveMetadata(
                metadata,
                ManagedConnectorDeclaredConnectClusterIdMetadataKey,
                "debeziumDeclaredConnectClusterId",
                ConnectClusterIdMetadataKey),
            ReportedConnectClusterId: ResolveMetadata(
                metadata,
                ManagedConnectorReportedConnectClusterIdMetadataKey,
                "debeziumReportedConnectClusterId"),
            DeclaredConnectorClass: ResolveMetadata(
                metadata,
                ManagedConnectorDeclaredConnectorClassMetadataKey,
                "debeziumDeclaredConnectorClass",
                ConnectorClassMetadataKey),
            ReportedConnectorClass: ResolveMetadata(
                metadata,
                ManagedConnectorReportedConnectorClassMetadataKey,
                "debeziumReportedConnectorClass"),
            DeclaredSourceProviderId: ResolveMetadata(
                metadata,
                ManagedConnectorDeclaredSourceProviderIdMetadataKey,
                "debeziumDeclaredSourceProviderId",
                SourceProviderIdMetadataKey),
            ReportedSourceProviderId: ResolveMetadata(
                metadata,
                ManagedConnectorReportedSourceProviderIdMetadataKey,
                "debeziumReportedSourceProviderId"),
            ExpectedTaskCount: ResolveNullableIntMetadata(metadata, ManagedConnectorExpectedTaskCountMetadataKey, "debeziumExpectedTaskCount"),
            ReportedTaskCount: ResolveNullableIntMetadata(metadata, ManagedConnectorReportedTaskCountMetadataKey, "debeziumReportedTaskCount"),
            DeclaredTaskIds: ResolveDelimitedMetadata(metadata, ManagedConnectorDeclaredTaskIdsMetadataKey, "debeziumDeclaredTaskIds", "taskIds"),
            ReportedTaskIds: ResolveDelimitedMetadata(metadata, ManagedConnectorReportedTaskIdsMetadataKey, "debeziumReportedTaskIds"),
            ActiveTaskIds: ResolveDelimitedMetadata(metadata, ManagedConnectorActiveTaskIdsMetadataKey, "debeziumActiveTaskIds"),
            ConnectorLifecycleState: ResolveMetadata(metadata, ManagedConnectorConnectorLifecycleStateMetadataKey, "debeziumConnectorLifecycleState"),
            TaskReconciliationState: ResolveMetadata(metadata, ManagedConnectorTaskReconciliationStateMetadataKey, "debeziumTaskReconciliationState"),
            ReconciliationState: ResolveMetadata(metadata, ManagedConnectorReconciliationStateMetadataKey, "debeziumReconciliationState"),
            ReconciliationReason: ResolveMetadata(metadata, ManagedConnectorReconciliationReasonMetadataKey, "debeziumReconciliationReason"));
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus CreateManagedConnectorActionPlanStatus(
        string state,
        string description,
        IReadOnlyList<string> categoryIds,
        IReadOnlyList<string> actionIds,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift)
    {
        return new CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus(state, description)
        {
            CategoryIds = categoryIds,
            ActionIds = actionIds,
            RemediationState = remediation.State,
            GovernanceState = governance.State,
            DriftState = drift.State
        };
    }

    private static string[] CreateManagedConnectorActionPlanCategories(
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        bool hasRuntimeRemediationAttention,
        bool isDriftBaselineIncomplete,
        bool isWaitingForRuntimeTruth)
    {
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);

        var categories = new List<string>(capacity: 4);
        if (remediation.IsBlocked)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories.BlockingRemediation);
        }
        else if (hasRuntimeRemediationAttention)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories.RuntimeRemediation);
        }

        if (governance.IsOutOfPolicy)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories.GovernanceOutOfPolicy);
        }
        else if (governance.RequiresControlPlaneSupport)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories.FutureControlPlaneDeferred);
        }

        if (isDriftBaselineIncomplete)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories.DriftBaselineIncomplete);
        }
        else if (isWaitingForRuntimeTruth)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories.WaitingForRuntimeTruth);
        }
        else if (drift.IsDrifted)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories.DriftDetected);
        }

        if (categories.Count == 0)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories.ObserveOnlySteadyState);
        }

        return [.. categories];
    }

    private static string[] CreateManagedConnectorActionIds(
        string primaryActionId,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        bool hasRuntimeRemediationAttention,
        bool isDriftBaselineIncomplete,
        bool isWaitingForRuntimeTruth)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryActionId);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);

        var actions = new List<string>(capacity: 4);
        AddManagedConnectorAction(actions, primaryActionId);

        if (remediation.IsBlocked || hasRuntimeRemediationAttention)
        {
            AddManagedConnectorAction(actions, CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.ResolveRuntimeRemediation);
        }

        if (governance.IsOutOfPolicy)
        {
            AddManagedConnectorAction(actions, CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.CompleteGovernanceDeclaration);
        }
        else if (governance.RequiresControlPlaneSupport)
        {
            AddManagedConnectorAction(actions, CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.DeferControlPlane);
        }

        if (isDriftBaselineIncomplete)
        {
            AddManagedConnectorAction(actions, CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.CompleteTaskBaseline);
        }
        else if (isWaitingForRuntimeTruth)
        {
            AddManagedConnectorAction(actions, CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.WaitForRuntimeReport);
        }
        else if (drift.IsDrifted)
        {
            AddManagedConnectorAction(actions, CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.InvestigateDrift);
        }

        if (actions.Count == 0)
        {
            AddManagedConnectorAction(actions, CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.KeepObserveOnly);
        }

        return [.. actions];
    }

    private static void AddManagedConnectorAction(
        List<string> actions,
        string actionId)
    {
        ArgumentNullException.ThrowIfNull(actions);
        if (string.IsNullOrWhiteSpace(actionId) ||
            string.Equals(actionId, CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.None, StringComparison.OrdinalIgnoreCase) ||
            actions.Contains(actionId, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        actions.Add(actionId.Trim());
    }

    private static string CreateManagedConnectorBlockedActionPlanDescription(string? remediationDescription)
    {
        return AppendManagedConnectorActionPlanDetail(
            "The managed connector is currently blocked by runtime remediation work before deeper managed-connector follow-through.",
            remediationDescription);
    }

    private static string CreateManagedConnectorRuntimeRemediationActionPlanDescription(string? remediationDescription)
    {
        return AppendManagedConnectorActionPlanDetail(
            "The managed connector currently needs runtime remediation before deeper managed-connector follow-through.",
            remediationDescription);
    }

    private static string CreateManagedConnectorGovernanceActionPlanDescription(string? governanceDescription)
    {
        return AppendManagedConnectorActionPlanDetail(
            "The managed connector should complete governance policy declarations before relying on deeper managed-connector follow-through.",
            governanceDescription);
    }

    private static string CreateManagedConnectorTaskBaselineActionPlanDescription(string? driftDescription)
    {
        return AppendManagedConnectorActionPlanDetail(
            "The managed connector should complete its declared task baseline before relying on desired-versus-observed drift follow-through.",
            driftDescription);
    }

    private static string CreateManagedConnectorWaitForRuntimeTruthActionPlanDescription(string? driftDescription)
    {
        return AppendManagedConnectorActionPlanDetail(
            "The managed connector should wait for more runtime truth before Cephalon can recommend deeper drift follow-through.",
            driftDescription);
    }

    private static string CreateManagedConnectorDriftActionPlanDescription(string? driftDescription)
    {
        return AppendManagedConnectorActionPlanDetail(
            "The managed connector should investigate desired-versus-observed drift before taking on deeper managed-connector follow-through.",
            driftDescription);
    }

    private static string CreateManagedConnectorDeferredControlPlaneActionPlanDescription(string? governanceDescription)
    {
        return AppendManagedConnectorActionPlanDetail(
            "The managed connector can keep surfacing shared runtime truth while Cephalon defers write-path control-plane ownership for the declared management mode.",
            governanceDescription);
    }

    private static string CreateManagedConnectorObserveActionPlanDescription(string? governanceDescription)
    {
        return AppendManagedConnectorActionPlanDetail(
            "The managed connector can continue in observe-only mode on the shared runtime surface.",
            governanceDescription);
    }

    private static string AppendManagedConnectorActionPlanDetail(
        string summary,
        string? detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        return string.IsNullOrWhiteSpace(detail)
            ? summary.Trim()
            : $"{summary.Trim()} {detail.Trim()}";
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus CreateManagedConnectorWritePathReadinessStatus(
        string state,
        string description,
        IReadOnlyList<string> categoryIds,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan)
    {
        return new CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus(state, description)
        {
            CategoryIds = categoryIds,
            ManagementMode = governance.ManagementMode,
            ReportingCoverageState = reportingCoverage.State,
            RemediationState = remediation.State,
            GovernanceState = governance.State,
            DriftState = drift.State,
            ActionPlanState = actionPlan.State,
            PrimaryActionId = actionPlan.PrimaryActionId
        };
    }

    private static string[] CreateManagedConnectorWritePathReadinessCategories(
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        bool hasRuntimeRemediationAttention,
        bool hasIncompleteReportingCoverage,
        bool isRuntimeTruthIncomplete,
        bool isObserveOnlyMode,
        bool isWritePathRequested)
    {
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);

        var categories = new List<string>(capacity: 5);
        if (remediation.IsBlocked)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.BlockingRemediation);
        }
        else if (hasRuntimeRemediationAttention)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.RuntimeRemediation);
        }

        if (hasIncompleteReportingCoverage)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.IncompleteReportingCoverage);
        }

        if (governance.IsOutOfPolicy)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.GovernanceOutOfPolicy);
        }

        if (isRuntimeTruthIncomplete)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.RuntimeTruthIncomplete);
        }

        if (drift.IsDrifted)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.DriftDetected);
        }

        if (isWritePathRequested)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.WritePathRequested);
        }
        else if (isObserveOnlyMode)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.ObserveOnlyMode);
        }

        if (categories.Count == 1 &&
            string.Equals(
                categories[0],
                CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.WritePathRequested,
                StringComparison.OrdinalIgnoreCase))
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories.WritePathReady);
        }

        return [.. categories];
    }

    private static string CreateManagedConnectorBlockedWritePathReadinessDescription(string? remediationDescription)
    {
        return AppendManagedConnectorWritePathReadinessDetail(
            "The managed connector is currently blocked by runtime remediation before Cephalon can consider shared write-path readiness.",
            remediationDescription);
    }

    private static string CreateManagedConnectorRuntimeRemediationWritePathReadinessDescription(string? remediationDescription)
    {
        return AppendManagedConnectorWritePathReadinessDetail(
            "The managed connector still needs runtime remediation attention before Cephalon can consider shared write-path readiness.",
            remediationDescription);
    }

    private static string CreateManagedConnectorReportingCoverageWritePathReadinessDescription(string? reportingCoverageDescription)
    {
        return AppendManagedConnectorWritePathReadinessDetail(
            "The managed connector does not yet report full declared-versus-reported coverage, so shared write-path readiness remains incomplete.",
            reportingCoverageDescription);
    }

    private static string CreateManagedConnectorGovernanceWritePathReadinessDescription(string? governanceDescription)
    {
        return AppendManagedConnectorWritePathReadinessDetail(
            "The managed connector is still out of policy for future write-path follow-through on the shared runtime surface.",
            governanceDescription);
    }

    private static string CreateManagedConnectorRuntimeTruthWritePathReadinessDescription(
        string? actionPlanDescription,
        string? driftDescription)
    {
        var detail = string.IsNullOrWhiteSpace(actionPlanDescription)
            ? driftDescription
            : actionPlanDescription;

        return AppendManagedConnectorWritePathReadinessDetail(
            "The managed connector does not yet report enough runtime truth for shared write-path readiness.",
            detail);
    }

    private static string CreateManagedConnectorDriftWritePathReadinessDescription(string? driftDescription)
    {
        return AppendManagedConnectorWritePathReadinessDetail(
            "The managed connector still reports desired-versus-observed drift, so shared write-path readiness remains incomplete.",
            driftDescription);
    }

    private static string CreateManagedConnectorReadyWritePathReadinessDescription(string? governanceDescription)
    {
        return AppendManagedConnectorWritePathReadinessDetail(
            "The managed connector currently satisfies the shared write-path readiness baseline for future Cephalon control-plane follow-through.",
            governanceDescription);
    }

    private static string CreateManagedConnectorDeferredWritePathReadinessDescription(string? governanceDescription)
    {
        return AppendManagedConnectorWritePathReadinessDetail(
            "The managed connector remains healthy on the shared runtime surface, but write-path readiness is deferred while it stays observe-only.",
            governanceDescription);
    }

    private static string AppendManagedConnectorWritePathReadinessDetail(
        string summary,
        string? detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        return string.IsNullOrWhiteSpace(detail)
            ? summary.Trim()
            : $"{summary.Trim()} {detail.Trim()}";
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus CreateManagedConnectorPreflightStatus(
        string state,
        string description,
        IReadOnlyList<string> categoryIds,
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness)
    {
        return new CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus(state, description)
        {
            CategoryIds = categoryIds,
            OperationId = operationId,
            ManagementMode = governance.ManagementMode,
            ReportingCoverageState = reportingCoverage.State,
            RemediationState = remediation.State,
            GovernanceState = governance.State,
            DriftState = drift.State,
            ActionPlanState = actionPlan.State,
            WritePathReadinessState = writePathReadiness.State,
            PrimaryActionId = actionPlan.PrimaryActionId
        };
    }

    private static string[] CreateManagedConnectorPreflightCategories(
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        bool hasRuntimeRemediationAttention,
        bool hasIncompleteReportingCoverage,
        bool isRuntimeTruthIncomplete,
        bool isObserveOnlyMode,
        bool isWritePathRequested,
        bool isReady)
    {
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);

        var categories = new List<string>(capacity: 6);
        if (remediation.IsBlocked)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.BlockingRemediation);
        }
        else if (hasRuntimeRemediationAttention)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.RuntimeRemediation);
        }

        if (hasIncompleteReportingCoverage)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.IncompleteReportingCoverage);
        }

        if (governance.IsOutOfPolicy)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.GovernanceOutOfPolicy);
        }

        if (isRuntimeTruthIncomplete)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.RuntimeTruthIncomplete);
        }

        if (drift.IsDrifted)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.DriftDetected);
        }

        if (isWritePathRequested)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.ReconcileIntent);
        }
        else if (isObserveOnlyMode)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.ObserveOnlyMode);
        }

        if (isReady)
        {
            categories.Add(CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories.PreflightReady);
        }

        return [.. categories];
    }

    private static string CreateManagedConnectorBlockedPreflightDescription(
        string? remediationDescription,
        string operationId)
    {
        return AppendManagedConnectorPreflightDetail(
            $"The managed connector is currently blocked by runtime remediation before Cephalon can preflight {CreateManagedConnectorPreflightOperationLabel(operationId)}.",
            remediationDescription);
    }

    private static string CreateManagedConnectorRuntimeRemediationPreflightDescription(
        string? remediationDescription,
        string operationId)
    {
        return AppendManagedConnectorPreflightDetail(
            $"The managed connector still needs runtime remediation attention before Cephalon can preflight {CreateManagedConnectorPreflightOperationLabel(operationId)}.",
            remediationDescription);
    }

    private static string CreateManagedConnectorReportingCoveragePreflightDescription(
        string? reportingCoverageDescription,
        string operationId)
    {
        return AppendManagedConnectorPreflightDetail(
            $"The managed connector does not yet report full declared-versus-reported coverage, so Cephalon cannot preflight {CreateManagedConnectorPreflightOperationLabel(operationId)} yet.",
            reportingCoverageDescription);
    }

    private static string CreateManagedConnectorGovernancePreflightDescription(
        string? governanceDescription,
        string operationId)
    {
        return AppendManagedConnectorPreflightDetail(
            $"The managed connector is still out of policy for future {CreateManagedConnectorPreflightOperationLabel(operationId)} on the shared runtime surface.",
            governanceDescription);
    }

    private static string CreateManagedConnectorRuntimeTruthPreflightDescription(
        string? actionPlanDescription,
        string? driftDescription,
        string operationId)
    {
        var detail = string.IsNullOrWhiteSpace(actionPlanDescription)
            ? driftDescription
            : actionPlanDescription;

        return AppendManagedConnectorPreflightDetail(
            $"The managed connector does not yet report enough runtime truth for Cephalon to preflight {CreateManagedConnectorPreflightOperationLabel(operationId)}.",
            detail);
    }

    private static string CreateManagedConnectorDriftPreflightDescription(
        string? driftDescription,
        string operationId)
    {
        return AppendManagedConnectorPreflightDetail(
            $"The managed connector still reports desired-versus-observed drift, so Cephalon cannot preflight {CreateManagedConnectorPreflightOperationLabel(operationId)} yet.",
            driftDescription);
    }

    private static string CreateManagedConnectorReadyPreflightDescription(
        string? governanceDescription,
        string operationId)
    {
        return AppendManagedConnectorPreflightDetail(
            $"The managed connector currently satisfies the shared baseline Cephalon would use to preflight {CreateManagedConnectorPreflightOperationLabel(operationId)}. Actual write-path execution remains deferred until a future control plane ships.",
            governanceDescription);
    }

    private static string CreateManagedConnectorDeferredPreflightDescription(string? governanceDescription)
    {
        return AppendManagedConnectorPreflightDetail(
            "The managed connector remains healthy on the shared runtime surface, but connector-management preflight is deferred while it stays observe-only.",
            governanceDescription);
    }

    private static string AppendManagedConnectorPreflightDetail(
        string summary,
        string? detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        return string.IsNullOrWhiteSpace(detail)
            ? summary.Trim()
            : $"{summary.Trim()} {detail.Trim()}";
    }

    private static string CreateManagedConnectorPreflightOperationLabel(string operationId)
    {
        if (string.Equals(
            operationId,
            CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.Reconcile,
            StringComparison.OrdinalIgnoreCase))
        {
            return "future reconcile follow-through";
        }

        if (string.Equals(
            operationId,
            CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.Pause,
            StringComparison.OrdinalIgnoreCase))
        {
            return "future pause follow-through";
        }

        if (string.Equals(
            operationId,
            CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.Resume,
            StringComparison.OrdinalIgnoreCase))
        {
            return "future resume follow-through";
        }

        if (string.Equals(
            operationId,
            CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.Restart,
            StringComparison.OrdinalIgnoreCase))
        {
            return "future restart follow-through";
        }

        if (string.Equals(
            operationId,
            CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds.Delete,
            StringComparison.OrdinalIgnoreCase))
        {
            return "future delete follow-through";
        }

        return "future managed-connector follow-through";
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus CreateManagedConnectorDryRunStatus(
        string state,
        string description,
        IReadOnlyList<string> categoryIds,
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus writePathReadiness,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        int potentialChangeCount,
        bool wouldApplyChanges)
    {
        return new CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus(state, description)
        {
            CategoryIds = categoryIds,
            OperationId = operationId,
            ManagementMode = governance.ManagementMode,
            ReportingCoverageState = reportingCoverage.State,
            RemediationState = remediation.State,
            GovernanceState = governance.State,
            DriftState = drift.State,
            ActionPlanState = actionPlan.State,
            WritePathReadinessState = writePathReadiness.State,
            PreflightState = preflight.State,
            PrimaryActionId = actionPlan.PrimaryActionId,
            ConnectorLifecycleState = drift.ConnectorLifecycleState,
            ReconciliationState = drift.ReconciliationState,
            MissingDeclaredTaskIds = drift.MissingDeclaredTaskIds,
            UnexpectedReportedTaskIds = drift.UnexpectedReportedTaskIds,
            PotentialChangeCount = potentialChangeCount,
            WouldApplyChanges = wouldApplyChanges
        };
    }

    private static string[] CreateManagedConnectorDryRunCategories(
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        CdcCaptureExecutionRuntimeRemediationStatus remediation,
        CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus governance,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus actionPlan,
        CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus preflight,
        string operationId,
        bool wouldApplyChanges)
    {
        ArgumentNullException.ThrowIfNull(reportingCoverage);
        ArgumentNullException.ThrowIfNull(remediation);
        ArgumentNullException.ThrowIfNull(governance);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(actionPlan);
        ArgumentNullException.ThrowIfNull(preflight);

        var categories = new List<string>(capacity: 10);
        static void AddCategory(List<string> values, string category)
        {
            if (!values.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(category);
            }
        }

        var hasRuntimeRemediationAttention =
            remediation.CategoryIds.Contains(CdcCaptureExecutionRuntimeRemediationCategories.StaleObservations, StringComparer.OrdinalIgnoreCase) ||
            remediation.CategoryIds.Contains(CdcCaptureExecutionRuntimeRemediationCategories.ReporterCoordinationIssues, StringComparer.OrdinalIgnoreCase);
        var isRuntimeTruthIncomplete =
            actionPlan.IsWaiting ||
            string.Equals(drift.State, CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown, StringComparison.OrdinalIgnoreCase);

        if (remediation.IsBlocked)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.BlockingRemediation);
        }
        else if (hasRuntimeRemediationAttention)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.RuntimeRemediation);
        }

        if (!reportingCoverage.HasFullCoverage)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.IncompleteReportingCoverage);
        }

        if (governance.IsOutOfPolicy)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.GovernanceOutOfPolicy);
        }

        if (isRuntimeTruthIncomplete)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.RuntimeTruthIncomplete);
        }

        if (preflight.IsDeferred)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.ObserveOnlyMode);
        }

        if (wouldApplyChanges)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.ChangePlanned);
        }
        else if (!preflight.IsDeferred && !preflight.RequiresAttention)
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.NoChangesRequired);
        }

        if (WouldManagedConnectorDryRunRequireLifecycleChange(operationId, wouldApplyChanges))
        {
            AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.LifecycleChange);
        }

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Reconcile, StringComparison.OrdinalIgnoreCase))
        {
            if (drift.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ConnectClusterMismatch, StringComparer.OrdinalIgnoreCase))
            {
                AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.ConnectClusterChange);
            }

            if (drift.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ConnectorClassMismatch, StringComparer.OrdinalIgnoreCase))
            {
                AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.ConnectorClassChange);
            }

            if (drift.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.SourceProviderMismatch, StringComparer.OrdinalIgnoreCase))
            {
                AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.SourceProviderChange);
            }

            if (HasManagedConnectorTaskTopologyDrift(drift))
            {
                AddCategory(categories, CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories.TaskTopologyChange);
            }
        }

        return [.. categories];
    }

    private static string ResolveManagedConnectorDryRunOperationId(string? managementMode)
    {
        if (string.IsNullOrWhiteSpace(managementMode))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.None;
        }

        var normalizedManagementMode = managementMode.Trim();
        if (string.Equals(normalizedManagementMode, "observe-only", StringComparison.OrdinalIgnoreCase))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.None;
        }

        if (string.Equals(normalizedManagementMode, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Pause, StringComparison.OrdinalIgnoreCase))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Pause;
        }

        if (string.Equals(normalizedManagementMode, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Resume, StringComparison.OrdinalIgnoreCase))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Resume;
        }

        if (string.Equals(normalizedManagementMode, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Restart, StringComparison.OrdinalIgnoreCase))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Restart;
        }

        if (string.Equals(normalizedManagementMode, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Delete, StringComparison.OrdinalIgnoreCase))
        {
            return CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Delete;
        }

        return CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Reconcile;
    }

    private static bool WouldManagedConnectorDryRunApplyChanges(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage)
    {
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(reportingCoverage);

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.None, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Reconcile, StringComparison.OrdinalIgnoreCase))
        {
            return drift.IsDrifted;
        }

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Pause, StringComparison.OrdinalIgnoreCase))
        {
            return !string.Equals(drift.ConnectorLifecycleState, "paused", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Resume, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(drift.ConnectorLifecycleState, "paused", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(drift.ConnectorLifecycleState, "inactive", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Restart, StringComparison.OrdinalIgnoreCase))
        {
            return !string.Equals(drift.ConnectorLifecycleState, "restarting", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Delete, StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrWhiteSpace(drift.ConnectorLifecycleState) || reportingCoverage.ReportedCaptureCount > 0;
        }

        return true;
    }

    private static bool WouldManagedConnectorDryRunRequireLifecycleChange(string operationId, bool wouldApplyChanges)
    {
        if (!wouldApplyChanges)
        {
            return false;
        }

        return !string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.None, StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Reconcile, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasManagedConnectorTaskTopologyDrift(CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift)
    {
        ArgumentNullException.ThrowIfNull(drift);

        return drift.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.MissingDeclaredTaskReports, StringComparer.OrdinalIgnoreCase) ||
               drift.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.UnexpectedReportedTasks, StringComparer.OrdinalIgnoreCase) ||
               drift.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.TaskCountMismatch, StringComparer.OrdinalIgnoreCase);
    }

    private static int CountManagedConnectorDryRunPotentialChanges(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage,
        bool wouldApplyChanges)
    {
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(reportingCoverage);

        if (!wouldApplyChanges)
        {
            return 0;
        }

        if (!string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Reconcile, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        var count = 0;
        if (drift.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ConnectClusterMismatch, StringComparer.OrdinalIgnoreCase))
        {
            count++;
        }

        if (drift.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ConnectorClassMismatch, StringComparer.OrdinalIgnoreCase))
        {
            count++;
        }

        if (drift.CategoryIds.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.SourceProviderMismatch, StringComparer.OrdinalIgnoreCase))
        {
            count++;
        }

        if (HasManagedConnectorTaskTopologyDrift(drift))
        {
            count++;
        }

        return Math.Max(count, 1);
    }

    private static string CreateManagedConnectorDeferredDryRunDescription(string? governanceDescription)
    {
        return AppendManagedConnectorDryRunDetail(
            "The managed connector remains healthy on the shared runtime surface, but dry-run write-path previews stay deferred while it remains observe-only.",
            governanceDescription);
    }

    private static string CreateManagedConnectorBlockedDryRunDescription(
        string? preflightDescription,
        string operationId,
        bool wouldApplyChanges,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage)
    {
        string? changeDetail = null;
        var summary = wouldApplyChanges
            ? $"Cephalon cannot trust a dry-run answer for {CreateManagedConnectorDryRunOperationLabel(operationId)} yet, but current runtime truth still suggests that operation would change the managed connector once blockers clear."
            : $"Cephalon cannot trust a dry-run answer for {CreateManagedConnectorDryRunOperationLabel(operationId)} yet because the managed connector has not satisfied shared preflight requirements.";
        var detail = wouldApplyChanges
            ? changeDetail = CreateManagedConnectorDryRunChangeDetail(operationId, drift, reportingCoverage)
            : preflightDescription;

        if (wouldApplyChanges && !string.IsNullOrWhiteSpace(preflightDescription))
        {
            detail = $"{preflightDescription.Trim()} {changeDetail!.Trim()}";
        }

        return AppendManagedConnectorDryRunDetail(summary, detail);
    }

    private static string CreateManagedConnectorWouldChangeDryRunDescription(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage)
    {
        return AppendManagedConnectorDryRunDetail(
            $"If Cephalon executed {CreateManagedConnectorDryRunOperationLabel(operationId)} now, the shared runtime truth suggests it would change the managed connector.",
            CreateManagedConnectorDryRunChangeDetail(operationId, drift, reportingCoverage));
    }

    private static string CreateManagedConnectorNoOpDryRunDescription(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage)
    {
        return AppendManagedConnectorDryRunDetail(
            $"If Cephalon executed {CreateManagedConnectorDryRunOperationLabel(operationId)} now, the shared runtime truth suggests no managed-connector changes would be required.",
            CreateManagedConnectorDryRunNoOpDetail(operationId, drift, reportingCoverage));
    }

    private static string CreateManagedConnectorDryRunChangeDetail(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage)
    {
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(reportingCoverage);

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Reconcile, StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(drift.Description)
                ? "The managed connector currently reports declared-versus-observed drift against its shared baseline."
                : drift.Description.Trim();
        }

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Pause, StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(drift.ConnectorLifecycleState)
                ? "The managed connector does not currently report a paused lifecycle posture."
                : $"The managed connector currently reports lifecycle state '{drift.ConnectorLifecycleState}', so a pause operation would still change runtime posture.";
        }

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Resume, StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(drift.ConnectorLifecycleState)
                ? "The managed connector currently does not report an active lifecycle posture."
                : $"The managed connector currently reports lifecycle state '{drift.ConnectorLifecycleState}', so a resume operation would still change runtime posture.";
        }

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Restart, StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(drift.ConnectorLifecycleState)
                ? "The managed connector currently does not report a restarting lifecycle posture."
                : $"The managed connector currently reports lifecycle state '{drift.ConnectorLifecycleState}', so a restart operation would still change runtime posture.";
        }

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Delete, StringComparison.OrdinalIgnoreCase))
        {
            return reportingCoverage.ReportedCaptureCount > 0 || !string.IsNullOrWhiteSpace(drift.ConnectorLifecycleState)
                ? "The managed connector currently reports live runtime presence on the shared surface, so a delete operation would still change runtime posture."
                : "The managed connector would still be treated as a live managed-connector declaration until delete follow-through completes.";
        }

        return "The managed connector currently reports potential write-path changes on the shared surface.";
    }

    private static string CreateManagedConnectorDryRunNoOpDetail(
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorDriftStatus drift,
        CdcCaptureExecutionRuntimeReportingCoverageStatus reportingCoverage)
    {
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(reportingCoverage);

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Reconcile, StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(drift.Description)
                ? "The managed connector currently reports no declared-versus-observed drift."
                : drift.Description.Trim();
        }

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Pause, StringComparison.OrdinalIgnoreCase))
        {
            return "The managed connector already reports a paused lifecycle posture.";
        }

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Resume, StringComparison.OrdinalIgnoreCase))
        {
            return "The managed connector already reports an active lifecycle posture.";
        }

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Restart, StringComparison.OrdinalIgnoreCase))
        {
            return "The managed connector already reports a restarting lifecycle posture.";
        }

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Delete, StringComparison.OrdinalIgnoreCase))
        {
            return reportingCoverage.ReportedCaptureCount == 0 && string.IsNullOrWhiteSpace(drift.ConnectorLifecycleState)
                ? "The shared runtime surface does not currently report live connector presence."
                : "The shared runtime surface already reflects the requested delete posture.";
        }

        return "The managed connector currently reports no shared write-path changes for the intended operation.";
    }

    private static string AppendManagedConnectorDryRunDetail(
        string summary,
        string? detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        return string.IsNullOrWhiteSpace(detail)
            ? summary.Trim()
            : $"{summary.Trim()} {detail.Trim()}";
    }

    private static string CreateManagedConnectorDryRunOperationLabel(string operationId)
    {
        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Reconcile, StringComparison.OrdinalIgnoreCase))
        {
            return "reconcile the managed connector";
        }

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Pause, StringComparison.OrdinalIgnoreCase))
        {
            return "pause the managed connector";
        }

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Resume, StringComparison.OrdinalIgnoreCase))
        {
            return "resume the managed connector";
        }

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Restart, StringComparison.OrdinalIgnoreCase))
        {
            return "restart the managed connector";
        }

        if (string.Equals(operationId, CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.Delete, StringComparison.OrdinalIgnoreCase))
        {
            return "delete the managed connector";
        }

        return "change the managed connector";
    }

    private static string CreateManagedConnectorInSyncDescription(string? reconciliationState)
    {
        if (string.IsNullOrWhiteSpace(reconciliationState) ||
            string.Equals(reconciliationState, "current", StringComparison.OrdinalIgnoreCase))
        {
            return "The managed connector currently reports no declared-versus-observed drift.";
        }

        return $"The managed connector currently reports no declared-versus-observed drift. Last reported reconciliation state '{reconciliationState}'.";
    }

    private static string CreateManagedConnectorDriftDescription(
        IReadOnlyCollection<string> categories,
        ManagedConnectorMetadataSnapshot snapshot,
        string[] missingDeclaredTaskIds,
        string[] unexpectedReportedTaskIds)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(snapshot);

        var messages = new List<string>(capacity: 6);

        if (categories.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.TaskCountMismatch, StringComparer.OrdinalIgnoreCase) &&
            snapshot.ExpectedTaskCount.HasValue &&
            snapshot.ReportedTaskCount.HasValue)
        {
            messages.Add($"Declared task count '{snapshot.ExpectedTaskCount.Value}' but last reported '{snapshot.ReportedTaskCount.Value}'.");
        }

        if (categories.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.MissingDeclaredTaskReports, StringComparer.OrdinalIgnoreCase) &&
            missingDeclaredTaskIds.Length > 0)
        {
            messages.Add(missingDeclaredTaskIds.Length == 1
                ? $"Declared task '{missingDeclaredTaskIds[0]}' was not reported."
                : $"Declared tasks '{string.Join(",", missingDeclaredTaskIds)}' were not reported.");
        }

        if (categories.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.UnexpectedReportedTasks, StringComparer.OrdinalIgnoreCase) &&
            unexpectedReportedTaskIds.Length > 0)
        {
            messages.Add(unexpectedReportedTaskIds.Length == 1
                ? $"Reported task '{unexpectedReportedTaskIds[0]}' was not declared."
                : $"Reported tasks '{string.Join(",", unexpectedReportedTaskIds)}' were not declared.");
        }

        if (categories.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ConnectClusterMismatch, StringComparer.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(snapshot.DeclaredConnectClusterId) &&
            !string.IsNullOrWhiteSpace(snapshot.ReportedConnectClusterId))
        {
            messages.Add($"Declared connector cluster '{snapshot.DeclaredConnectClusterId}' but last reported '{snapshot.ReportedConnectClusterId}'.");
        }

        if (categories.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ConnectorClassMismatch, StringComparer.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(snapshot.DeclaredConnectorClass) &&
            !string.IsNullOrWhiteSpace(snapshot.ReportedConnectorClass))
        {
            messages.Add($"Declared connector class '{snapshot.DeclaredConnectorClass}' but last reported '{snapshot.ReportedConnectorClass}'.");
        }

        if (categories.Contains(CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.SourceProviderMismatch, StringComparer.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(snapshot.DeclaredSourceProviderId) &&
            !string.IsNullOrWhiteSpace(snapshot.ReportedSourceProviderId))
        {
            messages.Add($"Declared source provider '{snapshot.DeclaredSourceProviderId}' but last reported '{snapshot.ReportedSourceProviderId}'.");
        }

        return messages.Count == 0
            ? "The managed connector currently reports declared-versus-observed drift."
            : string.Join(" ", messages);
    }

    private static string CreateManagedConnectorObserveOnlyDescription(
        string? reconciliationState,
        string? reconciliationReason)
    {
        if (string.IsNullOrWhiteSpace(reconciliationState) ||
            string.Equals(reconciliationState, "current", StringComparison.OrdinalIgnoreCase))
        {
            return "The managed connector is currently governed in observe-only mode.";
        }

        if (!string.IsNullOrWhiteSpace(reconciliationReason))
        {
            return $"The managed connector is currently governed in observe-only mode. {reconciliationReason}";
        }

        return $"The managed connector is currently governed in observe-only mode and last reported reconciliation state '{reconciliationState}'.";
    }

    private static string CreateManagedConnectorOutOfPolicyDescription(
        IReadOnlyCollection<string> categories,
        string? managementMode)
    {
        ArgumentNullException.ThrowIfNull(categories);

        var missingMessages = new List<string>(capacity: 4);
        if (categories.Contains(CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.MissingManagementMode, StringComparer.OrdinalIgnoreCase))
        {
            missingMessages.Add("management mode");
        }

        if (categories.Contains(CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.MissingConnectClusterId, StringComparer.OrdinalIgnoreCase))
        {
            missingMessages.Add("connector cluster id");
        }

        if (categories.Contains(CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.MissingConnectorClass, StringComparer.OrdinalIgnoreCase))
        {
            missingMessages.Add("connector class");
        }

        if (categories.Contains(CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.MissingSourceProviderId, StringComparer.OrdinalIgnoreCase))
        {
            missingMessages.Add("source provider id");
        }

        var description = missingMessages.Count switch
        {
            0 => "The managed connector is currently out of policy.",
            1 => $"The managed connector is currently out of policy because it does not declare {missingMessages[0]}.",
            2 => $"The managed connector is currently out of policy because it does not declare {missingMessages[0]} or {missingMessages[1]}.",
            _ => $"The managed connector is currently out of policy because it does not declare {string.Join(", ", missingMessages.Take(missingMessages.Count - 1))}, or {missingMessages[^1]}."
        };

        if (!string.IsNullOrWhiteSpace(managementMode) &&
            !string.Equals(managementMode, CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.ObserveOnly, StringComparison.OrdinalIgnoreCase))
        {
            return $"{description} It also declares future management mode '{managementMode}', which remains a later control-plane slice.";
        }

        return description;
    }

    private static CdcCaptureReporterCoordinationStatus CreateReporterCoordination(
        CdcCaptureExecutionRuntimeDescriptor runtime)
    {
        if (runtime.ReporterLeaseSeconds is not int reporterLeaseSeconds ||
            reporterLeaseSeconds <= 0)
        {
            return new CdcCaptureReporterCoordinationStatus(
                CdcCaptureReporterCoordinationStates.NotConfigured,
                "The execution runtime does not currently declare reporter-lease coordination.");
        }

        return new CdcCaptureReporterCoordinationStatus(
            CdcCaptureReporterCoordinationStates.Unreported,
            "The execution runtime has not reported any external reporter observations yet.");
    }

    private static CdcCaptureExecutionRuntimeReportingCoverageStatus CreateReportingCoverage(
        IReadOnlyList<string> declaredCaptureIds,
        IReadOnlyList<CdcCaptureRuntimeState> matchingStates)
    {
        var normalizedDeclaredCaptureIds = declaredCaptureIds
            .Where(static cdcCaptureId => !string.IsNullOrWhiteSpace(cdcCaptureId))
            .Select(static cdcCaptureId => cdcCaptureId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static cdcCaptureId => cdcCaptureId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var reportedCaptureIds = matchingStates
            .Where(static state => state.HasReports)
            .Select(static state => state.CdcCaptureId)
            .Where(static cdcCaptureId => !string.IsNullOrWhiteSpace(cdcCaptureId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static cdcCaptureId => cdcCaptureId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedDeclaredCaptureIds.Length == 0)
        {
            return new CdcCaptureExecutionRuntimeReportingCoverageStatus(
                CdcCaptureExecutionRuntimeReportingCoverageStates.NotBound,
                "The execution runtime does not currently resolve to any CDC captures.")
            {
                DeclaredCaptureCount = 0,
                ReportedCaptureCount = reportedCaptureIds.Length
            };
        }

        var unreportedCaptureIds = normalizedDeclaredCaptureIds
            .Except(reportedCaptureIds, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static cdcCaptureId => cdcCaptureId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (reportedCaptureIds.Length == 0)
        {
            return new CdcCaptureExecutionRuntimeReportingCoverageStatus(
                CdcCaptureExecutionRuntimeReportingCoverageStates.Unreported,
                normalizedDeclaredCaptureIds.Length == 1
                    ? $"Declared CDC capture '{normalizedDeclaredCaptureIds[0]}' has not reported runtime state yet."
                    : $"{normalizedDeclaredCaptureIds.Length} declared CDC captures have not reported runtime state yet.")
            {
                DeclaredCaptureCount = normalizedDeclaredCaptureIds.Length,
                ReportedCaptureCount = 0,
                UnreportedCdcCaptureIds = normalizedDeclaredCaptureIds
            };
        }

        if (unreportedCaptureIds.Length > 0)
        {
            return new CdcCaptureExecutionRuntimeReportingCoverageStatus(
                CdcCaptureExecutionRuntimeReportingCoverageStates.PartiallyReported,
                unreportedCaptureIds.Length == 1
                    ? $"Declared CDC capture '{unreportedCaptureIds[0]}' has not reported runtime state yet."
                    : $"{unreportedCaptureIds.Length} declared CDC captures have not reported runtime state yet.")
            {
                DeclaredCaptureCount = normalizedDeclaredCaptureIds.Length,
                ReportedCaptureCount = reportedCaptureIds.Length,
                UnreportedCdcCaptureIds = unreportedCaptureIds
            };
        }

        return new CdcCaptureExecutionRuntimeReportingCoverageStatus(
            CdcCaptureExecutionRuntimeReportingCoverageStates.FullyReported,
            normalizedDeclaredCaptureIds.Length == 1
                ? "The declared CDC capture has reported runtime state for the execution runtime."
                : "All declared CDC captures have reported runtime state for the execution runtime.")
        {
            DeclaredCaptureCount = normalizedDeclaredCaptureIds.Length,
            ReportedCaptureCount = reportedCaptureIds.Length
        };
    }

    private static CdcCaptureFreshnessStatus AggregateObservationFreshness(
        IReadOnlyList<CdcCaptureRuntimeState> matchingStates)
    {
        var reportedStates = matchingStates
            .Where(static state => state.HasReports)
            .ToArray();
        if (reportedStates.Length == 0)
        {
            return new CdcCaptureFreshnessStatus(CdcCaptureFreshnessStates.Unknown);
        }

        var knownStates = reportedStates
            .Select(static state => state.ObservationFreshness)
            .Where(static freshness => !string.Equals(freshness.State, CdcCaptureFreshnessStates.Unknown, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (knownStates.Length == 0)
        {
            return new CdcCaptureFreshnessStatus(CdcCaptureFreshnessStates.Unknown);
        }

        if (knownStates.Any(static freshness => string.Equals(freshness.State, CdcCaptureFreshnessStates.Stale, StringComparison.OrdinalIgnoreCase)))
        {
            var earliestKnownExpiry = knownStates
                .Where(static freshness => freshness.FreshUntilUtc.HasValue)
                .Select(static freshness => freshness.FreshUntilUtc)
                .Min();
            return new CdcCaptureFreshnessStatus(
                CdcCaptureFreshnessStates.Stale,
                freshUntilUtc: earliestKnownExpiry,
                description: "At least one CDC capture observation owned by the execution runtime is now stale.");
        }

        var freshStates = knownStates
            .Where(static freshness => string.Equals(freshness.State, CdcCaptureFreshnessStates.Fresh, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (freshStates.Length == knownStates.Length &&
            freshStates.All(static freshness => freshness.FreshUntilUtc.HasValue))
        {
            return new CdcCaptureFreshnessStatus(
                CdcCaptureFreshnessStates.Fresh,
                freshStates.Min(static freshness => freshness.FreshUntilUtc!.Value),
                "All CDC capture observations owned by the execution runtime remain within the configured freshness window.");
        }

        var earliestFreshExpiry = freshStates
            .Where(static freshness => freshness.FreshUntilUtc.HasValue)
            .Select(static freshness => freshness.FreshUntilUtc)
            .Min();
        return new CdcCaptureFreshnessStatus(
            CdcCaptureFreshnessStates.Mixed,
            freshUntilUtc: earliestFreshExpiry,
            description: "CDC capture observations owned by the execution runtime do not currently share the same freshness posture.");
    }

    private static CdcCaptureExecutionRuntimeReporterCoordinationRollup CreateReporterCoordinationRollup(
        IReadOnlyList<CdcCaptureRuntimeState> matchingStates)
    {
        if (matchingStates.Count == 0)
        {
            return CdcCaptureExecutionRuntimeReporterCoordinationRollup.Empty;
        }

        return new CdcCaptureExecutionRuntimeReporterCoordinationRollup(
            CoordinationStateBreakdown: CreateReporterCoordinationBreakdown(
                matchingStates.Select(static state => state.ReporterCoordination.State),
                CdcCaptureReporterCoordinationStates.Unknown),
            DegradedReasonBreakdown: CreateReporterCoordinationBreakdown(
                matchingStates.Select(static state => state.ReporterCoordination.DegradedReason),
                CdcCaptureReporterCoordinationIssueReasons.None))
        {
            ActiveReporterIds = GetReporterIdsByRole(
                matchingStates,
                CdcCaptureReporterParticipantRoles.Active),
            StandbyReporterIds = GetReporterIdsByRole(
                matchingStates,
                CdcCaptureReporterParticipantRoles.Standby),
            RejectedReporterIds = GetReporterIdsByRole(
                matchingStates,
                CdcCaptureReporterParticipantRoles.Rejected),
            DegradedCdcCaptureIds = matchingStates
                .Where(static state => state.HasReporterCoordinationIssue)
                .Select(static state => state.CdcCaptureId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static cdcCaptureId => cdcCaptureId, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    private static CdcCaptureReporterCoordinationBreakdownEntry[] CreateReporterCoordinationBreakdown(
        IEnumerable<string?> values,
        string fallbackId)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackId);

        return values
            .Select(value => string.IsNullOrWhiteSpace(value) ? fallbackId.Trim() : value.Trim())
            .GroupBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .Select(static group => new CdcCaptureReporterCoordinationBreakdownEntry(group.Key, group.Count()))
            .OrderBy(static item => item.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string[] GetReporterIdsByRole(
        IReadOnlyList<CdcCaptureRuntimeState> matchingStates,
        string role)
    {
        ArgumentNullException.ThrowIfNull(matchingStates);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        return matchingStates
            .SelectMany(static state => state.ReporterCoordination.ReporterParticipants)
            .Where(participant => string.Equals(participant.Role, role, StringComparison.OrdinalIgnoreCase))
            .Select(static participant => participant.ReporterId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static reporterId => reporterId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private string[] ResolveCaptureIds(string executionRuntimeId)
    {
        return captureCatalog.GetByExecutionRuntimeId(executionRuntimeId)
            .Select(static cdcCapture => cdcCapture.Id)
            .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? ResolveActiveReporterId(
        IReadOnlyList<CdcCaptureRuntimeState> matchingStates,
        DateTimeOffset now)
    {
        var activeReporterIds = matchingStates
            .Where(state =>
                !string.IsNullOrWhiteSpace(state.LastReporterId) &&
                IsReporterLeaseActive(state.ReporterLeaseExpiresAtUtc, now))
            .Select(static state => state.LastReporterId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static reporterId => reporterId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return activeReporterIds.Length == 1
            ? activeReporterIds[0]
            : null;
    }

    private static DateTimeOffset? ResolveActiveReporterLeaseExpiry(
        IReadOnlyList<CdcCaptureRuntimeState> matchingStates,
        string? activeReporterId)
    {
        if (string.IsNullOrWhiteSpace(activeReporterId))
        {
            return null;
        }

        return matchingStates
            .Where(state => string.Equals(state.LastReporterId, activeReporterId, StringComparison.OrdinalIgnoreCase))
            .Where(static state => state.ReporterLeaseExpiresAtUtc.HasValue)
            .Select(static state => state.ReporterLeaseExpiresAtUtc)
            .Max();
    }

    private static bool IsReporterLeaseActive(
        DateTimeOffset? reporterLeaseExpiresAtUtc,
        DateTimeOffset now)
    {
        return !reporterLeaseExpiresAtUtc.HasValue ||
               reporterLeaseExpiresAtUtc.Value >= now;
    }

    private static string[] ResolveDelimitedMetadata(
        IReadOnlyDictionary<string, string> metadata,
        params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!metadata.TryGetValue(key, out var value) ||
                string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            return value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(static item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static item => item, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return [];
    }

    private static string? ResolveMetadata(
        IReadOnlyDictionary<string, string> metadata,
        params string[] keys)
    {
        foreach (var key in keys)
        {
            if (metadata.TryGetValue(key, out var value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static int? ResolveNullableIntMetadata(
        IReadOnlyDictionary<string, string> metadata,
        params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!metadata.TryGetValue(key, out var value) ||
                string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (int.TryParse(value.Trim(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private sealed record ManagedConnectorMetadataSnapshot(
        string? ManagementMode,
        string? DeclaredConnectClusterId,
        string? ReportedConnectClusterId,
        string? DeclaredConnectorClass,
        string? ReportedConnectorClass,
        string? DeclaredSourceProviderId,
        string? ReportedSourceProviderId,
        int? ExpectedTaskCount,
        int? ReportedTaskCount,
        string[] DeclaredTaskIds,
        string[] ReportedTaskIds,
        string[] ActiveTaskIds,
        string? ConnectorLifecycleState,
        string? TaskReconciliationState,
        string? ReconciliationState,
        string? ReconciliationReason);
}
