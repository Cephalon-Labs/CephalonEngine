using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Http.Hosting;
using Microsoft.AspNetCore.Builder;

namespace Cephalon.Tests.Hosting;

public sealed class BehaviorRestProjectionTests
{
    [Fact]
    public void RestBehaviorModuleBuilderBuildNormalizesRouteGroupsAndEndpoints()
    {
        var builder = new RestBehaviorModuleBuilder();
        var group = builder.Group("/tests/cart")
            .WithTagName("Test Cart API")
            .WithTagDescription("Commands and queries exposed by the test cart REST surface.")
            .ApiVersion(2);

        group.MapGet<ProjectionCartBehavior>("/{cartId}");
        group.MapPost<ProjectionCartBehavior>("/{cartId}/items");

        var projection = builder.Build();

        Assert.Single(projection.OwnershipRegistrations);

        var routeGroup = Assert.Single(projection.Groups);
        Assert.Equal("/tests/cart", routeGroup.Prefix);
        Assert.Equal("Test Cart API", routeGroup.TagName);
        Assert.Equal("Commands and queries exposed by the test cart REST surface.", routeGroup.TagDescription);
        Assert.True(routeGroup.HasExplicitTagDescription);
        Assert.Equal(2, routeGroup.ApiVersionMajor);
        Assert.True(routeGroup.HasExplicitApiVersion);

        Assert.Collection(
            routeGroup.Endpoints,
            getEndpoint =>
            {
                Assert.Equal(RestBehaviorHttpMethod.Get, getEndpoint.Method);
                Assert.Equal(typeof(ProjectionCartBehavior), getEndpoint.BehaviorType);
                Assert.Equal("/{cartId}", getEndpoint.Pattern);
                Assert.Null(getEndpoint.ConfigureEndpoint);
            },
            postEndpoint =>
            {
                Assert.Equal(RestBehaviorHttpMethod.Post, postEndpoint.Method);
                Assert.Equal(typeof(ProjectionCartBehavior), postEndpoint.BehaviorType);
                Assert.Equal("/{cartId}/items", postEndpoint.Pattern);
                Assert.Null(postEndpoint.ConfigureEndpoint);
            });
    }

    [Fact]
    public void RestBehaviorModuleBuilderBuildKeepsOneOwnershipRegistrationWhenOneBehaviorMapsMultipleRoutes()
    {
        var builder = new RestBehaviorModuleBuilder();
        var group = builder.Group("/tests/cart");

        group.MapGet<ProjectionCartBehavior>("/{cartId}");
        group.MapDelete<ProjectionCartBehavior>("/{cartId}/items/{productId}");

        var projection = builder.Build();

        Assert.Single(projection.OwnershipRegistrations);
        Assert.Equal(2, Assert.Single(projection.Groups).Endpoints.Count);
    }

    [Fact]
    public void RestBehaviorModuleBuilderRejectsDifferentBehaviorTypesThatReuseTheSameBehaviorId()
    {
        var builder = new RestBehaviorModuleBuilder();
        var group = builder.Group("/tests/cart");

        group.MapGet<ProjectionCartBehavior>("/{cartId}");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            group.MapPost<ProjectionCartConflictBehavior>("/{cartId}/items"));

        Assert.Contains("different behavior types", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tests.cart.projection", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestBehaviorModuleBuilderRejectsDuplicateOwnershipWhenExplicitTopologyOverrideWasAlreadyDeclared()
    {
        var builder = new RestBehaviorModuleBuilder();
        builder.Internal<ProjectionCartBehavior>(topology => topology.AsCqrs());

        var group = builder.Group("/tests/cart");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            group.MapGet<ProjectionCartBehavior>("/{cartId}"));

        Assert.Contains("explicit internal/topology configuration", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Internal<TBehavior>()", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RestBehaviorModuleBaseReusesCompiledProjectionAcrossOwnershipRegistrationAndEndpointMaterialization()
    {
        var module = new ProjectionCountingRestModule();
        var ownershipBuilder = new RecordingBehaviorModuleBuilder();

        module.ConfigureBehaviors(ownershipBuilder);

        using var app = WebApplication.CreateBuilder().Build();
        ((IRestModule)module).MapRestEndpoints(app);

        module.ConfigureBehaviors(ownershipBuilder);

        Assert.Equal(1, module.ConfigureRestBehaviorsCallCount);
        Assert.Collection(
            ownershipBuilder.Registrations,
            registration =>
            {
                Assert.Equal(typeof(ProjectionCartBehavior), registration.BehaviorType);
                Assert.False(registration.HasExplicitTopologyOverride);
            });
    }

    [AppBehavior("tests.cart.projection")]
    private sealed class ProjectionCartBehavior : IAppBehavior<ProjectionCartInput, ProjectionCartOutput>
    {
        public Task<ProjectionCartOutput> HandleAsync(
            ProjectionCartInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProjectionCartOutput(input.CartId));
        }
    }

    [AppBehavior("tests.cart.projection")]
    private sealed class ProjectionCartConflictBehavior : IAppBehavior<ProjectionCartInput, ProjectionCartOutput>
    {
        public Task<ProjectionCartOutput> HandleAsync(
            ProjectionCartInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProjectionCartOutput(input.CartId));
        }
    }

    private sealed record ProjectionCartInput(string CartId, string? ProductId = null);

    private sealed record ProjectionCartOutput(string CartId);

    private sealed class ProjectionCountingRestModule : RestBehaviorModuleBase
    {
        public int ConfigureRestBehaviorsCallCount { get; private set; }

        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.projection.module",
            "Projection Test Module",
            "Exercises normalized REST behavior projection reuse.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            ConfigureRestBehaviorsCallCount++;

            behaviors.Group("/tests/cart")
                .MapGet<ProjectionCartBehavior>("/{cartId}");
        }
    }

    private sealed class RecordingBehaviorModuleBuilder : IBehaviorModuleBuilder
    {
        public List<OwnedBehaviorRegistration> Registrations { get; } = [];

        public IBehaviorModuleBuilder Add<TBehavior>()
            where TBehavior : class
        {
            Registrations.Add(new OwnedBehaviorRegistration(typeof(TBehavior), HasExplicitTopologyOverride: false));
            return this;
        }

        public IBehaviorModuleBuilder Add<TBehavior>(Action<IBehaviorTopologyBuilder> configureTopology)
            where TBehavior : class
        {
            ArgumentNullException.ThrowIfNull(configureTopology);
            Registrations.Add(new OwnedBehaviorRegistration(typeof(TBehavior), HasExplicitTopologyOverride: true));
            return this;
        }
    }

    private sealed record OwnedBehaviorRegistration(Type BehaviorType, bool HasExplicitTopologyOverride);
}
