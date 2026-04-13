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
    string AuthoringStyle,
    Action<RouteHandlerBuilder>? ConfigureEndpoint,
    Action<BehaviorRestEndpointGroup, string, IReadOnlyList<BehaviorRestBindingDescriptor>, Action<RouteHandlerBuilder>?> Map)
{
    internal static RestBehaviorEndpointProjection Create<TBehavior>(
        RestBehaviorHttpMethod method,
        string pattern,
        Action<RouteHandlerBuilder>? configureEndpoint,
        string authoringStyle,
        IReadOnlyList<BehaviorRestBindingDescriptor>? bindings = null)
        where TBehavior : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentException.ThrowIfNullOrWhiteSpace(authoringStyle);

        return new RestBehaviorEndpointProjection(
            method,
            ResolveBehaviorId(typeof(TBehavior)),
            typeof(TBehavior),
            pattern.Trim(),
            bindings ?? [],
            authoringStyle.Trim(),
            configureEndpoint,
            CreateMapDelegate<TBehavior>(method));
    }

    internal static RestBehaviorEndpointProjection Create<TBehavior>(
        BehaviorRestProfileDescriptor profile,
        Action<RouteHandlerBuilder>? configureEndpoint)
        where TBehavior : class
    {
        ArgumentNullException.ThrowIfNull(profile);

        var behaviorId = ResolveBehaviorId(typeof(TBehavior));
        if (!string.Equals(behaviorId, profile.BehaviorId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Resolved REST profile behavior id '{profile.BehaviorId}' does not match behavior type '{typeof(TBehavior).FullName}' with id '{behaviorId}'.");
        }

        return Create<TBehavior>(
            ConvertMethod(profile.Method),
            profile.RelativePattern,
            configureEndpoint,
            RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle,
            profile.Bindings);
    }

    internal void Apply(BehaviorRestEndpointGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.UseRuntimeAuthoringStyle(AuthoringStyle);
        Map(group, Pattern, Bindings, ConfigureEndpoint);
    }

    private static Action<BehaviorRestEndpointGroup, string, IReadOnlyList<BehaviorRestBindingDescriptor>, Action<RouteHandlerBuilder>?> CreateMapDelegate<TBehavior>(
        RestBehaviorHttpMethod method)
        where TBehavior : class
    {
        return method switch
        {
            RestBehaviorHttpMethod.Get => static (group, pattern, bindings, configureEndpoint) =>
                group.MapBehaviorGet<TBehavior>(pattern, bindings, configureEndpoint),
            RestBehaviorHttpMethod.Post => static (group, pattern, bindings, configureEndpoint) =>
                group.MapBehaviorPost<TBehavior>(pattern, bindings, configureEndpoint),
            RestBehaviorHttpMethod.Put => static (group, pattern, bindings, configureEndpoint) =>
                group.MapBehaviorPut<TBehavior>(pattern, bindings, configureEndpoint),
            RestBehaviorHttpMethod.Patch => static (group, pattern, bindings, configureEndpoint) =>
                group.MapBehaviorPatch<TBehavior>(pattern, bindings, configureEndpoint),
            RestBehaviorHttpMethod.Delete => static (group, pattern, bindings, configureEndpoint) =>
                group.MapBehaviorDelete<TBehavior>(pattern, bindings, configureEndpoint),
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
