using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal sealed class CdcCaptureExecutionRuntimeCatalog : ICdcCaptureExecutionRuntimeCatalog
{
    private const string ExecutionRuntimeMetadataPrefix = "executionRuntime.";
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
    private readonly TimeProvider timeProvider;

    public CdcCaptureExecutionRuntimeCatalog(
        CdcCaptureExecutionRuntimeDescriptorCatalog runtimeDescriptorCatalog,
        ICdcCaptureCatalog captureCatalog,
        ICdcCaptureRuntimeStateCatalog? runtimeStateCatalog = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(runtimeDescriptorCatalog);
        ArgumentNullException.ThrowIfNull(captureCatalog);

        this.captureCatalog = captureCatalog;
        this.runtimeStateCatalog = runtimeStateCatalog;
        this.timeProvider = timeProvider ?? TimeProvider.System;
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
            ManagedConnectorActionPlan = managedConnectorActionPlan
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
