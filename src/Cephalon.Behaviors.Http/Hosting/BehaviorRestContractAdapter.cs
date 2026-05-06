using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Services;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class BehaviorRestContractAdapter
{
    internal static BehaviorRestInputContractDescriptor ToRestInputContract(
        BehaviorContractDescriptor contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return new BehaviorRestInputContractDescriptor(
            contract.InputType,
            contract.InputIsScalar,
            contract.InputProperties
                .Select(static property => new BehaviorRestInputPropertyDescriptor(property.Name, property.Type))
                .ToArray());
    }
}
