using System.Collections.Concurrent;
using System.Reflection;
using Cephalon.Abstractions.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Behaviors.Services;

/// <summary>
/// Stores source-generated behavior registration hints for loaded behavior assemblies.
/// </summary>
/// <remarks>
/// The behavior source generator registers module descriptors through a module initializer.
/// Runtime auto-registration can then read generated registration, execution-slot, and topology
/// hints by assembly without reflecting over generated carrier methods.
/// </remarks>
public static class BehaviorGeneratedModuleRegistry
{
    private static readonly ConcurrentDictionary<Assembly, BehaviorGeneratedModuleRegistration> Registrations = new();

    /// <summary>
    /// Registers source-generated behavior module hints for a loaded assembly.
    /// </summary>
    /// <param name="assembly">The assembly that owns the generated behavior hints.</param>
    /// <param name="registration">The generated behavior module registration.</param>
    public static void Register(
        Assembly assembly,
        BehaviorGeneratedModuleRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(registration);

        Registrations[assembly] = registration;
    }

    /// <summary>
    /// Attempts to resolve generated behavior module hints for an assembly.
    /// </summary>
    /// <param name="assembly">The assembly that may have registered generated behavior hints.</param>
    /// <param name="registration">When found, the generated behavior module registration.</param>
    /// <returns><see langword="true" /> when generated hints were registered for the assembly.</returns>
    public static bool TryGetRegistration(
        Assembly assembly,
        out BehaviorGeneratedModuleRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return Registrations.TryGetValue(assembly, out registration!);
    }
}

/// <summary>
/// Describes the generated behavior-module hints for one loaded assembly.
/// </summary>
public sealed class BehaviorGeneratedModuleRegistration
{
    /// <summary>
    /// Initializes a new generated behavior-module registration.
    /// </summary>
    /// <param name="registerBehaviors">Registers generated behavior implementations into dependency injection.</param>
    /// <param name="executionSlots">The generated closed-generic execution slots for dispatch startup.</param>
    /// <param name="topologyDescriptors">The generated topology descriptors for behaviors with compile-time topology.</param>
    /// <param name="runtimeTopologyBehaviors">
    /// The generated behavior types that declared topology but could not be reduced to generated descriptors.
    /// </param>
    public BehaviorGeneratedModuleRegistration(
        Action<IServiceCollection> registerBehaviors,
        IReadOnlyList<BehaviorGeneratedExecutionSlotDescriptor> executionSlots,
        IReadOnlyList<BehaviorTopologyDescriptor> topologyDescriptors,
        IReadOnlyList<BehaviorGeneratedRuntimeTopologyDescriptor> runtimeTopologyBehaviors)
    {
        RegisterBehaviors = registerBehaviors ?? throw new ArgumentNullException(nameof(registerBehaviors));
        ExecutionSlots = executionSlots ?? throw new ArgumentNullException(nameof(executionSlots));
        TopologyDescriptors = topologyDescriptors ?? throw new ArgumentNullException(nameof(topologyDescriptors));
        RuntimeTopologyBehaviors = runtimeTopologyBehaviors ?? throw new ArgumentNullException(nameof(runtimeTopologyBehaviors));
    }

    /// <summary>
    /// Gets the generated behavior service-registration callback.
    /// </summary>
    public Action<IServiceCollection> RegisterBehaviors { get; }

    /// <summary>
    /// Gets the generated closed-generic execution slots for dispatch startup.
    /// </summary>
    public IReadOnlyList<BehaviorGeneratedExecutionSlotDescriptor> ExecutionSlots { get; }

    /// <summary>
    /// Gets the generated topology descriptors for behaviors with compile-time topology.
    /// </summary>
    public IReadOnlyList<BehaviorTopologyDescriptor> TopologyDescriptors { get; }

    /// <summary>
    /// Gets the generated behavior types that declared topology but could not be reduced to generated descriptors.
    /// </summary>
    public IReadOnlyList<BehaviorGeneratedRuntimeTopologyDescriptor> RuntimeTopologyBehaviors { get; }
}

/// <summary>
/// Describes a generated closed-generic behavior execution slot.
/// </summary>
public sealed class BehaviorGeneratedExecutionSlotDescriptor
{
    /// <summary>
    /// Initializes a new generated execution-slot descriptor.
    /// </summary>
    /// <param name="id">The stable behavior identifier.</param>
    /// <param name="type">The concrete behavior implementation type.</param>
    /// <param name="slot">The generated execution slot for the behavior.</param>
    public BehaviorGeneratedExecutionSlotDescriptor(
        string id,
        Type type,
        BehaviorExecutionSlot slot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Slot = slot ?? throw new ArgumentNullException(nameof(slot));
    }

    /// <summary>
    /// Gets the stable behavior identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the concrete behavior implementation type.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets the generated execution slot for the behavior.
    /// </summary>
    public BehaviorExecutionSlot Slot { get; }
}

/// <summary>
/// Describes a generated behavior type whose topology declaration could not be represented
/// by generated descriptors.
/// </summary>
public sealed class BehaviorGeneratedRuntimeTopologyDescriptor
{
    /// <summary>
    /// Initializes a new generated runtime-topology descriptor.
    /// </summary>
    /// <param name="id">The stable behavior identifier.</param>
    /// <param name="type">The concrete behavior implementation type.</param>
    public BehaviorGeneratedRuntimeTopologyDescriptor(
        string id,
        Type type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>
    /// Gets the stable behavior identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the concrete behavior implementation type.
    /// </summary>
    public Type Type { get; }
}
