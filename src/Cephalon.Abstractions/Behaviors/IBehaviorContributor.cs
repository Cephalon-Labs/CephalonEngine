namespace Cephalon.Abstractions.Behaviors;

/// <summary>Contributes behavior topology descriptors to the engine registry at startup.</summary>
public interface IBehaviorContributor
{
    /// <summary>Registers behaviors into the provided registry.</summary>
    void RegisterBehaviors(IBehaviorRegistry registry);
}
