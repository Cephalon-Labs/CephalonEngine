using Cephalon.Abstractions.Transports;
using Cephalon.Behaviors.Http.Abstractions;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class RestEndpointBindingDescriptorAdapter
{
    internal static RestEndpointBindingDescriptor[] ToRuntimeDescriptors(
        IReadOnlyList<BehaviorRestBindingDescriptor> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        if (bindings.Count == 0)
        {
            return [];
        }

        return bindings
            .Select(static binding => new RestEndpointBindingDescriptor(
                binding.PropertyName,
                ToRuntimeSource(binding.Source),
                binding.Name))
            .ToArray();
    }

    internal static BehaviorRestBindingDescriptor[] ToBehaviorDescriptors(
        IReadOnlyList<RestEndpointBindingDescriptor> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        if (bindings.Count == 0)
        {
            return [];
        }

        return bindings
            .Select(static binding => new BehaviorRestBindingDescriptor(
                binding.PropertyName,
                ToBehaviorSource(binding.Source),
                binding.Name))
            .ToArray();
    }

    internal static RestEndpointBindingSource ToRuntimeSource(BehaviorRestBindingSource source)
    {
        return source switch
        {
            BehaviorRestBindingSource.Route => RestEndpointBindingSource.Route,
            BehaviorRestBindingSource.Query => RestEndpointBindingSource.Query,
            BehaviorRestBindingSource.Header => RestEndpointBindingSource.Header,
            BehaviorRestBindingSource.Body => RestEndpointBindingSource.Body,
            _ => throw new InvalidOperationException(
                $"Unsupported behavior REST binding source '{source}'.")
        };
    }

    internal static BehaviorRestBindingSource ToBehaviorSource(RestEndpointBindingSource source)
    {
        return source switch
        {
            RestEndpointBindingSource.Route => BehaviorRestBindingSource.Route,
            RestEndpointBindingSource.Query => BehaviorRestBindingSource.Query,
            RestEndpointBindingSource.Header => BehaviorRestBindingSource.Header,
            RestEndpointBindingSource.Body => BehaviorRestBindingSource.Body,
            _ => throw new InvalidOperationException(
                $"Unsupported REST endpoint binding source '{source}'.")
        };
    }
}
