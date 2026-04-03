using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Execution;
using Cephalon.Engine.Manifest;

namespace Cephalon.Engine.Execution;

internal static class ExecutionGraphValidation
{
    public static ExecutionGraphDescriptor[] Validate(
        IEnumerable<ExecutionGraphDescriptor> graphs,
        IReadOnlyList<ModuleManifest> modules,
        IReadOnlyList<CapabilityManifest> capabilities)
    {
        ArgumentNullException.ThrowIfNull(graphs);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(capabilities);

        var graphArray = graphs.ToArray();
        var moduleIds = modules
            .Select(static module => module.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var capabilityKeys = capabilities
            .Select(static capability => capability.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seenGraphIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var graph in graphArray)
        {
            if (!seenGraphIds.Add(graph.Id))
            {
                throw new InvalidOperationException(
                    $"Execution graph '{graph.Id}' was contributed more than once.");
            }

            if (!moduleIds.Contains(graph.SourceModuleId))
            {
                throw new InvalidOperationException(
                    $"Execution graph '{graph.Id}' references unknown source module '{graph.SourceModuleId}'.");
            }

            if (graph.Nodes.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Execution graph '{graph.Id}' must declare at least one node.");
            }

            var nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in graph.Nodes)
            {
                if (!nodeIds.Add(node.Id))
                {
                    throw new InvalidOperationException(
                        $"Execution graph '{graph.Id}' contains duplicate node id '{node.Id}'.");
                }

                if (!string.IsNullOrWhiteSpace(node.ModuleId) && !moduleIds.Contains(node.ModuleId))
                {
                    throw new InvalidOperationException(
                        $"Execution graph '{graph.Id}' node '{node.Id}' references unknown module '{node.ModuleId}'.");
                }

                if (!string.IsNullOrWhiteSpace(node.CapabilityKey) && !capabilityKeys.Contains(node.CapabilityKey))
                {
                    throw new InvalidOperationException(
                        $"Execution graph '{graph.Id}' node '{node.Id}' references unknown capability '{node.CapabilityKey}'.");
                }
            }

            if (!nodeIds.Contains(graph.EntryNodeId))
            {
                throw new InvalidOperationException(
                    $"Execution graph '{graph.Id}' references unknown entry node '{graph.EntryNodeId}'.");
            }

            foreach (var edge in graph.Edges)
            {
                if (!nodeIds.Contains(edge.FromNodeId))
                {
                    throw new InvalidOperationException(
                        $"Execution graph '{graph.Id}' edge references unknown source node '{edge.FromNodeId}'.");
                }

                if (!nodeIds.Contains(edge.ToNodeId))
                {
                    throw new InvalidOperationException(
                        $"Execution graph '{graph.Id}' edge references unknown destination node '{edge.ToNodeId}'.");
                }
            }
        }

        return graphArray;
    }
}
