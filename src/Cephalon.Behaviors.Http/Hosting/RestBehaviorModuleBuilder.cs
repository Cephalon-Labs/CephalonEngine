using System.Reflection;
using Cephalon.Abstractions.Behaviors;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Http.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.Behaviors.Http.Hosting;

internal sealed class RestBehaviorModuleBuilder : IRestBehaviorModuleBuilder
{
    private static readonly Type AppBehaviorOpenGeneric = typeof(IAppBehavior<,>);
    private static readonly MethodInfo AddOwnedBehaviorMethod = typeof(IBehaviorModuleBuilder)
        .GetMethods()
        .Single(static method =>
            method.Name == nameof(IBehaviorModuleBuilder.Add) &&
            method.IsGenericMethodDefinition &&
            method.GetParameters().Length == 0);
    private static readonly MethodInfo AddOwnedBehaviorWithTopologyMethod = typeof(IBehaviorModuleBuilder)
        .GetMethods()
        .Single(static method =>
            method.Name == nameof(IBehaviorModuleBuilder.Add) &&
            method.IsGenericMethodDefinition &&
            method.GetParameters().Length == 1);
    private readonly Dictionary<string, RestBehaviorOwnershipDefinition> ownedBehaviors = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Action<IBehaviorModuleBuilder>> ownershipRegistrations = [];
    private readonly List<RestBehaviorRouteGroupState> groups = [];
    private readonly Type? ownerModuleType;

    internal RestBehaviorModuleBuilder(Type? ownerModuleType = null)
    {
        this.ownerModuleType = ownerModuleType;
    }

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

    public IRestBehaviorEndpointGroupBuilder GroupFromBehaviorIdPrefix(string behaviorIdPrefix)
        => Group(RestBehaviorAuthoringPathConventions.DeriveRouteGroupPrefixFromBehaviorIdPrefix(behaviorIdPrefix));

    internal RestBehaviorModuleProjection Build()
        => new(
            [.. ownershipRegistrations],
            [.. groups.Select(static state => state.ToProjection())]);

    private void RegisterOwnedBehavior<TBehavior>(Action<IBehaviorTopologyBuilder>? configureTopology)
        where TBehavior : class
        => RegisterOwnedBehavior(typeof(TBehavior), configureTopology);

    private void RegisterOwnedBehavior(Type behaviorType, Action<IBehaviorTopologyBuilder>? configureTopology)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        var ownership = RestBehaviorOwnershipDefinition.Create(behaviorType, configureTopology is not null);
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
                AddOwnedBehavior(builder, behaviorType);
            }
            else
            {
                AddOwnedBehavior(builder, behaviorType, configureTopology);
            }
        });

        ownedBehaviors.Add(ownership.BehaviorId, ownership);
    }

    private static void AddOwnedBehavior(
        IBehaviorModuleBuilder builder,
        Type behaviorType,
        Action<IBehaviorTopologyBuilder>? configureTopology = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(behaviorType);

        var method = (configureTopology is null
                ? AddOwnedBehaviorMethod
                : AddOwnedBehaviorWithTopologyMethod)
            .MakeGenericMethod(behaviorType);
        _ = method.Invoke(
            builder,
            configureTopology is null
                ? []
                : [configureTopology]);
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
            state.ProfileApiVersionSourceBehaviorId = null;
            if (!state.HasExplicitOpenApiDocumentName)
            {
                state.OpenApiDocumentName = ResolveVersionDocumentName(major);
            }

            return this;
        }

        public IRestBehaviorEndpointGroupBuilder WithOpenApiDocumentName(string openApiDocumentName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(openApiDocumentName);
            state.OpenApiDocumentName = openApiDocumentName.Trim();
            state.HasExplicitOpenApiDocumentName = true;
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

        public IRestBehaviorEndpointGroupBuilder AllowHostGovernance()
        {
            state.AllowHostGovernance = true;
            return this;
        }

        public IRestBehaviorEndpointGroupBuilder WithHostGovernanceScope(string hostGovernanceScope)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(hostGovernanceScope);
            state.HostGovernanceScope = hostGovernanceScope.Trim();
            return this;
        }

        public IRestBehaviorEndpointGroupBuilder MapGeneratedProfiles()
            => AddGeneratedProfiles(behaviorIdPrefix: null);

        public IRestBehaviorEndpointGroupBuilder MapGeneratedProfiles(string behaviorIdPrefix)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(behaviorIdPrefix);
            return AddGeneratedProfiles(behaviorIdPrefix.Trim());
        }

        public IRestBehaviorEndpointGroupBuilder MapProfile<TBehavior>(
            Action<RouteHandlerBuilder>? configureEndpoint = null)
            where TBehavior : class
            => AddProfileEndpoint<TBehavior>(configureTopology: null, configureEndpoint);

        public IRestBehaviorEndpointGroupBuilder MapProfile<TBehavior>(
            Action<IBehaviorTopologyBuilder> configureTopology,
            Action<RouteHandlerBuilder>? configureEndpoint = null)
            where TBehavior : class
            => AddProfileEndpoint<TBehavior>(configureTopology, configureEndpoint);

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
                configureEndpoint,
                RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle));
            return this;
        }

        private RestBehaviorEndpointGroupBuilder AddProfileEndpoint<TBehavior>(
            Action<IBehaviorTopologyBuilder>? configureTopology,
            Action<RouteHandlerBuilder>? configureEndpoint)
            where TBehavior : class
        {
            var profile = BehaviorRestProfileResolver.Resolve<TBehavior>();
            SeedProfileApiVersion(profile);
            moduleBuilder.RegisterOwnedBehavior<TBehavior>(configureTopology);
            state.Endpoints.Add(RestBehaviorEndpointProjection.Create<TBehavior>(
                profile,
                configureEndpoint));
            return this;
        }

        private RestBehaviorEndpointGroupBuilder AddGeneratedProfiles(string? behaviorIdPrefix)
        {
            var owningModuleType = moduleBuilder.ownerModuleType
                ?? throw new InvalidOperationException(
                    "MapGeneratedProfiles() requires an owning RestBehaviorModuleBase context so Cephalon can resolve generated REST profiles from the module assembly.");
            var effectiveBehaviorIdPrefix = string.IsNullOrWhiteSpace(behaviorIdPrefix)
                ? DeriveBehaviorIdPrefixFromGroupPrefix(state.Prefix)
                : behaviorIdPrefix.Trim();
            if (string.IsNullOrWhiteSpace(effectiveBehaviorIdPrefix))
            {
                throw new InvalidOperationException(
                    $"REST behavior-module group '{state.Prefix}' cannot derive a behavior-id prefix for MapGeneratedProfiles(). Supply MapGeneratedProfiles(\"prefix\") explicitly.");
            }

            var generatedProfiles = BehaviorRestProfileResolver.ResolveGeneratedProfiles(
                owningModuleType.Assembly,
                effectiveBehaviorIdPrefix);
            if (generatedProfiles.Count == 0)
            {
                throw new InvalidOperationException(
                    $"REST behavior-module group '{state.Prefix}' did not find any generated REST profiles in assembly '{owningModuleType.Assembly.FullName}' that match behavior-id prefix '{effectiveBehaviorIdPrefix}'.");
            }

            foreach (var generatedProfile in generatedProfiles)
            {
                SeedProfileApiVersion(generatedProfile.Profile);
                moduleBuilder.RegisterOwnedBehavior(generatedProfile.BehaviorType, configureTopology: null);
                state.Endpoints.Add(RestBehaviorEndpointProjection.Create(
                    generatedProfile.BehaviorType,
                    generatedProfile.Profile,
                    configureEndpoint: null,
                    RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle));
            }

            return this;
        }

        private void SeedProfileApiVersion(BehaviorRestProfileDescriptor profile)
        {
            ArgumentNullException.ThrowIfNull(profile);

            if (!profile.ApiVersionMajor.HasValue || state.HasExplicitApiVersion)
            {
                return;
            }

            if (!state.ApiVersionMajor.HasValue)
            {
                state.ApiVersionMajor = profile.ApiVersionMajor.Value;
                state.ProfileApiVersionSourceBehaviorId = profile.BehaviorId;
                if (!state.HasExplicitOpenApiDocumentName)
                {
                    state.OpenApiDocumentName = ResolveVersionDocumentName(profile.ApiVersionMajor.Value);
                }

                return;
            }

            if (state.ApiVersionMajor.Value == profile.ApiVersionMajor.Value)
            {
                return;
            }

            throw new InvalidOperationException(
                $"REST behavior-module group '{state.Prefix}' resolved conflicting profile API major versions ({state.ApiVersionMajor.Value} from '{state.ProfileApiVersionSourceBehaviorId}' and {profile.ApiVersionMajor.Value} from '{profile.BehaviorId}'). Set ApiVersion(...) explicitly or split the profiled behaviors into separate groups.");
        }

        private static string DeriveBehaviorIdPrefixFromGroupPrefix(string prefix)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

            return string.Join(
                ".",
                prefix.Trim()
                    .Trim('/')
                    .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        private static string ResolveVersionDocumentName(int apiVersionMajor) => $"v{apiVersionMajor}";
    }

    private sealed class RestBehaviorRouteGroupState(string prefix)
    {
        public string Prefix { get; } = prefix;

        public string? OpenApiDocumentName { get; set; }

        public bool HasExplicitOpenApiDocumentName { get; set; }

        public string? TagName { get; set; }

        public string? TagDescription { get; set; }

        public bool HasExplicitTagDescription { get; set; }

        public int? ApiVersionMajor { get; set; }

        public bool HasExplicitApiVersion { get; set; }

        public string? ProfileApiVersionSourceBehaviorId { get; set; }

        public bool AllowHostGovernance { get; set; }

        public string? HostGovernanceScope { get; set; }

        public List<Action<RouteGroupBuilder>> GroupConventions { get; } = [];

        public List<RestBehaviorEndpointProjection> Endpoints { get; } = [];

        public RestBehaviorRouteGroupProjection ToProjection()
        {
            return new RestBehaviorRouteGroupProjection(
                Prefix,
                OpenApiDocumentName,
                HasExplicitOpenApiDocumentName,
                TagName,
                TagDescription,
                HasExplicitTagDescription,
                ApiVersionMajor,
                HasExplicitApiVersion,
                ProfileApiVersionSourceBehaviorId,
                AllowHostGovernance,
                HostGovernanceScope,
                [.. GroupConventions],
                [.. Endpoints]);
        }
    }

    private sealed record RestBehaviorOwnershipDefinition(
        string BehaviorId,
        Type BehaviorType,
        bool HasExplicitTopologyOverride)
    {
        internal static RestBehaviorOwnershipDefinition Create(Type behaviorType, bool hasExplicitTopologyOverride)
        {
            ArgumentNullException.ThrowIfNull(behaviorType);

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
