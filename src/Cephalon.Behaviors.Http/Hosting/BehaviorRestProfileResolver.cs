using System.Collections.Concurrent;
using System.Reflection;
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

        var assembly = behaviorType.Assembly;
        var generatedProfiles = Cache.GetOrAdd(assembly, BuildGeneratedProfiles);
        if (generatedProfiles.Count == 0)
        {
            throw new InvalidOperationException(
                $"Cannot map '{behaviorType.FullName}' through MapProfile<TBehavior>() because assembly '{assembly.FullName}' does not expose generated REST profile hints. Rebuild the assembly with the current Cephalon.Behaviors.SourceGen package or use explicit MapGet<TBehavior>() / MapPost<TBehavior>() / MapPut<TBehavior>() / MapPatch<TBehavior>() / MapDelete<TBehavior>() mappings.");
        }

        var behaviorTypes = BehaviorTypeCache.GetOrAdd(assembly, BuildGeneratedProfileBehaviorTypes);
        if (behaviorTypes.Count == 0)
        {
            throw new InvalidOperationException(
                $"Cannot map '{behaviorType.FullName}' through MapProfile<TBehavior>() because assembly '{assembly.FullName}' exposes generated REST profile hints without generated behavior-type hints. Rebuild the assembly with the current Cephalon.Behaviors.SourceGen package or use explicit MapGet<TBehavior>() / MapPost<TBehavior>() / MapPut<TBehavior>() / MapPatch<TBehavior>() / MapDelete<TBehavior>() mappings.");
        }

        var behaviorIds = behaviorTypes
            .Where(pair => pair.Value == behaviorType)
            .Select(static pair => pair.Key)
            .ToArray();
        if (behaviorIds.Length == 0)
        {
            throw new InvalidOperationException(
                $"Cannot map '{behaviorType.FullName}' through MapProfile<TBehavior>() because assembly '{assembly.FullName}' does not expose a generated REST profile behavior-type hint for that behavior. Rebuild the assembly with the current Cephalon.Behaviors.SourceGen package or use explicit MapGet<TBehavior>() / MapPost<TBehavior>() / MapPut<TBehavior>() / MapPatch<TBehavior>() / MapDelete<TBehavior>() mappings.");
        }

        if (behaviorIds.Length > 1)
        {
            throw new InvalidOperationException(
                $"Cannot map '{behaviorType.FullName}' through MapProfile<TBehavior>() because assembly '{assembly.FullName}' exposes multiple generated REST profile behavior-type hints for that behavior type: {string.Join(", ", behaviorIds)}.");
        }

        if (!generatedProfiles.TryGetValue(behaviorIds[0], out var profile))
        {
            throw new InvalidOperationException(
                $"Cannot map '{behaviorType.FullName}' through MapProfile<TBehavior>() because assembly '{assembly.FullName}' exposes a generated behavior-type hint for behavior '{behaviorIds[0]}' without the matching generated REST profile descriptor.");
        }

        return Normalize(
            profile,
            assembly.FullName ?? assembly.GetName().Name ?? assembly.ToString(),
            validateBindings: true);
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
            throw new InvalidOperationException(
                $"Assembly '{assembly.FullName}' does not expose generated REST profile hints required by MapGeneratedProfiles(). Rebuild the assembly with the current Cephalon.Behaviors.SourceGen package or use explicit MapGet<TBehavior>() / MapPost<TBehavior>() / MapPut<TBehavior>() / MapPatch<TBehavior>() / MapDelete<TBehavior>() mappings.");
        }

        var behaviorTypes = BehaviorTypeCache.GetOrAdd(assembly, BuildGeneratedProfileBehaviorTypes);
        if (behaviorTypes.Count == 0)
        {
            throw new InvalidOperationException(
                $"Assembly '{assembly.FullName}' exposes generated REST profile hints, but it does not expose generated behavior-type hints required by MapGeneratedProfiles(). Rebuild the assembly with the current Cephalon.Behaviors.SourceGen package or use explicit MapGet<TBehavior>() / MapPost<TBehavior>() / MapPut<TBehavior>() / MapPatch<TBehavior>() / MapDelete<TBehavior>() mappings.");
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
                Normalize(profile, sourceIdentity, validateBindings: true)));
        }

        return resolvedProfiles;
    }

    private static IReadOnlyDictionary<string, BehaviorRestProfileDescriptor> BuildGeneratedProfiles(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (!BehaviorRestGeneratedProfileRegistry.TryGetProfiles(assembly, out var profiles) ||
            profiles.Count == 0)
        {
            return EmptyProfiles;
        }

        var resolved = new Dictionary<string, BehaviorRestProfileDescriptor>(StringComparer.OrdinalIgnoreCase);
        var sourceIdentity = assembly.FullName ?? assembly.GetName().Name ?? assembly.ToString();
        foreach (var profile in profiles)
        {
            var normalized = Normalize(profile, sourceIdentity);
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

        if (!BehaviorRestGeneratedProfileRegistry.TryGetBehaviorTypes(assembly, out var behaviorTypes) ||
            behaviorTypes.Count == 0)
        {
            return EmptyBehaviorTypes;
        }

        var resolved = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        foreach (var descriptor in behaviorTypes)
        {
            var id = descriptor.Id;
            var behaviorType = descriptor.Type;
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
        bool validateBindings = false)
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
                $"REST profile metadata for behavior '{descriptor.BehaviorId}' from '{sourceIdentity}' is missing a supported HTTP method. {BehaviorRestWireNameDiagnostics.DescribeMethodSupport()}");
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

        EnsureValidRoutePattern(normalizedPattern, descriptor.BehaviorId, sourceIdentity);

        if (descriptor.ApiVersionMajor.HasValue && descriptor.ApiVersionMajor.Value <= 0)
        {
            throw new InvalidOperationException(
                $"REST profile metadata for behavior '{descriptor.BehaviorId}' from '{sourceIdentity}' must use a positive API major version when one is specified.");
        }

        var normalizedInputContract = NormalizeInputContract(
            descriptor.InputContract,
            sourceIdentity,
            descriptor.BehaviorId);
        var normalizedBindings = !validateBindings
            ? NormalizeBindings(descriptor.Bindings)
            : NormalizeBindings(
                descriptor.Bindings,
                normalizedInputContract,
                descriptor.Method,
                normalizedPattern,
                sourceIdentity,
                descriptor.BehaviorId);
        if (descriptor.PreserveImplicitQueryFallback &&
            normalizedBindings.Length == 0)
        {
            throw new InvalidOperationException(
                $"REST profile metadata for behavior '{descriptor.BehaviorId}' from '{sourceIdentity}' sets PreserveImplicitQueryFallback, but preserved implicit query fallback requires at least one explicit binding descriptor.");
        }

        return new BehaviorRestProfileDescriptor(
            descriptor.BehaviorId.Trim(),
            descriptor.Method,
            normalizedPattern,
            descriptor.ApiVersionMajor,
            normalizedBindings,
            descriptor.PreserveImplicitQueryFallback)
        {
            InputContract = normalizedInputContract
        };
    }

    private static void EnsureValidRoutePattern(
        string relativePattern,
        string behaviorId,
        string sourceIdentity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePattern);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceIdentity);

        try
        {
            _ = RoutePatternFactory.Parse(relativePattern);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"REST profile metadata for behavior '{behaviorId}' from '{sourceIdentity}' must use a valid ASP.NET Core route pattern. Invalid pattern: '{relativePattern}'.",
                exception);
        }
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
        BehaviorRestInputContractDescriptor? inputContract,
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

        if (inputContract is null)
        {
            throw new InvalidOperationException(
                $"REST profile metadata for behavior '{behaviorId}' from '{sourceIdentity}' declares explicit input bindings without an input contract descriptor. Rebuild the assembly with the current Cephalon.Behaviors.SourceGen package or register BehaviorRestInputContractDescriptor metadata explicitly.");
        }

        if (inputContract.IsScalar)
        {
            throw new InvalidOperationException(
                $"REST profile metadata for behavior '{behaviorId}' from '{sourceIdentity}' cannot declare explicit input bindings because '{inputContract.InputType.FullName ?? inputContract.InputType.Name}' is a scalar input type.");
        }

        var inputProperties = (inputContract.Properties ?? Array.Empty<BehaviorRestInputPropertyDescriptor>())
            .ToDictionary(static property => property.Name, StringComparer.OrdinalIgnoreCase);
        if (inputProperties.Count == 0)
        {
            throw new InvalidOperationException(
                $"REST profile metadata for behavior '{behaviorId}' from '{sourceIdentity}' cannot declare explicit input bindings because '{inputContract.InputType.FullName ?? inputContract.InputType.Name}' does not expose public input properties.");
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
                    $"REST profile metadata for behavior '{behaviorId}' from '{sourceIdentity}' declares an explicit binding for '{binding.PropertyName}' without a supported binding source. {BehaviorRestWireNameDiagnostics.DescribeBindingSourceSupport()}");
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
                    $"REST profile metadata for behavior '{behaviorId}' from '{sourceIdentity}' cannot bind input property '{property.Name}' from the body for '{BehaviorRestWireNameDiagnostics.GetWireName(method)}' endpoints. {BehaviorRestWireNameDiagnostics.DescribeMethodSupport()}");
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

    private static bool BehaviorIdMatchesPrefix(string behaviorId, string behaviorIdPrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorIdPrefix);

        return behaviorId.Equals(behaviorIdPrefix, StringComparison.OrdinalIgnoreCase) ||
               behaviorId.StartsWith($"{behaviorIdPrefix}.", StringComparison.OrdinalIgnoreCase);
    }

    private static BehaviorRestInputContractDescriptor? NormalizeInputContract(
        BehaviorRestInputContractDescriptor? inputContract,
        string sourceIdentity,
        string behaviorId)
    {
        if (inputContract is null)
        {
            return null;
        }

        if (inputContract.InputType is null)
        {
            throw new InvalidOperationException(
                $"REST profile metadata for behavior '{behaviorId}' from '{sourceIdentity}' declares an input contract without an input type.");
        }

        var properties = new Dictionary<string, BehaviorRestInputPropertyDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in inputContract.Properties ?? Array.Empty<BehaviorRestInputPropertyDescriptor>())
        {
            if (string.IsNullOrWhiteSpace(property.Name))
            {
                throw new InvalidOperationException(
                    $"REST profile metadata for behavior '{behaviorId}' from '{sourceIdentity}' declares an input contract property without a name.");
            }

            if (property.Type is null)
            {
                throw new InvalidOperationException(
                    $"REST profile metadata for behavior '{behaviorId}' from '{sourceIdentity}' declares input contract property '{property.Name}' without a property type.");
            }

            var normalizedName = property.Name.Trim();
            if (!properties.TryAdd(
                    normalizedName,
                    new BehaviorRestInputPropertyDescriptor(normalizedName, property.Type)))
            {
                throw new InvalidOperationException(
                    $"REST profile metadata for behavior '{behaviorId}' from '{sourceIdentity}' declares multiple input contract properties named '{normalizedName}'.");
            }
        }

        return new BehaviorRestInputContractDescriptor(
            inputContract.InputType,
            inputContract.IsScalar,
            properties.Values.ToArray());
    }
}

internal sealed record ResolvedBehaviorRestProfile(
    Type BehaviorType,
    BehaviorRestProfileDescriptor Profile);
