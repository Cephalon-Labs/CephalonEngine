using System.Collections.Concurrent;
using System.Reflection;

namespace Cephalon.Behaviors.Http.Abstractions;

/// <summary>
/// Stores source-generated REST profile hints for loaded behavior assemblies.
/// </summary>
/// <remarks>
/// The behavior source generator registers profile descriptors through a module initializer.
/// Runtime profile projection can then read the descriptors by assembly without scanning generated
/// carrier methods through reflection.
/// </remarks>
public static class BehaviorRestGeneratedProfileRegistry
{
    private static readonly IReadOnlyList<BehaviorRestProfileDescriptor> EmptyProfiles =
        Array.Empty<BehaviorRestProfileDescriptor>();

    private static readonly IReadOnlyList<BehaviorRestProfileBehaviorTypeDescriptor> EmptyBehaviorTypes =
        Array.Empty<BehaviorRestProfileBehaviorTypeDescriptor>();

    private static readonly ConcurrentDictionary<Assembly, Registration> Registrations = new();

    /// <summary>
    /// Registers generated REST profile descriptors for a loaded behavior assembly.
    /// </summary>
    /// <param name="assembly">The assembly that owns the generated descriptors.</param>
    /// <param name="profiles">The generated REST profile descriptors.</param>
    /// <param name="behaviorTypes">The generated behavior-type descriptors paired with the profiles.</param>
    public static void Register(
        Assembly assembly,
        IReadOnlyList<BehaviorRestProfileDescriptor> profiles,
        IReadOnlyList<BehaviorRestProfileBehaviorTypeDescriptor> behaviorTypes)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(behaviorTypes);

        Registrations[assembly] = new Registration(
            profiles.ToArray(),
            behaviorTypes.ToArray());
    }

    /// <summary>
    /// Attempts to resolve generated REST profile descriptors for the supplied assembly.
    /// </summary>
    /// <param name="assembly">The assembly that may have registered generated REST profiles.</param>
    /// <param name="profiles">When found, the generated REST profile descriptors.</param>
    /// <returns><see langword="true" /> when descriptors were registered for the assembly.</returns>
    public static bool TryGetProfiles(
        Assembly assembly,
        out IReadOnlyList<BehaviorRestProfileDescriptor> profiles)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (Registrations.TryGetValue(assembly, out var registration))
        {
            profiles = registration.Profiles;
            return true;
        }

        profiles = EmptyProfiles;
        return false;
    }

    /// <summary>
    /// Attempts to resolve generated behavior-type descriptors for the supplied assembly.
    /// </summary>
    /// <param name="assembly">The assembly that may have registered generated REST profile behavior types.</param>
    /// <param name="behaviorTypes">When found, the generated behavior-type descriptors.</param>
    /// <returns><see langword="true" /> when descriptors were registered for the assembly.</returns>
    public static bool TryGetBehaviorTypes(
        Assembly assembly,
        out IReadOnlyList<BehaviorRestProfileBehaviorTypeDescriptor> behaviorTypes)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (Registrations.TryGetValue(assembly, out var registration))
        {
            behaviorTypes = registration.BehaviorTypes;
            return true;
        }

        behaviorTypes = EmptyBehaviorTypes;
        return false;
    }

    private sealed record Registration(
        IReadOnlyList<BehaviorRestProfileDescriptor> Profiles,
        IReadOnlyList<BehaviorRestProfileBehaviorTypeDescriptor> BehaviorTypes);
}
