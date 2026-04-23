using System.Globalization;
using Cephalon.Abstractions.Data;
using Cephalon.Data.Debezium.Configuration;

namespace Cephalon.Data.Debezium.Services;

internal sealed class DebeziumExecutionRuntimeReportSink(IServiceProvider serviceProvider, DebeziumDataOptions options)
    : ICdcCaptureExecutionRuntimeReportSink
{
    private const string ExecutionRuntimeMetadataPrefix = "executionRuntime.";
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
    private static readonly Type? RuntimeStateCatalogType =
        typeof(Cephalon.Data.Registration.DataEngineBuilderExtensions).Assembly
            .GetType("Cephalon.Data.Services.CdcCaptureRuntimeStateCatalog", throwOnError: false, ignoreCase: false);

    private readonly Dictionary<string, DebeziumConnectorOptions> connectorsById = options.Connectors
        .Where(static connector => !string.IsNullOrWhiteSpace(connector.Id))
        .ToDictionary(static connector => connector.Id.Trim(), Comparer);

    public ValueTask ReportAsync(
        string executionRuntimeId,
        IReadOnlyList<CdcCaptureRuntimeObservation> observations,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);
        ArgumentNullException.ThrowIfNull(observations);

        if (RuntimeStateCatalogType is null)
        {
            throw new InvalidOperationException(
                "Cephalon.Data.Debezium could not locate the shared Cephalon.Data runtime-state catalog required for external CDC reporting.");
        }

        var runtimeStateCatalog = serviceProvider.GetService(RuntimeStateCatalogType) as ICdcCaptureExecutionRuntimeReportSink;
        if (runtimeStateCatalog is null)
        {
            throw new InvalidOperationException(
                "Cephalon.Data.Debezium requires engine.AddData(...) so the shared Cephalon.Data runtime-state catalog can accept external Debezium runtime reports.");
        }

        var normalizedExecutionRuntimeId = executionRuntimeId.Trim();
        if (!connectorsById.TryGetValue(normalizedExecutionRuntimeId, out var connector))
        {
            return runtimeStateCatalog.ReportAsync(normalizedExecutionRuntimeId, observations, cancellationToken);
        }

        return runtimeStateCatalog.ReportAsync(
            normalizedExecutionRuntimeId,
            observations.Select(observation => NormalizeObservation(connector, observation)).ToArray(),
            cancellationToken);
    }

    private static CdcCaptureRuntimeObservation NormalizeObservation(
        DebeziumConnectorOptions connector,
        CdcCaptureRuntimeObservation observation)
    {
        var metadata = observation.Metadata.Count == 0
            ? new Dictionary<string, string>(Comparer)
            : new Dictionary<string, string>(observation.Metadata, Comparer);
        var declaredTaskIds = NormalizeList(connector.TaskIds);
        var expectedTaskCount = ResolveExpectedTaskCount(connector, declaredTaskIds);
        var managementMode = NormalizeConfiguredValue(connector.ManagementMode, "observe-only");
        var connectorState = NormalizeToken(ResolveMetadata(metadata, "connectorState"));
        var connectorLifecycleState = ResolveConnectorLifecycleState(connectorState);
        var rebalanceState = NormalizeToken(ResolveMetadata(metadata, "rebalanceState"));
        var reportedTaskIds = ResolveTaskIds(metadata, "reportedTaskIds", "taskIds");
        var activeTaskIds = ResolveTaskIds(metadata, "activeTaskIds");
        var failedTaskIds = ResolveTaskIds(metadata, "failedTaskIds");
        var pausedTaskIds = ResolveTaskIds(metadata, "pausedTaskIds");
        var restartingTaskIds = ResolveTaskIds(metadata, "restartingTaskIds");
        var taskStateSummary = NormalizeOptional(ResolveMetadata(metadata, "taskStateSummary"));
        var connectorGeneration = NormalizeConfiguredValue(ResolveMetadata(metadata, "connectorGeneration"));
        var workerId = NormalizeOptional(ResolveMetadata(metadata, "workerId"));

        if (reportedTaskIds.Length == 0)
        {
            reportedTaskIds = NormalizeList(
                activeTaskIds
                    .Concat(failedTaskIds)
                    .Concat(pausedTaskIds)
                    .Concat(restartingTaskIds));
        }

        var reportedTaskCount = ResolveReportedTaskCount(
            metadata,
            reportedTaskIds,
            activeTaskIds,
            failedTaskIds,
            pausedTaskIds,
            restartingTaskIds);
        var taskReconciliationState = ResolveTaskReconciliationState(
            declaredTaskIds,
            expectedTaskCount,
            reportedTaskIds,
            reportedTaskCount,
            failedTaskIds,
            pausedTaskIds,
            restartingTaskIds);
        var reconciliationState = ResolveReconciliationState(
            connectorLifecycleState,
            taskReconciliationState,
            rebalanceState);
        var reconciliationReason = DescribeReconciliationState(
            connectorLifecycleState,
            taskReconciliationState,
            rebalanceState,
            declaredTaskIds,
            expectedTaskCount,
            reportedTaskIds,
            reportedTaskCount,
            failedTaskIds,
            pausedTaskIds,
            restartingTaskIds);

        UpsertOptional(metadata, "debeziumManagementMode", managementMode);
        UpsertOptionalInt(metadata, "debeziumExpectedTaskCount", expectedTaskCount);
        UpsertOptionalList(metadata, "debeziumDeclaredTaskIds", declaredTaskIds);
        UpsertOptional(metadata, "debeziumConnectorState", connectorState);
        UpsertOptional(metadata, "debeziumConnectorLifecycleState", connectorLifecycleState);
        UpsertOptional(metadata, "debeziumTaskReconciliationState", taskReconciliationState);
        UpsertOptional(metadata, "debeziumReconciliationState", reconciliationState);
        UpsertOptional(metadata, "debeziumReconciliationReason", reconciliationReason);
        UpsertOptionalInt(metadata, "debeziumReportedTaskCount", reportedTaskCount);
        UpsertOptionalList(metadata, "debeziumReportedTaskIds", reportedTaskIds);
        UpsertOptionalList(metadata, "debeziumActiveTaskIds", activeTaskIds);
        UpsertOptionalList(metadata, "debeziumFailedTaskIds", failedTaskIds);
        UpsertOptionalList(metadata, "debeziumPausedTaskIds", pausedTaskIds);
        UpsertOptionalList(metadata, "debeziumRestartingTaskIds", restartingTaskIds);
        UpsertOptional(metadata, "debeziumTaskStateSummary", taskStateSummary);
        UpsertOptional(metadata, "debeziumRebalanceState", rebalanceState);
        UpsertOptional(metadata, "debeziumConnectorGeneration", connectorGeneration);
        UpsertOptional(metadata, "debeziumWorkerId", workerId);
        UpsertOptional(metadata, "managedConnectorManagementMode", managementMode);
        UpsertOptionalInt(metadata, "managedConnectorExpectedTaskCount", expectedTaskCount);
        UpsertOptionalList(metadata, "managedConnectorDeclaredTaskIds", declaredTaskIds);
        UpsertOptional(metadata, "managedConnectorConnectorLifecycleState", connectorLifecycleState);
        UpsertOptional(metadata, "managedConnectorTaskReconciliationState", taskReconciliationState);
        UpsertOptional(metadata, "managedConnectorReconciliationState", reconciliationState);
        UpsertOptional(metadata, "managedConnectorReconciliationReason", reconciliationReason);
        UpsertOptionalInt(metadata, "managedConnectorReportedTaskCount", reportedTaskCount);
        UpsertOptionalList(metadata, "managedConnectorReportedTaskIds", reportedTaskIds);
        UpsertOptionalList(metadata, "managedConnectorActiveTaskIds", activeTaskIds);

        UpsertExecutionRuntimeMetadata(metadata, "debeziumManagementMode", managementMode);
        UpsertExecutionRuntimeMetadata(metadata, "debeziumExpectedTaskCount", expectedTaskCount?.ToString(CultureInfo.InvariantCulture));
        UpsertExecutionRuntimeMetadata(metadata, "debeziumDeclaredTaskIds", JoinValues(declaredTaskIds));
        UpsertExecutionRuntimeMetadata(metadata, "debeziumConnectorState", connectorState);
        UpsertExecutionRuntimeMetadata(metadata, "debeziumConnectorLifecycleState", connectorLifecycleState);
        UpsertExecutionRuntimeMetadata(metadata, "debeziumTaskReconciliationState", taskReconciliationState);
        UpsertExecutionRuntimeMetadata(metadata, "debeziumReconciliationState", reconciliationState);
        UpsertExecutionRuntimeMetadata(metadata, "debeziumReconciliationReason", reconciliationReason);
        UpsertExecutionRuntimeMetadata(metadata, "debeziumReportedTaskCount", reportedTaskCount?.ToString(CultureInfo.InvariantCulture));
        UpsertExecutionRuntimeMetadata(metadata, "debeziumReportedTaskIds", JoinValues(reportedTaskIds));
        UpsertExecutionRuntimeMetadata(metadata, "debeziumActiveTaskIds", JoinValues(activeTaskIds));
        UpsertExecutionRuntimeMetadata(metadata, "debeziumFailedTaskIds", JoinValues(failedTaskIds));
        UpsertExecutionRuntimeMetadata(metadata, "debeziumPausedTaskIds", JoinValues(pausedTaskIds));
        UpsertExecutionRuntimeMetadata(metadata, "debeziumRestartingTaskIds", JoinValues(restartingTaskIds));
        UpsertExecutionRuntimeMetadata(metadata, "debeziumTaskStateSummary", taskStateSummary);
        UpsertExecutionRuntimeMetadata(metadata, "debeziumRebalanceState", rebalanceState);
        UpsertExecutionRuntimeMetadata(metadata, "debeziumConnectorGeneration", connectorGeneration);
        UpsertExecutionRuntimeMetadata(metadata, "debeziumWorkerId", workerId);
        UpsertExecutionRuntimeMetadata(metadata, "managedConnectorManagementMode", managementMode);
        UpsertExecutionRuntimeMetadata(metadata, "managedConnectorExpectedTaskCount", expectedTaskCount?.ToString(CultureInfo.InvariantCulture));
        UpsertExecutionRuntimeMetadata(metadata, "managedConnectorDeclaredTaskIds", JoinValues(declaredTaskIds));
        UpsertExecutionRuntimeMetadata(metadata, "managedConnectorConnectorLifecycleState", connectorLifecycleState);
        UpsertExecutionRuntimeMetadata(metadata, "managedConnectorTaskReconciliationState", taskReconciliationState);
        UpsertExecutionRuntimeMetadata(metadata, "managedConnectorReconciliationState", reconciliationState);
        UpsertExecutionRuntimeMetadata(metadata, "managedConnectorReconciliationReason", reconciliationReason);
        UpsertExecutionRuntimeMetadata(metadata, "managedConnectorReportedTaskCount", reportedTaskCount?.ToString(CultureInfo.InvariantCulture));
        UpsertExecutionRuntimeMetadata(metadata, "managedConnectorReportedTaskIds", JoinValues(reportedTaskIds));
        UpsertExecutionRuntimeMetadata(metadata, "managedConnectorActiveTaskIds", JoinValues(activeTaskIds));

        return new CdcCaptureRuntimeObservation(
            cdcCaptureId: observation.CdcCaptureId,
            outcome: observation.Outcome,
            observedAtUtc: observation.ObservedAtUtc,
            reportId: observation.ReportId,
            capturedChangeCount: observation.CapturedChangeCount,
            producedMessageCount: observation.ProducedMessageCount,
            changeId: observation.ChangeId,
            checkpoint: observation.Checkpoint,
            error: observation.Error,
            freshness: observation.Freshness,
            lag: observation.Lag,
            publication: observation.Publication,
            metadata: metadata,
            reporterId: observation.ReporterId,
            edgeNodeId: observation.EdgeNodeId);
    }

    private static string[] ResolveTaskIds(
        IReadOnlyDictionary<string, string> metadata,
        params string[] keys)
    {
        return NormalizeList(
            keys.SelectMany(key => SplitValues(ResolveMetadata(metadata, key))));
    }

    private static int? ResolveReportedTaskCount(
        IReadOnlyDictionary<string, string> metadata,
        string[] reportedTaskIds,
        string[] activeTaskIds,
        string[] failedTaskIds,
        string[] pausedTaskIds,
        string[] restartingTaskIds)
    {
        var rawTaskCount = ResolveMetadata(metadata, "reportedTaskCount", "taskCount");
        if (int.TryParse(rawTaskCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedTaskCount) &&
            parsedTaskCount >= 0)
        {
            return parsedTaskCount;
        }

        if (reportedTaskIds.Length > 0)
        {
            return reportedTaskIds.Length;
        }

        var derivedTaskIds = NormalizeList(
            activeTaskIds
                .Concat(failedTaskIds)
                .Concat(pausedTaskIds)
                .Concat(restartingTaskIds));
        return derivedTaskIds.Length > 0
            ? derivedTaskIds.Length
            : null;
    }

    private static int? ResolveExpectedTaskCount(
        DebeziumConnectorOptions connector,
        string[] declaredTaskIds)
    {
        if (connector.ExpectedTaskCount is >= 0)
        {
            return connector.ExpectedTaskCount;
        }

        return declaredTaskIds.Length > 0
            ? declaredTaskIds.Length
            : null;
    }

    private static string ResolveTaskReconciliationState(
        string[] declaredTaskIds,
        int? expectedTaskCount,
        string[] reportedTaskIds,
        int? reportedTaskCount,
        string[] failedTaskIds,
        string[] pausedTaskIds,
        string[] restartingTaskIds)
    {
        if (failedTaskIds.Length > 0)
        {
            return "task-failed";
        }

        if (restartingTaskIds.Length > 0)
        {
            return "task-restarting";
        }

        if (pausedTaskIds.Length > 0)
        {
            return "task-paused";
        }

        if (declaredTaskIds.Length > 0 &&
            reportedTaskIds.Length > 0 &&
            !declaredTaskIds.SequenceEqual(reportedTaskIds, Comparer))
        {
            return "task-mismatch";
        }

        if (expectedTaskCount.HasValue &&
            reportedTaskCount.HasValue &&
            expectedTaskCount.Value != reportedTaskCount.Value)
        {
            return "task-count-mismatch";
        }

        if (expectedTaskCount is > 0 &&
            reportedTaskCount == 0)
        {
            return "no-active-tasks";
        }

        if ((declaredTaskIds.Length > 0 || expectedTaskCount.HasValue) &&
            !reportedTaskCount.HasValue &&
            reportedTaskIds.Length == 0)
        {
            return "task-unreported";
        }

        return "current";
    }

    private static string ResolveReconciliationState(
        string? connectorLifecycleState,
        string taskReconciliationState,
        string? rebalanceState)
    {
        if (IsRebalancingState(rebalanceState))
        {
            return "rebalancing";
        }

        if (Comparer.Equals(connectorLifecycleState, "failed"))
        {
            return "connector-failed";
        }

        if (Comparer.Equals(connectorLifecycleState, "paused"))
        {
            return "connector-paused";
        }

        if (Comparer.Equals(connectorLifecycleState, "restarting"))
        {
            return "connector-restarting";
        }

        if (Comparer.Equals(connectorLifecycleState, "inactive"))
        {
            return "connector-inactive";
        }

        return taskReconciliationState;
    }

    private static string? DescribeReconciliationState(
        string? connectorLifecycleState,
        string taskReconciliationState,
        string? rebalanceState,
        string[] declaredTaskIds,
        int? expectedTaskCount,
        string[] reportedTaskIds,
        int? reportedTaskCount,
        string[] failedTaskIds,
        string[] pausedTaskIds,
        string[] restartingTaskIds)
    {
        if (IsRebalancingState(rebalanceState))
        {
            return string.IsNullOrWhiteSpace(rebalanceState)
                ? "The Debezium connector currently reports an active task rebalance."
                : $"The Debezium connector currently reports rebalance state '{rebalanceState}'.";
        }

        if (Comparer.Equals(connectorLifecycleState, "failed"))
        {
            return "The Debezium connector currently reports a failed lifecycle posture.";
        }

        if (Comparer.Equals(connectorLifecycleState, "paused"))
        {
            return "The Debezium connector currently reports a paused lifecycle posture.";
        }

        if (Comparer.Equals(connectorLifecycleState, "restarting"))
        {
            return "The Debezium connector currently reports a restarting lifecycle posture.";
        }

        if (Comparer.Equals(connectorLifecycleState, "inactive"))
        {
            return "The Debezium connector currently reports an inactive lifecycle posture.";
        }

        return taskReconciliationState switch
        {
            "task-failed" => $"The Debezium connector currently reports failed tasks: {JoinValues(failedTaskIds)}.",
            "task-restarting" => $"The Debezium connector currently reports restarting tasks: {JoinValues(restartingTaskIds)}.",
            "task-paused" => $"The Debezium connector currently reports paused tasks: {JoinValues(pausedTaskIds)}.",
            "task-mismatch" => $"The Debezium connector declared tasks '{JoinValues(declaredTaskIds)}' but last reported '{JoinValues(reportedTaskIds)}'.",
            "task-count-mismatch" => $"The Debezium connector expected {expectedTaskCount?.ToString(CultureInfo.InvariantCulture) ?? "an unknown number of"} tasks but last reported {reportedTaskCount?.ToString(CultureInfo.InvariantCulture) ?? "no"} tasks.",
            "no-active-tasks" => "The Debezium connector expected active tasks but last reported none.",
            "task-unreported" => "The Debezium connector has declared task expectations but the latest report did not include task ownership details.",
            _ => "The Debezium connector lifecycle and task ownership currently match the declared runtime expectations."
        };
    }

    private static bool IsRebalancingState(string? rebalanceState)
    {
        return rebalanceState is not null &&
               rebalanceState is "rebalancing" or "assigning" or "revoking" or "reconciling" or "syncing";
    }

    private static string? ResolveConnectorLifecycleState(string? connectorState)
    {
        return connectorState switch
        {
            null => "unreported",
            "running" => "running",
            "paused" => "paused",
            "failed" => "failed",
            "restarting" => "restarting",
            "unassigned" => "inactive",
            "stopped" => "inactive",
            _ => connectorState
        };
    }

    private static string[] SplitValues(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split([',', ';', '|', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string[] NormalizeList(IEnumerable<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(Comparer)
            .OrderBy(static value => value, Comparer)
            .ToArray() ?? [];
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

    private static string? NormalizeConfiguredValue(string? value, string? defaultValue = null)
    {
        var normalizedValue = NormalizeToken(value);
        if (!string.IsNullOrWhiteSpace(normalizedValue))
        {
            return normalizedValue;
        }

        return NormalizeToken(defaultValue);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string? NormalizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim()
            .Replace('_', '-')
            .Replace(' ', '-')
            .ToLowerInvariant();
    }

    private static string? JoinValues(string[] values)
    {
        return values.Length == 0
            ? null
            : string.Join(",", values);
    }

    private static void UpsertExecutionRuntimeMetadata(
        Dictionary<string, string> metadata,
        string key,
        string? value)
    {
        UpsertOptional(metadata, $"{ExecutionRuntimeMetadataPrefix}{key}", value);
    }

    private static void UpsertOptional(
        Dictionary<string, string> metadata,
        string key,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            metadata.Remove(key);
            return;
        }

        metadata[key] = value.Trim();
    }

    private static void UpsertOptionalInt(
        Dictionary<string, string> metadata,
        string key,
        int? value)
    {
        if (!value.HasValue)
        {
            metadata.Remove(key);
            return;
        }

        metadata[key] = value.Value.ToString(CultureInfo.InvariantCulture);
    }

    private static void UpsertOptionalList(
        Dictionary<string, string> metadata,
        string key,
        string[] values)
    {
        UpsertOptional(metadata, key, JoinValues(values));
    }
}
