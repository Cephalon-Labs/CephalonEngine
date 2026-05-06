using System.Collections.Concurrent;
using System.Reflection;

namespace Cephalon.Behaviors.Services;

/// <summary>
/// Stores source-generated or explicitly registered behavior contract descriptors by assembly.
/// </summary>
/// <remarks>
/// The behavior source generator registers descriptors through a module initializer. Advanced
/// hosts and tests can register equivalent descriptors explicitly, giving transport adapters a
/// deterministic contract source without scanning behavior types.
/// </remarks>
public static class BehaviorContractRegistry
{
    private static readonly IReadOnlyList<BehaviorContractDescriptor> EmptyContracts =
        Array.Empty<BehaviorContractDescriptor>();

    private static readonly ConcurrentDictionary<Assembly, IReadOnlyList<BehaviorContractDescriptor>> Registrations = new();

    /// <summary>
    /// Registers or merges behavior contract descriptors for a loaded assembly.
    /// </summary>
    /// <param name="assembly">The assembly that owns the descriptors.</param>
    /// <param name="contracts">The contract descriptors to register.</param>
    public static void Register(
        Assembly assembly,
        IReadOnlyList<BehaviorContractDescriptor> contracts)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(contracts);

        var incoming = contracts.ToArray();
        Registrations.AddOrUpdate(
            assembly,
            incoming,
            (_, existing) => Merge(existing, incoming));
    }

    /// <summary>
    /// Attempts to resolve behavior contract descriptors for the supplied assembly.
    /// </summary>
    /// <param name="assembly">The assembly that may have registered contract descriptors.</param>
    /// <param name="contracts">When found, the registered contract descriptors.</param>
    /// <returns><see langword="true" /> when descriptors were registered for the assembly.</returns>
    public static bool TryGetContracts(
        Assembly assembly,
        out IReadOnlyList<BehaviorContractDescriptor> contracts)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (Registrations.TryGetValue(assembly, out var registration))
        {
            contracts = registration;
            return true;
        }

        contracts = EmptyContracts;
        return false;
    }

    private static BehaviorContractDescriptor[] Merge(
        IReadOnlyList<BehaviorContractDescriptor> existing,
        IReadOnlyList<BehaviorContractDescriptor> incoming)
    {
        return existing
            .Concat(incoming)
            .GroupBy(static descriptor => descriptor.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.Last())
            .ToArray();
    }
}
