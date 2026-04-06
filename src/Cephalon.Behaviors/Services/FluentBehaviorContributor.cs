using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Services;

/// <summary>
/// A behavior contributor that carries a topology descriptor built via fluent DI registration.
/// This contributor represents Layer 4 (highest priority) in the topology resolution chain.
/// </summary>
public sealed class FluentBehaviorContributor : IBehaviorContributor
{
    private readonly BehaviorTopologyDescriptor _descriptor;

    /// <summary>
    /// Initializes the contributor with the pre-built topology descriptor.
    /// </summary>
    /// <param name="descriptor">The behavior topology descriptor to contribute.</param>
    public FluentBehaviorContributor(BehaviorTopologyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _descriptor = descriptor;
    }

    /// <summary>
    /// Returns the single behavior topology descriptor carried by this contributor.
    /// </summary>
    /// <returns>The contributed descriptor.</returns>
    public IReadOnlyList<BehaviorTopologyDescriptor> Contribute() => [_descriptor];
}
