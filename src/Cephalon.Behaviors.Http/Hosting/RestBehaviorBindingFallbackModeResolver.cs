using System.Reflection;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Transports;
using Cephalon.Behaviors.Http.Abstractions;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class RestBehaviorBindingFallbackModeResolver
{
    internal static RestEndpointBindingFallbackMode? ResolveForBehavior(
        Type behaviorType,
        RestBehaviorHttpMethod method,
        string pattern,
        IReadOnlyList<BehaviorRestBindingDescriptor> bindings,
        bool preserveImplicitQueryFallback)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        var contractInterface = behaviorType.GetInterfaces()
            .FirstOrDefault(static candidate =>
                candidate.IsGenericType &&
                candidate.GetGenericTypeDefinition() == typeof(IAppBehavior<,>));
        if (contractInterface is null)
        {
            return null;
        }

        return ResolveForInputType(
            contractInterface.GetGenericArguments()[0],
            method,
            pattern,
            bindings,
            preserveImplicitQueryFallback);
    }

    internal static RestEndpointBindingFallbackMode? ResolveForInputType(
        Type inputType,
        RestBehaviorHttpMethod method,
        string pattern,
        IReadOnlyList<BehaviorRestBindingDescriptor> bindings,
        bool preserveImplicitQueryFallback)
    {
        ArgumentNullException.ThrowIfNull(inputType);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(bindings);

        var effectiveInputType = Nullable.GetUnderlyingType(inputType) ?? inputType;
        if (IsSimpleInputType(effectiveInputType))
        {
            return null;
        }

        var inputProperties = effectiveInputType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.CanRead)
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

    private static bool IsSimpleInputType(Type inputType)
    {
        ArgumentNullException.ThrowIfNull(inputType);

        var type = Nullable.GetUnderlyingType(inputType) ?? inputType;
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(Guid) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(DateOnly) ||
               type == typeof(TimeOnly);
    }
}
