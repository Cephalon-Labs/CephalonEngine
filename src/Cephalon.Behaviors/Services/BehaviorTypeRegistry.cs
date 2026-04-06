namespace Cephalon.Behaviors.Services;

/// <summary>
/// Default mutable implementation of <see cref="IBehaviorTypeRegistry" />.
/// Populated during DI setup by <c>BehaviorCollectionBuilder.Register&lt;TBehavior&gt;()</c>
/// and consumed by <see cref="BehaviorDispatcher" /> at construction time.
/// </summary>
// NOT thread-safe: startup only
public sealed class BehaviorTypeRegistry : IBehaviorTypeRegistry
{
    private readonly Dictionary<string, Type> _map = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new empty <see cref="BehaviorTypeRegistry" />.
    /// </summary>
    public BehaviorTypeRegistry()
    {
    }

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
    public bool TryGetType(string behaviorId, out Type? behaviorType)
    {
        return _map.TryGetValue(behaviorId, out behaviorType);
    }

    /// <summary>
    /// Registers a mapping from <paramref name="behaviorId" /> to <paramref name="behaviorType" />.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier.</param>
    /// <param name="behaviorType">The concrete behavior implementation type.</param>
    public void Register(string behaviorId, Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorId);
        ArgumentNullException.ThrowIfNull(behaviorType);
        _map[behaviorId] = behaviorType;
    }
}
