using System.Collections.Concurrent;
using System.Reflection;
using Cephalon.Behaviors.Services;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class BehaviorRestEndpointContractResolver
{
    private static readonly IReadOnlyDictionary<Type, BehaviorContractDescriptor> EmptyContracts =
        new Dictionary<Type, BehaviorContractDescriptor>();

    private static readonly ConcurrentDictionary<Assembly, IReadOnlyDictionary<Type, BehaviorContractDescriptor>> Cache = new();

    internal static BehaviorContractDescriptor Resolve<TBehavior>()
        where TBehavior : class
        => Resolve(typeof(TBehavior));

    internal static BehaviorContractDescriptor Resolve(Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        var contracts = Cache.GetOrAdd(behaviorType.Assembly, BuildContracts);
        if (contracts.Count == 0)
        {
            throw new InvalidOperationException(
                $"Cannot map '{behaviorType.FullName}' as a behavior REST endpoint because assembly '{behaviorType.Assembly.FullName}' does not expose behavior contract descriptors. Rebuild the assembly with the current Cephalon.Behaviors.SourceGen package or register BehaviorContractDescriptor metadata explicitly through BehaviorContractRegistry.");
        }

        if (!contracts.TryGetValue(behaviorType, out var contract))
        {
            throw new InvalidOperationException(
                $"Cannot map '{behaviorType.FullName}' as a behavior REST endpoint because assembly '{behaviorType.Assembly.FullName}' does not expose a behavior contract descriptor for that behavior type. Rebuild the assembly with the current Cephalon.Behaviors.SourceGen package or register BehaviorContractDescriptor metadata explicitly through BehaviorContractRegistry.");
        }

        return contract;
    }

    private static IReadOnlyDictionary<Type, BehaviorContractDescriptor> BuildContracts(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (!BehaviorContractRegistry.TryGetContracts(assembly, out var contracts) ||
            contracts.Count == 0)
        {
            return EmptyContracts;
        }

        var byType = new Dictionary<Type, BehaviorContractDescriptor>();
        foreach (var contract in contracts)
        {
            ValidateContract(assembly, contract);
            if (byType.TryGetValue(contract.BehaviorType, out var existingContract) &&
                !string.Equals(existingContract.Id, contract.Id, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Assembly '{assembly.FullName}' exposes duplicate behavior contract descriptors for type '{contract.BehaviorType.FullName}' with different behavior ids.");
            }

            byType[contract.BehaviorType] = contract;
        }

        return byType;
    }

    private static void ValidateContract(Assembly assembly, BehaviorContractDescriptor contract)
    {
        if (contract is null)
        {
            throw new InvalidOperationException(
                $"Assembly '{assembly.FullName}' exposes a null behavior contract descriptor.");
        }

        if (string.IsNullOrWhiteSpace(contract.Id))
        {
            throw new InvalidOperationException(
                $"Assembly '{assembly.FullName}' exposes a behavior contract descriptor with an empty behavior id.");
        }

        if (contract.BehaviorType is null)
        {
            throw new InvalidOperationException(
                $"Assembly '{assembly.FullName}' exposes behavior contract '{contract.Id}' without a behavior type.");
        }

        if (contract.InputType is null ||
            contract.OutputType is null ||
            contract.ResponseType is null)
        {
            throw new InvalidOperationException(
                $"Assembly '{assembly.FullName}' exposes behavior contract '{contract.Id}' without complete input/output/response type metadata.");
        }

        var inputProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in contract.InputProperties)
        {
            if (property is null ||
                string.IsNullOrWhiteSpace(property.Name) ||
                property.Type is null)
            {
                throw new InvalidOperationException(
                    $"Assembly '{assembly.FullName}' exposes behavior contract '{contract.Id}' with incomplete input property metadata.");
            }

            if (!inputProperties.Add(property.Name.Trim()))
            {
                throw new InvalidOperationException(
                    $"Assembly '{assembly.FullName}' exposes behavior contract '{contract.Id}' with duplicate input property '{property.Name}'.");
            }
        }
    }
}
