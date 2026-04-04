namespace Cephalon.Abstractions.Data;

/// <summary>
/// Exposes the projections visible to the current runtime.
/// </summary>
public interface IProjectionCatalog
{
    /// <summary>
    /// Gets all projections visible to the current runtime.
    /// </summary>
    IReadOnlyList<ProjectionDescriptor> Projections { get; }

    /// <summary>
    /// Gets one projection by its stable identifier.
    /// </summary>
    /// <param name="projectionId">The projection identifier to resolve.</param>
    /// <returns>The matching projection, or <see langword="null" /> when it is not active.</returns>
    ProjectionDescriptor? GetById(string projectionId);

    /// <summary>
    /// Gets all projections contributed by the requested module.
    /// </summary>
    /// <param name="sourceModuleId">The source module identifier to filter by.</param>
    /// <returns>The matching projections, or an empty list when the module contributed none.</returns>
    IReadOnlyList<ProjectionDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all projections that target the requested store identifier.
    /// </summary>
    /// <param name="targetStoreId">The target store identifier to filter by.</param>
    /// <returns>The matching projections, or an empty list when no projection targets the store.</returns>
    IReadOnlyList<ProjectionDescriptor> GetByTargetStore(string targetStoreId);
}
