namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Exposes the hosted executions visible to the current runtime.
/// </summary>
public interface IHostedExecutionRuntimeCatalog
{
    /// <summary>
    /// Gets all hosted executions visible to the current runtime.
    /// </summary>
    IReadOnlyList<HostedExecutionDescriptor> HostedExecutions { get; }

    /// <summary>
    /// Gets one hosted execution by its stable identifier.
    /// </summary>
    /// <param name="hostedExecutionId">The hosted-execution identifier to resolve.</param>
    /// <returns>The matching hosted execution, or <see langword="null" /> when it is not active.</returns>
    HostedExecutionDescriptor? GetById(string hostedExecutionId);

    /// <summary>
    /// Gets all hosted executions contributed by the requested module.
    /// </summary>
    /// <param name="sourceModuleId">The source module identifier to filter by.</param>
    /// <returns>The matching hosted executions, or an empty list when the module contributed none.</returns>
    IReadOnlyList<HostedExecutionDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all hosted executions linked to one execution graph.
    /// </summary>
    /// <param name="executionGraphId">The execution-graph identifier to filter by.</param>
    /// <returns>The matching hosted executions, or an empty list when none link to that graph.</returns>
    IReadOnlyList<HostedExecutionDescriptor> GetByExecutionGraph(string executionGraphId);
}
