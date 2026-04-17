using System.Reflection;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Http.Abstractions;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class BehaviorRestBindingPlanNormalizer
{
    internal static IReadOnlyList<BehaviorRestBindingDescriptor> Normalize(
        string sourceLabel,
        Type behaviorType,
        RestBehaviorHttpMethod method,
        string pattern,
        IReadOnlyList<BehaviorRestBindingDescriptor> bindings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLabel);
        ArgumentNullException.ThrowIfNull(behaviorType);

        var contractInterface = behaviorType.GetInterfaces()
            .FirstOrDefault(static candidate =>
                candidate.IsGenericType &&
                candidate.GetGenericTypeDefinition() == typeof(IAppBehavior<,>))
            ?? throw new InvalidOperationException(
                $"Behavior type '{behaviorType.FullName}' does not implement IAppBehavior<TInput, TOutput>.");

        return NormalizeForInputType(
            sourceLabel,
            contractInterface.GetGenericArguments()[0],
            method,
            pattern,
            bindings);
    }

    internal static IReadOnlyList<BehaviorRestBindingDescriptor> NormalizeForInputType(
        string sourceLabel,
        Type inputType,
        RestBehaviorHttpMethod method,
        string pattern,
        IReadOnlyList<BehaviorRestBindingDescriptor> bindings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLabel);
        ArgumentNullException.ThrowIfNull(inputType);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(bindings);

        if (bindings.Count == 0)
        {
            return [];
        }

        var effectiveInputType = Nullable.GetUnderlyingType(inputType) ?? inputType;
        if (IsSimpleInputType(effectiveInputType))
        {
            throw new InvalidOperationException(
                $"{sourceLabel} declares explicit REST bindings, but input type '{effectiveInputType.FullName}' is scalar. Explicit REST bindings currently require an object input.");
        }

        var routeParameters = RoutePatternFactory.Parse(pattern)
            .Parameters
            .Select(static parameter => parameter.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var inputProperties = effectiveInputType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.CanRead)
            .Select(static property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<BehaviorRestBindingDescriptor>(bindings.Count);
        var seenProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var acceptsBody = method is RestBehaviorHttpMethod.Post or RestBehaviorHttpMethod.Put or RestBehaviorHttpMethod.Patch;

        foreach (var binding in bindings)
        {
            if (binding is null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(binding.PropertyName))
            {
                throw new InvalidOperationException(
                    $"{sourceLabel} declares an explicit REST binding with an empty property name.");
            }

            var propertyName = binding.PropertyName.Trim();
            if (!seenProperties.Add(propertyName))
            {
                throw new InvalidOperationException(
                    $"{sourceLabel} declares more than one explicit REST binding for input property '{propertyName}'.");
            }

            if (!inputProperties.Contains(propertyName))
            {
                throw new InvalidOperationException(
                    $"{sourceLabel} declares an explicit REST binding for input property '{propertyName}', but '{effectiveInputType.FullName}' does not expose a matching public property.");
            }

            if (!Enum.IsDefined(binding.Source) || binding.Source == BehaviorRestBindingSource.Unspecified)
            {
                throw new InvalidOperationException(
                    $"{sourceLabel} declares an explicit REST binding for input property '{propertyName}' without a supported source. {BehaviorRestWireNameDiagnostics.DescribeBindingSourceSupport()}");
            }

            var sourceName = string.IsNullOrWhiteSpace(binding.Name)
                ? null
                : binding.Name.Trim();
            var effectiveSourceName = sourceName ?? propertyName;

            if (binding.Source == BehaviorRestBindingSource.Route &&
                !routeParameters.Contains(effectiveSourceName))
            {
                throw new InvalidOperationException(
                    $"{sourceLabel} declares a route binding for input property '{propertyName}' using placeholder '{effectiveSourceName}', but route pattern '{pattern}' does not declare that placeholder.");
            }

            if (binding.Source == BehaviorRestBindingSource.Body && !acceptsBody)
            {
                throw new InvalidOperationException(
                    $"{sourceLabel} declares a body binding for input property '{propertyName}', but REST method '{BehaviorRestWireNameDiagnostics.GetWireName(method)}' does not accept a request body. {BehaviorRestWireNameDiagnostics.DescribeMethodSupport()}");
            }

            normalized.Add(new BehaviorRestBindingDescriptor(propertyName, binding.Source, sourceName));
        }

        return normalized;
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
