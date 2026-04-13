using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Http.Abstractions;
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
                Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, getEndpoint.AuthoringStyle);
                Assert.Null(getEndpoint.ConfigureEndpoint);
            },
            postEndpoint =>
            {
                Assert.Equal(RestBehaviorHttpMethod.Post, postEndpoint.Method);
                Assert.Equal(typeof(ProjectionCartBehavior), postEndpoint.BehaviorType);
                Assert.Equal("/{cartId}/items", postEndpoint.Pattern);
                Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, postEndpoint.AuthoringStyle);
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
    public void RestBehaviorModuleBuilderBuildMapsProfileMetadataIntoProjectionAndSeedsGroupVersion()
    {
        var builder = new RestBehaviorModuleBuilder();

        builder.Group("/tests/profile-cart")
            .MapProfile<ProfileProjectionGetBehavior>();

        var projection = builder.Build();

        Assert.Single(projection.OwnershipRegistrations);

        var routeGroup = Assert.Single(projection.Groups);
        Assert.Equal("/tests/profile-cart", routeGroup.Prefix);
        Assert.Equal(3, routeGroup.ApiVersionMajor);
        Assert.False(routeGroup.HasExplicitApiVersion);
        Assert.Equal("tests.profile.projection.get", routeGroup.ProfileApiVersionSourceBehaviorId);

        var endpoint = Assert.Single(routeGroup.Endpoints);
        Assert.Equal(RestBehaviorHttpMethod.Get, endpoint.Method);
        Assert.Equal(typeof(ProfileProjectionGetBehavior), endpoint.BehaviorType);
        Assert.Equal("/{cartId}", endpoint.Pattern);
        Assert.Empty(endpoint.Bindings);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, endpoint.AuthoringStyle);
    }

    [Fact]
    public void RestBehaviorModuleBuilderBuildLetsExplicitGroupApiVersionOverrideProfileVersion()
    {
        var builder = new RestBehaviorModuleBuilder();

        builder.Group("/tests/profile-cart")
            .ApiVersion(7)
            .MapProfile<ProfileProjectionGetBehavior>();

        var routeGroup = Assert.Single(builder.Build().Groups);

        Assert.Equal(7, routeGroup.ApiVersionMajor);
        Assert.True(routeGroup.HasExplicitApiVersion);
        Assert.Null(routeGroup.ProfileApiVersionSourceBehaviorId);
    }

    [Fact]
    public void RestBehaviorModuleBuilderRejectsProfileMappingsWithoutRestProfileMetadata()
    {
        var builder = new RestBehaviorModuleBuilder();
        var group = builder.Group("/tests/profile-cart");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            group.MapProfile<ProjectionCartBehavior>());

        Assert.Contains("MapProfile<TBehavior>()", exception.Message, StringComparison.Ordinal);
        Assert.Contains("BehaviorRestProfile", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RestBehaviorModuleBuilderRejectsConflictingProfileApiVersionsWithinOneGroup()
    {
        var builder = new RestBehaviorModuleBuilder();
        var group = builder.Group("/tests/profile-cart");

        group.MapProfile<ProfileProjectionGetBehavior>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            group.MapProfile<ProfileProjectionConflictBehavior>());

        Assert.Contains("conflicting profile API major versions", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tests.profile.projection.get", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tests.profile.projection.conflict", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestBehaviorModuleBuilderBuildMapsProfileBindingsIntoProjection()
    {
        var builder = new RestBehaviorModuleBuilder();

        builder.Group("/tests/profile-cart")
            .MapProfile<ProfileProjectionBoundBehavior>();

        var endpoint = Assert.Single(Assert.Single(builder.Build().Groups).Endpoints);

        Assert.Collection(
            endpoint.Bindings,
            orderId =>
            {
                Assert.Equal("CartId", orderId.PropertyName);
                Assert.Equal(BehaviorRestBindingSource.Route, orderId.Source);
                Assert.Equal("cartId", orderId.Name);
            },
            quantity =>
            {
                Assert.Equal("Quantity", quantity.PropertyName);
                Assert.Equal(BehaviorRestBindingSource.Query, quantity.Source);
                Assert.Equal("quantity", quantity.Name);
            },
            correlationId =>
            {
                Assert.Equal("CorrelationId", correlationId.PropertyName);
                Assert.Equal(BehaviorRestBindingSource.Header, correlationId.Source);
                Assert.Equal("X-Correlation-Id", correlationId.Name);
            },
            note =>
            {
                Assert.Equal("Note", note.PropertyName);
                Assert.Equal(BehaviorRestBindingSource.Body, note.Source);
                Assert.Equal("note", note.Name);
            });
    }

    [Fact]
    public void RestBehaviorModuleBuilderRejectsProfileBindingsForUnknownInputProperty()
    {
        var builder = new RestBehaviorModuleBuilder();
        var group = builder.Group("/tests/profile-cart");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            group.MapProfile<ProfileProjectionUnknownBindingBehavior>());

        Assert.Contains("unknown input property", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestBehaviorModuleBuilderRejectsProfileBindingsForScalarInput()
    {
        var builder = new RestBehaviorModuleBuilder();
        var group = builder.Group("/tests/profile-cart");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            group.MapProfile<ProfileProjectionScalarBindingBehavior>());

        Assert.Contains("scalar input type", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestBehaviorModuleBuilderRejectsProfileBodyBindingsForGetEndpoints()
    {
        var builder = new RestBehaviorModuleBuilder();
        var group = builder.Group("/tests/profile-cart");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            group.MapProfile<ProfileProjectionGetBodyBindingBehavior>());

        Assert.Contains("cannot bind input property", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Get", exception.Message, StringComparison.OrdinalIgnoreCase);
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

    [AppBehavior("tests.profile.projection.get")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{cartId}", ApiVersionMajor = 3)]
    private sealed class ProfileProjectionGetBehavior : IAppBehavior<ProjectionCartInput, ProjectionCartOutput>
    {
        public Task<ProjectionCartOutput> HandleAsync(
            ProjectionCartInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProjectionCartOutput(input.CartId));
        }
    }

    [AppBehavior("tests.profile.projection.conflict")]
    [BehaviorRestProfile(BehaviorRestMethod.Post, "/{cartId}/items", ApiVersionMajor = 4)]
    private sealed class ProfileProjectionConflictBehavior : IAppBehavior<ProjectionCartInput, ProjectionCartOutput>
    {
        public Task<ProjectionCartOutput> HandleAsync(
            ProjectionCartInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProjectionCartOutput(input.CartId));
        }
    }

    [AppBehavior("tests.profile.projection.bound")]
    [BehaviorRestProfile(BehaviorRestMethod.Post, "/{cartId}/items", ApiVersionMajor = 6)]
    [BehaviorRestBinding(nameof(ProfileProjectionBoundInput.CartId), BehaviorRestBindingSource.Route, Name = "cartId")]
    [BehaviorRestBinding(nameof(ProfileProjectionBoundInput.Quantity), BehaviorRestBindingSource.Query, Name = "quantity")]
    [BehaviorRestBinding(nameof(ProfileProjectionBoundInput.CorrelationId), BehaviorRestBindingSource.Header, Name = "X-Correlation-Id")]
    [BehaviorRestBinding(nameof(ProfileProjectionBoundInput.Note), BehaviorRestBindingSource.Body, Name = "note")]
    private sealed class ProfileProjectionBoundBehavior : IAppBehavior<ProfileProjectionBoundInput, ProjectionCartOutput>
    {
        public Task<ProjectionCartOutput> HandleAsync(
            ProfileProjectionBoundInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProjectionCartOutput(input.CartId));
        }
    }

    [AppBehavior("tests.profile.projection.unknown-binding")]
    [BehaviorRestProfile(BehaviorRestMethod.Post, "/{cartId}/items", ApiVersionMajor = 6)]
    [BehaviorRestBinding("MissingProperty", BehaviorRestBindingSource.Query, Name = "quantity")]
    private sealed class ProfileProjectionUnknownBindingBehavior : IAppBehavior<ProfileProjectionBoundInput, ProjectionCartOutput>
    {
        public Task<ProjectionCartOutput> HandleAsync(
            ProfileProjectionBoundInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProjectionCartOutput(input.CartId));
        }
    }

    [AppBehavior("tests.profile.projection.scalar-binding")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{value}", ApiVersionMajor = 6)]
    [BehaviorRestBinding("Value", BehaviorRestBindingSource.Route, Name = "value")]
    private sealed class ProfileProjectionScalarBindingBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(
            string input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(input);
        }
    }

    [AppBehavior("tests.profile.projection.get-body-binding")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{cartId}", ApiVersionMajor = 6)]
    [BehaviorRestBinding(nameof(ProfileProjectionBoundInput.Note), BehaviorRestBindingSource.Body, Name = "note")]
    private sealed class ProfileProjectionGetBodyBindingBehavior : IAppBehavior<ProfileProjectionBoundInput, ProjectionCartOutput>
    {
        public Task<ProjectionCartOutput> HandleAsync(
            ProfileProjectionBoundInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProjectionCartOutput(input.CartId));
        }
    }

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

    private sealed record ProfileProjectionBoundInput(
        string CartId,
        int Quantity,
        string? CorrelationId,
        string? Note);
}
