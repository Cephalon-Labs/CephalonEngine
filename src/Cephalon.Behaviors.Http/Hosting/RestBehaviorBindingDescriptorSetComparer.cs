using Cephalon.Abstractions.Transports;
using Cephalon.Behaviors.Http.Abstractions;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class RestBehaviorBindingDescriptorSetComparer
{
    internal static bool Equivalent(
        IReadOnlyList<BehaviorRestBindingDescriptor> left,
        IReadOnlyList<BehaviorRestBindingDescriptor> right)
    {
        return EquivalentCore(
            left,
            right,
            static binding => binding.PropertyName,
            static binding => (int)binding.Source,
            static binding => binding.Name);
    }

    internal static bool Equivalent(
        IReadOnlyList<RestEndpointBindingDescriptor> left,
        IReadOnlyList<RestEndpointBindingDescriptor> right)
    {
        return EquivalentCore(
            left,
            right,
            static binding => binding.PropertyName,
            static binding => (int)binding.Source,
            static binding => binding.Name);
    }

    private static bool EquivalentCore<TDescriptor>(
        IReadOnlyList<TDescriptor> left,
        IReadOnlyList<TDescriptor> right,
        Func<TDescriptor, string> propertyNameSelector,
        Func<TDescriptor, int> sourceSelector,
        Func<TDescriptor, string?> nameSelector)
        where TDescriptor : class
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(propertyNameSelector);
        ArgumentNullException.ThrowIfNull(sourceSelector);
        ArgumentNullException.ThrowIfNull(nameSelector);

        if (left.Count != right.Count)
        {
            return false;
        }

        if (left.Count == 0)
        {
            return true;
        }

        var leftByProperty = new Dictionary<string, (int Source, string? Name)>(StringComparer.OrdinalIgnoreCase);
        foreach (var descriptor in left)
        {
            if (descriptor is null)
            {
                return false;
            }

            var propertyName = NormalizePropertyName(propertyNameSelector(descriptor));
            if (propertyName is null)
            {
                return false;
            }

            if (!leftByProperty.TryAdd(
                    propertyName,
                    (sourceSelector(descriptor), NormalizeBindingName(nameSelector(descriptor)))))
            {
                return false;
            }
        }

        foreach (var descriptor in right)
        {
            if (descriptor is null)
            {
                return false;
            }

            var propertyName = NormalizePropertyName(propertyNameSelector(descriptor));
            if (propertyName is null ||
                !leftByProperty.TryGetValue(propertyName, out var leftDescriptor) ||
                leftDescriptor.Source != sourceSelector(descriptor) ||
                !string.Equals(
                    leftDescriptor.Name,
                    NormalizeBindingName(nameSelector(descriptor)),
                    StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static string? NormalizePropertyName(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string? NormalizeBindingName(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
