namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Exposes the effective strangler-fig migration policy visible to the current runtime.
/// </summary>
public interface IStranglerFigMigrationRuntimeCatalog
{
    /// <summary>
    /// Gets all effective strangler-fig migration-policy answers visible to the current runtime.
    /// </summary>
    IReadOnlyList<StranglerFigMigrationRuntimeDescriptor> Routes { get; }

    /// <summary>
    /// Gets one effective strangler-fig migration-policy answer by its stable route identifier.
    /// </summary>
    /// <param name="routeId">The route identifier to resolve.</param>
    /// <returns>The matching runtime descriptor, or <see langword="null" /> when it is not active.</returns>
    StranglerFigMigrationRuntimeDescriptor? GetById(string routeId);

    /// <summary>
    /// Gets all effective strangler-fig migration-policy answers owned by the requested module.
    /// </summary>
    /// <param name="sourceModuleId">The module identifier to filter by.</param>
    /// <returns>The matching runtime descriptors, or an empty list when none are active.</returns>
    IReadOnlyList<StranglerFigMigrationRuntimeDescriptor> GetBySourceModule(string sourceModuleId);
}
