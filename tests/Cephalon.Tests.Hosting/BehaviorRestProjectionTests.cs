using System.Reflection;
using System.Reflection.Emit;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Hosting;
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
    public void RestBehaviorModuleBuilderBuildMapsGeneratedProfilesIntoProjectionAndSeedsGroupVersion()
    {
        var builder = new RestBehaviorModuleBuilder(typeof(GeneratedProjectionRestModule));

        builder.Group("/tests/generated-projection")
            .MapGeneratedProfiles("tests.generated.projection.publish");

        var projection = builder.Build();

        Assert.Equal(2, projection.OwnershipRegistrations.Count);

        var routeGroup = Assert.Single(projection.Groups);
        Assert.Equal("/tests/generated-projection", routeGroup.Prefix);
        Assert.Equal(9, routeGroup.ApiVersionMajor);
        Assert.False(routeGroup.HasExplicitApiVersion);
        Assert.Equal("tests.generated.projection.publish.get", routeGroup.ProfileApiVersionSourceBehaviorId);

        Assert.Collection(
            routeGroup.Endpoints,
            getEndpoint =>
            {
                Assert.Equal(RestBehaviorHttpMethod.Get, getEndpoint.Method);
                Assert.Equal(typeof(GeneratedProjectionGetBehavior), getEndpoint.BehaviorType);
                Assert.Equal("/{cartId}", getEndpoint.Pattern);
                Assert.Empty(getEndpoint.Bindings);
                Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, getEndpoint.AuthoringStyle);
            },
            postEndpoint =>
            {
                Assert.Equal(RestBehaviorHttpMethod.Post, postEndpoint.Method);
                Assert.Equal(typeof(GeneratedProjectionPostBehavior), postEndpoint.BehaviorType);
                Assert.Equal("/{cartId}/items", postEndpoint.Pattern);
                Assert.Empty(postEndpoint.Bindings);
                Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, postEndpoint.AuthoringStyle);
            });
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverPublishesHigherPrecedenceDslCandidateAndSuppressesProfileCandidate()
    {
        var builder = new RestBehaviorModuleBuilder();
        var group = builder.Group("/tests/profile-precedence")
            .ApiVersion(8);

        group.MapProfile<ProfileProjectionGetBehavior>();
        group.MapGet<ProfileProjectionGetBehavior>("/explicit/{cartId}");

        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            new ModuleDescriptor(
                "tests.rest.profile-precedence",
                "Profile Precedence Module",
                "Exercises precedence resolution between module DSL and profile shorthand.",
                version: "1.0.0"),
            new ApiRoutesOptions(),
            builder.Build().Groups);

        Assert.Equal(2, candidates.Count);

        var published = Assert.Single(candidates, static item =>
            item.Candidate.Status == RestEndpointCandidateStatus.Published);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, published.Candidate.AuthoringStyle);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleDslPrecedenceRank, published.Candidate.PrecedenceRank);
        Assert.Equal("tests.profile.projection.get", published.Candidate.ProjectedEndpoint.BehaviorId);
        Assert.Equal("/api/v8/tests/profile-precedence/explicit/{cartId}", published.Candidate.ProjectedEndpoint.RoutePattern);

        var suppressed = Assert.Single(candidates, static item =>
            item.Candidate.Status == RestEndpointCandidateStatus.Suppressed);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, suppressed.Candidate.AuthoringStyle);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfilePrecedenceRank, suppressed.Candidate.PrecedenceRank);
        Assert.Equal("tests.profile.projection.get", suppressed.Candidate.ProjectedEndpoint.BehaviorId);
        Assert.Equal("/api/v8/tests/profile-precedence/{cartId}", suppressed.Candidate.ProjectedEndpoint.RoutePattern);
        Assert.Equal(published.Candidate.Id, suppressed.Candidate.SuppressedByCandidateId);
        Assert.Contains("higher-precedence authoring style", suppressed.Candidate.SuppressionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverPublishesHigherPrecedenceProfileCandidateAndSuppressesGeneratedCandidate()
    {
        var builder = new RestBehaviorModuleBuilder(typeof(GeneratedProjectionRestModule));
        var group = builder.Group("/tests/generated-profile-precedence")
            .ApiVersion(10);

        group.MapGeneratedProfiles("tests.generated.projection.precedence");
        group.MapProfile<GeneratedProjectionProfilePrecedenceBehavior>();

        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            new ModuleDescriptor(
                "tests.rest.generated-profile-precedence",
                "Generated Profile Precedence Module",
                "Exercises precedence resolution between explicit profile and generated shorthand.",
                version: "1.0.0"),
            new ApiRoutesOptions(),
            builder.Build().Groups);

        Assert.Equal(2, candidates.Count);

        var published = Assert.Single(candidates, static item =>
            item.Candidate.Status == RestEndpointCandidateStatus.Published);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, published.Candidate.AuthoringStyle);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfilePrecedenceRank, published.Candidate.PrecedenceRank);
        Assert.Equal("tests.generated.projection.precedence.lookup", published.Candidate.ProjectedEndpoint.BehaviorId);
        Assert.Equal("/api/v10/tests/generated-profile-precedence/{cartId}", published.Candidate.ProjectedEndpoint.RoutePattern);

        var suppressed = Assert.Single(candidates, static item =>
            item.Candidate.Status == RestEndpointCandidateStatus.Suppressed);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, suppressed.Candidate.AuthoringStyle);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedPrecedenceRank, suppressed.Candidate.PrecedenceRank);
        Assert.Equal("tests.generated.projection.precedence.lookup", suppressed.Candidate.ProjectedEndpoint.BehaviorId);
        Assert.Equal("/api/v10/tests/generated-profile-precedence/{cartId}", suppressed.Candidate.ProjectedEndpoint.RoutePattern);
        Assert.Equal(published.Candidate.Id, suppressed.Candidate.SuppressedByCandidateId);
        Assert.Contains("higher-precedence authoring style", suppressed.Candidate.SuppressionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverPublishesHigherPrecedenceDslCandidateAndSuppressesGeneratedAndProfileCandidates()
    {
        var builder = new RestBehaviorModuleBuilder(typeof(GeneratedProjectionRestModule));
        var group = builder.Group("/tests/generated-three-way-precedence")
            .ApiVersion(11);

        group.MapGeneratedProfiles("tests.generated.projection.threeway");
        group.MapProfile<GeneratedProjectionThreeWayBehavior>();
        group.MapGet<GeneratedProjectionThreeWayBehavior>("/explicit/{cartId}");

        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            new ModuleDescriptor(
                "tests.rest.generated-three-way-precedence",
                "Generated Three-Way Precedence Module",
                "Exercises precedence resolution across explicit DSL, profile shorthand, and generated shorthand.",
                version: "1.0.0"),
            new ApiRoutesOptions(),
            builder.Build().Groups);

        Assert.Equal(3, candidates.Count);

        var published = Assert.Single(candidates, static item =>
            item.Candidate.Status == RestEndpointCandidateStatus.Published);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, published.Candidate.AuthoringStyle);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleDslPrecedenceRank, published.Candidate.PrecedenceRank);
        Assert.Equal("/api/v11/tests/generated-three-way-precedence/explicit/{cartId}", published.Candidate.ProjectedEndpoint.RoutePattern);

        var suppressedProfile = Assert.Single(candidates, static item =>
            item.Candidate.Status == RestEndpointCandidateStatus.Suppressed &&
            string.Equals(item.Candidate.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfilePrecedenceRank, suppressedProfile.Candidate.PrecedenceRank);
        Assert.Equal(published.Candidate.Id, suppressedProfile.Candidate.SuppressedByCandidateId);

        var suppressedGenerated = Assert.Single(candidates, static item =>
            item.Candidate.Status == RestEndpointCandidateStatus.Suppressed &&
            string.Equals(item.Candidate.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedPrecedenceRank, suppressedGenerated.Candidate.PrecedenceRank);
        Assert.Equal(published.Candidate.Id, suppressedGenerated.Candidate.SuppressedByCandidateId);
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverLetsGeneratedCandidatePublishWhenProfileCandidateIsSuppressedByGovernance()
    {
        var builder = new RestBehaviorModuleBuilder(typeof(GeneratedProjectionRestModule));
        var group = builder.Group("/tests/generated-profile-governance")
            .ApiVersion(10);

        group.MapGeneratedProfiles("tests.generated.projection.precedence");
        group.MapProfile<GeneratedProjectionProfilePrecedenceBehavior>();

        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            new ModuleDescriptor(
                "tests.rest.generated-profile-governance",
                "Generated Profile Governance Module",
                "Exercises governance-driven suppression between explicit profile and generated shorthand.",
                version: "1.0.0"),
            new ApiRoutesOptions(),
            builder.Build().Groups,
            [
                new RestEndpointSuppressionOptions(
                    id: "prefer-generated",
                    behaviorIds: ["tests.generated.projection.precedence.lookup"],
                    authoringStyles: [RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle])
            ]);

        Assert.Equal(2, candidates.Count);

        var published = Assert.Single(candidates, static item =>
            item.Candidate.Status == RestEndpointCandidateStatus.Published);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, published.Candidate.AuthoringStyle);
        Assert.Equal("tests.generated.projection.precedence.lookup", published.Candidate.ProjectedEndpoint.BehaviorId);
        Assert.Null(published.Candidate.SuppressedByCandidateId);
        Assert.Null(published.Candidate.SuppressedBySuppressionId);

        var suppressed = Assert.Single(candidates, static item =>
            item.Candidate.Status == RestEndpointCandidateStatus.Suppressed);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, suppressed.Candidate.AuthoringStyle);
        Assert.Equal("prefer-generated", suppressed.Candidate.SuppressedBySuppressionId);
        Assert.Null(suppressed.Candidate.SuppressedByCandidateId);
        Assert.Contains("prefer-generated", suppressed.Candidate.SuppressionReason, StringComparison.Ordinal);
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverDefaultsGovernanceSuppressionToShorthandAuthoringStylesOnly()
    {
        var builder = new RestBehaviorModuleBuilder(typeof(GeneratedProjectionRestModule));
        var group = builder.Group("/tests/generated-three-way-governance")
            .ApiVersion(11);

        group.MapGeneratedProfiles("tests.generated.projection.threeway");
        group.MapProfile<GeneratedProjectionThreeWayBehavior>();
        group.MapGet<GeneratedProjectionThreeWayBehavior>("/explicit/{cartId}");

        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            new ModuleDescriptor(
                "tests.rest.generated-three-way-governance",
                "Generated Three-Way Governance Module",
                "Exercises default shorthand-only governance suppression.",
                version: "1.0.0"),
            new ApiRoutesOptions(),
            builder.Build().Groups,
            [
                new RestEndpointSuppressionOptions(
                    id: "hide-shorthand",
                    behaviorIds: ["tests.generated.projection.threeway.lookup"])
            ]);

        Assert.Equal(3, candidates.Count);

        var published = Assert.Single(candidates, static item =>
            item.Candidate.Status == RestEndpointCandidateStatus.Published);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, published.Candidate.AuthoringStyle);
        Assert.Null(published.Candidate.SuppressedBySuppressionId);

        var suppressedProfile = Assert.Single(candidates, static item =>
            item.Candidate.Status == RestEndpointCandidateStatus.Suppressed &&
            string.Equals(item.Candidate.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal("hide-shorthand", suppressedProfile.Candidate.SuppressedBySuppressionId);
        Assert.Null(suppressedProfile.Candidate.SuppressedByCandidateId);

        var suppressedGenerated = Assert.Single(candidates, static item =>
            item.Candidate.Status == RestEndpointCandidateStatus.Suppressed &&
            string.Equals(item.Candidate.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal("hide-shorthand", suppressedGenerated.Candidate.SuppressedBySuppressionId);
        Assert.Null(suppressedGenerated.Candidate.SuppressedByCandidateId);
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverPrefersTheMostSpecificGovernanceRuleWhenMultipleRulesMatch()
    {
        var builder = new RestBehaviorModuleBuilder(typeof(GeneratedProjectionRestModule));
        var group = builder.Group("/tests/generated-specific-governance")
            .ApiVersion(12);

        group.MapGeneratedProfiles("tests.generated.projection.precedence");
        group.MapProfile<GeneratedProjectionProfilePrecedenceBehavior>();

        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            new ModuleDescriptor(
                "tests.rest.generated-specific-governance",
                "Generated Specific Governance Module",
                "Exercises deterministic governance precedence across multiple matching rules.",
                version: "1.0.0"),
            new ApiRoutesOptions(),
            builder.Build().Groups,
            [
                new RestEndpointSuppressionOptions(
                    id: "module-rule",
                    sourceModuleIds: ["tests.rest.generated-specific-governance"],
                    authoringStyles: [RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle]),
                new RestEndpointSuppressionOptions(
                    id: "behavior-module-rule",
                    behaviorIds: ["tests.generated.projection.precedence.lookup"],
                    sourceModuleIds: ["tests.rest.generated-specific-governance"],
                    authoringStyles: [RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle])
            ]);

        var published = Assert.Single(candidates, static item =>
            item.Candidate.Status == RestEndpointCandidateStatus.Published);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, published.Candidate.AuthoringStyle);

        var suppressed = Assert.Single(candidates, static item =>
            item.Candidate.Status == RestEndpointCandidateStatus.Suppressed);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, suppressed.Candidate.AuthoringStyle);
        Assert.Equal("behavior-module-rule", suppressed.Candidate.SuppressedBySuppressionId);
    }

    [Fact]
    public void RestEndpointSuppressionOptionsRejectRulesWithoutBehaviorOrModuleTargets()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new RestEndpointSuppressionOptions(
                id: "invalid",
                authoringStyles: [RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle]));

        Assert.Contains("behavior id or source module id", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverAppliesApiVersionOverrideToShorthandCandidates()
    {
        var builder = new RestBehaviorModuleBuilder(typeof(GeneratedProjectionRestModule));
        builder.Group("/tests/generated-override")
            .MapGeneratedProfiles("tests.generated.projection.precedence");

        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            new ModuleDescriptor(
                "tests.rest.generated-override",
                "Generated Override Module",
                "Exercises shorthand API-version override resolution.",
                version: "1.0.0"),
            new ApiRoutesOptions(),
            builder.Build().Groups,
            overrides:
            [
                new RestEndpointOverrideOptions(
                    id: "prefer-v6",
                    behaviorIds: ["tests.generated.projection.precedence.lookup"],
                    apiVersionMajor: 6)
            ]);

        var candidate = Assert.Single(candidates);
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Candidate.Status);
        Assert.Equal("prefer-v6", candidate.Candidate.AppliedOverrideId);
        Assert.Equal("/api/v6/tests/generated-override/{cartId}", candidate.Candidate.ProjectedEndpoint.RoutePattern);
        Assert.Equal("v6", candidate.Candidate.ProjectedEndpoint.OpenApiDocumentName);
        Assert.Equal(6, candidate.Candidate.ProjectedEndpoint.ApiVersionMajor);
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverAppliesMethodOverrideToShorthandCandidates()
    {
        var builder = new RestBehaviorModuleBuilder(typeof(GeneratedProjectionRestModule));
        builder.Group("/tests/generated-method-override")
            .MapGeneratedProfiles("tests.generated.projection.precedence");

        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            new ModuleDescriptor(
                "tests.rest.generated-method-override",
                "Generated Method Override Module",
                "Exercises shorthand HTTP-method override resolution.",
                version: "1.0.0"),
            new ApiRoutesOptions(),
            builder.Build().Groups,
            overrides:
            [
                new RestEndpointOverrideOptions(
                    id: "prefer-delete",
                    behaviorIds: ["tests.generated.projection.precedence.lookup"],
                    method: "DELETE")
            ]);

        var candidate = Assert.Single(candidates);
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Candidate.Status);
        Assert.Equal("prefer-delete", candidate.Candidate.AppliedOverrideId);
        Assert.Equal("DELETE", candidate.Candidate.ProjectedEndpoint.Method);
        Assert.Equal("/api/v10/tests/generated-method-override/{cartId}", candidate.Candidate.ProjectedEndpoint.RoutePattern);
        Assert.Equal("v10", candidate.Candidate.ProjectedEndpoint.OpenApiDocumentName);
        Assert.Equal(10, candidate.Candidate.ProjectedEndpoint.ApiVersionMajor);
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverAppliesPatternOverrideToShorthandCandidates()
    {
        var builder = new RestBehaviorModuleBuilder(typeof(GeneratedProjectionRestModule));
        builder.Group("/tests/generated-pattern-override")
            .MapGeneratedProfiles("tests.generated.projection.precedence");

        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            new ModuleDescriptor(
                "tests.rest.generated-pattern-override",
                "Generated Pattern Override Module",
                "Exercises shorthand route-pattern override resolution.",
                version: "1.0.0"),
            new ApiRoutesOptions(),
            builder.Build().Groups,
            overrides:
            [
                new RestEndpointOverrideOptions(
                    id: "prefer-lookup-path",
                    behaviorIds: ["tests.generated.projection.precedence.lookup"],
                    pattern: "/lookup/{cartId}")
            ]);

        var candidate = Assert.Single(candidates);
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Candidate.Status);
        Assert.Equal("prefer-lookup-path", candidate.Candidate.AppliedOverrideId);
        Assert.Equal("/lookup/{cartId}", candidate.Candidate.ProjectedEndpoint.Metadata["relativePattern"]);
        Assert.Equal("/api/v10/tests/generated-pattern-override/lookup/{cartId}", candidate.Candidate.ProjectedEndpoint.RoutePattern);
        Assert.Equal("v10", candidate.Candidate.ProjectedEndpoint.OpenApiDocumentName);
        Assert.Equal(10, candidate.Candidate.ProjectedEndpoint.ApiVersionMajor);
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverAppliesBindingOverrideToShorthandCandidates()
    {
        var builder = new RestBehaviorModuleBuilder();
        builder.Group("/tests/profile-binding-override")
            .MapProfile<ProfileProjectionBoundBehavior>();

        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            new ModuleDescriptor(
                "tests.rest.profile-binding-override",
                "Profile Binding Override Module",
                "Exercises shorthand binding-plan override resolution.",
                version: "1.0.0"),
            new ApiRoutesOptions(),
            builder.Build().Groups,
            overrides:
            [
                new RestEndpointOverrideOptions(
                    id: "prefer-short-bindings",
                    behaviorIds: ["tests.profile.projection.bound"],
                    bindings:
                    [
                        new RestEndpointBindingDescriptor("Quantity", RestEndpointBindingSource.Query, "qty"),
                        new RestEndpointBindingDescriptor("CorrelationId", RestEndpointBindingSource.Header, "X-Trace-Id"),
                        new RestEndpointBindingDescriptor("Note", RestEndpointBindingSource.Body, "memo")
                    ])
            ]);

        var candidate = Assert.Single(candidates);
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Candidate.Status);
        Assert.Equal("prefer-short-bindings", candidate.Candidate.AppliedOverrideId);
        Assert.Equal("/api/v6/tests/profile-binding-override/{cartId}/items", candidate.Candidate.ProjectedEndpoint.RoutePattern);
        Assert.Equal("v6", candidate.Candidate.ProjectedEndpoint.OpenApiDocumentName);
        Assert.Equal(6, candidate.Candidate.ProjectedEndpoint.ApiVersionMajor);
        Assert.Equal(3, candidate.Candidate.ProjectedEndpoint.BindingDescriptors.Count);
        Assert.DoesNotContain(candidate.Candidate.ProjectedEndpoint.BindingDescriptors, static binding =>
            string.Equals(binding.PropertyName, "CartId", StringComparison.Ordinal));
        Assert.Contains(candidate.Candidate.ProjectedEndpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "Quantity" &&
            binding.Source == RestEndpointBindingSource.Query &&
            binding.Name == "qty");
        Assert.Contains(candidate.Candidate.ProjectedEndpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "CorrelationId" &&
            binding.Source == RestEndpointBindingSource.Header &&
            binding.Name == "X-Trace-Id");
        Assert.Contains(candidate.Candidate.ProjectedEndpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "Note" &&
            binding.Source == RestEndpointBindingSource.Body &&
            binding.Name == "memo");
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverAllowsPlaceholderRenameWhenOverrideBindingsCoverTheRenamedRouteSet()
    {
        var builder = new RestBehaviorModuleBuilder();
        builder.Group("/tests/profile-binding-rename")
            .MapProfile<ProfileProjectionBoundBehavior>();

        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            new ModuleDescriptor(
                "tests.rest.profile-binding-rename",
                "Profile Binding Rename Module",
                "Exercises placeholder-renaming override resolution when the effective route-binding plan is explicit.",
                version: "1.0.0"),
            new ApiRoutesOptions(),
            builder.Build().Groups,
            overrides:
            [
                new RestEndpointOverrideOptions(
                    id: "prefer-renamed-placeholder",
                    behaviorIds: ["tests.profile.projection.bound"],
                    pattern: "/lookup/{id}/items",
                    bindings:
                    [
                        new RestEndpointBindingDescriptor("CartId", RestEndpointBindingSource.Route, "id"),
                        new RestEndpointBindingDescriptor("Quantity", RestEndpointBindingSource.Query, "quantity"),
                        new RestEndpointBindingDescriptor("CorrelationId", RestEndpointBindingSource.Header, "X-Correlation-Id"),
                        new RestEndpointBindingDescriptor("Note", RestEndpointBindingSource.Body, "note")
                    ])
            ]);

        var candidate = Assert.Single(candidates);
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Candidate.Status);
        Assert.Equal("prefer-renamed-placeholder", candidate.Candidate.AppliedOverrideId);
        Assert.Equal("/lookup/{id}/items", candidate.Candidate.ProjectedEndpoint.Metadata["relativePattern"]);
        Assert.Equal("/api/v6/tests/profile-binding-rename/lookup/{id}/items", candidate.Candidate.ProjectedEndpoint.RoutePattern);
        Assert.Equal(4, candidate.Candidate.ProjectedEndpoint.BindingDescriptors.Count);
        Assert.Contains(candidate.Candidate.ProjectedEndpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "CartId" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "id");
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverAllowsPlaceholderRemovalWhenAffectedPropertiesStayExplicitlyBound()
    {
        var builder = new RestBehaviorModuleBuilder();
        builder.Group("/tests/profile-binding-removal")
            .MapProfile<ProfileProjectionBoundBehavior>();

        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            new ModuleDescriptor(
                "tests.rest.profile-binding-removal",
                "Profile Binding Removal Module",
                "Exercises placeholder-removal override resolution when both route and affected property coverage stay explicit.",
                version: "1.0.0"),
            new ApiRoutesOptions(),
            builder.Build().Groups,
            overrides:
            [
                new RestEndpointOverrideOptions(
                    id: "prefer-query-identity",
                    behaviorIds: ["tests.profile.projection.bound"],
                    pattern: "/lookup/items",
                    bindings:
                    [
                        new RestEndpointBindingDescriptor("CartId", RestEndpointBindingSource.Query, "cartId"),
                        new RestEndpointBindingDescriptor("Quantity", RestEndpointBindingSource.Query, "quantity"),
                        new RestEndpointBindingDescriptor("CorrelationId", RestEndpointBindingSource.Header, "X-Correlation-Id"),
                        new RestEndpointBindingDescriptor("Note", RestEndpointBindingSource.Body, "note")
                    ])
            ]);

        var candidate = Assert.Single(candidates);
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Candidate.Status);
        Assert.Equal("prefer-query-identity", candidate.Candidate.AppliedOverrideId);
        Assert.Equal("/lookup/items", candidate.Candidate.ProjectedEndpoint.Metadata["relativePattern"]);
        Assert.Equal("/api/v6/tests/profile-binding-removal/lookup/items", candidate.Candidate.ProjectedEndpoint.RoutePattern);
        Assert.Equal(4, candidate.Candidate.ProjectedEndpoint.BindingDescriptors.Count);
        Assert.DoesNotContain(candidate.Candidate.ProjectedEndpoint.BindingDescriptors, static binding =>
            binding.Source == RestEndpointBindingSource.Route);
        Assert.Contains(candidate.Candidate.ProjectedEndpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "CartId" &&
            binding.Source == RestEndpointBindingSource.Query &&
            binding.Name == "cartId");
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverRejectsMethodOverrideWhenEffectiveBindingsNoLongerMatchRestContract()
    {
        var builder = new RestBehaviorModuleBuilder();
        builder.Group("/tests/profile-binding-invalid-method")
            .MapProfile<ProfileProjectionBoundBehavior>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            RestBehaviorProjectionCandidateResolver.ResolveCandidates(
                new ModuleDescriptor(
                    "tests.rest.profile-binding-invalid-method",
                    "Profile Binding Invalid Method Module",
                    "Exercises fail-fast validation when a shorthand method override leaves an invalid binding plan.",
                    version: "1.0.0"),
                new ApiRoutesOptions(),
                builder.Build().Groups,
                overrides:
                [
                    new RestEndpointOverrideOptions(
                        id: "prefer-get",
                        behaviorIds: ["tests.profile.projection.bound"],
                        method: "GET")
                ]));

        Assert.Contains("prefer-get", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not accept a request body", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverKeepsExplicitGroupApiVersionAuthoritativeOverHostOverride()
    {
        var builder = new RestBehaviorModuleBuilder(typeof(GeneratedProjectionRestModule));
        builder.Group("/tests/generated-override-explicit")
            .ApiVersion(8)
            .MapGeneratedProfiles("tests.generated.projection.precedence");

        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            new ModuleDescriptor(
                "tests.rest.generated-override-explicit",
                "Generated Override Explicit Module",
                "Exercises explicit group-version precedence over host overrides.",
                version: "1.0.0"),
            new ApiRoutesOptions(),
            builder.Build().Groups,
            overrides:
            [
                new RestEndpointOverrideOptions(
                    id: "prefer-v6",
                    behaviorIds: ["tests.generated.projection.precedence.lookup"],
                    apiVersionMajor: 6)
            ]);

        var candidate = Assert.Single(candidates);
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Candidate.Status);
        Assert.Null(candidate.Candidate.AppliedOverrideId);
        Assert.Equal("/api/v8/tests/generated-override-explicit/{cartId}", candidate.Candidate.ProjectedEndpoint.RoutePattern);
        Assert.Equal("v8", candidate.Candidate.ProjectedEndpoint.OpenApiDocumentName);
        Assert.Equal(8, candidate.Candidate.ProjectedEndpoint.ApiVersionMajor);
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverLetsMethodOverrideApplyWhileKeepingExplicitGroupApiVersionAuthoritative()
    {
        var builder = new RestBehaviorModuleBuilder(typeof(GeneratedProjectionRestModule));
        builder.Group("/tests/generated-method-override-explicit")
            .ApiVersion(8)
            .MapGeneratedProfiles("tests.generated.projection.precedence");

        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            new ModuleDescriptor(
                "tests.rest.generated-method-override-explicit",
                "Generated Method Override Explicit Module",
                "Exercises method override resolution when the owning group already chose its API version explicitly.",
                version: "1.0.0"),
            new ApiRoutesOptions(),
            builder.Build().Groups,
            overrides:
            [
                new RestEndpointOverrideOptions(
                    id: "prefer-delete-v6",
                    behaviorIds: ["tests.generated.projection.precedence.lookup"],
                    apiVersionMajor: 6,
                    method: "DELETE")
            ]);

        var candidate = Assert.Single(candidates);
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Candidate.Status);
        Assert.Equal("prefer-delete-v6", candidate.Candidate.AppliedOverrideId);
        Assert.Equal("DELETE", candidate.Candidate.ProjectedEndpoint.Method);
        Assert.Equal("/api/v8/tests/generated-method-override-explicit/{cartId}", candidate.Candidate.ProjectedEndpoint.RoutePattern);
        Assert.Equal("v8", candidate.Candidate.ProjectedEndpoint.OpenApiDocumentName);
        Assert.Equal(8, candidate.Candidate.ProjectedEndpoint.ApiVersionMajor);
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverLetsPatternOverrideApplyWhileKeepingExplicitGroupApiVersionAuthoritative()
    {
        var builder = new RestBehaviorModuleBuilder(typeof(GeneratedProjectionRestModule));
        builder.Group("/tests/generated-pattern-override-explicit")
            .ApiVersion(8)
            .MapGeneratedProfiles("tests.generated.projection.precedence");

        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            new ModuleDescriptor(
                "tests.rest.generated-pattern-override-explicit",
                "Generated Pattern Override Explicit Module",
                "Exercises pattern override resolution when the owning group already chose its API version explicitly.",
                version: "1.0.0"),
            new ApiRoutesOptions(),
            builder.Build().Groups,
            overrides:
            [
                new RestEndpointOverrideOptions(
                    id: "prefer-lookup-v6",
                    behaviorIds: ["tests.generated.projection.precedence.lookup"],
                    apiVersionMajor: 6,
                    pattern: "/lookup/{cartId}")
            ]);

        var candidate = Assert.Single(candidates);
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Candidate.Status);
        Assert.Equal("prefer-lookup-v6", candidate.Candidate.AppliedOverrideId);
        Assert.Equal("/lookup/{cartId}", candidate.Candidate.ProjectedEndpoint.Metadata["relativePattern"]);
        Assert.Equal("/api/v8/tests/generated-pattern-override-explicit/lookup/{cartId}", candidate.Candidate.ProjectedEndpoint.RoutePattern);
        Assert.Equal("v8", candidate.Candidate.ProjectedEndpoint.OpenApiDocumentName);
        Assert.Equal(8, candidate.Candidate.ProjectedEndpoint.ApiVersionMajor);
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverRejectsPatternOverridesThatChangeRoutePlaceholderSet()
    {
        var builder = new RestBehaviorModuleBuilder(typeof(GeneratedProjectionRestModule));
        builder.Group("/tests/generated-pattern-override-invalid")
            .MapGeneratedProfiles("tests.generated.projection.precedence");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            RestBehaviorProjectionCandidateResolver.ResolveCandidates(
                new ModuleDescriptor(
                    "tests.rest.generated-pattern-override-invalid",
                    "Generated Pattern Override Invalid Module",
                    "Exercises fail-fast validation for placeholder-changing pattern overrides.",
                    version: "1.0.0"),
                new ApiRoutesOptions(),
                builder.Build().Groups,
                overrides:
                [
                    new RestEndpointOverrideOptions(
                        id: "prefer-renamed-placeholder",
                        behaviorIds: ["tests.generated.projection.precedence.lookup"],
                        pattern: "/lookup/{id}")
                ]));

        Assert.Contains("explicit route-binding plan", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverRejectsPlaceholderRemovalWhenOriginalRouteCoverageReliesOnInference()
    {
        var builder = new RestBehaviorModuleBuilder();
        builder.Group("/tests/profile-binding-removal-inference")
            .MapProfile<ProfileProjectionBoundInferenceBehavior>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            RestBehaviorProjectionCandidateResolver.ResolveCandidates(
                new ModuleDescriptor(
                    "tests.rest.profile-binding-removal-inference",
                    "Profile Binding Removal Inference Module",
                    "Exercises fail-fast validation when placeholder removal would rewrite an inference-backed route.",
                    version: "1.0.0"),
                new ApiRoutesOptions(),
                builder.Build().Groups,
                overrides:
                [
                    new RestEndpointOverrideOptions(
                        id: "prefer-query-identity",
                        behaviorIds: ["tests.profile.projection.bound.inference"],
                        pattern: "/lookup/items",
                        bindings:
                        [
                            new RestEndpointBindingDescriptor("CartId", RestEndpointBindingSource.Query, "cartId"),
                            new RestEndpointBindingDescriptor("Quantity", RestEndpointBindingSource.Query, "quantity"),
                            new RestEndpointBindingDescriptor("CorrelationId", RestEndpointBindingSource.Header, "X-Correlation-Id"),
                            new RestEndpointBindingDescriptor("Note", RestEndpointBindingSource.Body, "note")
                        ])
                ]));

        Assert.Contains("original projection", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("explicit route-binding plan", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverRejectsPlaceholderRemovalWhenAffectedPropertiesAreNoLongerExplicitlyBound()
    {
        var builder = new RestBehaviorModuleBuilder();
        builder.Group("/tests/profile-binding-placeholder-shape")
            .MapProfile<ProfileProjectionBoundBehavior>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            RestBehaviorProjectionCandidateResolver.ResolveCandidates(
                new ModuleDescriptor(
                    "tests.rest.profile-binding-placeholder-shape",
                    "Profile Binding Placeholder Shape Module",
                    "Exercises fail-fast validation for placeholder additions or removals.",
                    version: "1.0.0"),
                new ApiRoutesOptions(),
                builder.Build().Groups,
                overrides:
                [
                    new RestEndpointOverrideOptions(
                        id: "prefer-no-route-placeholder",
                        behaviorIds: ["tests.profile.projection.bound"],
                        pattern: "/lookup",
                        bindings:
                        [
                            new RestEndpointBindingDescriptor("Quantity", RestEndpointBindingSource.Query, "quantity"),
                            new RestEndpointBindingDescriptor("CorrelationId", RestEndpointBindingSource.Header, "X-Correlation-Id"),
                            new RestEndpointBindingDescriptor("Note", RestEndpointBindingSource.Body, "note")
                        ])
                ]));

        Assert.Contains("affected original route-bound property", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverRejectsPatternOverridesThatAddPlaceholders()
    {
        var builder = new RestBehaviorModuleBuilder();
        builder.Group("/tests/profile-binding-placeholder-addition")
            .MapProfile<ProfileProjectionBoundBehavior>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            RestBehaviorProjectionCandidateResolver.ResolveCandidates(
                new ModuleDescriptor(
                    "tests.rest.profile-binding-placeholder-addition",
                    "Profile Binding Placeholder Addition Module",
                    "Exercises fail-fast validation for placeholder additions.",
                    version: "1.0.0"),
                new ApiRoutesOptions(),
                builder.Build().Groups,
                overrides:
                [
                    new RestEndpointOverrideOptions(
                        id: "prefer-added-placeholder",
                        behaviorIds: ["tests.profile.projection.bound"],
                        pattern: "/lookup/{cartId}/{tenantId}/items",
                        bindings:
                        [
                            new RestEndpointBindingDescriptor("CartId", RestEndpointBindingSource.Route, "cartId"),
                            new RestEndpointBindingDescriptor("Quantity", RestEndpointBindingSource.Query, "quantity"),
                            new RestEndpointBindingDescriptor("CorrelationId", RestEndpointBindingSource.Header, "X-Correlation-Id"),
                            new RestEndpointBindingDescriptor("Note", RestEndpointBindingSource.Body, "note")
                        ])
                ]));

        Assert.Contains("placeholder additions", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestBehaviorProjectionCandidateResolverPrefersTheMostSpecificApiVersionOverrideWhenMultipleRulesMatch()
    {
        var builder = new RestBehaviorModuleBuilder(typeof(GeneratedProjectionRestModule));
        builder.Group("/tests/generated-specific-override")
            .MapGeneratedProfiles("tests.generated.projection.precedence");

        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            new ModuleDescriptor(
                "tests.rest.generated-specific-override",
                "Generated Specific Override Module",
                "Exercises deterministic API-version override precedence.",
                version: "1.0.0"),
            new ApiRoutesOptions(),
            builder.Build().Groups,
            overrides:
            [
                new RestEndpointOverrideOptions(
                    id: "module-rule",
                    sourceModuleIds: ["tests.rest.generated-specific-override"],
                    apiVersionMajor: 7),
                new RestEndpointOverrideOptions(
                    id: "behavior-module-rule",
                    behaviorIds: ["tests.generated.projection.precedence.lookup"],
                    sourceModuleIds: ["tests.rest.generated-specific-override"],
                    apiVersionMajor: 6)
            ]);

        var candidate = Assert.Single(candidates);
        Assert.Equal("behavior-module-rule", candidate.Candidate.AppliedOverrideId);
        Assert.Equal(6, candidate.Candidate.ProjectedEndpoint.ApiVersionMajor);
        Assert.Equal("/api/v6/tests/generated-specific-override/{cartId}", candidate.Candidate.ProjectedEndpoint.RoutePattern);
    }

    [Fact]
    public void RestEndpointOverrideOptionsRejectRulesWithoutBehaviorOrModuleTargets()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new RestEndpointOverrideOptions(
                id: "invalid",
                apiVersionMajor: 6));

        Assert.Contains("behavior id or source module id", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestEndpointOverrideOptionsRejectRulesWithoutOverrideActions()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new RestEndpointOverrideOptions(
                id: "invalid",
                behaviorIds: ["tests.generated.projection.precedence.lookup"]));

        Assert.Contains("override action", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestEndpointOverrideOptionsRejectUnsupportedHttpMethod()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new RestEndpointOverrideOptions(
                id: "invalid",
                behaviorIds: ["tests.generated.projection.precedence.lookup"],
                method: "HEAD"));

        Assert.Contains("supported methods", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestEndpointOverrideOptionsRejectPatternsWithoutLeadingSlash()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new RestEndpointOverrideOptions(
                id: "invalid",
                behaviorIds: ["tests.generated.projection.precedence.lookup"],
                pattern: "lookup/{cartId}"));

        Assert.Contains("start with '/'", exception.Message, StringComparison.OrdinalIgnoreCase);
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
    public void BehaviorRestProfileResolverRejectsProfileBindingsForUnknownInputPropertyFromAttributeFallback()
    {
        var behaviorType = CreateDynamicProfileBehaviorType(
            "tests.profile.projection.unknown-binding.dynamic",
            BehaviorRestMethod.Post,
            "/{cartId}/items",
            typeof(DynamicProfileBindingInput),
            new DynamicProfileBindingDefinition("MissingProperty", BehaviorRestBindingSource.Query, "quantity"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            BehaviorRestProfileResolver.Resolve(behaviorType));

        Assert.Contains("unknown input property", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BehaviorRestProfileResolverRejectsProfileBindingsForScalarInputFromAttributeFallback()
    {
        var behaviorType = CreateDynamicProfileBehaviorType(
            "tests.profile.projection.scalar-binding.dynamic",
            BehaviorRestMethod.Get,
            "/{value}",
            typeof(string),
            new DynamicProfileBindingDefinition("Value", BehaviorRestBindingSource.Route, "value"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            BehaviorRestProfileResolver.Resolve(behaviorType));

        Assert.Contains("scalar input type", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BehaviorRestProfileResolverRejectsProfileBodyBindingsForGetEndpointsFromAttributeFallback()
    {
        var behaviorType = CreateDynamicProfileBehaviorType(
            "tests.profile.projection.get-body-binding.dynamic",
            BehaviorRestMethod.Get,
            "/{cartId}",
            typeof(DynamicProfileBindingInput),
            new DynamicProfileBindingDefinition(nameof(DynamicProfileBindingInput.Note), BehaviorRestBindingSource.Body, "note"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            BehaviorRestProfileResolver.Resolve(behaviorType));

        Assert.Contains("cannot bind input property", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Get", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BehaviorRestProfileResolverRejectsProfileBindingsWithoutSupportedSourceFromAttributeFallback()
    {
        var behaviorType = CreateDynamicProfileBehaviorType(
            "tests.profile.projection.unsupported-source.dynamic",
            BehaviorRestMethod.Post,
            "/{cartId}",
            typeof(DynamicProfileBindingInput),
            new DynamicProfileBindingDefinition(
                nameof(DynamicProfileBindingInput.CartId),
                Enum.ToObject(typeof(BehaviorRestBindingSource), 999),
                "cartId"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            BehaviorRestProfileResolver.Resolve(behaviorType));

        Assert.Contains("supported binding source", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BehaviorRestProfileResolverRejectsDuplicateProfileBindingsFromAttributeFallback()
    {
        var behaviorType = CreateDynamicProfileBehaviorType(
            "tests.profile.projection.duplicate-binding.dynamic",
            BehaviorRestMethod.Post,
            "/{cartId}",
            typeof(DynamicProfileBindingInput),
            new DynamicProfileBindingDefinition(nameof(DynamicProfileBindingInput.CartId), BehaviorRestBindingSource.Route, "cartId"),
            new DynamicProfileBindingDefinition(nameof(DynamicProfileBindingInput.CartId), BehaviorRestBindingSource.Query, "cartId"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            BehaviorRestProfileResolver.Resolve(behaviorType));

        Assert.Contains("multiple explicit bindings", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BehaviorRestProfileResolverRejectsProfileRouteBindingsWhenPlaceholderIsMissingFromPatternFromAttributeFallback()
    {
        var behaviorType = CreateDynamicProfileBehaviorType(
            "tests.profile.projection.missing-route-placeholder.dynamic",
            BehaviorRestMethod.Post,
            "/{cartId}/items",
            typeof(DynamicProfileBindingInput),
            new DynamicProfileBindingDefinition(nameof(DynamicProfileBindingInput.CartId), BehaviorRestBindingSource.Route, "missingCartId"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            BehaviorRestProfileResolver.Resolve(behaviorType));

        Assert.Contains("does not declare that placeholder", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("missingCartId", exception.Message, StringComparison.OrdinalIgnoreCase);
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

    private static Type CreateDynamicProfileBehaviorType(
        string behaviorId,
        BehaviorRestMethod method,
        string relativePattern,
        Type inputType,
        params DynamicProfileBindingDefinition[] bindings)
    {
        var assemblyName = new AssemblyName($"Cephalon.Tests.Hosting.DynamicProfiles.{Guid.NewGuid():N}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        var typeBuilder = moduleBuilder.DefineType(
            $"DynamicProfileBehavior_{Guid.NewGuid():N}",
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);

        var behaviorInterface = typeof(IAppBehavior<,>).MakeGenericType(inputType, typeof(string));
        typeBuilder.AddInterfaceImplementation(behaviorInterface);
        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

        typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
            typeof(AppBehaviorAttribute).GetConstructor([typeof(string)])!,
            [behaviorId]));

        typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
            typeof(BehaviorRestProfileAttribute).GetConstructor([typeof(BehaviorRestMethod), typeof(string)])!,
            [method, relativePattern],
            [typeof(BehaviorRestProfileAttribute).GetProperty(nameof(BehaviorRestProfileAttribute.ApiVersionMajor))!],
            [6]));

        var bindingConstructor = typeof(BehaviorRestBindingAttribute).GetConstructor(
            [typeof(string), typeof(BehaviorRestBindingSource)])!;
        var bindingNameProperty = typeof(BehaviorRestBindingAttribute).GetProperty(nameof(BehaviorRestBindingAttribute.Name))!;
        foreach (var binding in bindings)
        {
            if (binding.Name is null)
            {
                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                    bindingConstructor,
                    [binding.PropertyName, binding.SourceValue]));
            }
            else
            {
                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                    bindingConstructor,
                    [binding.PropertyName, binding.SourceValue],
                    [bindingNameProperty],
                    [binding.Name]));
            }
        }

        var handleAsyncMethod = typeBuilder.DefineMethod(
            nameof(IAppBehavior<object, string>.HandleAsync),
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
            typeof(Task<string>),
            [inputType, typeof(IBehaviorContext), typeof(CancellationToken)]);
        var il = handleAsyncMethod.GetILGenerator();
        il.Emit(OpCodes.Ldstr, "ok");
        il.Emit(OpCodes.Call, typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(typeof(string)));
        il.Emit(OpCodes.Ret);
        typeBuilder.DefineMethodOverride(handleAsyncMethod, behaviorInterface.GetMethod(nameof(IAppBehavior<object, string>.HandleAsync))!);

        return typeBuilder.CreateType()!;
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

    private sealed class GeneratedProjectionRestModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.generated-projection",
            "Generated Projection Test Module",
            "Provides module ownership context for generated REST projection tests.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
        }
    }

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

    [AppBehavior("tests.generated.projection.publish.get")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{cartId}", ApiVersionMajor = 9)]
    private sealed class GeneratedProjectionGetBehavior : IAppBehavior<ProjectionCartInput, ProjectionCartOutput>
    {
        public Task<ProjectionCartOutput> HandleAsync(
            ProjectionCartInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProjectionCartOutput(input.CartId));
        }
    }

    [AppBehavior("tests.generated.projection.publish.post")]
    [BehaviorRestProfile(BehaviorRestMethod.Post, "/{cartId}/items", ApiVersionMajor = 9)]
    private sealed class GeneratedProjectionPostBehavior : IAppBehavior<ProjectionCartInput, ProjectionCartOutput>
    {
        public Task<ProjectionCartOutput> HandleAsync(
            ProjectionCartInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProjectionCartOutput(input.CartId));
        }
    }

    [AppBehavior("tests.generated.projection.precedence.lookup")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{cartId}", ApiVersionMajor = 10)]
    private sealed class GeneratedProjectionProfilePrecedenceBehavior : IAppBehavior<ProjectionCartInput, ProjectionCartOutput>
    {
        public Task<ProjectionCartOutput> HandleAsync(
            ProjectionCartInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProjectionCartOutput(input.CartId));
        }
    }

    [AppBehavior("tests.generated.projection.threeway.lookup")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{cartId}", ApiVersionMajor = 11)]
    private sealed class GeneratedProjectionThreeWayBehavior : IAppBehavior<ProjectionCartInput, ProjectionCartOutput>
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

    [AppBehavior("tests.profile.projection.bound.inference")]
    [BehaviorRestProfile(BehaviorRestMethod.Post, "/{cartId}/items", ApiVersionMajor = 6)]
    [BehaviorRestBinding(nameof(ProfileProjectionBoundInput.Quantity), BehaviorRestBindingSource.Query, Name = "quantity")]
    [BehaviorRestBinding(nameof(ProfileProjectionBoundInput.CorrelationId), BehaviorRestBindingSource.Header, Name = "X-Correlation-Id")]
    [BehaviorRestBinding(nameof(ProfileProjectionBoundInput.Note), BehaviorRestBindingSource.Body, Name = "note")]
    private sealed class ProfileProjectionBoundInferenceBehavior : IAppBehavior<ProfileProjectionBoundInput, ProjectionCartOutput>
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

    private sealed record DynamicProfileBindingDefinition(
        string PropertyName,
        object SourceValue,
        string? Name = null);
}

public sealed class DynamicProfileBindingInput
{
    public string CartId { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public string? CorrelationId { get; init; }

    public string? Note { get; init; }
}
