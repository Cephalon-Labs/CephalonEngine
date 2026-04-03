using Cephalon.Agentics.Configuration;
using Cephalon.Abstractions.Execution;
using Cephalon.Engine.Runtime;

namespace Cephalon.Agentics.Services;

internal sealed class AgentToolCatalog : IAgentToolCatalog
{
    private readonly Dictionary<string, AgentToolDescriptor> index;

    public AgentToolCatalog(
        AgenticRuntimeOptions options,
        IEnumerable<IAgentToolContributor> contributors,
        IRuntime runtime,
        IExecutionRuntimeCatalog executionGraphs,
        IHostedExecutionRuntimeCatalog hostedExecutions)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contributors);
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(executionGraphs);
        ArgumentNullException.ThrowIfNull(hostedExecutions);

        var registry = new AgentToolRegistry();
        foreach (var tool in options.Tools)
        {
            registry.Add(tool);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterTools(registry);
        }

        Tools = registry.Build();
        ValidateLinks(Tools, runtime, executionGraphs, hostedExecutions);
        index = Tools.ToDictionary(static tool => tool.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<AgentToolDescriptor> Tools { get; }

    public bool TryGet(string toolId, out AgentToolDescriptor tool)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolId);

        return index.TryGetValue(toolId.Trim(), out tool!);
    }

    private static void ValidateLinks(
        IReadOnlyList<AgentToolDescriptor> tools,
        IRuntime runtime,
        IExecutionRuntimeCatalog executionGraphs,
        IHostedExecutionRuntimeCatalog hostedExecutions)
    {
        var capabilities = runtime.Manifest.Capabilities
            .ToDictionary(static capability => capability.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var tool in tools)
        {
            foreach (var capabilityKey in tool.CapabilityKeys)
            {
                if (!capabilities.ContainsKey(capabilityKey))
                {
                    throw new InvalidOperationException(
                        $"Agent tool '{tool.Id}' references unknown capability '{capabilityKey}'.");
                }
            }

            if (!string.IsNullOrWhiteSpace(tool.ExecutionGraphId) &&
                executionGraphs.GetById(tool.ExecutionGraphId) is null)
            {
                throw new InvalidOperationException(
                    $"Agent tool '{tool.Id}' references unknown execution graph '{tool.ExecutionGraphId}'.");
            }

            if (!string.IsNullOrWhiteSpace(tool.HostedExecutionId) &&
                hostedExecutions.GetById(tool.HostedExecutionId) is null)
            {
                throw new InvalidOperationException(
                    $"Agent tool '{tool.Id}' references unknown hosted execution '{tool.HostedExecutionId}'.");
            }
        }
    }
}
