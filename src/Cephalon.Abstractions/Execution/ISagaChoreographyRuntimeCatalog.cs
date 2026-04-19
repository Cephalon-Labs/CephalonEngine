namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Exposes the active saga-choreography behaviors visible to the current runtime.
/// </summary>
public interface ISagaChoreographyRuntimeCatalog
{
    /// <summary>
    /// Gets all active saga-choreography behaviors visible to the current runtime.
    /// </summary>
    IReadOnlyList<SagaChoreographyRuntimeDescriptor> SagaChoreographies { get; }

    /// <summary>
    /// Gets one saga-choreography behavior by its stable behavior identifier.
    /// </summary>
    /// <param name="behaviorId">The choreography behavior identifier to resolve.</param>
    /// <returns>The matching choreography descriptor, or <see langword="null" /> when it is not active.</returns>
    SagaChoreographyRuntimeDescriptor? GetById(string behaviorId);

    /// <summary>
    /// Gets all saga-choreography behaviors contributed by the requested module.
    /// </summary>
    /// <param name="sourceModuleId">The source module identifier to filter by.</param>
    /// <returns>The matching choreographies, or an empty list when the module contributed none.</returns>
    IReadOnlyList<SagaChoreographyRuntimeDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all saga-choreography behaviors exposed over the requested transport.
    /// </summary>
    /// <param name="transportId">The stable transport identifier to filter by.</param>
    /// <returns>The matching choreographies, or an empty list when none expose that transport.</returns>
    IReadOnlyList<SagaChoreographyRuntimeDescriptor> GetByTransportId(string transportId);
}
