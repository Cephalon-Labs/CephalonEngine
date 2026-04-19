namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Exposes the active durable-execution workflows visible to the current runtime.
/// </summary>
public interface IDurableExecutionRuntimeCatalog
{
    /// <summary>
    /// Gets all active durable-execution workflows visible to the current runtime.
    /// </summary>
    IReadOnlyList<DurableExecutionRuntimeDescriptor> DurableExecutions { get; }

    /// <summary>
    /// Gets one durable-execution workflow by its stable behavior identifier.
    /// </summary>
    /// <param name="behaviorId">The durable behavior identifier to resolve.</param>
    /// <returns>The matching durable workflow descriptor, or <see langword="null" /> when it is not active.</returns>
    DurableExecutionRuntimeDescriptor? GetById(string behaviorId);

    /// <summary>
    /// Gets all durable-execution workflows contributed by the requested module.
    /// </summary>
    /// <param name="sourceModuleId">The source module identifier to filter by.</param>
    /// <returns>The matching durable workflows, or an empty list when the module contributed none.</returns>
    IReadOnlyList<DurableExecutionRuntimeDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all durable-execution workflows exposed over the requested transport.
    /// </summary>
    /// <param name="transportId">The stable transport identifier to filter by.</param>
    /// <returns>The matching durable workflows, or an empty list when none expose that transport.</returns>
    IReadOnlyList<DurableExecutionRuntimeDescriptor> GetByTransportId(string transportId);
}
