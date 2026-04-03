namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Receives execution graphs contributed by active modules.
/// </summary>
public interface IExecutionGraphRegistry
{
    /// <summary>
    /// Adds an execution graph to the current runtime composition.
    /// </summary>
    /// <param name="graph">The execution graph to register.</param>
    void Add(ExecutionGraphDescriptor graph);
}
