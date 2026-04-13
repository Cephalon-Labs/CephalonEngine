using Cephalon.Abstractions.Behaviors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.Behaviors.Http.Hosting;

internal sealed class RestBehaviorModuleBuilder : IRestBehaviorModuleBuilder
{
    private static readonly Type AppBehaviorOpenGeneric = typeof(IAppBehavior<,>);
    private readonly Dictionary<string, RestBehaviorOwnershipDefinition> ownedBehaviors = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Action<IBehaviorModuleBuilder>> ownershipRegistrations = [];
    private readonly List<RestBehaviorRouteGroupState> groups = [];

    public IRestBehaviorModuleBuilder Internal<TBehavior>()
        where TBehavior : class
    {
        RegisterOwnedBehavior<TBehavior>(configureTopology: null);
        return this;
    }

    public IRestBehaviorModuleBuilder Internal<TBehavior>(Action<IBehaviorTopologyBuilder> configureTopology)
        where TBehavior : class
    {
        ArgumentNullException.ThrowIfNull(configureTopology);
        RegisterOwnedBehavior<TBehavior>(configureTopology);
        return this;
    }

    public IRestBehaviorEndpointGroupBuilder Group(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        var state = new RestBehaviorRouteGroupState(prefix.Trim());
        groups.Add(state);
        return new RestBehaviorEndpointGroupBuilder(this, state);
    }

    internal RestBehaviorModuleProjection Build()
        => new(
            [.. ownershipRegistrations],
            [.. groups.Select(static state => state.ToProjection())]);

    private void RegisterOwnedBehavior<TBehavior>(Action<IBehaviorTopologyBuilder>? configureTopology)
        where TBehavior : class
    {
        var ownership = RestBehaviorOwnershipDefinition.Create<TBehavior>(configureTopology is not null);
        if (ownedBehaviors.TryGetValue(ownership.BehaviorId, out var existing))
        {
            if (existing.BehaviorType != ownership.BehaviorType)
            {
                throw new InvalidOperationException(
                    $"Behavior id '{ownership.BehaviorId}' was declared more than once with different behavior types inside the same REST behavior module.");
            }

            if (existing.HasExplicitTopologyOverride || ownership.HasExplicitTopologyOverride)
            {
                throw new InvalidOperationException(
                    $"Behavior '{ownership.BehaviorId}' was declared more than once with explicit internal/topology configuration inside the same REST behavior module. Declare the behavior once through Internal<TBehavior>(), then map it from one or more REST routes.");
            }

            return;
        }

        ownershipRegistrations.Add(builder =>
        {
            if (configureTopology is null)
            {
                builder.Add<TBehavior>();
            }
            else
            {
                builder.Add<TBehavior>(configureTopology);
            }
        });

        ownedBehaviors.Add(ownership.BehaviorId, ownership);
    }

    private sealed class RestBehaviorEndpointGroupBuilder(
        RestBehaviorModuleBuilder moduleBuilder,
        RestBehaviorRouteGroupState state)
        : IRestBehaviorEndpointGroupBuilder
    {
        public IRestBehaviorEndpointGroupBuilder ApiVersion(int major)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(major);
            state.ApiVersionMajor = major;
            state.HasExplicitApiVersion = true;
            return this;
        }

        public IRestBehaviorEndpointGroupBuilder WithTagName(string tagName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
            state.TagName = tagName.Trim();
            return this;
        }

        public IRestBehaviorEndpointGroupBuilder WithTagDescription(string? description)
        {
            state.TagDescription = string.IsNullOrWhiteSpace(description)
                ? null
                : description.Trim();
            state.HasExplicitTagDescription = true;
            return this;
        }

        public IRestBehaviorEndpointGroupBuilder Configure(Action<RouteGroupBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            state.GroupConventions.Add(configure);
            return this;
        }

        public IRestBehaviorEndpointGroupBuilder MapGet<TBehavior>(
            string pattern,
            Action<RouteHandlerBuilder>? configureEndpoint = null)
            where TBehavior : class
            => AddEndpoint<TBehavior>(RestBehaviorHttpMethod.Get, pattern, configureTopology: null, configureEndpoint);

        public IRestBehaviorEndpointGroupBuilder MapGet<TBehavior>(
            string pattern,
            Action<IBehaviorTopologyBuilder> configureTopology,
            Action<RouteHandlerBuilder>? configureEndpoint = null)
            where TBehavior : class
            => AddEndpoint<TBehavior>(RestBehaviorHttpMethod.Get, pattern, configureTopology, configureEndpoint);

        public IRestBehaviorEndpointGroupBuilder MapPost<TBehavior>(
            string pattern,
            Action<RouteHandlerBuilder>? configureEndpoint = null)
            where TBehavior : class
            => AddEndpoint<TBehavior>(RestBehaviorHttpMethod.Post, pattern, configureTopology: null, configureEndpoint);

        public IRestBehaviorEndpointGroupBuilder MapPost<TBehavior>(
            string pattern,
            Action<IBehaviorTopologyBuilder> configureTopology,
            Action<RouteHandlerBuilder>? configureEndpoint = null)
            where TBehavior : class
            => AddEndpoint<TBehavior>(RestBehaviorHttpMethod.Post, pattern, configureTopology, configureEndpoint);

        public IRestBehaviorEndpointGroupBuilder MapPut<TBehavior>(
            string pattern,
            Action<RouteHandlerBuilder>? configureEndpoint = null)
            where TBehavior : class
            => AddEndpoint<TBehavior>(RestBehaviorHttpMethod.Put, pattern, configureTopology: null, configureEndpoint);

        public IRestBehaviorEndpointGroupBuilder MapPut<TBehavior>(
            string pattern,
            Action<IBehaviorTopologyBuilder> configureTopology,
            Action<RouteHandlerBuilder>? configureEndpoint = null)
            where TBehavior : class
            => AddEndpoint<TBehavior>(RestBehaviorHttpMethod.Put, pattern, configureTopology, configureEndpoint);

        public IRestBehaviorEndpointGroupBuilder MapPatch<TBehavior>(
            string pattern,
            Action<RouteHandlerBuilder>? configureEndpoint = null)
            where TBehavior : class
            => AddEndpoint<TBehavior>(RestBehaviorHttpMethod.Patch, pattern, configureTopology: null, configureEndpoint);

        public IRestBehaviorEndpointGroupBuilder MapPatch<TBehavior>(
            string pattern,
            Action<IBehaviorTopologyBuilder> configureTopology,
            Action<RouteHandlerBuilder>? configureEndpoint = null)
            where TBehavior : class
            => AddEndpoint<TBehavior>(RestBehaviorHttpMethod.Patch, pattern, configureTopology, configureEndpoint);

        public IRestBehaviorEndpointGroupBuilder MapDelete<TBehavior>(
            string pattern,
            Action<RouteHandlerBuilder>? configureEndpoint = null)
            where TBehavior : class
            => AddEndpoint<TBehavior>(RestBehaviorHttpMethod.Delete, pattern, configureTopology: null, configureEndpoint);

        public IRestBehaviorEndpointGroupBuilder MapDelete<TBehavior>(
            string pattern,
            Action<IBehaviorTopologyBuilder> configureTopology,
            Action<RouteHandlerBuilder>? configureEndpoint = null)
            where TBehavior : class
            => AddEndpoint<TBehavior>(RestBehaviorHttpMethod.Delete, pattern, configureTopology, configureEndpoint);

        private RestBehaviorEndpointGroupBuilder AddEndpoint<TBehavior>(
            RestBehaviorHttpMethod method,
            string pattern,
            Action<IBehaviorTopologyBuilder>? configureTopology,
            Action<RouteHandlerBuilder>? configureEndpoint)
            where TBehavior : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

            moduleBuilder.RegisterOwnedBehavior<TBehavior>(configureTopology);
            state.Endpoints.Add(RestBehaviorEndpointProjection.Create<TBehavior>(
                method,
                pattern,
                configureEndpoint));
            return this;
        }
    }

    private sealed class RestBehaviorRouteGroupState(string prefix)
    {
        public string Prefix { get; } = prefix;

        public string? TagName { get; set; }

        public string? TagDescription { get; set; }

        public bool HasExplicitTagDescription { get; set; }

        public int? ApiVersionMajor { get; set; }

        public bool HasExplicitApiVersion { get; set; }

        public List<Action<RouteGroupBuilder>> GroupConventions { get; } = [];

        public List<RestBehaviorEndpointProjection> Endpoints { get; } = [];

        public RestBehaviorRouteGroupProjection ToProjection()
        {
            return new RestBehaviorRouteGroupProjection(
                Prefix,
                TagName,
                TagDescription,
                HasExplicitTagDescription,
                ApiVersionMajor,
                HasExplicitApiVersion,
                [.. GroupConventions],
                [.. Endpoints]);
        }
    }

    private sealed record RestBehaviorOwnershipDefinition(
        string BehaviorId,
        Type BehaviorType,
        bool HasExplicitTopologyOverride)
    {
        internal static RestBehaviorOwnershipDefinition Create<TBehavior>(bool hasExplicitTopologyOverride)
            where TBehavior : class
        {
            var behaviorType = typeof(TBehavior);
            var attribute = behaviorType.GetCustomAttributes(typeof(AppBehaviorAttribute), inherit: false)
                .OfType<AppBehaviorAttribute>()
                .SingleOrDefault()
                ?? throw new InvalidOperationException(
                    $"Cannot declare '{behaviorType.FullName}' as a REST behavior-module behavior because it is missing [AppBehavior(id)].");

            if (!behaviorType.GetInterfaces().Any(static candidate =>
                    candidate.IsGenericType &&
                    candidate.GetGenericTypeDefinition() == AppBehaviorOpenGeneric))
            {
                throw new InvalidOperationException(
                    $"Cannot declare '{behaviorType.FullName}' as a REST behavior-module behavior because it does not implement IAppBehavior<TInput, TOutput>.");
            }

            return new RestBehaviorOwnershipDefinition(
                attribute.Id,
                behaviorType,
                hasExplicitTopologyOverride);
        }
    }
}
