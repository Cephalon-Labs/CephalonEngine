namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Exposes the execution graphs visible to the current runtime.
/// </summary>
public interface IExecutionRuntimeCatalog
{
    /// <summary>
    /// Gets all execution graphs visible to the current runtime.
    /// </summary>
    IReadOnlyList<ExecutionGraphDescriptor> Graphs { get; }

    /// <summary>
    /// Gets one execution graph by its stable identifier.
    /// </summary>
    /// <param name="graphId">The execution-graph identifier to resolve.</param>
    /// <returns>The matching graph, or <see langword="null" /> when it is not active.</returns>
    ExecutionGraphDescriptor? GetById(string graphId);

    /// <summary>
    /// Gets all execution graphs contributed by the requested module.
    /// </summary>
    /// <param name="sourceModuleId">The source module identifier to filter by.</param>
    /// <returns>The matching execution graphs, or an empty list when the module contributed none.</returns>
    IReadOnlyList<ExecutionGraphDescriptor> GetBySourceModule(string sourceModuleId);
}
