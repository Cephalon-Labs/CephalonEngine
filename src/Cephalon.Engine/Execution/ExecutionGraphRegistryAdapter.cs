using Cephalon.Abstractions.Execution;

namespace Cephalon.Engine.Execution;

internal sealed class ExecutionGraphRegistryAdapter(
    string moduleId,
    List<ExecutionGraphDescriptor> graphs) : IExecutionGraphRegistry
{
    public void Add(ExecutionGraphDescriptor graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        if (!string.Equals(graph.SourceModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Execution graph '{graph.Id}' declared source module '{graph.SourceModuleId}', but it was contributed by module '{moduleId}'.");
        }

        graphs.Add(graph);
    }
}
