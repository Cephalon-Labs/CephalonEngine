using System.Collections.Concurrent;
using System.Reflection;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Http.Abstractions;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class BehaviorRestProfileResolver
{
    private static readonly IReadOnlyDictionary<string, BehaviorRestProfileDescriptor> EmptyProfiles =
        new Dictionary<string, BehaviorRestProfileDescriptor>(StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlyDictionary<string, Type> EmptyBehaviorTypes =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<Assembly, IReadOnlyDictionary<string, BehaviorRestProfileDescriptor>> Cache = new();
    private static readonly ConcurrentDictionary<Assembly, IReadOnlyDictionary<string, Type>> BehaviorTypeCache = new();

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

    internal static IReadOnlyList<ResolvedBehaviorRestProfile> ResolveGeneratedProfiles(
        Assembly assembly,
        string behaviorIdPrefix)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorIdPrefix);

        var normalizedPrefix = behaviorIdPrefix.Trim();
        var generatedProfiles = Cache.GetOrAdd(assembly, BuildGeneratedProfiles);
        if (generatedProfiles.Count == 0)
        {
            return ScanAssemblyProfiles(assembly, normalizedPrefix);
        }

        var behaviorTypes = BehaviorTypeCache.GetOrAdd(assembly, BuildGeneratedProfileBehaviorTypes);
        if (behaviorTypes.Count == 0)
        {
            throw new InvalidOperationException(
                $"Assembly '{assembly.FullName}' exposes generated REST profile hints, but it does not expose generated behavior-type hints required by MapGeneratedProfiles(). Rebuild the assembly with the current Cephalon.Behaviors.SourceGen package or use explicit MapProfile<TBehavior>() mappings.");
        }

        var sourceIdentity = assembly.FullName ?? assembly.GetName().Name ?? assembly.ToString();
        var matchedProfiles = generatedProfiles.Values
            .Where(profile => BehaviorIdMatchesPrefix(profile.BehaviorId, normalizedPrefix))
            .OrderBy(static profile => profile.BehaviorId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (matchedProfiles.Length == 0)
        {
            return [];
        }

        var resolvedProfiles = new List<ResolvedBehaviorRestProfile>(matchedProfiles.Length);
        foreach (var profile in matchedProfiles)
        {
            if (!behaviorTypes.TryGetValue(profile.BehaviorId, out var behaviorType))
            {
                throw new InvalidOperationException(
                    $"Assembly '{assembly.FullName}' exposes a generated REST profile hint for behavior '{profile.BehaviorId}', but it is missing the matching generated behavior-type hint required by MapGeneratedProfiles().");
            }

            resolvedProfiles.Add(new ResolvedBehaviorRestProfile(
                behaviorType,
                Normalize(profile, sourceIdentity, behaviorType)));
        }

        return resolvedProfiles;
    }

    private static ResolvedBehaviorRestProfile[] ScanAssemblyProfiles(
        Assembly assembly,
        string behaviorIdPrefix)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorIdPrefix);

        Type[] behaviorTypes;
        try
        {
            behaviorTypes = assembly.DefinedTypes
                .Select(static typeInfo => typeInfo.AsType())
                .ToArray();
        }
        catch (ReflectionTypeLoadException exception)
        {
            behaviorTypes = exception.Types
                .Where(static type => type is not null)
                .Cast<Type>()
                .ToArray();
        }

        return behaviorTypes
            .Where(static behaviorType =>
                behaviorType.IsClass &&
                !behaviorType.IsAbstract &&
                behaviorType.GetCustomAttribute<AppBehaviorAttribute>(inherit: false) is not null &&
                behaviorType.GetCustomAttribute<BehaviorRestProfileAttribute>(inherit: false) is not null)
            .Select(behaviorType => new ResolvedBehaviorRestProfile(
                behaviorType,
                Resolve(behaviorType)))
            .Where(resolved => BehaviorIdMatchesPrefix(resolved.Profile.BehaviorId, behaviorIdPrefix))
            .OrderBy(static resolved => resolved.Profile.BehaviorId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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

    private static IReadOnlyDictionary<string, Type> BuildGeneratedProfileBehaviorTypes(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var marker = assembly.GetCustomAttribute<ContainsBehaviorsAttribute>();
        var registrationType = marker?.RegistrationType;
        if (registrationType is null)
        {
            return EmptyBehaviorTypes;
        }

        var method = registrationType.GetMethod(
            "GetRestProfileBehaviorTypes",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);
        if (method?.Invoke(null, null) is not IReadOnlyList<(string Id, Type Type)> behaviorTypes ||
            behaviorTypes.Count == 0)
        {
            return EmptyBehaviorTypes;
        }

        var resolved = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, behaviorType) in behaviorTypes)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidOperationException(
                    $"Assembly '{assembly.FullName}' produced a generated REST profile behavior-type hint with an empty behavior id.");
            }

            if (behaviorType is null)
            {
                throw new InvalidOperationException(
                    $"Assembly '{assembly.FullName}' produced a generated REST profile behavior-type hint for behavior '{id}' without a concrete behavior type.");
            }

            if (!resolved.TryAdd(id.Trim(), behaviorType))
            {
                throw new InvalidOperationException(
                    $"Assembly '{assembly.FullName}' produced duplicate generated REST profile behavior-type hints for behavior '{id}'.");
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
                normalizedPattern,
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
        string relativePattern,
        string sourceIdentity,
        string behaviorId)
    {
        if (bindings is null || bindings.Count == 0)
        {
            return [];
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(relativePattern);

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

        var routeParameters = RoutePatternFactory.Parse(relativePattern)
            .Parameters
            .Select(static parameter => parameter.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
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

            var effectiveBindingName = string.IsNullOrWhiteSpace(binding.Name)
                ? property.Name
                : binding.Name.Trim();
            if (binding.Source == BehaviorRestBindingSource.Route &&
                !routeParameters.Contains(effectiveBindingName))
            {
                throw new InvalidOperationException(
                    $"REST profile metadata for behavior '{behaviorId}' from '{sourceIdentity}' declares a route binding for input property '{property.Name}' using placeholder '{effectiveBindingName}', but route pattern '{relativePattern}' does not declare that placeholder.");
            }

            normalized.Add(
                property.Name,
                new BehaviorRestBindingDescriptor(
                    property.Name,
                    binding.Source,
                    effectiveBindingName));
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

    private static bool BehaviorIdMatchesPrefix(string behaviorId, string behaviorIdPrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorIdPrefix);

        return behaviorId.Equals(behaviorIdPrefix, StringComparison.OrdinalIgnoreCase) ||
               behaviorId.StartsWith($"{behaviorIdPrefix}.", StringComparison.OrdinalIgnoreCase);
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

internal sealed record ResolvedBehaviorRestProfile(
    Type BehaviorType,
    BehaviorRestProfileDescriptor Profile);
