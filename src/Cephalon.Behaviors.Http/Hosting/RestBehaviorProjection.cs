using Cephalon.Abstractions.Behaviors;
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
    IReadOnlyList<Action<RouteGroupBuilder>> GroupConventions,
    IReadOnlyList<RestBehaviorEndpointProjection> Endpoints);

internal sealed record RestBehaviorEndpointProjection(
    RestBehaviorHttpMethod Method,
    string BehaviorId,
    Type BehaviorType,
    string Pattern,
    Action<RouteHandlerBuilder>? ConfigureEndpoint,
    Action<BehaviorRestEndpointGroup, string, Action<RouteHandlerBuilder>?> Map)
{
    internal static RestBehaviorEndpointProjection Create<TBehavior>(
        RestBehaviorHttpMethod method,
        string pattern,
        Action<RouteHandlerBuilder>? configureEndpoint)
        where TBehavior : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        return new RestBehaviorEndpointProjection(
            method,
            ResolveBehaviorId(typeof(TBehavior)),
            typeof(TBehavior),
            pattern.Trim(),
            configureEndpoint,
            CreateMapDelegate<TBehavior>(method));
    }

    internal void Apply(BehaviorRestEndpointGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        Map(group, Pattern, ConfigureEndpoint);
    }

    private static Action<BehaviorRestEndpointGroup, string, Action<RouteHandlerBuilder>?> CreateMapDelegate<TBehavior>(
        RestBehaviorHttpMethod method)
        where TBehavior : class
    {
        return method switch
        {
            RestBehaviorHttpMethod.Get => static (group, pattern, configureEndpoint) =>
                group.MapBehaviorGet<TBehavior>(pattern, configureEndpoint),
            RestBehaviorHttpMethod.Post => static (group, pattern, configureEndpoint) =>
                group.MapBehaviorPost<TBehavior>(pattern, configureEndpoint),
            RestBehaviorHttpMethod.Put => static (group, pattern, configureEndpoint) =>
                group.MapBehaviorPut<TBehavior>(pattern, configureEndpoint),
            RestBehaviorHttpMethod.Patch => static (group, pattern, configureEndpoint) =>
                group.MapBehaviorPatch<TBehavior>(pattern, configureEndpoint),
            RestBehaviorHttpMethod.Delete => static (group, pattern, configureEndpoint) =>
                group.MapBehaviorDelete<TBehavior>(pattern, configureEndpoint),
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
}

internal enum RestBehaviorHttpMethod
{
    Get,
    Post,
    Put,
    Patch,
    Delete
}
