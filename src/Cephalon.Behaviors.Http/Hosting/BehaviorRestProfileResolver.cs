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
            return profile;
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
                attribute.ApiVersionMajor > 0 ? attribute.ApiVersionMajor : null),
            behaviorType.FullName ?? behaviorType.Name);
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
        string sourceIdentity)
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

        return new BehaviorRestProfileDescriptor(
            descriptor.BehaviorId.Trim(),
            descriptor.Method,
            normalizedPattern,
            descriptor.ApiVersionMajor);
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
}
