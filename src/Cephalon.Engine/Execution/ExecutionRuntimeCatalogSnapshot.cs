using Cephalon.Abstractions.Execution;

namespace Cephalon.Engine.Execution;

internal sealed class ExecutionRuntimeCatalogSnapshot : IExecutionRuntimeCatalog
{
    private readonly IReadOnlyList<ExecutionGraphDescriptor> graphs;
    private readonly Dictionary<string, ExecutionGraphDescriptor> graphsById;
    private readonly Dictionary<string, IReadOnlyList<ExecutionGraphDescriptor>> graphsBySourceModule;

    public ExecutionRuntimeCatalogSnapshot(IEnumerable<ExecutionGraphDescriptor> graphs)
    {
        ArgumentNullException.ThrowIfNull(graphs);

        this.graphs = graphs.ToArray();
        graphsById = this.graphs.ToDictionary(static graph => graph.Id, StringComparer.OrdinalIgnoreCase);
        graphsBySourceModule = this.graphs
            .GroupBy(static graph => graph.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<ExecutionGraphDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ExecutionGraphDescriptor> Graphs => graphs;

    public ExecutionGraphDescriptor? GetById(string graphId)
    {
        if (string.IsNullOrWhiteSpace(graphId))
        {
            return null;
        }

        return graphsById.TryGetValue(graphId.Trim(), out var graph)
            ? graph
            : null;
    }

    public IReadOnlyList<ExecutionGraphDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return graphsBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }
}
