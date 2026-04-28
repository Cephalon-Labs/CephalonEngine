using Cephalon.Abstractions.Technologies;
using Cephalon.Agentics.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Abstractions.Execution;
using Cephalon.Engine.Manifest;
using System.Globalization;

namespace Cephalon.Agentics.Services;

internal sealed class AgenticsRuntimeSurfaceContributor(
    IAgentToolCatalog catalog,
    AgenticRuntimeOptions options,
    IEnumerable<IAgentToolExecutor> executors,
    IEnumerable<IAgentToolRunCatalog> runCatalogs,
    IRuntime runtime,
    IExecutionRuntimeCatalog executionGraphs,
    IHostedExecutionRuntimeCatalog hostedExecutions) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var capabilityIndex = runtime.Manifest.Capabilities.ToDictionary(
            static capability => capability.Key,
            StringComparer.OrdinalIgnoreCase);
        var graphStateIndex = runtime.OperationalStory.ExecutionGraphs.ToDictionary(
            static graph => graph.GraphId,
            StringComparer.OrdinalIgnoreCase);
        var hostedExecutionStateIndex = runtime.OperationalStory.HostedExecutions.ToDictionary(
            static execution => execution.HostedExecutionId,
            StringComparer.OrdinalIgnoreCase);
        var runCatalog = runCatalogs.FirstOrDefault();
        var executorIndex = executors
            .GroupBy(static executor => executor.ToolId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.Count(),
                StringComparer.OrdinalIgnoreCase);

        return new TechnologyRuntimeSurface(
            technologyId: "agentic-workloads",
            surfaceId: "agent-tools",
            displayName: "Agent Tools",
            description: "Registered agent tools, managed execution readiness, and reported run state available to the active agentic runtime.",
            entries: catalog.Tools
                .Select(tool => CreateEntry(
                    tool,
                    options,
                    executorIndex,
                    runCatalog,
                    capabilityIndex,
                    graphStateIndex,
                    hostedExecutionStateIndex,
                    executionGraphs,
                    hostedExecutions))
                .ToArray());
    }

    private static TechnologyRuntimeEntry CreateEntry(
        AgentToolDescriptor tool,
        AgenticRuntimeOptions options,
        Dictionary<string, int> executorIndex,
        IAgentToolRunCatalog? runCatalog,
        Dictionary<string, CapabilityManifest> capabilityIndex,
        Dictionary<string, RuntimeExecutionGraphState> graphStateIndex,
        Dictionary<string, RuntimeHostedExecutionState> hostedExecutionStateIndex,
        IExecutionRuntimeCatalog executionGraphs,
        IHostedExecutionRuntimeCatalog hostedExecutions)
    {
        var metadata = new Dictionary<string, string>(tool.Metadata, StringComparer.OrdinalIgnoreCase);
        if (tool.Tags.Count > 0)
        {
            metadata["tags"] = string.Join(",", tool.Tags);
        }

        var executorCount = executorIndex.GetValueOrDefault(tool.Id);
        var runs = runCatalog?.GetByToolId(tool.Id) ?? [];
        var latestRun = runs
            .OrderByDescending(static run => run.LastObservedAtUtc)
            .ThenBy(static run => run.RunId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        metadata["executionEnabled"] = options.EnableExecution.ToString().ToLowerInvariant();
        metadata["executionOwnership"] = options.EnableExecution && executorCount > 0
            ? "cephalon-managed"
            : options.EnableExecution
                ? "awaiting-executor"
                : "not-configured";
        metadata["executorConfigured"] = (executorCount > 0).ToString().ToLowerInvariant();
        metadata["executorCount"] = executorCount.ToString(CultureInfo.InvariantCulture);
        metadata["runtimeState"] = latestRun is null ? "not-reported" : "reported";
        metadata["runCount"] = runs.Count.ToString(CultureInfo.InvariantCulture);

        if (tool.CapabilityKeys.Count > 0)
        {
            metadata["capabilityKeys"] = string.Join(",", tool.CapabilityKeys);
            metadata["capabilityDisplayNames"] = string.Join(
                ",",
                tool.CapabilityKeys.Select(capabilityKey => capabilityIndex[capabilityKey].DisplayName));
        }

        var hostedExecution = !string.IsNullOrWhiteSpace(tool.HostedExecutionId)
            ? hostedExecutions.GetById(tool.HostedExecutionId)
            : null;
        var resolvedExecutionGraphId = tool.ExecutionGraphId ?? hostedExecution?.ExecutionGraphId;
        if (!string.IsNullOrWhiteSpace(resolvedExecutionGraphId))
        {
            var executionGraph = executionGraphs.GetById(resolvedExecutionGraphId!);
            metadata["executionGraphId"] = executionGraph!.Id;
            metadata["executionGraphDisplayName"] = executionGraph.DisplayName;

            if (graphStateIndex.TryGetValue(executionGraph.Id, out var graphState))
            {
                metadata["executionGraphPhase"] = graphState.LastObservedPhase ?? "unknown";
                metadata["executionGraphIsActive"] = graphState.IsActive.ToString().ToLowerInvariant();
            }
        }

        if (hostedExecution is not null)
        {
            metadata["hostedExecutionId"] = hostedExecution.Id;
            metadata["hostedExecutionDisplayName"] = hostedExecution.DisplayName;
            metadata["hostedExecutionKind"] = hostedExecution.Kind;

            if (hostedExecutionStateIndex.TryGetValue(hostedExecution.Id, out var hostedExecutionState))
            {
                metadata["hostedExecutionPhase"] = hostedExecutionState.LastObservedPhase ?? "unknown";
                metadata["hostedExecutionIsActive"] = hostedExecutionState.IsActive.ToString().ToLowerInvariant();
            }
        }

        metadata["orchestrationLinked"] = (
            tool.CapabilityKeys.Count > 0 ||
            !string.IsNullOrWhiteSpace(tool.ExecutionGraphId) ||
            !string.IsNullOrWhiteSpace(tool.HostedExecutionId))
            .ToString()
            .ToLowerInvariant();

        if (latestRun is not null)
        {
            metadata["lastRunId"] = latestRun.RunId;
            metadata["lastOutcome"] = latestRun.LastOutcome ?? "unknown";
            metadata["lastObservedAtUtc"] = latestRun.LastObservedAtUtc?.ToString("O") ?? string.Empty;
            metadata["lastAttempt"] = latestRun.LastAttempt.ToString(CultureInfo.InvariantCulture);
            metadata["startedCount"] = latestRun.StartedCount.ToString(CultureInfo.InvariantCulture);
            metadata["succeededCount"] = latestRun.SucceededCount.ToString(CultureInfo.InvariantCulture);
            metadata["failedCount"] = latestRun.FailedCount.ToString(CultureInfo.InvariantCulture);
            metadata["skippedCount"] = latestRun.SkippedCount.ToString(CultureInfo.InvariantCulture);
            metadata["approvalRequiredCount"] = latestRun.ApprovalRequiredCount.ToString(CultureInfo.InvariantCulture);
            metadata["deniedCount"] = latestRun.DeniedCount.ToString(CultureInfo.InvariantCulture);
            metadata["totalReports"] = latestRun.TotalReports.ToString(CultureInfo.InvariantCulture);
            metadata["requiresApproval"] = latestRun.RequiresApproval.ToString().ToLowerInvariant();
            metadata["isTerminal"] = latestRun.IsTerminal.ToString().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(latestRun.LastActorId))
            {
                metadata["lastActorId"] = latestRun.LastActorId;
            }

            if (!string.IsNullOrWhiteSpace(latestRun.LastCorrelationId))
            {
                metadata["lastCorrelationId"] = latestRun.LastCorrelationId;
            }

            if (!string.IsNullOrWhiteSpace(latestRun.LastOutputSummary))
            {
                metadata["lastOutputSummary"] = latestRun.LastOutputSummary;
            }

            if (!string.IsNullOrWhiteSpace(latestRun.LastError))
            {
                metadata["lastError"] = latestRun.LastError;
            }

            if (latestRun.Metadata.Count > 0)
            {
                metadata["reportedMetadataKeys"] = string.Join(
                    ",",
                    latestRun.Metadata.Keys.OrderBy(static key => key, StringComparer.OrdinalIgnoreCase));

                foreach (var pair in latestRun.Metadata.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(pair.Key))
                    {
                        metadata[$"reported.{pair.Key.Trim()}"] = pair.Value;
                    }
                }
            }
        }

        return new TechnologyRuntimeEntry(
            id: tool.Id,
            displayName: tool.DisplayName,
            description: tool.Description,
            metadata: metadata);
    }
}
