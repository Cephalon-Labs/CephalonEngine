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
    string? OpenApiDocumentName = null,
    bool HasExplicitOpenApiDocumentName = false,
    string? TagName = null,
    string? TagDescription = null,
    bool HasExplicitTagDescription = false,
    int? ApiVersionMajor = null,
    bool HasExplicitApiVersion = false,
    string? ProfileApiVersionSourceBehaviorId = null,
    bool AllowHostGovernance = false,
    string? HostGovernanceScope = null,
    IReadOnlyList<Action<RouteGroupBuilder>> GroupConventions = null!,
    IReadOnlyList<RestBehaviorEndpointProjection> Endpoints = null!);

internal sealed record RestBehaviorEndpointProjection(
    RestBehaviorHttpMethod Method,
    string BehaviorId,
    Type BehaviorType,
    string Pattern,
    IReadOnlyList<BehaviorRestBindingDescriptor> Bindings,
    bool PreserveImplicitQueryFallback,
    string AuthoringStyle,
    Action<RouteHandlerBuilder>? ConfigureEndpoint)
{
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
            configureEndpoint);
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

        return new RestBehaviorEndpointProjection(
            ConvertMethod(profile.Method),
            profile.BehaviorId,
            behaviorType,
            profile.RelativePattern.Trim(),
            profile.Bindings ?? [],
            profile.PreserveImplicitQueryFallback,
            authoringStyle,
            configureEndpoint);
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
                ConfigureEndpoint);
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
                ConfigureEndpoint);
    }

    internal RestBehaviorEndpointProjection WithBindings(IReadOnlyList<BehaviorRestBindingDescriptor> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        return RestBehaviorBindingDescriptorSetComparer.Equivalent(Bindings, bindings)
            ? this
            : new RestBehaviorEndpointProjection(
                Method,
                BehaviorId,
                BehaviorType,
                Pattern,
                bindings.ToArray(),
                PreserveImplicitQueryFallback,
                AuthoringStyle,
                ConfigureEndpoint);
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
                ConfigureEndpoint);
    }

    internal RouteHandlerBuilder Apply(BehaviorRestEndpointGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.UseRuntimeAuthoringStyle(AuthoringStyle);
        return group.MapBehavior(
            BehaviorType,
            Method,
            Pattern,
            Bindings,
            PreserveImplicitQueryFallback,
            ConfigureEndpoint);
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
            _ => throw new InvalidOperationException(
                $"Unsupported behavior REST profile method '{method}'. {BehaviorRestWireNameDiagnostics.DescribeMethodSupport()}")
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
                $"Unsupported REST behavior HTTP method '{method}'. {BehaviorRestWireNameDiagnostics.DescribeMethodSupport()}")
        };
    }
}
