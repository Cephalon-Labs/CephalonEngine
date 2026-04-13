using System.Collections.Concurrent;
using System.Reflection;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Http.Abstractions;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class BehaviorRestProfileResolver
{
    private static readonly IReadOnlyDictionary<string, BehaviorRestProfileDescriptor> EmptyProfiles =
        new Dictionary<string, BehaviorRestProfileDescriptor>(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<Assembly, IReadOnlyDictionary<string, BehaviorRestProfileDescriptor>> Cache = new();

    internal static BehaviorRestProfileDescriptor Resolve<TBehavior>()
        where TBehavior : class
        => Resolve(typeof(TBehavior));

    internal static BehaviorRestProfileDescriptor Resolve(Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        var behaviorId = ResolveBehaviorId(behaviorType);
        var generatedProfiles = Cache.GetOrAdd(behaviorType.Assembly, BuildGeneratedProfiles);
        if (generatedProfiles.TryGetValue(behaviorId, out var profile))
        {
            return Normalize(
                profile,
                behaviorType.Assembly.FullName ?? behaviorType.Assembly.GetName().Name ?? behaviorType.Assembly.ToString(),
                behaviorType);
        }

        var attribute = behaviorType.GetCustomAttributes(typeof(BehaviorRestProfileAttribute), inherit: false)
            .OfType<BehaviorRestProfileAttribute>()
            .SingleOrDefault();

        if (attribute is null)
        {
            throw new InvalidOperationException(
                $"Cannot map '{behaviorType.FullName}' through MapProfile<TBehavior>() because it does not declare [BehaviorRestProfile(...)].");
        }

        return Normalize(
            new BehaviorRestProfileDescriptor(
                behaviorId,
                attribute.Method,
                attribute.RelativePattern,
                attribute.ApiVersionMajor > 0 ? attribute.ApiVersionMajor : null,
                ExtractAttributeBindings(behaviorType)),
            behaviorType.FullName ?? behaviorType.Name,
            behaviorType);
    }

    private static IReadOnlyDictionary<string, BehaviorRestProfileDescriptor> BuildGeneratedProfiles(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var marker = assembly.GetCustomAttribute<ContainsBehaviorsAttribute>();
        var registrationType = marker?.RegistrationType;
        if (registrationType is null)
        {
            return EmptyProfiles;
        }

        var method = registrationType.GetMethod(
            "GetRestProfiles",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);
        if (method?.Invoke(null, null) is not IReadOnlyList<BehaviorRestProfileDescriptor> profiles ||
            profiles.Count == 0)
        {
            return EmptyProfiles;
        }

        var resolved = new Dictionary<string, BehaviorRestProfileDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in profiles)
        {
            var normalized = Normalize(profile, registrationType.FullName ?? registrationType.Name);
            if (!resolved.TryAdd(normalized.BehaviorId, normalized))
            {
                throw new InvalidOperationException(
                    $"Assembly '{assembly.FullName}' produced duplicate REST profile hints for behavior '{normalized.BehaviorId}'.");
            }
        }

        return resolved;
    }

    private static BehaviorRestProfileDescriptor Normalize(
        BehaviorRestProfileDescriptor descriptor,
        string sourceIdentity,
        Type? behaviorType = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceIdentity);

        if (string.IsNullOrWhiteSpace(descriptor.BehaviorId))
        {
            throw new InvalidOperationException(
                $"REST profile metadata from '{sourceIdentity}' is missing a behavior id.");
        }

        if (!Enum.IsDefined(descriptor.Method) ||
            descriptor.Method == BehaviorRestMethod.Unspecified)
        {
            throw new InvalidOperationException(
                $"REST profile metadata for behavior '{descriptor.BehaviorId}' from '{sourceIdentity}' is missing a supported HTTP method.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.RelativePattern))
        {
            throw new InvalidOperationException(
                $"REST profile metadata for behavior '{descriptor.BehaviorId}' from '{sourceIdentity}' is missing a relative route pattern.");
        }

        var normalizedPattern = descriptor.RelativePattern.Trim();
        if (!normalizedPattern.StartsWith('/'))
        {
            throw new InvalidOperationException(
                $"REST profile metadata for behavior '{descriptor.BehaviorId}' from '{sourceIdentity}' must use a relative route pattern that starts with '/'.");
        }

        if (descriptor.ApiVersionMajor.HasValue && descriptor.ApiVersionMajor.Value <= 0)
        {
            throw new InvalidOperationException(
                $"REST profile metadata for behavior '{descriptor.BehaviorId}' from '{sourceIdentity}' must use a positive API major version when one is specified.");
        }

        var normalizedBindings = behaviorType is null
            ? NormalizeBindings(descriptor.Bindings)
            : NormalizeBindings(
                descriptor.Bindings,
                behaviorType,
                descriptor.Method,
                sourceIdentity,
                descriptor.BehaviorId);

        return new BehaviorRestProfileDescriptor(
            descriptor.BehaviorId.Trim(),
            descriptor.Method,
            normalizedPattern,
            descriptor.ApiVersionMajor,
            normalizedBindings);
    }

    private static BehaviorRestBindingDescriptor[] ExtractAttributeBindings(Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        return behaviorType.GetCustomAttributes(typeof(BehaviorRestBindingAttribute), inherit: false)
            .OfType<BehaviorRestBindingAttribute>()
            .Select(static attribute => new BehaviorRestBindingDescriptor(
                attribute.PropertyName,
                attribute.Source,
                attribute.Name))
            .ToArray();
    }

    private static BehaviorRestBindingDescriptor[] NormalizeBindings(
        IReadOnlyList<BehaviorRestBindingDescriptor>? bindings)
    {
        if (bindings is null || bindings.Count == 0)
        {
            return [];
        }

        return bindings
            .Select(static binding => new BehaviorRestBindingDescriptor(
                binding.PropertyName.Trim(),
                binding.Source,
                string.IsNullOrWhiteSpace(binding.Name) ? null : binding.Name.Trim()))
            .ToArray();
    }

    private static BehaviorRestBindingDescriptor[] NormalizeBindings(
        IReadOnlyList<BehaviorRestBindingDescriptor>? bindings,
        Type behaviorType,
        BehaviorRestMethod method,
        string sourceIdentity,
        string behaviorId)
    {
        if (bindings is null || bindings.Count == 0)
        {
            return [];
        }

        var inputType = ResolveInputType(behaviorType);
        if (IsSimpleInputType(inputType))
        {
            throw new InvalidOperationException(
                $"REST profile metadata for behavior '{behaviorId}' from '{sourceIdentity}' cannot declare explicit input bindings because '{inputType.FullName ?? inputType.Name}' is a scalar input type.");
        }

        var inputProperties = ResolveInputProperties(inputType);
        if (inputProperties.Count == 0)
        {
            throw new InvalidOperationException(
                $"REST profile metadata for behavior '{behaviorId}' from '{sourceIdentity}' cannot declare explicit input bindings because '{inputType.FullName ?? inputType.Name}' does not expose public input properties.");
        }

        var normalized = new Dictionary<string, BehaviorRestBindingDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var binding in bindings)
        {
            if (string.IsNullOrWhiteSpace(binding.PropertyName))
            {
                throw new InvalidOperationException(
                    $"REST profile metadata for behavior '{behaviorId}' from '{sourceIdentity}' declares an explicit binding without a target input property name.");
            }

            if (!Enum.IsDefined(binding.Source) || binding.Source == BehaviorRestBindingSource.Unspecified)
            {
                throw new InvalidOperationException(
                    $"REST profile metadata for behavior '{behaviorId}' from '{sourceIdentity}' declares an explicit binding for '{binding.PropertyName}' without a supported binding source.");
            }

            if (!inputProperties.TryGetValue(binding.PropertyName.Trim(), out var property))
            {
                throw new InvalidOperationException(
                    $"REST profile metadata for behavior '{behaviorId}' from '{sourceIdentity}' binds unknown input property '{binding.PropertyName}'.");
            }

            if ((method == BehaviorRestMethod.Get || method == BehaviorRestMethod.Delete) &&
                binding.Source == BehaviorRestBindingSource.Body)
            {
                throw new InvalidOperationException(
                    $"REST profile metadata for behavior '{behaviorId}' from '{sourceIdentity}' cannot bind input property '{property.Name}' from the body for {method} endpoints.");
            }

            if (normalized.ContainsKey(property.Name))
            {
                throw new InvalidOperationException(
                    $"REST profile metadata for behavior '{behaviorId}' from '{sourceIdentity}' declares multiple explicit bindings for input property '{property.Name}'.");
            }

            normalized.Add(
                property.Name,
                new BehaviorRestBindingDescriptor(
                    property.Name,
                    binding.Source,
                    string.IsNullOrWhiteSpace(binding.Name)
                        ? property.Name
                        : binding.Name.Trim()));
        }

        return normalized.Values.ToArray();
    }

    private static string ResolveBehaviorId(Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        return behaviorType.GetCustomAttributes(typeof(AppBehaviorAttribute), inherit: false)
            .OfType<AppBehaviorAttribute>()
            .SingleOrDefault()
            ?.Id
            ?? throw new InvalidOperationException(
                $"Cannot resolve a REST profile for '{behaviorType.FullName}' because it is missing [AppBehavior(id)].");
    }

    private static Type ResolveInputType(Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        return behaviorType.GetInterfaces()
            .FirstOrDefault(static candidate =>
                candidate.IsGenericType &&
                candidate.GetGenericTypeDefinition() == typeof(IAppBehavior<,>))
            ?.GetGenericArguments()[0]
            ?? throw new InvalidOperationException(
                $"Cannot resolve REST profile input bindings for '{behaviorType.FullName}' because it does not implement IAppBehavior<TInput, TOutput>.");
    }

    private static Dictionary<string, PropertyInfo> ResolveInputProperties(Type inputType)
    {
        ArgumentNullException.ThrowIfNull(inputType);

        return inputType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.GetMethod is not null)
            .ToDictionary(static property => property.Name, StringComparer.OrdinalIgnoreCase);
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
