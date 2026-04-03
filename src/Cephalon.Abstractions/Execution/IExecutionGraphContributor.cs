namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Contributes one or more execution graphs to the active runtime.
/// </summary>
public interface IExecutionGraphContributor
{
    /// <summary>
    /// Registers one or more execution graphs owned by the contributor.
    /// </summary>
    /// <param name="graphs">The execution-graph registry receiving graph descriptors.</param>
    void RegisterExecutionGraphs(IExecutionGraphRegistry graphs);
}
