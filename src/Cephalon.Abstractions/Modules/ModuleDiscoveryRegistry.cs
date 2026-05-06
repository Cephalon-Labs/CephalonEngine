using System.Reflection;

namespace Cephalon.Abstractions.Modules;

/// <summary>
/// Stores compile-time module discovery descriptors registered by generated code or explicit module packages.
/// </summary>
/// <remarks>
/// The registry is assembly-scoped so hosts can keep using configuration-driven assembly discovery while
/// the engine consumes a closed descriptor table instead of scanning every type in the assembly.
/// </remarks>
public static class ModuleDiscoveryRegistry
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<Assembly, IReadOnlyList<ModuleDiscoveryDescriptor>> DescriptorsByAssembly = [];

    /// <summary>
    /// Registers module descriptors for an assembly.
    /// </summary>
    /// <param name="assembly">The assembly that owns the module descriptors.</param>
    /// <param name="descriptors">The descriptors to register.</param>
    public static void Register(Assembly assembly, IEnumerable<ModuleDiscoveryDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(descriptors);

        var normalized = descriptors
            .Where(static descriptor => descriptor is not null)
            .Select(descriptor =>
            {
                if (!ReferenceEquals(descriptor.Assembly, assembly))
                {
                    throw new ArgumentException(
                        $"Module discovery descriptor '{descriptor.ModuleType.FullName}' belongs to assembly '{descriptor.Assembly.FullName}', not '{assembly.FullName}'.",
                        nameof(descriptors));
                }

                return descriptor;
            })
            .GroupBy(static descriptor => descriptor.ModuleType)
            .Select(static group => group.Last())
            .OrderBy(static descriptor => descriptor.ModuleType.FullName, StringComparer.Ordinal)
            .ToArray();

        lock (SyncRoot)
        {
            DescriptorsByAssembly[assembly] = DescriptorsByAssembly.TryGetValue(assembly, out var existing)
                ? existing
                    .Concat(normalized)
                    .GroupBy(static descriptor => descriptor.ModuleType)
                    .Select(static group => group.Last())
                    .OrderBy(static descriptor => descriptor.ModuleType.FullName, StringComparer.Ordinal)
                    .ToArray()
                : normalized;
        }
    }

    /// <summary>
    /// Tries to get registered module descriptors for an assembly.
    /// </summary>
    /// <param name="assembly">The assembly whose descriptors should be returned.</param>
    /// <param name="descriptors">The registered descriptors, or an empty list when none were registered.</param>
    /// <returns><see langword="true" /> when descriptors are registered for the assembly; otherwise <see langword="false" />.</returns>
    public static bool TryGetDescriptors(
        Assembly assembly,
        out IReadOnlyList<ModuleDiscoveryDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        lock (SyncRoot)
        {
            if (DescriptorsByAssembly.TryGetValue(assembly, out var registered))
            {
                descriptors = registered;
                return true;
            }
        }

        descriptors = [];
        return false;
    }

    /// <summary>
    /// Gets registered module descriptors for an assembly.
    /// </summary>
    /// <param name="assembly">The assembly whose descriptors should be returned.</param>
    /// <returns>The registered descriptors, or an empty list when none were registered.</returns>
    public static IReadOnlyList<ModuleDiscoveryDescriptor> GetDescriptors(Assembly assembly)
    {
        return TryGetDescriptors(assembly, out var descriptors) ? descriptors : [];
    }
}
