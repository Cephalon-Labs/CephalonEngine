namespace Cephalon.Abstractions.Behaviors;

/// <summary>Receives behavior topology descriptors from contributors.</summary>
public interface IBehaviorRegistry
{
    /// <summary>Adds a behavior topology descriptor to the registry.</summary>
    void Add(BehaviorTopologyDescriptor descriptor);
}
