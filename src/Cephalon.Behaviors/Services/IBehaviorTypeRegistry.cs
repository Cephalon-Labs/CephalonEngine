namespace Cephalon.Behaviors.Services;

/// <summary>
/// Maps behavior identifiers to their concrete implementation types.
/// This registry is populated during DI setup and consumed by
/// <see cref="BehaviorDispatcher" /> at construction time to build its dispatch table.
/// </summary>
public interface IBehaviorTypeRegistry
{
    /// <summary>
    /// Attempts to resolve the concrete behavior type for the given identifier.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier to look up.</param>
    /// <param name="behaviorType">
    /// When this method returns <see langword="true" />, receives the concrete behavior type;
    /// otherwise <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the identifier is registered; otherwise <see langword="false" />.
    /// </returns>
    bool TryGetType(string behaviorId, out Type? behaviorType);

    /// <summary>
    /// Registers a mapping from <paramref name="behaviorId" /> to <paramref name="behaviorType" />.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier.</param>
    /// <param name="behaviorType">The concrete behavior implementation type.</param>
    void Register(string behaviorId, Type behaviorType);
}
