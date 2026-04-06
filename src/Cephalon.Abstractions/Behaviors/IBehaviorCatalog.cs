namespace Cephalon.Abstractions.Behaviors;

/// <summary>Provides read access to all registered behavior topology descriptors.</summary>
public interface IBehaviorCatalog
{
    /// <summary>Gets all registered behavior topology descriptors.</summary>
    IReadOnlyList<BehaviorTopologyDescriptor> All { get; }

    /// <summary>Finds a behavior by identifier (case-insensitive), or returns <see langword="null"/> if not found.</summary>
    BehaviorTopologyDescriptor? FindById(string behaviorId);

    /// <summary>Gets all behaviors registered with the specified pattern.</summary>
    IReadOnlyList<BehaviorTopologyDescriptor> GetByPattern(string pattern);

    /// <summary>Gets all behaviors registered with the specified transport.</summary>
    IReadOnlyList<BehaviorTopologyDescriptor> GetByTransport(string transportId);
}
