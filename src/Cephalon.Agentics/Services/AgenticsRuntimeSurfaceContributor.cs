using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Runtime;
using Cephalon.Abstractions.Execution;
using Cephalon.Engine.Manifest;

namespace Cephalon.Agentics.Services;

internal sealed class AgenticsRuntimeSurfaceContributor(
    IAgentToolCatalog catalog,
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

        return new TechnologyRuntimeSurface(
            technologyId: "agentic-workloads",
            surfaceId: "agent-tools",
            displayName: "Agent Tools",
            description: "Registered agent tools available to the active agentic runtime.",
            entries: catalog.Tools
                .Select(tool => CreateEntry(
                    tool,
                    capabilityIndex,
                    graphStateIndex,
                    hostedExecutionStateIndex,
                    executionGraphs,
                    hostedExecutions))
                .ToArray());
    }

    private static TechnologyRuntimeEntry CreateEntry(
        AgentToolDescriptor tool,
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

        return new TechnologyRuntimeEntry(
            id: tool.Id,
            displayName: tool.DisplayName,
            description: tool.Description,
            metadata: metadata);
    }
}
