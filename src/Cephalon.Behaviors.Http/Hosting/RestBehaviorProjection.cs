using System.Reflection;
using Cephalon.Abstractions.Behaviors;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Http.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.Behaviors.Http.Hosting;

internal sealed record RestBehaviorModuleProjection(
    IReadOnlyList<Action<IBehaviorModuleBuilder>> OwnershipRegistrations,
    IReadOnlyList<RestBehaviorRouteGroupProjection> Groups);

internal sealed record RestBehaviorRouteGroupProjection(
    string Prefix,
    string? TagName,
    string? TagDescription,
    bool HasExplicitTagDescription,
    int? ApiVersionMajor,
    bool HasExplicitApiVersion,
    string? ProfileApiVersionSourceBehaviorId,
    IReadOnlyList<Action<RouteGroupBuilder>> GroupConventions,
    IReadOnlyList<RestBehaviorEndpointProjection> Endpoints);

internal sealed record RestBehaviorEndpointProjection(
    RestBehaviorHttpMethod Method,
    string BehaviorId,
    Type BehaviorType,
    string Pattern,
    IReadOnlyList<BehaviorRestBindingDescriptor> Bindings,
    bool PreserveImplicitQueryFallback,
    string AuthoringStyle,
    Action<RouteHandlerBuilder>? ConfigureEndpoint,
    Func<BehaviorRestEndpointGroup, string, IReadOnlyList<BehaviorRestBindingDescriptor>, bool, Action<RouteHandlerBuilder>?, RouteHandlerBuilder> Map)
{
    private static readonly MethodInfo CreateMapDelegateFactoryMethod =
        typeof(RestBehaviorEndpointProjection).GetMethod(
            nameof(CreateMapDelegateFactory),
            BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Required REST behavior projection factory was not found.");

    internal static RestBehaviorEndpointProjection Create<TBehavior>(
        RestBehaviorHttpMethod method,
        string pattern,
        Action<RouteHandlerBuilder>? configureEndpoint,
        string authoringStyle,
        IReadOnlyList<BehaviorRestBindingDescriptor>? bindings = null,
        bool preserveImplicitQueryFallback = false)
        where TBehavior : class
        => Create(
            typeof(TBehavior),
            method,
            pattern,
            configureEndpoint,
            authoringStyle,
            bindings,
            preserveImplicitQueryFallback);

    internal static RestBehaviorEndpointProjection Create(
        Type behaviorType,
        RestBehaviorHttpMethod method,
        string pattern,
        Action<RouteHandlerBuilder>? configureEndpoint,
        string authoringStyle,
        IReadOnlyList<BehaviorRestBindingDescriptor>? bindings = null,
        bool preserveImplicitQueryFallback = false)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentException.ThrowIfNullOrWhiteSpace(authoringStyle);

        return new RestBehaviorEndpointProjection(
            method,
            ResolveBehaviorId(behaviorType),
            behaviorType,
            pattern.Trim(),
            bindings ?? [],
            preserveImplicitQueryFallback,
            authoringStyle.Trim(),
            configureEndpoint,
            CreateMapDelegate(behaviorType, method));
    }

    internal static RestBehaviorEndpointProjection Create<TBehavior>(
        BehaviorRestProfileDescriptor profile,
        Action<RouteHandlerBuilder>? configureEndpoint)
        where TBehavior : class
        => Create(
            typeof(TBehavior),
            profile,
            configureEndpoint,
            RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle);

    internal static RestBehaviorEndpointProjection Create(
        Type behaviorType,
        BehaviorRestProfileDescriptor profile,
        Action<RouteHandlerBuilder>? configureEndpoint,
        string authoringStyle)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(authoringStyle);

        var behaviorId = ResolveBehaviorId(behaviorType);
        if (!string.Equals(behaviorId, profile.BehaviorId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Resolved REST profile behavior id '{profile.BehaviorId}' does not match behavior type '{behaviorType.FullName}' with id '{behaviorId}'.");
        }

        return Create(
            behaviorType,
            ConvertMethod(profile.Method),
            profile.RelativePattern,
            configureEndpoint,
            authoringStyle,
            profile.Bindings);
    }

    internal RestBehaviorEndpointProjection WithMethod(RestBehaviorHttpMethod method)
    {
        return method == Method
            ? this
            : new RestBehaviorEndpointProjection(
                method,
                BehaviorId,
                BehaviorType,
                Pattern,
                Bindings,
                PreserveImplicitQueryFallback,
                AuthoringStyle,
                ConfigureEndpoint,
                CreateMapDelegate(BehaviorType, method));
    }

    internal RestBehaviorEndpointProjection WithPattern(string pattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var normalizedPattern = pattern.Trim();
        return string.Equals(normalizedPattern, Pattern, StringComparison.Ordinal)
            ? this
            : new RestBehaviorEndpointProjection(
                Method,
                BehaviorId,
                BehaviorType,
                normalizedPattern,
                Bindings,
                PreserveImplicitQueryFallback,
                AuthoringStyle,
                ConfigureEndpoint,
                Map);
    }

    internal RestBehaviorEndpointProjection WithBindings(IReadOnlyList<BehaviorRestBindingDescriptor> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        return Bindings.SequenceEqual(bindings)
            ? this
            : new RestBehaviorEndpointProjection(
                Method,
                BehaviorId,
                BehaviorType,
                Pattern,
                bindings.ToArray(),
                PreserveImplicitQueryFallback,
                AuthoringStyle,
                ConfigureEndpoint,
                Map);
    }

    internal RestBehaviorEndpointProjection WithPreserveImplicitQueryFallback(bool preserveImplicitQueryFallback)
    {
        return PreserveImplicitQueryFallback == preserveImplicitQueryFallback
            ? this
            : new RestBehaviorEndpointProjection(
                Method,
                BehaviorId,
                BehaviorType,
                Pattern,
                Bindings,
                preserveImplicitQueryFallback,
                AuthoringStyle,
                ConfigureEndpoint,
                Map);
    }

    internal RouteHandlerBuilder Apply(BehaviorRestEndpointGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.UseRuntimeAuthoringStyle(AuthoringStyle);
        return Map(group, Pattern, Bindings, PreserveImplicitQueryFallback, ConfigureEndpoint);
    }

    private static Func<BehaviorRestEndpointGroup, string, IReadOnlyList<BehaviorRestBindingDescriptor>, bool, Action<RouteHandlerBuilder>?, RouteHandlerBuilder> CreateMapDelegate(
        Type behaviorType,
        RestBehaviorHttpMethod method)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        var closedMethod = CreateMapDelegateFactoryMethod.MakeGenericMethod(behaviorType);
        return (Func<BehaviorRestEndpointGroup, string, IReadOnlyList<BehaviorRestBindingDescriptor>, bool, Action<RouteHandlerBuilder>?, RouteHandlerBuilder>)closedMethod.Invoke(
            null,
            [method])!;
    }

    private static Func<BehaviorRestEndpointGroup, string, IReadOnlyList<BehaviorRestBindingDescriptor>, bool, Action<RouteHandlerBuilder>?, RouteHandlerBuilder> CreateMapDelegateFactory<TBehavior>(
        RestBehaviorHttpMethod method)
        where TBehavior : class
        => CreateMapDelegate<TBehavior>(method);

    private static Func<BehaviorRestEndpointGroup, string, IReadOnlyList<BehaviorRestBindingDescriptor>, bool, Action<RouteHandlerBuilder>?, RouteHandlerBuilder> CreateMapDelegate<TBehavior>(
        RestBehaviorHttpMethod method)
        where TBehavior : class
    {
        return method switch
        {
            RestBehaviorHttpMethod.Get => static (group, pattern, bindings, preserveImplicitQueryFallback, configureEndpoint) =>
                group.MapBehaviorGet<TBehavior>(pattern, bindings, preserveImplicitQueryFallback, configureEndpoint),
            RestBehaviorHttpMethod.Post => static (group, pattern, bindings, preserveImplicitQueryFallback, configureEndpoint) =>
                group.MapBehaviorPost<TBehavior>(pattern, bindings, preserveImplicitQueryFallback, configureEndpoint),
            RestBehaviorHttpMethod.Put => static (group, pattern, bindings, preserveImplicitQueryFallback, configureEndpoint) =>
                group.MapBehaviorPut<TBehavior>(pattern, bindings, preserveImplicitQueryFallback, configureEndpoint),
            RestBehaviorHttpMethod.Patch => static (group, pattern, bindings, preserveImplicitQueryFallback, configureEndpoint) =>
                group.MapBehaviorPatch<TBehavior>(pattern, bindings, preserveImplicitQueryFallback, configureEndpoint),
            RestBehaviorHttpMethod.Delete => static (group, pattern, bindings, preserveImplicitQueryFallback, configureEndpoint) =>
                group.MapBehaviorDelete<TBehavior>(pattern, bindings, preserveImplicitQueryFallback, configureEndpoint),
            _ => throw new InvalidOperationException($"Unsupported REST behavior HTTP method '{method}'.")
        };
    }

    private static string ResolveBehaviorId(Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        return behaviorType.GetCustomAttributes(typeof(AppBehaviorAttribute), inherit: false)
            .OfType<AppBehaviorAttribute>()
            .SingleOrDefault()
            ?.Id
            ?? throw new InvalidOperationException(
                $"Behavior type '{behaviorType.FullName}' is missing [AppBehavior(id)].");
    }

    private static RestBehaviorHttpMethod ConvertMethod(BehaviorRestMethod method)
    {
        return method switch
        {
            BehaviorRestMethod.Get => RestBehaviorHttpMethod.Get,
            BehaviorRestMethod.Post => RestBehaviorHttpMethod.Post,
            BehaviorRestMethod.Put => RestBehaviorHttpMethod.Put,
            BehaviorRestMethod.Patch => RestBehaviorHttpMethod.Patch,
            BehaviorRestMethod.Delete => RestBehaviorHttpMethod.Delete,
            _ => throw new InvalidOperationException($"Unsupported behavior REST profile method '{method}'.")
        };
    }
}

internal enum RestBehaviorHttpMethod
{
    Get,
    Post,
    Put,
    Patch,
    Delete
}

internal static class RestBehaviorHttpMethodParser
{
    internal static RestBehaviorHttpMethod Parse(string method)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        return method.Trim().ToUpperInvariant() switch
        {
            "GET" => RestBehaviorHttpMethod.Get,
            "POST" => RestBehaviorHttpMethod.Post,
            "PUT" => RestBehaviorHttpMethod.Put,
            "PATCH" => RestBehaviorHttpMethod.Patch,
            "DELETE" => RestBehaviorHttpMethod.Delete,
            _ => throw new InvalidOperationException(
                $"Unsupported REST behavior HTTP method '{method}'.")
        };
    }
}
