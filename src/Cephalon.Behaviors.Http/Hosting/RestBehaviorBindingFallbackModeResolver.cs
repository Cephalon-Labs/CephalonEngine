using Cephalon.Abstractions.Transports;
using Cephalon.Behaviors.Http.Abstractions;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class RestBehaviorBindingFallbackModeResolver
{
    internal static RestEndpointBindingFallbackMode? ResolveForInputContract(
        BehaviorRestInputContractDescriptor inputContract,
        RestBehaviorHttpMethod method,
        string pattern,
        IReadOnlyList<BehaviorRestBindingDescriptor> bindings,
        bool preserveImplicitQueryFallback)
    {
        ArgumentNullException.ThrowIfNull(inputContract);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(bindings);

        if (inputContract.IsScalar)
        {
            return null;
        }

        var inputProperties = (inputContract.Properties ?? Array.Empty<BehaviorRestInputPropertyDescriptor>())
            .Select(static property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (inputProperties.Count == 0)
        {
            return null;
        }

        var explicitlyBoundProperties = bindings
            .Where(static binding => binding is not null && !string.IsNullOrWhiteSpace(binding.PropertyName))
            .Select(static binding => binding.PropertyName.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var routePlaceholders = RoutePatternFactory.Parse(pattern.Trim())
            .Parameters
            .Select(static parameter => parameter.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var hasRemainingImplicitFallbackSurface = inputProperties.Any(propertyName =>
            !explicitlyBoundProperties.Contains(propertyName) &&
            !routePlaceholders.Contains(propertyName));

        if (preserveImplicitQueryFallback &&
            bindings.Count > 0 &&
            hasRemainingImplicitFallbackSurface)
        {
            return RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback;
        }

        if (bindings.Count == 0 ||
            method is not (RestBehaviorHttpMethod.Post or RestBehaviorHttpMethod.Put or RestBehaviorHttpMethod.Patch))
        {
            return null;
        }

        return hasRemainingImplicitFallbackSurface
            ? RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback
            : null;
    }
}
