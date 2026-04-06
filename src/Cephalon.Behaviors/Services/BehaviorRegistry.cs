using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Services;

/// <summary>Simple list-backed registry that collects <see cref="BehaviorTopologyDescriptor"/> instances from contributors.</summary>
public sealed class BehaviorRegistry : IBehaviorRegistry
{
    private readonly List<BehaviorTopologyDescriptor> _descriptors = [];

    /// <summary>Initializes a new instance of <see cref="BehaviorRegistry"/>.</summary>
    public BehaviorRegistry() { }

    /// <inheritdoc />
    public void Add(BehaviorTopologyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _descriptors.Add(descriptor);
    }

    /// <summary>Returns a snapshot of all currently registered descriptors.</summary>
    public IReadOnlyList<BehaviorTopologyDescriptor> GetAll() => _descriptors.ToArray();
}
