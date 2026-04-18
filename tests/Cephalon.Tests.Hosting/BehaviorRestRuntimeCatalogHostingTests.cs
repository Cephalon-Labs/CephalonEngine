using System.Net.Http.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Diagnostics;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Modules;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cephalon.Tests.Hosting;

public sealed class BehaviorRestRuntimeCatalogHostingTests
{
    [Fact]
    public async Task MapCephalonExposesResolvedRestEndpointsAndSnapshot()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "2";
        builder.Configuration["OpenApi:DefaultVersion"] = "2";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new CatalogRestModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.Equal(2, endpoints.Length);

        var getEndpoint = Assert.Single(endpoints, static endpoint => endpoint.Method == "GET");
        Assert.Equal("rest-api", getEndpoint.TransportId);
        Assert.Equal("module-dsl", getEndpoint.SourceKind);
        Assert.Equal("tests.rest.runtime-catalog", getEndpoint.SourceModuleId);
        Assert.Equal("1.0.0", getEndpoint.SourceModuleVersion);
        Assert.Equal(1, getEndpoint.SourceModuleVersionMajor);
        Assert.Equal("tests.rest.runtime-catalog.get", getEndpoint.BehaviorId);
        Assert.Equal("/api/v2/tests/runtime-catalog/cart/{cartId}", getEndpoint.RoutePattern);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, getEndpoint.AuthoringStyle);
        Assert.Equal("/api/v2/tests/runtime-catalog/cart", getEndpoint.RouteGroupPrefix);
        Assert.Equal("/{cartId}", getEndpoint.RelativePattern);
        Assert.Contains("GetCatalogCartBehavior", getEndpoint.BehaviorType, StringComparison.Ordinal);
        Assert.Equal("tests.rest.runtime-catalog.get:GET:/{cartId}", getEndpoint.SourceId);
        Assert.Contains("Runtime Catalog API", getEndpoint.Tags);
        Assert.Equal("v2", getEndpoint.OpenApiDocumentName);
        Assert.Equal(2, getEndpoint.ApiVersionMajor);
        Assert.Equal("GET", getEndpoint.Metadata["method"]);
        Assert.Equal(getEndpoint.RouteGroupPrefix, getEndpoint.Metadata["routeGroupPrefix"]);
        Assert.Equal(getEndpoint.RelativePattern, getEndpoint.Metadata["relativePattern"]);
        Assert.Equal(getEndpoint.BehaviorType, getEndpoint.Metadata["behaviorType"]);
        Assert.Equal(getEndpoint.SourceId, getEndpoint.Metadata["sourceId"]);
        Assert.Empty(getEndpoint.BindingDescriptors);

        var endpointById = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor>($"/engine/rest-endpoints/{getEndpoint.Id}");

        Assert.NotNull(endpointById);
        Assert.Equal(getEndpoint.Id, endpointById.Id);
        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.RestEndpoints, endpoint => endpoint.Id == getEndpoint.Id);
        Assert.Contains(snapshot.RestEndpoints, endpoint => endpoint.BehaviorId == "tests.rest.runtime-catalog.add-item");
    }

    [Fact]
    public void MapCephalonRejectsCollidingResolvedRestEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new CollidingLookupModuleOne());
            engine.AddModule(new CollidingLookupModuleTwo());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        var app = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(() => app.MapCephalon());

        Assert.Contains("Resolved public REST endpoint collision detected", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GET /api/v1/tests/runtime-catalog/collisions/items/{itemId}", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tests.rest.runtime-collision.one", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tests.rest.runtime-collision.two", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MapCephalonExposesManualRestEndpointsFromLegacyModulesAndAdditionalBehaviorHelpers()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ManualRuntimeCatalogModule());
            engine.AddModule(new ManualBehaviorHelperRuntimeModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);

        var manualEndpoint = Assert.Single(endpoints, static endpoint =>
            string.Equals(endpoint.SourceModuleId, "tests.rest.manual-runtime", StringComparison.Ordinal));
        Assert.Equal("manual", manualEndpoint.SourceKind);
        Assert.Equal("GET", manualEndpoint.Method);
        Assert.Null(manualEndpoint.BehaviorId);
        Assert.Equal("/api/tests/manual-runtime/orders/{orderId}", manualEndpoint.RoutePattern);
        Assert.Equal("tests.manual-runtime.orders.get", manualEndpoint.EndpointName);
        Assert.Equal("v4", manualEndpoint.OpenApiDocumentName);
        Assert.Equal(4, manualEndpoint.ApiVersionMajor);
        Assert.Contains("Manual Runtime API", manualEndpoint.Tags);
        Assert.Equal("Gets a manual runtime order.", manualEndpoint.Summary);
        Assert.Equal("Publishes a legacy Minimal API route into the runtime catalog.", manualEndpoint.Description);
        Assert.Equal(RestEndpointRuntimeMetadata.MinimalApiAuthoringStyle, manualEndpoint.AuthoringStyle);
        Assert.Null(manualEndpoint.BehaviorType);
        Assert.Equal("tests.rest.manual-runtime:GET:/api/tests/manual-runtime/orders/{orderId}", manualEndpoint.SourceId);
        Assert.Null(manualEndpoint.CandidateId);
        Assert.Null(manualEndpoint.OriginalProjection);
        Assert.Null(manualEndpoint.OriginalEndpointName);
        Assert.Null(manualEndpoint.OriginalSummary);
        Assert.Null(manualEndpoint.OriginalDescription);
        Assert.Equal(RestEndpointRuntimeMetadata.MinimalApiAuthoringStyle, manualEndpoint.Metadata["authoringStyle"]);
        Assert.Equal(manualEndpoint.SourceId, manualEndpoint.Metadata["sourceId"]);
        Assert.Empty(manualEndpoint.BindingDescriptors);

        var behaviorHelperEndpoint = Assert.Single(endpoints, static endpoint =>
            string.Equals(endpoint.BehaviorId, "tests.rest.manual-helper.get", StringComparison.Ordinal));
        Assert.Equal("manual", behaviorHelperEndpoint.SourceKind);
        Assert.Equal("GET", behaviorHelperEndpoint.Method);
        Assert.Equal("/api/v3/tests/manual-helper/orders/{orderId}", behaviorHelperEndpoint.RoutePattern);
        Assert.Equal("v3", behaviorHelperEndpoint.OpenApiDocumentName);
        Assert.Equal(3, behaviorHelperEndpoint.ApiVersionMajor);
        Assert.Contains("Manual Helper API", behaviorHelperEndpoint.Tags);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorHelperAuthoringStyle, behaviorHelperEndpoint.AuthoringStyle);
        Assert.Equal("/api/v3/tests/manual-helper/orders", behaviorHelperEndpoint.RouteGroupPrefix);
        Assert.Equal("/{orderId}", behaviorHelperEndpoint.RelativePattern);
        Assert.Contains("GetManualHelperOrderBehavior", behaviorHelperEndpoint.BehaviorType, StringComparison.Ordinal);
        Assert.Equal("tests.rest.manual-helper.get:GET:/{orderId}", behaviorHelperEndpoint.SourceId);
        Assert.Null(behaviorHelperEndpoint.CandidateId);
        Assert.Null(behaviorHelperEndpoint.OriginalProjection);
        Assert.Null(behaviorHelperEndpoint.OriginalEndpointName);
        Assert.Null(behaviorHelperEndpoint.OriginalSummary);
        Assert.Null(behaviorHelperEndpoint.OriginalDescription);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorHelperAuthoringStyle, behaviorHelperEndpoint.Metadata["authoringStyle"]);
        Assert.Equal(behaviorHelperEndpoint.BehaviorType, behaviorHelperEndpoint.Metadata["behaviorType"]);
        Assert.Equal(behaviorHelperEndpoint.SourceId, behaviorHelperEndpoint.Metadata["sourceId"]);
        Assert.Empty(behaviorHelperEndpoint.BindingDescriptors);

        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.RestEndpoints, endpoint => endpoint.Id == manualEndpoint.Id);
        Assert.Contains(snapshot.RestEndpoints, endpoint => endpoint.Id == behaviorHelperEndpoint.Id);
    }

    [Fact]
    public async Task MapCephalonExposesProfileDrivenModuleDslEndpointsInRuntimeCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "3";
        builder.Configuration["OpenApi:DefaultVersion"] = "3";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileDrivenRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.rest.profile.lookup", StringComparison.Ordinal));
        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.lookup", StringComparison.Ordinal));
        Assert.Equal("module-dsl", endpoint.SourceKind);
        Assert.Equal("/api/v3/tests/profile-runtime/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal("tests_rest_profile_runtime.v3.tests_rest_profile_lookup", endpoint.EndpointName);
        Assert.Equal("v3", endpoint.OpenApiDocumentName);
        Assert.Equal(3, endpoint.ApiVersionMajor);
        Assert.Contains("Profile Runtime API", endpoint.Tags);
        Assert.Equal("tests.rest.profile.lookup", endpoint.Summary);
        Assert.Equal("Publishes profile-driven REST endpoints for runtime catalog coverage.", endpoint.Description);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, endpoint.AuthoringStyle);
        Assert.Equal("/api/v3/tests/profile-runtime/orders", endpoint.RouteGroupPrefix);
        Assert.Equal("/{orderId}", endpoint.RelativePattern);
        Assert.Contains("GetProfileRuntimeOrderBehavior", endpoint.BehaviorType, StringComparison.Ordinal);
        Assert.Equal("tests.rest.profile.lookup:GET:/{orderId}", endpoint.SourceId);
        Assert.Equal(candidate.Id, endpoint.CandidateId);
        Assert.Empty(endpoint.BindingDescriptors);
        Assert.Equal(endpoint.EndpointName, candidate.ProjectedEndpoint.EndpointName);
        Assert.Equal(endpoint.Summary, candidate.ProjectedEndpoint.Summary);
        Assert.Equal(endpoint.Description, candidate.ProjectedEndpoint.Description);
        Assert.Equal(endpoint.AuthoringStyle, candidate.ProjectedEndpoint.AuthoringStyle);
        Assert.Equal(endpoint.RouteGroupPrefix, candidate.ProjectedEndpoint.RouteGroupPrefix);
        Assert.Equal(endpoint.RelativePattern, candidate.ProjectedEndpoint.RelativePattern);
        Assert.Equal(endpoint.BehaviorType, candidate.ProjectedEndpoint.BehaviorType);
        Assert.Equal(endpoint.SourceId, candidate.ProjectedEndpoint.SourceId);
        Assert.Equal(candidate.Id, candidate.ProjectedEndpoint.CandidateId);
        Assert.NotNull(endpoint.OriginalProjection);
        Assert.Equal(candidate.OriginalProjection.ApiVersionMajor, endpoint.OriginalProjection!.ApiVersionMajor);
        Assert.Equal(candidate.OriginalProjection.OpenApiDocumentName, endpoint.OriginalProjection.OpenApiDocumentName);
        Assert.Equal(candidate.OriginalProjection.Method, endpoint.OriginalProjection.Method);
        Assert.Equal(candidate.OriginalProjection.RouteGroupPrefix, endpoint.OriginalProjection.RouteGroupPrefix);
        Assert.Equal(candidate.OriginalProjection.RelativePattern, endpoint.OriginalProjection.RelativePattern);
        Assert.Equal(candidate.OriginalProjection.RoutePattern, endpoint.OriginalProjection.RoutePattern);
        Assert.Equal(candidate.OriginalProjection.BindingDescriptors.Count, endpoint.OriginalProjection.BindingDescriptors.Count);
        Assert.Equal(endpoint.EndpointName, endpoint.OriginalEndpointName);
        Assert.Equal(endpoint.Summary, endpoint.OriginalSummary);
        Assert.Equal(endpoint.Description, endpoint.OriginalDescription);
        Assert.Equal(endpoint.OriginalEndpointName, candidate.ProjectedEndpoint.OriginalEndpointName);
        Assert.Equal(endpoint.OriginalSummary, candidate.ProjectedEndpoint.OriginalSummary);
        Assert.Equal(endpoint.OriginalDescription, candidate.ProjectedEndpoint.OriginalDescription);

        var payload = await client.GetFromJsonAsync<ProfileRuntimeOrderOutput>("/api/v3/tests/profile-runtime/orders/ord-42");
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonLetsExplicitGroupVersionOverrideBehaviorRestProfileVersion()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "5";
        builder.Configuration["OpenApi:DefaultVersion"] = "5";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileVersionOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");

        Assert.NotNull(endpoints);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.rest.profile.override", StringComparison.Ordinal));
        Assert.Equal("/api/v5/tests/profile-runtime/override/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal("v5", endpoint.OpenApiDocumentName);
        Assert.Equal(5, endpoint.ApiVersionMajor);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, endpoint.Metadata["authoringStyle"]);

        var payload = await client.GetFromJsonAsync<ProfileRuntimeOrderOutput>("/api/v5/tests/profile-runtime/override/orders/ord-77");
        Assert.NotNull(payload);
        Assert.Equal("ord-77", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonExposesGeneratedModuleDslEndpointsInRuntimeCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.generated.runtime.lookup", StringComparison.Ordinal));
        Assert.Equal("module-dsl", endpoint.SourceKind);
        Assert.Equal("/api/v4/tests/generated/runtime/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal("v4", endpoint.OpenApiDocumentName);
        Assert.Equal(4, endpoint.ApiVersionMajor);
        Assert.Contains("Generated Runtime API", endpoint.Tags);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, endpoint.AuthoringStyle);
        Assert.Equal("/api/v4/tests/generated/runtime", endpoint.RouteGroupPrefix);
        Assert.Equal("/orders/{orderId}", endpoint.RelativePattern);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtime.lookup", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal(candidate.Id, endpoint.CandidateId);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, candidate.AuthoringStyle);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);
        Assert.Equal(endpoint.AuthoringStyle, candidate.ProjectedEndpoint.AuthoringStyle);
        Assert.Equal(endpoint.RouteGroupPrefix, candidate.ProjectedEndpoint.RouteGroupPrefix);
        Assert.Equal(endpoint.RelativePattern, candidate.ProjectedEndpoint.RelativePattern);
        Assert.Equal(candidate.Id, candidate.ProjectedEndpoint.CandidateId);

        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.Id, endpoint.Id, StringComparison.Ordinal) &&
            string.Equals(item.AuthoringStyle, endpoint.AuthoringStyle, StringComparison.Ordinal) &&
            string.Equals(item.RouteGroupPrefix, endpoint.RouteGroupPrefix, StringComparison.Ordinal) &&
            string.Equals(item.RelativePattern, endpoint.RelativePattern, StringComparison.Ordinal) &&
            string.Equals(item.CandidateId, candidate.Id, StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            item.Status == RestEndpointCandidateStatus.Published);

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v4/tests/generated/runtime/orders/ord-42");
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
    }

    [Fact]
    public async Task AddRestBehaviorModuleLetsHostsPublishProfileDrivenEndpointsWithoutDedicatedModuleType()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "3";
        builder.Configuration["OpenApi:DefaultVersion"] = "3";
        builder.AddCephalon(engine =>
        {
            engine.AddRestBehaviorModule<GetProfileRuntimeOrderBehavior>(
                "tests.rest.inline-profile-runtime",
                "Inline Profile Runtime Module",
                "Publishes profile-driven REST endpoints through inline module authoring.",
                behaviors => behaviors.Group("/tests/inline/profile-runtime/orders")
                    .WithTagName("Inline Profile Runtime API")
                    .MapProfile<GetProfileRuntimeOrderBehavior>(),
                version: "1.0.0");
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.SourceModuleId, "tests.rest.inline-profile-runtime", StringComparison.Ordinal));
        Assert.Equal("module-dsl", endpoint.SourceKind);
        Assert.Equal("tests.rest.profile.lookup", endpoint.BehaviorId);
        Assert.Equal("/api/v3/tests/inline/profile-runtime/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal("v3", endpoint.OpenApiDocumentName);
        Assert.Equal(3, endpoint.ApiVersionMajor);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, endpoint.Metadata["authoringStyle"]);
        Assert.Equal("/api/v3/tests/inline/profile-runtime/orders", endpoint.RouteGroupPrefix);
        Assert.Equal("/{orderId}", endpoint.RelativePattern);
        Assert.Contains("Inline Profile Runtime API", endpoint.Tags);
        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.Id, endpoint.Id, StringComparison.Ordinal));

        var payload = await client.GetFromJsonAsync<ProfileRuntimeOrderOutput>("/api/v3/tests/inline/profile-runtime/orders/ord-inline");
        Assert.NotNull(payload);
        Assert.Equal("ord-inline", payload.OrderId);
    }

    [Fact]
    public async Task AddRestBehaviorModuleLetsHostsPublishGeneratedEndpointsWithoutDedicatedModuleType()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.AddCephalon(engine =>
        {
            engine.AddRestBehaviorModule<GetGeneratedRuntimeOrderBehavior>(
                new ModuleDescriptor(
                    "tests.rest.inline-generated-runtime",
                    "Inline Generated Runtime Module",
                    "Publishes generated REST endpoints through inline module authoring.",
                    version: "1.0.0"),
                behaviors => behaviors.Group("/tests/inline/generated-runtime")
                    .WithTagName("Inline Generated Runtime API")
                    .MapGeneratedProfiles("tests.generated.runtime"));
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(snapshot);

        var moduleEndpoints = endpoints
            .Where(static candidate =>
                string.Equals(candidate.SourceModuleId, "tests.rest.inline-generated-runtime", StringComparison.Ordinal))
            .OrderBy(static candidate => candidate.Method, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(2, moduleEndpoints.Length);

        var getEndpoint = Assert.Single(moduleEndpoints, static endpoint => endpoint.Method == "GET");
        Assert.Equal("tests.generated.runtime.lookup", getEndpoint.BehaviorId);
        Assert.Equal("/api/v4/tests/inline/generated-runtime/orders/{orderId}", getEndpoint.RoutePattern);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, getEndpoint.Metadata["authoringStyle"]);
        Assert.Contains("Inline Generated Runtime API", getEndpoint.Tags);

        var postEndpoint = Assert.Single(moduleEndpoints, static endpoint => endpoint.Method == "POST");
        Assert.Equal("tests.generated.runtime.create", postEndpoint.BehaviorId);
        Assert.Equal("/api/v4/tests/inline/generated-runtime/orders/{orderId}/items", postEndpoint.RoutePattern);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, postEndpoint.Metadata["authoringStyle"]);

        Assert.Equal(2, candidates.Count(static candidate =>
            string.Equals(candidate.ProjectedEndpoint.SourceModuleId, "tests.rest.inline-generated-runtime", StringComparison.Ordinal) &&
            candidate.Status == RestEndpointCandidateStatus.Published &&
            string.Equals(candidate.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal)));
        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.SourceModuleId, "tests.rest.inline-generated-runtime", StringComparison.Ordinal) &&
            string.Equals(item.BehaviorId, "tests.generated.runtime.lookup", StringComparison.Ordinal));

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v4/tests/inline/generated-runtime/orders/ord-inline");
        Assert.NotNull(payload);
        Assert.Equal("ord-inline", payload.OrderId);
    }

    [Fact]
    public async Task GroupFromBehaviorIdPrefixDerivesGeneratedRouteGroupsWithoutRepeatingManualPath()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.AddCephalon(engine =>
        {
            engine.AddRestBehaviorModule<GetGeneratedRuntimeOrderBehavior>(
                new ModuleDescriptor(
                    "tests.rest.derived-generated-runtime",
                    "Derived Generated Runtime Module",
                    "Publishes generated REST endpoints through a behavior-id-derived route group.",
                    version: "1.0.0"),
                behaviors => behaviors.GroupFromBehaviorIdPrefix("tests.generated.runtime")
                    .WithTagName("Derived Generated Runtime API")
                    .MapGeneratedProfiles());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(snapshot);

        var moduleEndpoints = endpoints
            .Where(static candidate =>
                string.Equals(candidate.SourceModuleId, "tests.rest.derived-generated-runtime", StringComparison.Ordinal))
            .OrderBy(static candidate => candidate.Method, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(2, moduleEndpoints.Length);

        var getEndpoint = Assert.Single(moduleEndpoints, static endpoint => endpoint.Method == "GET");
        Assert.Equal("/api/v4/tests/generated/runtime/orders/{orderId}", getEndpoint.RoutePattern);
        Assert.Equal("/api/v4/tests/generated/runtime", getEndpoint.RouteGroupPrefix);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, getEndpoint.Metadata["authoringStyle"]);
        Assert.Contains("Derived Generated Runtime API", getEndpoint.Tags);

        var postEndpoint = Assert.Single(moduleEndpoints, static endpoint => endpoint.Method == "POST");
        Assert.Equal("/api/v4/tests/generated/runtime/orders/{orderId}/items", postEndpoint.RoutePattern);
        Assert.Equal("/api/v4/tests/generated/runtime", postEndpoint.RouteGroupPrefix);

        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.SourceModuleId, "tests.rest.derived-generated-runtime", StringComparison.Ordinal) &&
            string.Equals(item.RoutePattern, "/api/v4/tests/generated/runtime/orders/{orderId}", StringComparison.Ordinal));

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v4/tests/generated/runtime/orders/ord-derived");
        Assert.NotNull(payload);
        Assert.Equal("ord-derived", payload.OrderId);
    }

    [Fact]
    public async Task AddGeneratedRestBehaviorModuleDerivesRouteGroupsAndPublishesGeneratedProfiles()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "5";
        builder.Configuration["OpenApi:DefaultVersion"] = "5";
        builder.AddCephalon(engine =>
        {
            engine.AddGeneratedRestBehaviorModule<GetGeneratedRuntimeOrderBehavior>(
                "tests.rest.inline-derived-generated-runtime",
                "Inline Derived Generated Runtime Module",
                "Publishes generated REST endpoints through the generated inline helper.",
                "tests.generated.runtime",
                group => group
                    .ApiVersion(5)
                    .WithTagName("Inline Derived Generated Runtime API"),
                version: "1.0.0");
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(snapshot);

        var moduleEndpoints = endpoints
            .Where(static candidate =>
                string.Equals(candidate.SourceModuleId, "tests.rest.inline-derived-generated-runtime", StringComparison.Ordinal))
            .OrderBy(static candidate => candidate.Method, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(2, moduleEndpoints.Length);

        var getEndpoint = Assert.Single(moduleEndpoints, static endpoint => endpoint.Method == "GET");
        Assert.Equal("/api/v5/tests/generated/runtime/orders/{orderId}", getEndpoint.RoutePattern);
        Assert.Equal("v5", getEndpoint.OpenApiDocumentName);
        Assert.Equal(5, getEndpoint.ApiVersionMajor);
        Assert.Equal("/api/v5/tests/generated/runtime", getEndpoint.RouteGroupPrefix);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, getEndpoint.Metadata["authoringStyle"]);
        Assert.Contains("Inline Derived Generated Runtime API", getEndpoint.Tags);

        Assert.Equal(2, candidates.Count(static candidate =>
            string.Equals(candidate.ProjectedEndpoint.SourceModuleId, "tests.rest.inline-derived-generated-runtime", StringComparison.Ordinal) &&
            candidate.Status == RestEndpointCandidateStatus.Published &&
            string.Equals(candidate.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal)));
        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.SourceModuleId, "tests.rest.inline-derived-generated-runtime", StringComparison.Ordinal) &&
            string.Equals(item.RoutePattern, "/api/v5/tests/generated/runtime/orders/{orderId}", StringComparison.Ordinal));

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v5/tests/generated/runtime/orders/ord-inline-derived");
        Assert.NotNull(payload);
        Assert.Equal("ord-inline-derived", payload.OrderId);
    }

    [Fact]
    public async Task AddGeneratedRestBehaviorModuleStringOverloadDerivesGeneratedPrefixFromModuleId()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.AddCephalon(engine =>
        {
            engine.AddGeneratedRestBehaviorModule<GetGeneratedRuntimeOrderBehavior>(
                "tests.generated.runtime",
                "Descriptor Id Generated Runtime Module",
                "Publishes generated REST endpoints through the descriptor-id inline helper.",
                group => group
                    .ApiVersion(6)
                    .WithTagName("Descriptor Id Generated Runtime API"),
                version: "1.0.0");
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(snapshot);

        var moduleEndpoints = endpoints
            .Where(static candidate =>
                string.Equals(candidate.SourceModuleId, "tests.generated.runtime", StringComparison.Ordinal))
            .OrderBy(static candidate => candidate.Method, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(2, moduleEndpoints.Length);

        var getEndpoint = Assert.Single(moduleEndpoints, static endpoint => endpoint.Method == "GET");
        Assert.Equal("/api/v6/tests/generated/runtime/orders/{orderId}", getEndpoint.RoutePattern);
        Assert.Equal("v6", getEndpoint.OpenApiDocumentName);
        Assert.Equal(6, getEndpoint.ApiVersionMajor);
        Assert.Equal("/api/v6/tests/generated/runtime", getEndpoint.RouteGroupPrefix);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, getEndpoint.Metadata["authoringStyle"]);
        Assert.Contains("Descriptor Id Generated Runtime API", getEndpoint.Tags);

        Assert.Equal(2, candidates.Count(static candidate =>
            string.Equals(candidate.ProjectedEndpoint.SourceModuleId, "tests.generated.runtime", StringComparison.Ordinal) &&
            candidate.Status == RestEndpointCandidateStatus.Published &&
            string.Equals(candidate.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal)));
        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.SourceModuleId, "tests.generated.runtime", StringComparison.Ordinal) &&
            string.Equals(item.RoutePattern, "/api/v6/tests/generated/runtime/orders/{orderId}", StringComparison.Ordinal));

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v6/tests/generated/runtime/orders/ord-descriptor-id");
        Assert.NotNull(payload);
        Assert.Equal("ord-descriptor-id", payload.OrderId);
    }

    [Fact]
    public async Task AddGeneratedRestBehaviorModuleGroupsDerivesMultipleRouteGroupsAndPublishesGeneratedProfiles()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "7";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "8";
        builder.Configuration["OpenApi:DefaultVersion"] = "7";
        builder.AddCephalon(engine =>
        {
            engine.AddGeneratedRestBehaviorModuleGroups<GetGeneratedGroupedRuntimeOrderBehavior>(
                "tests.rest.inline-generated-grouped-runtime",
                "Inline Generated Grouped Runtime Module",
                "Publishes grouped generated REST endpoints through the inline helper.",
                "tests.generated.runtimegrouped",
                group => group
                    .WithTagName("Inline Generated Grouped Runtime API")
                    .WithHostGovernanceScope("inline-generated-grouped"),
                version: "1.0.0");
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var publicationGroups = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor[]>("/engine/rest-endpoint-publication-groups");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(publicationGroups);
        Assert.NotNull(snapshot);

        var moduleEndpoints = endpoints
            .Where(static endpoint =>
                string.Equals(endpoint.SourceModuleId, "tests.rest.inline-generated-grouped-runtime", StringComparison.Ordinal))
            .OrderBy(static endpoint => endpoint.RoutePattern, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(3, moduleEndpoints.Length);

        var inventoryEndpoint = Assert.Single(moduleEndpoints, static endpoint =>
            string.Equals(endpoint.BehaviorId, "tests.generated.runtimegrouped.inventory.lookup", StringComparison.Ordinal));
        Assert.Equal("/api/v8/tests/generated/runtimegrouped/inventory/{orderId}", inventoryEndpoint.RoutePattern);
        Assert.Equal("/api/v8/tests/generated/runtimegrouped/inventory", inventoryEndpoint.RouteGroupPrefix);
        Assert.Equal("v8", inventoryEndpoint.OpenApiDocumentName);
        Assert.Equal(8, inventoryEndpoint.ApiVersionMajor);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, inventoryEndpoint.AuthoringStyle);
        Assert.Contains("Inline Generated Grouped Runtime API", inventoryEndpoint.Tags);

        var ordersLookupEndpoint = Assert.Single(moduleEndpoints, static endpoint =>
            string.Equals(endpoint.BehaviorId, "tests.generated.runtimegrouped.orders.lookup", StringComparison.Ordinal));
        Assert.Equal("/api/v7/tests/generated/runtimegrouped/orders/{orderId}", ordersLookupEndpoint.RoutePattern);
        Assert.Equal("/api/v7/tests/generated/runtimegrouped/orders", ordersLookupEndpoint.RouteGroupPrefix);
        Assert.Equal("v7", ordersLookupEndpoint.OpenApiDocumentName);
        Assert.Equal(7, ordersLookupEndpoint.ApiVersionMajor);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, ordersLookupEndpoint.AuthoringStyle);
        Assert.Contains("Inline Generated Grouped Runtime API", ordersLookupEndpoint.Tags);

        var moduleCandidates = candidates
            .Where(static candidate =>
                string.Equals(candidate.ProjectedEndpoint.SourceModuleId, "tests.rest.inline-generated-grouped-runtime", StringComparison.Ordinal))
            .OrderBy(static candidate => candidate.ProjectedEndpoint.RoutePattern, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(3, moduleCandidates.Length);
        Assert.All(
            moduleCandidates,
            candidate =>
            {
                Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
                Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, candidate.AuthoringStyle);
                Assert.True(candidate.OriginalProjection.AllowsHostGovernance);
                Assert.Equal("inline-generated-grouped", candidate.OriginalProjection.HostGovernanceScope);
            });

        Assert.Contains(
            publicationGroups,
            group =>
                string.Equals(group.BehaviorId, "tests.generated.runtimegrouped.orders.lookup", StringComparison.Ordinal) &&
                group.SourceModuleIds.SequenceEqual(["tests.rest.inline-generated-grouped-runtime"]) &&
                group.PublishedCandidateIds.Count == 1 &&
                group.HostGovernanceEligibleCandidateIds.Count == 1);
        Assert.Contains(
            snapshot.RestEndpointPublicationGroups,
            group =>
                string.Equals(group.BehaviorId, "tests.generated.runtimegrouped.inventory.lookup", StringComparison.Ordinal) &&
                group.SourceModuleIds.SequenceEqual(["tests.rest.inline-generated-grouped-runtime"]) &&
                group.PublishedCandidateIds.Count == 1);

        var ordersPayload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>(
            "/api/v7/tests/generated/runtimegrouped/orders/ord-inline-grouped");
        var inventoryPayload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>(
            "/api/v8/tests/generated/runtimegrouped/inventory/ord-inline-inventory");
        Assert.NotNull(ordersPayload);
        Assert.NotNull(inventoryPayload);
        Assert.Equal("ord-inline-grouped", ordersPayload.OrderId);
        Assert.Equal("ord-inline-inventory", inventoryPayload.OrderId);
    }

    [Fact]
    public async Task AddGeneratedRestBehaviorModuleGroupsCanConfigureEachDerivedGroupFromItsGeneratedPrefix()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "8";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "11";
        builder.Configuration["OpenApi:DefaultVersion"] = "8";
        builder.AddCephalon(engine =>
        {
            engine.AddGeneratedRestBehaviorModuleGroups<GetGeneratedGroupedRuntimeOrderBehavior>(
                "tests.rest.inline-generated-grouped-runtime.per-prefix",
                "Inline Generated Grouped Runtime Per Prefix Module",
                "Publishes grouped generated REST endpoints through the inline helper with per-derived-group configuration.",
                "tests.generated.runtimegrouped",
                (derivedBehaviorIdPrefix, group) =>
                {
                    if (string.Equals(derivedBehaviorIdPrefix, "tests.generated.runtimegrouped.orders", StringComparison.Ordinal))
                    {
                        group.ApiVersion(11)
                            .WithTagName("Inline Generated Orders Runtime API")
                            .WithHostGovernanceScope("inline-generated-orders");
                    }
                    else if (string.Equals(derivedBehaviorIdPrefix, "tests.generated.runtimegrouped.inventory", StringComparison.Ordinal))
                    {
                        group.WithTagName("Inline Generated Inventory Runtime API")
                            .WithHostGovernanceScope("inline-generated-inventory");
                    }
                },
                version: "1.0.0");
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(snapshot);

        var moduleEndpoints = endpoints
            .Where(static endpoint =>
                string.Equals(endpoint.SourceModuleId, "tests.rest.inline-generated-grouped-runtime.per-prefix", StringComparison.Ordinal))
            .OrderBy(static endpoint => endpoint.RoutePattern, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(3, moduleEndpoints.Length);

        var inventoryEndpoint = Assert.Single(moduleEndpoints, static endpoint =>
            string.Equals(endpoint.BehaviorId, "tests.generated.runtimegrouped.inventory.lookup", StringComparison.Ordinal));
        Assert.Equal("/api/v8/tests/generated/runtimegrouped/inventory/{orderId}", inventoryEndpoint.RoutePattern);
        Assert.Equal("/api/v8/tests/generated/runtimegrouped/inventory", inventoryEndpoint.RouteGroupPrefix);
        Assert.Equal("v8", inventoryEndpoint.OpenApiDocumentName);
        Assert.Equal(8, inventoryEndpoint.ApiVersionMajor);
        Assert.Contains("Inline Generated Inventory Runtime API", inventoryEndpoint.Tags);

        var ordersLookupEndpoint = Assert.Single(moduleEndpoints, static endpoint =>
            string.Equals(endpoint.BehaviorId, "tests.generated.runtimegrouped.orders.lookup", StringComparison.Ordinal));
        Assert.Equal("/api/v11/tests/generated/runtimegrouped/orders/{orderId}", ordersLookupEndpoint.RoutePattern);
        Assert.Equal("/api/v11/tests/generated/runtimegrouped/orders", ordersLookupEndpoint.RouteGroupPrefix);
        Assert.Equal("v11", ordersLookupEndpoint.OpenApiDocumentName);
        Assert.Equal(11, ordersLookupEndpoint.ApiVersionMajor);
        Assert.Contains("Inline Generated Orders Runtime API", ordersLookupEndpoint.Tags);

        var ordersCreateEndpoint = Assert.Single(moduleEndpoints, static endpoint =>
            string.Equals(endpoint.BehaviorId, "tests.generated.runtimegrouped.orders.create", StringComparison.Ordinal));
        Assert.Equal("/api/v11/tests/generated/runtimegrouped/orders/{orderId}/items", ordersCreateEndpoint.RoutePattern);
        Assert.Equal("/api/v11/tests/generated/runtimegrouped/orders", ordersCreateEndpoint.RouteGroupPrefix);
        Assert.Equal("v11", ordersCreateEndpoint.OpenApiDocumentName);
        Assert.Equal(11, ordersCreateEndpoint.ApiVersionMajor);
        Assert.Contains("Inline Generated Orders Runtime API", ordersCreateEndpoint.Tags);

        var inventoryCandidate = Assert.Single(candidates, static candidate =>
            string.Equals(candidate.ProjectedEndpoint.BehaviorId, "tests.generated.runtimegrouped.inventory.lookup", StringComparison.Ordinal));
        Assert.True(inventoryCandidate.OriginalProjection.AllowsHostGovernance);
        Assert.Equal("inline-generated-inventory", inventoryCandidate.OriginalProjection.HostGovernanceScope);

        var ordersCandidates = candidates
            .Where(static candidate =>
                string.Equals(candidate.ProjectedEndpoint.SourceModuleId, "tests.rest.inline-generated-grouped-runtime.per-prefix", StringComparison.Ordinal) &&
                string.Equals(candidate.ProjectedEndpoint.RouteGroupPrefix, "/api/v11/tests/generated/runtimegrouped/orders", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(2, ordersCandidates.Length);
        Assert.All(
            ordersCandidates,
            candidate =>
            {
                Assert.True(candidate.OriginalProjection.AllowsHostGovernance);
                Assert.Equal("inline-generated-orders", candidate.OriginalProjection.HostGovernanceScope);
                Assert.Contains("Inline Generated Orders Runtime API", candidate.ProjectedEndpoint.Tags);
            });

        Assert.Contains(snapshot.RestEndpoints, static item =>
            string.Equals(item.SourceModuleId, "tests.rest.inline-generated-grouped-runtime.per-prefix", StringComparison.Ordinal) &&
            string.Equals(item.RoutePattern, "/api/v11/tests/generated/runtimegrouped/orders/{orderId}", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointCandidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimegrouped.inventory.lookup", StringComparison.Ordinal) &&
            item.OriginalProjection.AllowsHostGovernance &&
            string.Equals(item.OriginalProjection.HostGovernanceScope, "inline-generated-inventory", StringComparison.Ordinal));

        var ordersPayload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>(
            "/api/v11/tests/generated/runtimegrouped/orders/ord-inline-orders");
        var inventoryPayload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>(
            "/api/v8/tests/generated/runtimegrouped/inventory/ord-inline-inventory");
        Assert.NotNull(ordersPayload);
        Assert.NotNull(inventoryPayload);
        Assert.Equal("ord-inline-orders", ordersPayload.OrderId);
        Assert.Equal("ord-inline-inventory", inventoryPayload.OrderId);
    }

    [Fact]
    public async Task AddGeneratedRestBehaviorModuleGroupsStringOverloadDerivesGeneratedPrefixFromModuleId()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "7";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "8";
        builder.Configuration["OpenApi:DefaultVersion"] = "7";
        builder.AddCephalon(engine =>
        {
            engine.AddGeneratedRestBehaviorModuleGroups<GetGeneratedGroupedRuntimeOrderBehavior>(
                "tests.generated.runtimegrouped",
                "Descriptor Id Generated Grouped Runtime Module",
                "Publishes grouped generated REST endpoints by deriving the root prefix from the module id.",
                group => group
                    .WithTagName("Descriptor Id Generated Grouped Runtime API")
                    .WithHostGovernanceScope("descriptor-generated-grouped"),
                version: "1.0.0");
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(snapshot);

        var moduleEndpoints = endpoints
            .Where(static endpoint =>
                string.Equals(endpoint.SourceModuleId, "tests.generated.runtimegrouped", StringComparison.Ordinal))
            .OrderBy(static endpoint => endpoint.RoutePattern, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(3, moduleEndpoints.Length);
        Assert.All(
            moduleEndpoints,
            endpoint =>
            {
                Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, endpoint.AuthoringStyle);
                Assert.Contains("Descriptor Id Generated Grouped Runtime API", endpoint.Tags);
            });
        Assert.Contains(moduleEndpoints, static endpoint =>
            string.Equals(endpoint.RoutePattern, "/api/v7/tests/generated/runtimegrouped/orders/{orderId}", StringComparison.Ordinal));
        Assert.Contains(moduleEndpoints, static endpoint =>
            string.Equals(endpoint.RoutePattern, "/api/v7/tests/generated/runtimegrouped/orders/{orderId}/items", StringComparison.Ordinal));
        Assert.Contains(moduleEndpoints, static endpoint =>
            string.Equals(endpoint.RoutePattern, "/api/v8/tests/generated/runtimegrouped/inventory/{orderId}", StringComparison.Ordinal));

        var moduleCandidates = candidates
            .Where(static candidate =>
                string.Equals(candidate.ProjectedEndpoint.SourceModuleId, "tests.generated.runtimegrouped", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(3, moduleCandidates.Length);
        Assert.All(
            moduleCandidates,
            candidate =>
            {
                Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
                Assert.True(candidate.OriginalProjection.AllowsHostGovernance);
                Assert.Equal("descriptor-generated-grouped", candidate.OriginalProjection.HostGovernanceScope);
            });
        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.SourceModuleId, "tests.generated.runtimegrouped", StringComparison.Ordinal) &&
            string.Equals(item.RoutePattern, "/api/v7/tests/generated/runtimegrouped/orders/{orderId}", StringComparison.Ordinal));

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>(
            "/api/v7/tests/generated/runtimegrouped/orders/ord-derived-grouped");
        Assert.NotNull(payload);
        Assert.Equal("ord-derived-grouped", payload.OrderId);
    }

    [Fact]
    public void AddGeneratedRestBehaviorModuleGroupsRejectsBehaviorIdPrefixesWithEmptySegments()
    {
        var builder = WebApplication.CreateBuilder();

        var exception = Assert.Throws<ArgumentException>(() =>
            builder.AddCephalon(engine =>
            {
                engine.AddGeneratedRestBehaviorModuleGroups<GetGeneratedGroupedRuntimeOrderBehavior>(
                    new ModuleDescriptor(
                        "tests.rest.invalid-inline-generated-grouped-runtime",
                        "Invalid Inline Generated Grouped Runtime Module",
                        "Attempts to derive grouped route groups from an invalid behavior id prefix.",
                        version: "1.0.0"),
                    "tests..generated.runtimegrouped");
            }));

        Assert.Contains("non-empty dot-separated segments", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddGeneratedRestBehaviorModuleGroupsRejectsDescriptorIdsThatCannotDeriveGeneratedPrefixes()
    {
        var builder = WebApplication.CreateBuilder();

        var exception = Assert.Throws<ArgumentException>(() =>
            builder.AddCephalon(engine =>
            {
                engine.AddGeneratedRestBehaviorModuleGroups<GetGeneratedGroupedRuntimeOrderBehavior>(
                    new ModuleDescriptor(
                        "tests..generated.runtimegrouped",
                        "Invalid Descriptor Id Generated Grouped Runtime Module",
                        "Attempts to derive grouped generated behavior-id prefixes from an invalid module id.",
                        version: "1.0.0"));
            }));

        Assert.Contains("cannot be used as an inline generated REST behavior-id prefix", exception.Message, StringComparison.Ordinal);
        Assert.Contains("non-empty dot-separated segments", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapGeneratedProfileGroupsDerivesMultipleGeneratedRouteGroupsAndPreservesModuleOwnership()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "7";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "8";
        builder.Configuration["OpenApi:DefaultVersion"] = "7";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedGroupedRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var publicationGroups = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor[]>("/engine/rest-endpoint-publication-groups");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(publicationGroups);
        Assert.NotNull(snapshot);

        var moduleEndpoints = endpoints
            .Where(static endpoint =>
                string.Equals(endpoint.SourceModuleId, "tests.rest.generated-grouped-runtime", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(3, moduleEndpoints.Length);

        var inventoryEndpoint = Assert.Single(moduleEndpoints, static endpoint =>
            string.Equals(endpoint.BehaviorId, "tests.generated.runtimegrouped.inventory.lookup", StringComparison.Ordinal));
        Assert.Equal("/api/v8/tests/generated/runtimegrouped/inventory/{orderId}", inventoryEndpoint.RoutePattern);
        Assert.Equal("/api/v8/tests/generated/runtimegrouped/inventory", inventoryEndpoint.RouteGroupPrefix);
        Assert.Equal("v8", inventoryEndpoint.OpenApiDocumentName);
        Assert.Equal(8, inventoryEndpoint.ApiVersionMajor);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, inventoryEndpoint.AuthoringStyle);

        var ordersCreateEndpoint = Assert.Single(moduleEndpoints, static endpoint =>
            string.Equals(endpoint.BehaviorId, "tests.generated.runtimegrouped.orders.create", StringComparison.Ordinal));
        Assert.Equal("/api/v7/tests/generated/runtimegrouped/orders/{orderId}/items", ordersCreateEndpoint.RoutePattern);
        Assert.Equal("/api/v7/tests/generated/runtimegrouped/orders", ordersCreateEndpoint.RouteGroupPrefix);
        Assert.Equal("v7", ordersCreateEndpoint.OpenApiDocumentName);
        Assert.Equal(7, ordersCreateEndpoint.ApiVersionMajor);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, ordersCreateEndpoint.AuthoringStyle);

        var ordersLookupEndpoint = Assert.Single(moduleEndpoints, static endpoint =>
            string.Equals(endpoint.BehaviorId, "tests.generated.runtimegrouped.orders.lookup", StringComparison.Ordinal));
        Assert.Equal("/api/v7/tests/generated/runtimegrouped/orders/{orderId}", ordersLookupEndpoint.RoutePattern);
        Assert.Equal("/api/v7/tests/generated/runtimegrouped/orders", ordersLookupEndpoint.RouteGroupPrefix);
        Assert.Equal("v7", ordersLookupEndpoint.OpenApiDocumentName);
        Assert.Equal(7, ordersLookupEndpoint.ApiVersionMajor);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, ordersLookupEndpoint.AuthoringStyle);

        var moduleCandidates = candidates
            .Where(static candidate =>
                string.Equals(candidate.ProjectedEndpoint.SourceModuleId, "tests.rest.generated-grouped-runtime", StringComparison.Ordinal))
            .OrderBy(static candidate => candidate.ProjectedEndpoint.RoutePattern, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(3, moduleCandidates.Length);
        Assert.All(
            moduleCandidates,
            candidate =>
            {
                Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
                Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, candidate.AuthoringStyle);
                Assert.True(candidate.OriginalProjection.AllowsHostGovernance);
                Assert.Equal("generated-grouped", candidate.OriginalProjection.HostGovernanceScope);
            });

        Assert.Contains(
            publicationGroups,
            group =>
                string.Equals(group.BehaviorId, "tests.generated.runtimegrouped.orders.lookup", StringComparison.Ordinal) &&
                group.SourceModuleIds.SequenceEqual(["tests.rest.generated-grouped-runtime"]) &&
                group.PublishedCandidateIds.Count == 1 &&
                group.Candidates.Count == 1 &&
                group.HostGovernanceEligibleCandidateIds.Count == 1 &&
                group.HostGovernanceIneligibleCandidateIds.Count == 0);
        Assert.Contains(
            snapshot.RestEndpointPublicationGroups,
            group =>
                string.Equals(group.BehaviorId, "tests.generated.runtimegrouped.inventory.lookup", StringComparison.Ordinal) &&
                group.SourceModuleIds.SequenceEqual(["tests.rest.generated-grouped-runtime"]) &&
                group.PublishedCandidateIds.Count == 1 &&
                group.Candidates.Count == 1);

        var ordersPayload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>(
            "/api/v7/tests/generated/runtimegrouped/orders/ord-grouped");
        var inventoryPayload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>(
            "/api/v8/tests/generated/runtimegrouped/inventory/ord-inventory");
        Assert.NotNull(ordersPayload);
        Assert.NotNull(inventoryPayload);
        Assert.Equal("ord-grouped", ordersPayload.OrderId);
        Assert.Equal("ord-inventory", inventoryPayload.OrderId);
    }

    [Fact]
    public async Task MapCephalonAppliesBehaviorIdPrefixSuppressionsToGeneratedGroupedRuntimeCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "7";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "8";
        builder.Configuration["OpenApi:DefaultVersion"] = "7";
        builder.Configuration["RestApi:Suppressions:hide-grouped-orders:BehaviorIdPrefixes:0"] = "tests.generated.runtimegrouped.orders";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedGroupedRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();
        var suppressionCatalog = app.Services.GetRequiredService<IRestEndpointSuppressionRuntimeCatalog>();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var suppressions = await client.GetFromJsonAsync<RestEndpointSuppressionDescriptor[]>("/engine/rest-endpoint-suppressions");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(suppressions);
        Assert.NotNull(snapshot);

        var moduleEndpoints = endpoints
            .Where(static endpoint =>
                string.Equals(endpoint.SourceModuleId, "tests.rest.generated-grouped-runtime", StringComparison.Ordinal))
            .ToArray();
        var publishedEndpoint = Assert.Single(moduleEndpoints);
        Assert.Equal("tests.generated.runtimegrouped.inventory.lookup", publishedEndpoint.BehaviorId);
        Assert.Equal("/api/v8/tests/generated/runtimegrouped/inventory/{orderId}", publishedEndpoint.RoutePattern);

        var ordersLookupCandidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimegrouped.orders.lookup", StringComparison.Ordinal));
        var ordersCreateCandidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimegrouped.orders.create", StringComparison.Ordinal));
        Assert.Equal("hide-grouped-orders", ordersLookupCandidate.SuppressedBySuppressionId);
        Assert.Equal("hide-grouped-orders", ordersCreateCandidate.SuppressedBySuppressionId);
        Assert.Equal(["hide-grouped-orders"], ordersLookupCandidate.MatchedSuppressionIds);
        Assert.Equal(["hide-grouped-orders"], ordersCreateCandidate.MatchedSuppressionIds);
        Assert.Equal(RestEndpointGovernanceRuleSelectionBasis.SingleMatch, ordersLookupCandidate.SuppressionSelectionBasis);
        Assert.Equal(RestEndpointGovernanceRuleSelectionBasis.SingleMatch, ordersCreateCandidate.SuppressionSelectionBasis);

        var rule = Assert.Single(suppressions);
        Assert.Equal("hide-grouped-orders", rule.Id);
        Assert.Empty(rule.BehaviorIds);
        Assert.Equal(["tests.generated.runtimegrouped.orders"], rule.BehaviorIdPrefixes);
        Assert.Equal(2, rule.MatchedCandidateIds.Count);
        Assert.Equal(2, rule.SuppressedCandidateIds.Count);
        Assert.Equal([RestEndpointGovernanceRuleSelectionBasis.SingleMatch], rule.SelectionBases);

        Assert.Equal(
            ["hide-grouped-orders"],
            suppressionCatalog.GetByBehaviorId("tests.generated.runtimegrouped.orders.lookup").Select(static item => item.Id).ToArray());
        Assert.Equal(
            ["hide-grouped-orders"],
            suppressionCatalog.GetByBehaviorId("tests.generated.runtimegrouped.orders.create").Select(static item => item.Id).ToArray());
        Assert.Empty(suppressionCatalog.GetByBehaviorId("tests.generated.runtimegrouped.inventory.lookup"));

        Assert.Contains(snapshot.RestEndpointSuppressions, item =>
            string.Equals(item.Id, "hide-grouped-orders", StringComparison.Ordinal) &&
            item.BehaviorIdPrefixes.SequenceEqual(["tests.generated.runtimegrouped.orders"]) &&
            item.SuppressedCandidateIds.Count == 2);
        Assert.DoesNotContain(snapshot.RestEndpoints, item =>
            string.Equals(item.BehaviorId, "tests.generated.runtimegrouped.orders.lookup", StringComparison.Ordinal));

        var ordersResponse = await client.GetAsync("/api/v7/tests/generated/runtimegrouped/orders/ord-grouped");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, ordersResponse.StatusCode);

        var inventoryPayload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>(
            "/api/v8/tests/generated/runtimegrouped/inventory/ord-inventory");
        Assert.NotNull(inventoryPayload);
        Assert.Equal("ord-inventory", inventoryPayload.OrderId);
    }

    [Fact]
    public async Task MapCephalonPrefersExactBehaviorSuppressionOverPrefixTargetedRuleAcrossGroupedOperatorSurfaces()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "7";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "8";
        builder.Configuration["OpenApi:DefaultVersion"] = "7";
        builder.Configuration["RestApi:Suppressions:hide-grouped-orders:BehaviorIdPrefixes:0"] = "tests.generated.runtimegrouped.orders";
        builder.Configuration["RestApi:Suppressions:hide-grouped-orders-lookup:Behaviors:0"] = "tests.generated.runtimegrouped.orders.lookup";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedGroupedRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();
        var suppressionCatalog = app.Services.GetRequiredService<IRestEndpointSuppressionRuntimeCatalog>();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var groups = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor[]>("/engine/rest-endpoint-publication-groups");
        var lookupGroupByBehavior = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor>(
            "/engine/rest-endpoint-publication-groups/tests.generated.runtimegrouped.orders.lookup");
        var createGroupByBehavior = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor>(
            "/engine/rest-endpoint-publication-groups/tests.generated.runtimegrouped.orders.create");
        var policies = await client.GetFromJsonAsync<RestEndpointAuthoringPolicyDescriptor[]>("/engine/rest-endpoint-authoring-policies");
        var lookupPolicyByBehavior = await client.GetFromJsonAsync<RestEndpointAuthoringPolicyDescriptor>(
            "/engine/rest-endpoint-authoring-policies/tests.generated.runtimegrouped.orders.lookup");
        var createPolicyByBehavior = await client.GetFromJsonAsync<RestEndpointAuthoringPolicyDescriptor>(
            "/engine/rest-endpoint-authoring-policies/tests.generated.runtimegrouped.orders.create");
        var suppressions = await client.GetFromJsonAsync<RestEndpointSuppressionDescriptor[]>("/engine/rest-endpoint-suppressions");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(groups);
        Assert.NotNull(lookupGroupByBehavior);
        Assert.NotNull(createGroupByBehavior);
        Assert.NotNull(policies);
        Assert.NotNull(lookupPolicyByBehavior);
        Assert.NotNull(createPolicyByBehavior);
        Assert.NotNull(suppressions);
        Assert.NotNull(snapshot);

        var moduleEndpoints = endpoints
            .Where(static endpoint =>
                string.Equals(endpoint.SourceModuleId, "tests.rest.generated-grouped-runtime", StringComparison.Ordinal))
            .ToArray();
        var publishedEndpoint = Assert.Single(moduleEndpoints);
        Assert.Equal("tests.generated.runtimegrouped.inventory.lookup", publishedEndpoint.BehaviorId);

        var ordersLookupCandidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimegrouped.orders.lookup", StringComparison.Ordinal));
        var ordersCreateCandidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimegrouped.orders.create", StringComparison.Ordinal));
        Assert.Equal("hide-grouped-orders-lookup", ordersLookupCandidate.SuppressedBySuppressionId);
        Assert.Equal(
            ["hide-grouped-orders-lookup", "hide-grouped-orders"],
            ordersLookupCandidate.MatchedSuppressionIds);
        Assert.Equal(
            RestEndpointGovernanceRuleSelectionBasis.NarrowerBehaviorScope,
            ordersLookupCandidate.SuppressionSelectionBasis);
        Assert.Equal("hide-grouped-orders", ordersCreateCandidate.SuppressedBySuppressionId);
        Assert.Equal(["hide-grouped-orders"], ordersCreateCandidate.MatchedSuppressionIds);
        Assert.Equal(RestEndpointGovernanceRuleSelectionBasis.SingleMatch, ordersCreateCandidate.SuppressionSelectionBasis);

        var exactRule = Assert.Single(suppressions, static item => string.Equals(item.Id, "hide-grouped-orders-lookup", StringComparison.Ordinal));
        Assert.Equal(["tests.generated.runtimegrouped.orders.lookup"], exactRule.BehaviorIds);
        Assert.Empty(exactRule.BehaviorIdPrefixes);
        Assert.Equal([ordersLookupCandidate.Id], exactRule.MatchedCandidateIds);
        Assert.Equal([ordersLookupCandidate.Id], exactRule.SuppressedCandidateIds);
        Assert.Equal([RestEndpointGovernanceRuleSelectionBasis.NarrowerBehaviorScope], exactRule.SelectionBases);

        var prefixRule = Assert.Single(suppressions, static item => string.Equals(item.Id, "hide-grouped-orders", StringComparison.Ordinal));
        Assert.Empty(prefixRule.BehaviorIds);
        Assert.Equal(["tests.generated.runtimegrouped.orders"], prefixRule.BehaviorIdPrefixes);
        Assert.Contains(ordersLookupCandidate.Id, prefixRule.MatchedCandidateIds, StringComparer.Ordinal);
        Assert.Contains(ordersCreateCandidate.Id, prefixRule.MatchedCandidateIds, StringComparer.Ordinal);
        Assert.Equal([ordersCreateCandidate.Id], prefixRule.SuppressedCandidateIds);

        Assert.Equal(
            ["hide-grouped-orders", "hide-grouped-orders-lookup"],
            suppressionCatalog.GetByBehaviorId("tests.generated.runtimegrouped.orders.lookup").Select(static item => item.Id).ToArray());
        Assert.Equal(
            ["hide-grouped-orders"],
            suppressionCatalog.GetByBehaviorId("tests.generated.runtimegrouped.orders.create").Select(static item => item.Id).ToArray());
        Assert.Empty(suppressionCatalog.GetByBehaviorId("tests.generated.runtimegrouped.inventory.lookup"));

        var lookupGroup = Assert.Single(groups, static item =>
            string.Equals(item.BehaviorId, "tests.generated.runtimegrouped.orders.lookup", StringComparison.Ordinal));
        Assert.False(lookupGroup.AuthoringPolicy.IsConfigured);
        Assert.False(lookupGroup.AuthoringPolicy.AllowMultiplePublishedCandidates);
        Assert.Empty(lookupGroup.PublishedCandidateIds);
        Assert.Equal([ordersLookupCandidate.Id], lookupGroup.GovernanceSuppressedCandidateIds);
        Assert.Equal(2, lookupGroup.GovernanceSuppressionSummaries.Count);
        var lookupExactSuppression = Assert.Single(lookupGroup.GovernanceSuppressionSummaries, static item =>
            string.Equals(item.RuleId, "hide-grouped-orders-lookup", StringComparison.Ordinal));
        Assert.Equal([ordersLookupCandidate.Id], lookupExactSuppression.MatchedCandidateIds);
        Assert.Equal([ordersLookupCandidate.Id], lookupExactSuppression.SuppressedCandidateIds);
        Assert.Equal(
            RestEndpointGovernanceRuleSelectionBasis.NarrowerBehaviorScope,
            Assert.Single(lookupExactSuppression.SelectionBasisSummaries).SelectionBasis);
        var lookupPrefixSuppression = Assert.Single(lookupGroup.GovernanceSuppressionSummaries, static item =>
            string.Equals(item.RuleId, "hide-grouped-orders", StringComparison.Ordinal));
        Assert.Equal([ordersLookupCandidate.Id], lookupPrefixSuppression.MatchedCandidateIds);
        Assert.Empty(lookupPrefixSuppression.SuppressedCandidateIds);
        Assert.Empty(lookupPrefixSuppression.SelectionBasisSummaries);
        var lookupGeneratedStyle = Assert.Single(lookupGroup.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([ordersLookupCandidate.Id], lookupGeneratedStyle.GovernanceSuppressedCandidateIds);
        Assert.Equal(2, lookupGeneratedStyle.GovernanceSuppressionSummaries.Count);

        var createGroup = Assert.Single(groups, static item =>
            string.Equals(item.BehaviorId, "tests.generated.runtimegrouped.orders.create", StringComparison.Ordinal));
        Assert.False(createGroup.AuthoringPolicy.IsConfigured);
        Assert.False(createGroup.AuthoringPolicy.AllowMultiplePublishedCandidates);
        Assert.Empty(createGroup.PublishedCandidateIds);
        Assert.Equal([ordersCreateCandidate.Id], createGroup.GovernanceSuppressedCandidateIds);
        var createSuppression = Assert.Single(createGroup.GovernanceSuppressionSummaries);
        Assert.Equal("hide-grouped-orders", createSuppression.RuleId);
        Assert.Equal([ordersCreateCandidate.Id], createSuppression.MatchedCandidateIds);
        Assert.Equal([ordersCreateCandidate.Id], createSuppression.SuppressedCandidateIds);
        Assert.Equal(
            RestEndpointGovernanceRuleSelectionBasis.SingleMatch,
            Assert.Single(createSuppression.SelectionBasisSummaries).SelectionBasis);
        var createGeneratedStyle = Assert.Single(createGroup.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([ordersCreateCandidate.Id], createGeneratedStyle.GovernanceSuppressedCandidateIds);
        Assert.Single(createGeneratedStyle.GovernanceSuppressionSummaries);

        var lookupPolicy = Assert.Single(policies, static item =>
            string.Equals(item.BehaviorId, "tests.generated.runtimegrouped.orders.lookup", StringComparison.Ordinal));
        Assert.False(lookupPolicy.IsConfigured);
        Assert.False(lookupPolicy.AllowMultiplePublishedCandidates);
        Assert.Equal([ordersLookupCandidate.Id], lookupPolicy.GovernanceSuppressedCandidateIds);
        Assert.Equal(2, lookupPolicy.GovernanceSuppressionSummaries.Count);
        var lookupExactPolicySuppression = Assert.Single(lookupPolicy.GovernanceSuppressionSummaries, static item =>
            string.Equals(item.RuleId, "hide-grouped-orders-lookup", StringComparison.Ordinal));
        Assert.Equal([ordersLookupCandidate.Id], lookupExactPolicySuppression.MatchedCandidateIds);
        Assert.Equal([ordersLookupCandidate.Id], lookupExactPolicySuppression.SuppressedCandidateIds);
        Assert.Equal(
            RestEndpointGovernanceRuleSelectionBasis.NarrowerBehaviorScope,
            Assert.Single(lookupExactPolicySuppression.SelectionBasisSummaries).SelectionBasis);
        var lookupPrefixPolicySuppression = Assert.Single(lookupPolicy.GovernanceSuppressionSummaries, static item =>
            string.Equals(item.RuleId, "hide-grouped-orders", StringComparison.Ordinal));
        Assert.Equal([ordersLookupCandidate.Id], lookupPrefixPolicySuppression.MatchedCandidateIds);
        Assert.Empty(lookupPrefixPolicySuppression.SuppressedCandidateIds);
        Assert.Empty(lookupPrefixPolicySuppression.SelectionBasisSummaries);
        var lookupPolicyGeneratedStyle = Assert.Single(lookupPolicy.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal(2, lookupPolicyGeneratedStyle.GovernanceSuppressionSummaries.Count);

        var createPolicy = Assert.Single(policies, static item =>
            string.Equals(item.BehaviorId, "tests.generated.runtimegrouped.orders.create", StringComparison.Ordinal));
        Assert.False(createPolicy.IsConfigured);
        Assert.False(createPolicy.AllowMultiplePublishedCandidates);
        Assert.Equal([ordersCreateCandidate.Id], createPolicy.GovernanceSuppressedCandidateIds);
        var createPolicySuppression = Assert.Single(createPolicy.GovernanceSuppressionSummaries);
        Assert.Equal("hide-grouped-orders", createPolicySuppression.RuleId);
        Assert.Equal([ordersCreateCandidate.Id], createPolicySuppression.MatchedCandidateIds);
        Assert.Equal([ordersCreateCandidate.Id], createPolicySuppression.SuppressedCandidateIds);
        Assert.Equal(
            RestEndpointGovernanceRuleSelectionBasis.SingleMatch,
            Assert.Single(createPolicySuppression.SelectionBasisSummaries).SelectionBasis);
        var createPolicyGeneratedStyle = Assert.Single(createPolicy.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Single(createPolicyGeneratedStyle.GovernanceSuppressionSummaries);

        Assert.Equal(lookupGroup.GovernanceSuppressionSummaries.Count, lookupGroupByBehavior.GovernanceSuppressionSummaries.Count);
        Assert.Equal(createGroup.GovernanceSuppressionSummaries.Count, createGroupByBehavior.GovernanceSuppressionSummaries.Count);
        Assert.Equal(lookupPolicy.GovernanceSuppressionSummaries.Count, lookupPolicyByBehavior.GovernanceSuppressionSummaries.Count);
        Assert.Equal(createPolicy.GovernanceSuppressionSummaries.Count, createPolicyByBehavior.GovernanceSuppressionSummaries.Count);

        Assert.Contains(snapshot.RestEndpointPublicationGroups, item =>
            string.Equals(item.BehaviorId, lookupGroup.BehaviorId, StringComparison.Ordinal) &&
            item.GovernanceSuppressionSummaries.Count == 2 &&
            item.GovernanceSuppressedCandidateIds.SequenceEqual([ordersLookupCandidate.Id]));
        Assert.Contains(snapshot.RestEndpointPublicationGroups, item =>
            string.Equals(item.BehaviorId, createGroup.BehaviorId, StringComparison.Ordinal) &&
            item.GovernanceSuppressionSummaries.Count == 1 &&
            item.GovernanceSuppressedCandidateIds.SequenceEqual([ordersCreateCandidate.Id]));
        Assert.Contains(snapshot.RestEndpointAuthoringPolicies, item =>
            string.Equals(item.BehaviorId, lookupPolicy.BehaviorId, StringComparison.Ordinal) &&
            item.GovernanceSuppressionSummaries.Count == 2 &&
            item.GovernanceSuppressedCandidateIds.SequenceEqual([ordersLookupCandidate.Id]));
        Assert.Contains(snapshot.RestEndpointAuthoringPolicies, item =>
            string.Equals(item.BehaviorId, createPolicy.BehaviorId, StringComparison.Ordinal) &&
            item.GovernanceSuppressionSummaries.Count == 1 &&
            item.GovernanceSuppressedCandidateIds.SequenceEqual([ordersCreateCandidate.Id]));
        Assert.Contains(snapshot.RestEndpointSuppressions, item =>
            string.Equals(item.Id, "hide-grouped-orders-lookup", StringComparison.Ordinal) &&
            item.SelectionBases.SequenceEqual([RestEndpointGovernanceRuleSelectionBasis.NarrowerBehaviorScope]));
        Assert.Contains(snapshot.RestEndpointSuppressions, item =>
            string.Equals(item.Id, "hide-grouped-orders", StringComparison.Ordinal) &&
            item.BehaviorIdPrefixes.SequenceEqual(["tests.generated.runtimegrouped.orders"]) &&
            item.SuppressedCandidateIds.SequenceEqual([ordersCreateCandidate.Id]));
    }

    [Fact]
    public void AddGeneratedRestBehaviorModuleRejectsBehaviorIdPrefixesWithEmptySegments()
    {
        var builder = WebApplication.CreateBuilder();

        var exception = Assert.Throws<ArgumentException>(() =>
            builder.AddCephalon(engine =>
            {
                engine.AddGeneratedRestBehaviorModule<GetGeneratedRuntimeOrderBehavior>(
                    new ModuleDescriptor(
                        "tests.rest.invalid-inline-generated-runtime",
                        "Invalid Inline Generated Runtime Module",
                        "Attempts to derive a route group from an invalid behavior id prefix.",
                        version: "1.0.0"),
                    "tests..generated.runtime");
            }));

        Assert.Contains("non-empty dot-separated segments", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddGeneratedRestBehaviorModuleRejectsDescriptorIdsThatCannotDeriveGeneratedPrefixes()
    {
        var builder = WebApplication.CreateBuilder();

        var exception = Assert.Throws<ArgumentException>(() =>
            builder.AddCephalon(engine =>
            {
                engine.AddGeneratedRestBehaviorModule<GetGeneratedRuntimeOrderBehavior>(
                    new ModuleDescriptor(
                        "tests..generated.runtime",
                        "Invalid Descriptor Id Generated Runtime Module",
                        "Attempts to derive a generated behavior-id prefix from an invalid module id.",
                        version: "1.0.0"));
            }));

        Assert.Contains("cannot be used as an inline generated REST behavior-id prefix", exception.Message, StringComparison.Ordinal);
        Assert.Contains("non-empty dot-separated segments", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonAppliesRestApiVersionOverridesAndExposesOverrideCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-v6:Behaviors:0"] = "tests.generated.runtimeoverride.lookup";
        builder.Configuration["RestApi:Overrides:prefer-v6:ApiVersionMajor"] = "6";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedVersionOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/generated/runtime/override/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal("v6", endpoint.OpenApiDocumentName);
        Assert.Equal(6, endpoint.ApiVersionMajor);
        Assert.Equal(["prefer-v6"], endpoint.MatchedOverrideIds);
        Assert.NotNull(endpoint.OriginalProjection);
        Assert.Equal(4, endpoint.OriginalProjection!.ApiVersionMajor);
        Assert.Equal("v4", endpoint.OriginalProjection.OpenApiDocumentName);
        Assert.Equal("GET", endpoint.OriginalProjection.Method);
        Assert.Equal("/api/v4/tests/generated/runtime/override", endpoint.OriginalProjection.RouteGroupPrefix);
        Assert.Equal("/orders/{orderId}", endpoint.OriginalProjection.RelativePattern);
        Assert.Equal("/api/v4/tests/generated/runtime/override/orders/{orderId}", endpoint.OriginalProjection.RoutePattern);
        Assert.Equal("Generated Override API", endpoint.OriginalProjection.TagName);
        Assert.Equal(
            "tests_rest_generated_runtime_override.v4.tests_generated_runtimeoverride_lookup",
            endpoint.OriginalEndpointName);
        Assert.Equal("tests.generated.runtimeoverride.lookup", endpoint.OriginalSummary);
        Assert.Equal(
            "Publishes generated shorthand so REST governance can override the effective API version.",
            endpoint.OriginalDescription);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, endpoint.Metadata["authoringStyle"]);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-v6", candidate.AppliedOverrideId);
        Assert.Equal(4, candidate.OriginalProjection.ApiVersionMajor);
        Assert.Equal("v4", candidate.OriginalProjection.OpenApiDocumentName);
        Assert.Equal("GET", candidate.OriginalProjection.Method);
        Assert.Equal("/api/v4/tests/generated/runtime/override", candidate.OriginalProjection.RouteGroupPrefix);
        Assert.Equal("/orders/{orderId}", candidate.OriginalProjection.RelativePattern);
        Assert.Equal("/api/v4/tests/generated/runtime/override/orders/{orderId}", candidate.OriginalProjection.RoutePattern);
        Assert.Equal("Generated Override API", candidate.OriginalProjection.TagName);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);
        Assert.Equal(endpoint.OriginalEndpointName, candidate.ProjectedEndpoint.OriginalEndpointName);
        Assert.Equal(endpoint.OriginalSummary, candidate.ProjectedEndpoint.OriginalSummary);
        Assert.Equal(endpoint.OriginalDescription, candidate.ProjectedEndpoint.OriginalDescription);

        var rule = Assert.Single(overrides);
        Assert.Equal("prefer-v6", rule.Id);
        Assert.Equal(6, rule.ApiVersionMajor);
        Assert.Contains("tests.generated.runtimeoverride.lookup", rule.BehaviorIds);

        Assert.Contains(snapshot.RestEndpointOverrides, item =>
            string.Equals(item.Id, "prefer-v6", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            string.Equals(item.AppliedOverrideId, "prefer-v6", StringComparison.Ordinal) &&
            string.Equals(item.ProjectedEndpoint.OriginalEndpointName, "tests_rest_generated_runtime_override.v4.tests_generated_runtimeoverride_lookup", StringComparison.Ordinal) &&
            item.OriginalProjection.ApiVersionMajor == 4 &&
            string.Equals(item.OriginalProjection.RoutePattern, "/api/v4/tests/generated/runtime/override/orders/{orderId}", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.Id, endpoint.Id, StringComparison.Ordinal) &&
            string.Equals(item.AppliedOverrideId, "prefer-v6", StringComparison.Ordinal) &&
            item.MatchedOverrideIds.SequenceEqual(["prefer-v6"]) &&
            string.Equals(item.OriginalEndpointName, "tests_rest_generated_runtime_override.v4.tests_generated_runtimeoverride_lookup", StringComparison.Ordinal) &&
            string.Equals(item.OriginalSummary, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal) &&
            string.Equals(item.OriginalDescription, "Publishes generated shorthand so REST governance can override the effective API version.", StringComparison.Ordinal) &&
            item.OriginalProjection is not null &&
            item.OriginalProjection.ApiVersionMajor == 4 &&
            string.Equals(item.OriginalProjection.RoutePattern, "/api/v4/tests/generated/runtime/override/orders/{orderId}", StringComparison.Ordinal));

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v6/tests/generated/runtime/override/orders/ord-42");
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonDoesNotApplyRestGovernanceToExplicitDslRoutesWithoutOptIn()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "9";
        builder.Configuration["OpenApi:DefaultVersion"] = "9";
        builder.Configuration["RestApi:Overrides:govern-disabled-explicit:AuthoringStyles:0"] = RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:Overrides:govern-disabled-explicit:HostGovernanceScopes:0"] = "explicit-disabled";
        builder.Configuration["RestApi:Overrides:govern-disabled-explicit:Pattern"] = "/governed/{orderId}";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ExplicitDslHostGovernanceDisabledRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.dsl.runtimeoverride.disabled.lookup", StringComparison.Ordinal));
        Assert.Equal("/api/v9/tests/dsl/runtime/override-disabled/orders/{orderId}", endpoint.RoutePattern);
        Assert.Null(endpoint.AppliedOverrideId);
        Assert.Empty(endpoint.MatchedOverrideIds);
        Assert.NotNull(endpoint.OriginalProjection);
        Assert.False(endpoint.OriginalProjection!.AllowsHostGovernance);
        Assert.Equal("explicit-disabled", endpoint.OriginalProjection.HostGovernanceScope);
        Assert.Equal(endpoint.RoutePattern, endpoint.OriginalProjection.RoutePattern);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.dsl.runtimeoverride.disabled.lookup", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Null(candidate.AppliedOverrideId);
        Assert.Empty(candidate.MatchedOverrideIds);
        Assert.False(candidate.OriginalProjection.AllowsHostGovernance);
        Assert.Equal("explicit-disabled", candidate.OriginalProjection.HostGovernanceScope);
        Assert.Equal("/api/v9/tests/dsl/runtime/override-disabled/orders/{orderId}", candidate.OriginalProjection.RoutePattern);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);

        var originalPayload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v9/tests/dsl/runtime/override-disabled/orders/ord-disabled");
        Assert.NotNull(originalPayload);
        Assert.Equal("ord-disabled", originalPayload.OrderId);

        var governedResponse = await client.GetAsync("/api/v9/tests/dsl/runtime/override-disabled/orders/governed/ord-disabled");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, governedResponse.StatusCode);
    }

    [Fact]
    public async Task MapCephalonExposesSkippedRestGovernanceRulesForExplicitDslRoutesWithoutOptIn()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "9";
        builder.Configuration["OpenApi:DefaultVersion"] = "9";
        builder.Configuration["RestApi:Suppressions:skip-disabled-explicit:AuthoringStyles:0"] = RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:Suppressions:skip-disabled-explicit:HostGovernanceScopes:0"] = "explicit-disabled";
        builder.Configuration["RestApi:Overrides:rewrite-disabled-explicit:AuthoringStyles:0"] = RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:Overrides:rewrite-disabled-explicit:HostGovernanceScopes:0"] = "explicit-disabled";
        builder.Configuration["RestApi:Overrides:rewrite-disabled-explicit:Pattern"] = "/governed/{orderId}";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ExplicitDslHostGovernanceDisabledRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.dsl.runtimeoverride.disabled.lookup", StringComparison.Ordinal));
        Assert.Equal("/api/v9/tests/dsl/runtime/override-disabled/orders/{orderId}", endpoint.RoutePattern);
        Assert.Empty(endpoint.MatchedOverrideIds);
        Assert.Equal(["skip-disabled-explicit"], endpoint.SkippedSuppressionIds);
        Assert.Equal(["rewrite-disabled-explicit"], endpoint.SkippedOverrideIds);
        Assert.NotNull(endpoint.OriginalProjection);
        Assert.False(endpoint.OriginalProjection!.AllowsHostGovernance);
        Assert.Equal("explicit-disabled", endpoint.OriginalProjection.HostGovernanceScope);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.dsl.runtimeoverride.disabled.lookup", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Empty(candidate.MatchedSuppressionIds);
        Assert.Empty(candidate.MatchedOverrideIds);
        Assert.Equal(["skip-disabled-explicit"], candidate.SkippedSuppressionIds);
        Assert.Equal(["rewrite-disabled-explicit"], candidate.SkippedOverrideIds);
        Assert.False(candidate.OriginalProjection.AllowsHostGovernance);
        Assert.Equal("explicit-disabled", candidate.OriginalProjection.HostGovernanceScope);
        Assert.Equal(["skip-disabled-explicit"], candidate.ProjectedEndpoint.SkippedSuppressionIds);
        Assert.Equal(["rewrite-disabled-explicit"], candidate.ProjectedEndpoint.SkippedOverrideIds);
        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.Id, endpoint.Id, StringComparison.Ordinal) &&
            item.SkippedSuppressionIds.SequenceEqual(["skip-disabled-explicit"]) &&
            item.SkippedOverrideIds.SequenceEqual(["rewrite-disabled-explicit"]));
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            item.SkippedSuppressionIds.SequenceEqual(["skip-disabled-explicit"]) &&
            item.SkippedOverrideIds.SequenceEqual(["rewrite-disabled-explicit"]));

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v9/tests/dsl/runtime/override-disabled/orders/ord-visible");
        Assert.NotNull(payload);
        Assert.Equal("ord-visible", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonAppliesRestGovernanceToExplicitDslRoutesWhenGroupOptsIn()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "9";
        builder.Configuration["OpenApi:DefaultVersion"] = "9";
        builder.Configuration["RestApi:Overrides:govern-enabled-explicit:AuthoringStyles:0"] = RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:Overrides:govern-enabled-explicit:HostGovernanceScopes:0"] = "explicit-enabled";
        builder.Configuration["RestApi:Overrides:govern-enabled-explicit:Pattern"] = "/governed/{orderId}";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ExplicitDslHostGovernanceEnabledRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.dsl.runtimeoverride.enabled.lookup", StringComparison.Ordinal));
        Assert.Equal("/api/v9/tests/dsl/runtime/override-enabled/orders/governed/{orderId}", endpoint.RoutePattern);
        Assert.Equal("govern-enabled-explicit", endpoint.AppliedOverrideId);
        Assert.Equal(["govern-enabled-explicit"], endpoint.MatchedOverrideIds);
        Assert.NotNull(endpoint.OriginalProjection);
        Assert.True(endpoint.OriginalProjection!.AllowsHostGovernance);
        Assert.Equal("explicit-enabled", endpoint.OriginalProjection.HostGovernanceScope);
        Assert.Equal("/api/v9/tests/dsl/runtime/override-enabled/orders/{orderId}", endpoint.OriginalProjection.RoutePattern);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.dsl.runtimeoverride.enabled.lookup", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("govern-enabled-explicit", candidate.AppliedOverrideId);
        Assert.Equal(["govern-enabled-explicit"], candidate.MatchedOverrideIds);
        Assert.True(candidate.OriginalProjection.AllowsHostGovernance);
        Assert.Equal("explicit-enabled", candidate.OriginalProjection.HostGovernanceScope);
        Assert.Equal("/api/v9/tests/dsl/runtime/override-enabled/orders/{orderId}", candidate.OriginalProjection.RoutePattern);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "govern-enabled-explicit", StringComparison.Ordinal));
        Assert.Empty(rule.BehaviorIds);
        Assert.Contains(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, rule.AuthoringStyles);
        Assert.Contains("explicit-enabled", rule.HostGovernanceScopes, StringComparer.Ordinal);

        var governedPayload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v9/tests/dsl/runtime/override-enabled/orders/governed/ord-enabled");
        Assert.NotNull(governedPayload);
        Assert.Equal("ord-enabled", governedPayload.OrderId);

        var originalResponse = await client.GetAsync("/api/v9/tests/dsl/runtime/override-enabled/orders/ord-enabled");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, originalResponse.StatusCode);
    }

    [Fact]
    public async Task MapCephalonSuppressesExplicitDslRoutesWhenGroupOptsInToHostGovernance()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "9";
        builder.Configuration["OpenApi:DefaultVersion"] = "9";
        builder.Configuration["RestApi:Suppressions:suppress-governed-explicit:AuthoringStyles:0"] = RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:Suppressions:suppress-governed-explicit:HostGovernanceScopes:0"] = "explicit-suppression";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ExplicitDslHostGovernanceSuppressionRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var suppressions = await client.GetFromJsonAsync<RestEndpointSuppressionDescriptor[]>("/engine/rest-endpoint-suppressions");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(suppressions);
        Assert.NotNull(snapshot);
        Assert.DoesNotContain(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.dsl.runtimesuppression.enabled.lookup", StringComparison.Ordinal));

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.dsl.runtimesuppression.enabled.lookup", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Suppressed, candidate.Status);
        Assert.Equal("suppress-governed-explicit", candidate.SuppressedBySuppressionId);
        Assert.Equal(["suppress-governed-explicit"], candidate.MatchedSuppressionIds);
        Assert.True(candidate.OriginalProjection.AllowsHostGovernance);
        Assert.Equal("explicit-suppression", candidate.OriginalProjection.HostGovernanceScope);
        Assert.Equal("/api/v9/tests/dsl/runtime/suppression-enabled/orders/{orderId}", candidate.OriginalProjection.RoutePattern);
        Assert.Null(candidate.SuppressedByCandidateId);

        var suppression = Assert.Single(suppressions, static item => string.Equals(item.Id, "suppress-governed-explicit", StringComparison.Ordinal));
        Assert.Empty(suppression.BehaviorIds);
        Assert.Contains(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, suppression.AuthoringStyles);
        Assert.Contains("explicit-suppression", suppression.HostGovernanceScopes, StringComparer.Ordinal);
        Assert.Contains(snapshot.RestEndpointSuppressions, item =>
            string.Equals(item.Id, "suppress-governed-explicit", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            string.Equals(item.SuppressedBySuppressionId, "suppress-governed-explicit", StringComparison.Ordinal) &&
            item.OriginalProjection.AllowsHostGovernance &&
            string.Equals(item.OriginalProjection.HostGovernanceScope, "explicit-suppression", StringComparison.Ordinal));

        var response = await client.GetAsync("/api/v9/tests/dsl/runtime/suppression-enabled/orders/ord-suppressed");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MapCephalonAppliesRestMethodOverridesAndExposesOverrideCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.Configuration["RestApi:Overrides:prefer-delete:Behaviors:0"] = "tests.generated.runtimeoverride.lookup";
        builder.Configuration["RestApi:Overrides:prefer-delete:Method"] = "DELETE";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedVersionOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var rawOverrides = await client.GetStringAsync("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");
        var rawSnapshot = await client.GetStringAsync("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal("DELETE", endpoint.Method);
        Assert.Equal("/api/v4/tests/generated/runtime/override/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal("v4", endpoint.OpenApiDocumentName);
        Assert.Equal(4, endpoint.ApiVersionMajor);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-delete", candidate.AppliedOverrideId);
        Assert.Equal("DELETE", candidate.ProjectedEndpoint.Method);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "prefer-delete", StringComparison.Ordinal));
        Assert.Equal("DELETE", rule.Method);
        Assert.Null(rule.ApiVersionMajor);

        Assert.Contains(snapshot.RestEndpointOverrides, item =>
            string.Equals(item.Id, "prefer-delete", StringComparison.Ordinal) &&
            string.Equals(item.Method, "DELETE", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            string.Equals(item.AppliedOverrideId, "prefer-delete", StringComparison.Ordinal));

        var getResponse = await client.GetAsync("/api/v4/tests/generated/runtime/override/orders/ord-42");
        Assert.Equal(System.Net.HttpStatusCode.MethodNotAllowed, getResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync("/api/v4/tests/generated/runtime/override/orders/ord-42");
        deleteResponse.EnsureSuccessStatusCode();
        var payload = await deleteResponse.Content.ReadFromJsonAsync<GeneratedRuntimeOrderOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonAppliesTagNameOverridesAndExposesOverrideCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.Configuration["RestApi:Overrides:prefer-public-tag:Behaviors:0"] = "tests.generated.runtimeoverride.lookup";
        builder.Configuration["RestApi:Overrides:prefer-public-tag:TagName"] = "Generated Public API";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedVersionOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var groups = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor[]>("/engine/rest-endpoint-publication-groups");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");
        var groupsJson = await client.GetStringAsync("/engine/rest-endpoint-publication-groups");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(groups);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal("/api/v4/tests/generated/runtime/override/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal(["Generated Public API"], endpoint.Tags);
        Assert.Equal("prefer-public-tag", endpoint.AppliedOverrideId);
        Assert.Equal(["prefer-public-tag"], endpoint.MatchedOverrideIds);
        Assert.NotNull(endpoint.OriginalProjection);
        Assert.Equal("Generated Override API", endpoint.OriginalProjection!.TagName);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-public-tag", candidate.AppliedOverrideId);
        Assert.Equal(["Generated Public API"], candidate.ProjectedEndpoint.Tags);
        Assert.Equal("Generated Override API", candidate.OriginalProjection.TagName);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "prefer-public-tag", StringComparison.Ordinal));
        Assert.Equal("Generated Public API", rule.TagName);

        Assert.Contains(snapshot.RestEndpointOverrides, item =>
            string.Equals(item.Id, "prefer-public-tag", StringComparison.Ordinal) &&
            string.Equals(item.TagName, "Generated Public API", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            string.Equals(item.AppliedOverrideId, "prefer-public-tag", StringComparison.Ordinal) &&
            item.ProjectedEndpoint.Tags.SequenceEqual(["Generated Public API"]) &&
            string.Equals(item.OriginalProjection.TagName, "Generated Override API", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.Id, endpoint.Id, StringComparison.Ordinal) &&
            string.Equals(item.AppliedOverrideId, "prefer-public-tag", StringComparison.Ordinal) &&
            item.Tags.SequenceEqual(["Generated Public API"]) &&
            item.OriginalProjection is not null &&
            string.Equals(item.OriginalProjection.TagName, "Generated Override API", StringComparison.Ordinal));

        var routeEndpoint = Assert.Single(
            ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(static dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>(),
            static item => string.Equals(item.RoutePattern.RawText, "/api/v4/tests/generated/runtime/override/orders/{orderId}", StringComparison.Ordinal));
        Assert.Equal(
            ["Generated Public API"],
            routeEndpoint.Metadata.OfType<ITagsMetadata>().SelectMany(static metadata => metadata.Tags).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
        Assert.Equal("prefer-public-tag", routeEndpoint.Metadata.GetMetadata<RestEndpointAppliedOverrideMetadata>()?.OverrideId);

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v4/tests/generated/runtime/override/orders/ord-42");
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonAppliesOpenApiDocumentNameOverridesAndExposesOverrideCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.Configuration["RestApi:Overrides:prefer-public-document:Behaviors:0"] = "tests.generated.runtimeoverride.lookup";
        builder.Configuration["RestApi:Overrides:prefer-public-document:OpenApiDocumentName"] = "public";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedVersionOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal("/api/v4/tests/generated/runtime/override/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal("public", endpoint.OpenApiDocumentName);
        Assert.Equal(4, endpoint.ApiVersionMajor);
        Assert.Equal("prefer-public-document", endpoint.AppliedOverrideId);
        Assert.Equal(["prefer-public-document"], endpoint.MatchedOverrideIds);
        Assert.NotNull(endpoint.OriginalProjection);
        Assert.Equal("v4", endpoint.OriginalProjection!.OpenApiDocumentName);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-public-document", candidate.AppliedOverrideId);
        Assert.Equal("public", candidate.ProjectedEndpoint.OpenApiDocumentName);
        Assert.Equal("v4", candidate.OriginalProjection.OpenApiDocumentName);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "prefer-public-document", StringComparison.Ordinal));
        Assert.Equal("public", rule.OpenApiDocumentName);

        Assert.Contains(snapshot.RestEndpointOverrides, item =>
            string.Equals(item.Id, "prefer-public-document", StringComparison.Ordinal) &&
            string.Equals(item.OpenApiDocumentName, "public", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            string.Equals(item.AppliedOverrideId, "prefer-public-document", StringComparison.Ordinal) &&
            string.Equals(item.ProjectedEndpoint.OpenApiDocumentName, "public", StringComparison.Ordinal) &&
            string.Equals(item.OriginalProjection.OpenApiDocumentName, "v4", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.Id, endpoint.Id, StringComparison.Ordinal) &&
            string.Equals(item.AppliedOverrideId, "prefer-public-document", StringComparison.Ordinal) &&
            item.MatchedOverrideIds.SequenceEqual(["prefer-public-document"]) &&
            string.Equals(item.OpenApiDocumentName, "public", StringComparison.Ordinal) &&
            item.OriginalProjection is not null &&
            string.Equals(item.OriginalProjection.OpenApiDocumentName, "v4", StringComparison.Ordinal));

        var routeEndpoint = Assert.Single(
            ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(static dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>(),
            static item => string.Equals(item.RoutePattern.RawText, "/api/v4/tests/generated/runtime/override/orders/{orderId}", StringComparison.Ordinal));
        Assert.Equal("public", routeEndpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>()?.EndpointGroupName);
        Assert.Equal("prefer-public-document", routeEndpoint.Metadata.GetMetadata<RestEndpointAppliedOverrideMetadata>()?.OverrideId);

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v4/tests/generated/runtime/override/orders/ord-42");
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonKeepsExplicitOpenApiDocumentNameWhenProfileSeededApiVersionOverridesChangeRouteVersion()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-v6:Behaviors:0"] = "tests.rest.profile.documentpinned.lookup";
        builder.Configuration["RestApi:Overrides:prefer-v6:ApiVersionMajor"] = "6";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileSeededDocumentVersionOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.documentpinned.lookup", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile/runtime/document-pinned/{orderId}", endpoint.RoutePattern);
        Assert.Equal("public", endpoint.OpenApiDocumentName);
        Assert.Equal(6, endpoint.ApiVersionMajor);
        Assert.Equal("prefer-v6", endpoint.AppliedOverrideId);
        Assert.Equal(["prefer-v6"], endpoint.MatchedOverrideIds);
        Assert.Equal([RestEndpointOverrideActionKind.ApiVersionMajor], endpoint.SelectedOverrideActionKinds);
        Assert.Equal([RestEndpointOverrideActionKind.ApiVersionMajor], endpoint.AppliedOverrideActionKinds);
        Assert.NotNull(endpoint.OriginalProjection);
        Assert.Equal(4, endpoint.OriginalProjection!.ApiVersionMajor);
        Assert.Equal("public", endpoint.OriginalProjection.OpenApiDocumentName);
        Assert.Equal("/api/v4/tests/profile/runtime/document-pinned/{orderId}", endpoint.OriginalProjection.RoutePattern);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.documentpinned.lookup", StringComparison.Ordinal));
        Assert.Equal("prefer-v6", candidate.AppliedOverrideId);
        Assert.Equal("public", candidate.ProjectedEndpoint.OpenApiDocumentName);
        Assert.Equal(4, candidate.OriginalProjection.ApiVersionMajor);
        Assert.Equal("public", candidate.OriginalProjection.OpenApiDocumentName);
        Assert.Equal([RestEndpointOverrideActionKind.ApiVersionMajor], candidate.SelectedOverrideActionKinds);
        Assert.Equal([RestEndpointOverrideActionKind.ApiVersionMajor], candidate.AppliedOverrideActionKinds);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "prefer-v6", StringComparison.Ordinal));
        Assert.Equal(6, rule.ApiVersionMajor);
        Assert.Null(rule.OpenApiDocumentName);
        Assert.Equal([RestEndpointOverrideActionKind.ApiVersionMajor], rule.ActionKinds);

        var routeEndpoint = Assert.Single(
            ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(static dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>(),
            static item => string.Equals(item.RoutePattern.RawText, "/api/v6/tests/profile/runtime/document-pinned/{orderId}", StringComparison.Ordinal));
        Assert.Equal("public", routeEndpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>()?.EndpointGroupName);
        Assert.Equal("prefer-v6", routeEndpoint.Metadata.GetMetadata<RestEndpointAppliedOverrideMetadata>()?.OverrideId);

        var payload = await client.GetFromJsonAsync<ProfileRuntimeOrderOutput>("/api/v6/tests/profile/runtime/document-pinned/ord-99");
        Assert.NotNull(payload);
        Assert.Equal("ord-99", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonAppliesEndpointMetadataOverridesAndExposesOverrideCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.Configuration["RestApi:Overrides:prefer-public-docs:Behaviors:0"] = "tests.generated.runtimeoverride.lookup";
        builder.Configuration["RestApi:Overrides:prefer-public-docs:EndpointName"] = "tests.generated.runtimeoverride.public.lookup";
        builder.Configuration["RestApi:Overrides:prefer-public-docs:Summary"] = "Gets a generated runtime order through host-governed endpoint metadata.";
        builder.Configuration["RestApi:Overrides:prefer-public-docs:Description"] = "Publishes shorthand endpoint metadata overrides into runtime catalogs and ASP.NET Core endpoint metadata.";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedVersionOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal("/api/v4/tests/generated/runtime/override/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal("tests.generated.runtimeoverride.public.lookup", endpoint.EndpointName);
        Assert.Equal(
            "Gets a generated runtime order through host-governed endpoint metadata.",
            endpoint.Summary);
        Assert.Equal(
            "Publishes shorthand endpoint metadata overrides into runtime catalogs and ASP.NET Core endpoint metadata.",
            endpoint.Description);
        Assert.Equal(
            "tests_rest_generated_runtime_override.v4.tests_generated_runtimeoverride_lookup",
            endpoint.OriginalEndpointName);
        Assert.Equal("tests.generated.runtimeoverride.lookup", endpoint.OriginalSummary);
        Assert.Equal(
            "Publishes generated shorthand so REST governance can override the effective API version.",
            endpoint.OriginalDescription);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-public-docs", candidate.AppliedOverrideId);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);
        Assert.Equal(["prefer-public-docs"], endpoint.MatchedOverrideIds);
        Assert.Equal(endpoint.EndpointName, candidate.ProjectedEndpoint.EndpointName);
        Assert.Equal(endpoint.Summary, candidate.ProjectedEndpoint.Summary);
        Assert.Equal(endpoint.Description, candidate.ProjectedEndpoint.Description);
        Assert.Equal(endpoint.OriginalEndpointName, candidate.ProjectedEndpoint.OriginalEndpointName);
        Assert.Equal(endpoint.OriginalSummary, candidate.ProjectedEndpoint.OriginalSummary);
        Assert.Equal(endpoint.OriginalDescription, candidate.ProjectedEndpoint.OriginalDescription);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "prefer-public-docs", StringComparison.Ordinal));
        Assert.Equal("tests.generated.runtimeoverride.public.lookup", rule.EndpointName);
        Assert.Equal(
            "Gets a generated runtime order through host-governed endpoint metadata.",
            rule.Summary);
        Assert.Equal(
            "Publishes shorthand endpoint metadata overrides into runtime catalogs and ASP.NET Core endpoint metadata.",
            rule.Description);

        Assert.Contains(snapshot.RestEndpointOverrides, item =>
            string.Equals(item.Id, "prefer-public-docs", StringComparison.Ordinal) &&
            string.Equals(item.EndpointName, "tests.generated.runtimeoverride.public.lookup", StringComparison.Ordinal) &&
            string.Equals(item.Summary, "Gets a generated runtime order through host-governed endpoint metadata.", StringComparison.Ordinal) &&
            string.Equals(item.Description, "Publishes shorthand endpoint metadata overrides into runtime catalogs and ASP.NET Core endpoint metadata.", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            string.Equals(item.AppliedOverrideId, "prefer-public-docs", StringComparison.Ordinal) &&
            string.Equals(item.ProjectedEndpoint.EndpointName, "tests.generated.runtimeoverride.public.lookup", StringComparison.Ordinal) &&
            string.Equals(item.ProjectedEndpoint.OriginalEndpointName, "tests_rest_generated_runtime_override.v4.tests_generated_runtimeoverride_lookup", StringComparison.Ordinal) &&
            string.Equals(item.ProjectedEndpoint.Summary, "Gets a generated runtime order through host-governed endpoint metadata.", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.Id, endpoint.Id, StringComparison.Ordinal) &&
            string.Equals(item.OriginalEndpointName, "tests_rest_generated_runtime_override.v4.tests_generated_runtimeoverride_lookup", StringComparison.Ordinal) &&
            string.Equals(item.OriginalSummary, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal) &&
            string.Equals(item.OriginalDescription, "Publishes generated shorthand so REST governance can override the effective API version.", StringComparison.Ordinal) &&
            item.MatchedOverrideIds.SequenceEqual(["prefer-public-docs"]));

        var routeEndpoint = Assert.Single(
            ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(static dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>(),
            static item => string.Equals(item.RoutePattern.RawText, "/api/v4/tests/generated/runtime/override/orders/{orderId}", StringComparison.Ordinal));
        Assert.Equal(
            "tests.generated.runtimeoverride.public.lookup",
            routeEndpoint.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName);
        Assert.Equal(
            "Gets a generated runtime order through host-governed endpoint metadata.",
            routeEndpoint.Metadata.OfType<IEndpointSummaryMetadata>().LastOrDefault()?.Summary);
        Assert.Equal(
            "Publishes shorthand endpoint metadata overrides into runtime catalogs and ASP.NET Core endpoint metadata.",
            routeEndpoint.Metadata.OfType<IEndpointDescriptionMetadata>().LastOrDefault()?.Description);

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v4/tests/generated/runtime/override/orders/ord-42");
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonAppliesEndpointMetadataClearOverridesAndExposesOverrideCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.Configuration["RestApi:Overrides:clear-public-docs:Behaviors:0"] = "tests.generated.runtimeoverride.lookup";
        builder.Configuration["RestApi:Overrides:clear-public-docs:ClearEndpointName"] = "true";
        builder.Configuration["RestApi:Overrides:clear-public-docs:ClearSummary"] = "true";
        builder.Configuration["RestApi:Overrides:clear-public-docs:ClearDescription"] = "true";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedVersionOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal("/api/v4/tests/generated/runtime/override/orders/{orderId}", endpoint.RoutePattern);
        Assert.Null(endpoint.EndpointName);
        Assert.Null(endpoint.Summary);
        Assert.Null(endpoint.Description);
        Assert.Equal(
            "tests_rest_generated_runtime_override.v4.tests_generated_runtimeoverride_lookup",
            endpoint.OriginalEndpointName);
        Assert.Equal("tests.generated.runtimeoverride.lookup", endpoint.OriginalSummary);
        Assert.Equal(
            "Publishes generated shorthand so REST governance can override the effective API version.",
            endpoint.OriginalDescription);
        Assert.Equal("clear-public-docs", endpoint.AppliedOverrideId);
        Assert.Equal(["clear-public-docs"], endpoint.MatchedOverrideIds);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("clear-public-docs", candidate.AppliedOverrideId);
        Assert.Null(candidate.ProjectedEndpoint.EndpointName);
        Assert.Null(candidate.ProjectedEndpoint.Summary);
        Assert.Null(candidate.ProjectedEndpoint.Description);
        Assert.Equal(endpoint.OriginalEndpointName, candidate.ProjectedEndpoint.OriginalEndpointName);
        Assert.Equal(endpoint.OriginalSummary, candidate.ProjectedEndpoint.OriginalSummary);
        Assert.Equal(endpoint.OriginalDescription, candidate.ProjectedEndpoint.OriginalDescription);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "clear-public-docs", StringComparison.Ordinal));
        Assert.True(rule.ClearEndpointName);
        Assert.True(rule.ClearSummary);
        Assert.True(rule.ClearDescription);
        Assert.Null(rule.EndpointName);
        Assert.Null(rule.Summary);
        Assert.Null(rule.Description);

        Assert.Contains(snapshot.RestEndpointOverrides, item =>
            string.Equals(item.Id, "clear-public-docs", StringComparison.Ordinal) &&
            item.ClearEndpointName &&
            item.ClearSummary &&
            item.ClearDescription);
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            string.Equals(item.AppliedOverrideId, "clear-public-docs", StringComparison.Ordinal) &&
            item.ProjectedEndpoint.EndpointName is null &&
            item.ProjectedEndpoint.Summary is null &&
            item.ProjectedEndpoint.Description is null);
        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.Id, endpoint.Id, StringComparison.Ordinal) &&
            item.EndpointName is null &&
            item.Summary is null &&
            item.Description is null &&
            string.Equals(item.OriginalEndpointName, endpoint.OriginalEndpointName, StringComparison.Ordinal) &&
            item.MatchedOverrideIds.SequenceEqual(["clear-public-docs"]));

        var routeEndpoint = Assert.Single(
            ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(static dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>(),
            static item => string.Equals(item.RoutePattern.RawText, "/api/v4/tests/generated/runtime/override/orders/{orderId}", StringComparison.Ordinal));
        Assert.Null(routeEndpoint.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName);
        Assert.Null(routeEndpoint.Metadata.OfType<IEndpointSummaryMetadata>().LastOrDefault()?.Summary);
        Assert.Null(routeEndpoint.Metadata.OfType<IEndpointDescriptionMetadata>().LastOrDefault()?.Description);

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v4/tests/generated/runtime/override/orders/ord-42");
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonDoesNotExposeAppliedOverrideIdForNoOpEndpointMetadataRewriteOnPublishedCandidate()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.Configuration["RestApi:Overrides:prefer-module-docs-noop:Behaviors:0"] = "tests.profile.runtimenoop.metadata";
        builder.Configuration["RestApi:Overrides:prefer-module-docs-noop:EndpointName"] = "tests.profile.runtimeoverride.noop.metadata.lookup";
        builder.Configuration["RestApi:Overrides:prefer-module-docs-noop:Summary"] = "Gets a profile-backed runtime order through explicit module metadata.";
        builder.Configuration["RestApi:Overrides:prefer-module-docs-noop:Description"] = "Publishes explicit module metadata so a matching host rule becomes a runtime no-op.";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileMetadataRewriteNoOpRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var groups = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor[]>("/engine/rest-endpoint-publication-groups");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");
        var groupsJson = await client.GetStringAsync("/engine/rest-endpoint-publication-groups");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(groups);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.profile.runtimenoop.metadata", StringComparison.Ordinal));
        Assert.Equal("/api/v4/tests/profile/runtime/noop/metadata/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal("tests.profile.runtimeoverride.noop.metadata.lookup", endpoint.EndpointName);
        Assert.Equal(
            "Gets a profile-backed runtime order through explicit module metadata.",
            endpoint.Summary);
        Assert.Equal(
            "Publishes explicit module metadata so a matching host rule becomes a runtime no-op.",
            endpoint.Description);
        Assert.Equal(
            "tests_rest_profile_runtime_noop_metadata.v4.tests_profile_runtimenoop_metadata",
            endpoint.OriginalEndpointName);
        Assert.Equal("tests.profile.runtimenoop.metadata", endpoint.OriginalSummary);
        Assert.Equal(
            "Publishes a profile-backed route whose explicit module metadata already matches the host metadata rule.",
            endpoint.OriginalDescription);
        Assert.Null(endpoint.AppliedOverrideId);
        Assert.Equal("prefer-module-docs-noop", endpoint.SelectedOverrideId);
        Assert.Contains("prefer-module-docs-noop", endpoint.MatchedOverrideIds);
        Assert.Equal(
            [
                RestEndpointOverrideActionKind.EndpointName,
                RestEndpointOverrideActionKind.Summary,
                RestEndpointOverrideActionKind.Description
            ],
            endpoint.SelectedOverrideActionKinds);
        Assert.Empty(endpoint.AppliedOverrideActionKinds);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.profile.runtimenoop.metadata", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Null(candidate.AppliedOverrideId);
        Assert.Equal("prefer-module-docs-noop", candidate.SelectedOverrideId);
        Assert.Equal(endpoint.EndpointName, candidate.ProjectedEndpoint.EndpointName);
        Assert.Equal(endpoint.Summary, candidate.ProjectedEndpoint.Summary);
        Assert.Equal(endpoint.Description, candidate.ProjectedEndpoint.Description);
        Assert.Equal(endpoint.OriginalEndpointName, candidate.ProjectedEndpoint.OriginalEndpointName);
        Assert.Equal(endpoint.OriginalSummary, candidate.ProjectedEndpoint.OriginalSummary);
        Assert.Equal(endpoint.OriginalDescription, candidate.ProjectedEndpoint.OriginalDescription);
        Assert.Contains("prefer-module-docs-noop", candidate.MatchedOverrideIds);
        Assert.Equal(
            [
                RestEndpointOverrideActionKind.EndpointName,
                RestEndpointOverrideActionKind.Summary,
                RestEndpointOverrideActionKind.Description
            ],
            candidate.SelectedOverrideActionKinds);
        Assert.Empty(candidate.AppliedOverrideActionKinds);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "prefer-module-docs-noop", StringComparison.Ordinal));
        Assert.Equal("tests.profile.runtimeoverride.noop.metadata.lookup", rule.EndpointName);
        Assert.Equal(
            "Gets a profile-backed runtime order through explicit module metadata.",
            rule.Summary);
        Assert.Equal(
            "Publishes explicit module metadata so a matching host rule becomes a runtime no-op.",
            rule.Description);
        Assert.Equal(
            [
                RestEndpointOverrideActionKind.EndpointName,
                RestEndpointOverrideActionKind.Summary,
                RestEndpointOverrideActionKind.Description
            ],
            rule.ActionKinds);
        Assert.Equal([candidate.Id], rule.MatchedCandidateIds);
        Assert.Equal([candidate.Id], rule.SelectedCandidateIds);
        Assert.Empty(rule.AppliedCandidateIds);
        Assert.Empty(rule.SkippedCandidateIds);
        Assert.Equal([candidate.OverrideSelectionBasis!.Value], rule.SelectionBases);
        var ruleSelectionBasisSummary = Assert.Single(rule.SelectionBasisSummaries);
        Assert.Equal(candidate.OverrideSelectionBasis.Value, ruleSelectionBasisSummary.SelectionBasis);
        Assert.Equal([candidate.Id], ruleSelectionBasisSummary.CandidateIds);
        Assert.Equal(candidate.SelectedOverrideActionKinds, rule.SelectedActionKinds);
        Assert.Equal(
            candidate.SelectedOverrideActionKinds,
            rule.SelectedActionKindSummaries.Select(static item => item.ActionKind).ToArray());
        Assert.All(
            rule.SelectedActionKindSummaries,
            summary => Assert.Equal([candidate.Id], summary.CandidateIds));
        Assert.Empty(rule.AppliedActionKinds);
        Assert.Empty(rule.AppliedActionKindSummaries);

        var group = Assert.Single(groups, static item =>
            string.Equals(item.BehaviorId, "tests.profile.runtimenoop.metadata", StringComparison.Ordinal));
        Assert.Single(group.GovernanceOverrideSummaries);
        var overrideSummary = Assert.Single(group.GovernanceOverrideSummaries, static item =>
            string.Equals(item.RuleId, "prefer-module-docs-noop", StringComparison.Ordinal));
        Assert.Equal([candidate.Id], overrideSummary.MatchedCandidateIds);
        Assert.Equal([candidate.Id], overrideSummary.SelectedCandidateIds);
        Assert.Empty(overrideSummary.AppliedCandidateIds);
        Assert.NotNull(candidate.OverrideSelectionBasis);
        var overrideSelectionBasisSummary = Assert.Single(overrideSummary.SelectionBasisSummaries);
        Assert.Equal(candidate.OverrideSelectionBasis.Value, overrideSelectionBasisSummary.SelectionBasis);
        Assert.Equal([candidate.Id], overrideSelectionBasisSummary.CandidateIds);
        Assert.Equal(candidate.SelectedOverrideActionKinds, overrideSummary.SelectedActionKindSummaries.Select(static item => item.ActionKind).ToArray());
        Assert.All(
            overrideSummary.SelectedActionKindSummaries,
            summary => Assert.Equal([candidate.Id], summary.CandidateIds));
        Assert.Empty(overrideSummary.AppliedActionKindSummaries);
        var profileStyle = Assert.Single(group.AuthoringStyleSummaries);
        var profileOverrideSummary = Assert.Single(profileStyle.GovernanceOverrideSummaries);
        Assert.Equal("prefer-module-docs-noop", profileOverrideSummary.RuleId);
        Assert.Equal([candidate.Id], profileOverrideSummary.MatchedCandidateIds);
        Assert.Equal([candidate.Id], profileOverrideSummary.SelectedCandidateIds);
        Assert.Empty(profileOverrideSummary.AppliedCandidateIds);
        Assert.Equal(candidate.OverrideSelectionBasis.Value, Assert.Single(profileOverrideSummary.SelectionBasisSummaries).SelectionBasis);
        Assert.Equal(candidate.SelectedOverrideActionKinds, profileOverrideSummary.SelectedActionKindSummaries.Select(static item => item.ActionKind).ToArray());
        Assert.Empty(profileOverrideSummary.AppliedActionKindSummaries);

        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            item.AppliedOverrideId is null &&
            string.Equals(item.SelectedOverrideId, "prefer-module-docs-noop", StringComparison.Ordinal) &&
            string.Equals(item.ProjectedEndpoint.EndpointName, "tests.profile.runtimeoverride.noop.metadata.lookup", StringComparison.Ordinal) &&
            item.MatchedOverrideIds.Contains("prefer-module-docs-noop"));
        Assert.Contains(snapshot.RestEndpointPublicationGroups, item =>
            string.Equals(item.BehaviorId, "tests.profile.runtimenoop.metadata", StringComparison.Ordinal) &&
            item.GovernanceOverrideSummaries.Count == 1 &&
            string.Equals(item.GovernanceOverrideSummaries[0].RuleId, "prefer-module-docs-noop", StringComparison.Ordinal) &&
            item.GovernanceOverrideSummaries[0].AppliedCandidateIds.Count == 0 &&
            item.GovernanceOverrideSummaries[0].SelectionBasisSummaries.Count == 1 &&
            item.GovernanceOverrideSummaries[0].SelectedActionKindSummaries.Count == candidate.SelectedOverrideActionKinds.Count &&
            item.GovernanceOverrideSummaries[0].AppliedActionKindSummaries.Count == 0);
        Assert.Contains(snapshot.RestEndpointOverrides, item =>
            string.Equals(item.Id, "prefer-module-docs-noop", StringComparison.Ordinal) &&
            item.MatchedCandidateIds.Count == 1 &&
            string.Equals(item.MatchedCandidateIds[0], candidate.Id, StringComparison.Ordinal) &&
            item.SelectedCandidateIds.Count == 1 &&
            string.Equals(item.SelectedCandidateIds[0], candidate.Id, StringComparison.Ordinal) &&
            item.AppliedCandidateIds.Count == 0 &&
            item.SkippedCandidateIds.Count == 0 &&
            item.SelectionBases.Count == 1 &&
            item.SelectionBases[0] == candidate.OverrideSelectionBasis &&
            item.SelectedActionKinds.Count == candidate.SelectedOverrideActionKinds.Count &&
            item.AppliedActionKinds.Count == 0);
        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.Id, endpoint.Id, StringComparison.Ordinal) &&
            item.AppliedOverrideId is null &&
            string.Equals(item.SelectedOverrideId, "prefer-module-docs-noop", StringComparison.Ordinal) &&
            string.Equals(item.EndpointName, "tests.profile.runtimeoverride.noop.metadata.lookup", StringComparison.Ordinal) &&
            item.MatchedOverrideIds.Contains("prefer-module-docs-noop"));
        Assert.Contains("\"ruleId\":\"prefer-module-docs-noop\"", groupsJson, StringComparison.Ordinal);
        Assert.Contains("\"selectionBasisSummaries\":", groupsJson, StringComparison.Ordinal);
        Assert.Contains("\"selectedActionKindSummaries\":", groupsJson, StringComparison.Ordinal);
        Assert.Contains("\"appliedActionKindSummaries\":[]", groupsJson, StringComparison.Ordinal);

        var routeEndpoint = Assert.Single(
            ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(static dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>(),
            static item => string.Equals(item.RoutePattern.RawText, "/api/v4/tests/profile/runtime/noop/metadata/orders/{orderId}", StringComparison.Ordinal));
        Assert.Equal(
            "tests.profile.runtimeoverride.noop.metadata.lookup",
            routeEndpoint.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName);
        Assert.Equal(
            "Gets a profile-backed runtime order through explicit module metadata.",
            routeEndpoint.Metadata.OfType<IEndpointSummaryMetadata>().LastOrDefault()?.Summary);
        Assert.Equal(
            "Publishes explicit module metadata so a matching host rule becomes a runtime no-op.",
            routeEndpoint.Metadata.OfType<IEndpointDescriptionMetadata>().LastOrDefault()?.Description);
        Assert.Null(routeEndpoint.Metadata.GetMetadata<RestEndpointAppliedOverrideMetadata>()?.OverrideId);

        var response = await client.GetAsync("/api/v4/tests/profile/runtime/noop/metadata/orders/ord-42");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<GeneratedRuntimeOrderOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonDoesNotExposeAppliedOverrideIdForNoOpEndpointMetadataClearOnPublishedEndpoint()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.Configuration["RestApi:Overrides:clear-module-description-noop:Behaviors:0"] = "tests.profile.runtimenoop.clear.metadata";
        builder.Configuration["RestApi:Overrides:clear-module-description-noop:ClearDescription"] = "true";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileMetadataClearNoOpRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.profile.runtimenoop.clear.metadata", StringComparison.Ordinal));
        Assert.Equal("/api/v4/tests/profile/runtime/noop/clear/metadata/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal(
            "tests_rest_profile_runtime_noop_clear_metadata.v4.tests_profile_runtimenoop_clear_metadata",
            endpoint.EndpointName);
        Assert.Equal("tests.profile.runtimenoop.clear.metadata", endpoint.Summary);
        Assert.Null(endpoint.Description);
        Assert.Equal(
            "Publishes a profile-backed route whose explicit module configuration already clears description metadata before host governance runs.",
            endpoint.OriginalDescription);
        Assert.Null(endpoint.AppliedOverrideId);
        Assert.Equal("clear-module-description-noop", endpoint.SelectedOverrideId);
        Assert.Contains("clear-module-description-noop", endpoint.MatchedOverrideIds);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.profile.runtimenoop.clear.metadata", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Null(candidate.AppliedOverrideId);
        Assert.Equal("clear-module-description-noop", candidate.SelectedOverrideId);
        Assert.Equal(endpoint.EndpointName, candidate.ProjectedEndpoint.EndpointName);
        Assert.Equal(endpoint.Summary, candidate.ProjectedEndpoint.Summary);
        Assert.Null(candidate.ProjectedEndpoint.Description);
        Assert.Contains("clear-module-description-noop", candidate.MatchedOverrideIds);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "clear-module-description-noop", StringComparison.Ordinal));
        Assert.True(rule.ClearDescription);
        Assert.Null(rule.Description);

        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            item.AppliedOverrideId is null &&
            string.Equals(item.SelectedOverrideId, "clear-module-description-noop", StringComparison.Ordinal) &&
            item.ProjectedEndpoint.Description is null &&
            item.MatchedOverrideIds.Contains("clear-module-description-noop"));
        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.Id, endpoint.Id, StringComparison.Ordinal) &&
            item.AppliedOverrideId is null &&
            string.Equals(item.SelectedOverrideId, "clear-module-description-noop", StringComparison.Ordinal) &&
            item.Description is null &&
            item.MatchedOverrideIds.Contains("clear-module-description-noop"));

        var routeEndpoint = Assert.Single(
            ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(static dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>(),
            static item => string.Equals(item.RoutePattern.RawText, "/api/v4/tests/profile/runtime/noop/clear/metadata/orders/{orderId}", StringComparison.Ordinal));
        Assert.Equal(
            "tests_rest_profile_runtime_noop_clear_metadata.v4.tests_profile_runtimenoop_clear_metadata",
            routeEndpoint.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName);
        Assert.Equal(
            "tests.profile.runtimenoop.clear.metadata",
            routeEndpoint.Metadata.OfType<IEndpointSummaryMetadata>().LastOrDefault()?.Summary);
        Assert.Null(routeEndpoint.Metadata.OfType<IEndpointDescriptionMetadata>().LastOrDefault()?.Description);
        Assert.Null(routeEndpoint.Metadata.GetMetadata<RestEndpointAppliedOverrideMetadata>()?.OverrideId);

        var response = await client.GetAsync("/api/v4/tests/profile/runtime/noop/clear/metadata/orders/ord-42");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<GeneratedRuntimeOrderOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonAppliesRequiredCapabilityOverridesAndExposesOverrideCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.Configuration["Engine:Trust:Capabilities:restricted.original"] = "Denied";
        builder.Configuration["Engine:Trust:Capabilities:restricted.override"] = "Allowed";
        builder.Configuration["RestApi:Overrides:prefer-public-capability:Behaviors:0"] = "tests.profile.runtimeoverride.capability";
        builder.Configuration["RestApi:Overrides:prefer-public-capability:RequiredCapabilityKey"] = "restricted.override";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileCapabilityOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.profile.runtimeoverride.capability", StringComparison.Ordinal));
        Assert.Equal("/api/v4/tests/profile/runtime/override/capability/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal("restricted.override", endpoint.RequiredCapabilityKey);
        Assert.Equal("restricted.original", endpoint.OriginalRequiredCapabilityKey);
        Assert.Equal("prefer-public-capability", endpoint.AppliedOverrideId);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.profile.runtimeoverride.capability", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-public-capability", candidate.AppliedOverrideId);
        Assert.Equal("restricted.override", candidate.ProjectedEndpoint.RequiredCapabilityKey);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "prefer-public-capability", StringComparison.Ordinal));
        Assert.Equal("restricted.override", rule.RequiredCapabilityKey);

        Assert.Contains(snapshot.RestEndpointOverrides, item =>
            string.Equals(item.Id, "prefer-public-capability", StringComparison.Ordinal) &&
            string.Equals(item.RequiredCapabilityKey, "restricted.override", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            string.Equals(item.AppliedOverrideId, "prefer-public-capability", StringComparison.Ordinal) &&
            string.Equals(item.ProjectedEndpoint.RequiredCapabilityKey, "restricted.override", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.Id, endpoint.Id, StringComparison.Ordinal) &&
            string.Equals(item.RequiredCapabilityKey, "restricted.override", StringComparison.Ordinal) &&
            string.Equals(item.OriginalRequiredCapabilityKey, "restricted.original", StringComparison.Ordinal) &&
            string.Equals(item.AppliedOverrideId, "prefer-public-capability", StringComparison.Ordinal));

        var routeEndpoint = Assert.Single(
            ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(static dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>(),
            static item => string.Equals(item.RoutePattern.RawText, "/api/v4/tests/profile/runtime/override/capability/orders/{orderId}", StringComparison.Ordinal));
        Assert.Equal(
            "restricted.override",
            routeEndpoint.Metadata.OfType<RestEndpointCapabilityMetadata>().LastOrDefault()?.CapabilityKey);

        var response = await client.GetAsync("/api/v4/tests/profile/runtime/override/capability/orders/ord-42");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<GeneratedRuntimeOrderOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonClearsRequiredCapabilityOverridesAndExposesOverrideCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.Configuration["Engine:Trust:Capabilities:restricted.original"] = "Denied";
        builder.Configuration["RestApi:Overrides:clear-public-capability:Behaviors:0"] = "tests.profile.runtimeclear.capability";
        builder.Configuration["RestApi:Overrides:clear-public-capability:ClearRequiredCapability"] = "true";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileCapabilityClearRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.profile.runtimeclear.capability", StringComparison.Ordinal));
        Assert.Equal("/api/v4/tests/profile/runtime/clear/capability/orders/{orderId}", endpoint.RoutePattern);
        Assert.Null(endpoint.RequiredCapabilityKey);
        Assert.Equal("restricted.original", endpoint.OriginalRequiredCapabilityKey);
        Assert.Equal("clear-public-capability", endpoint.AppliedOverrideId);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.profile.runtimeclear.capability", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("clear-public-capability", candidate.AppliedOverrideId);
        Assert.Null(candidate.ProjectedEndpoint.RequiredCapabilityKey);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "clear-public-capability", StringComparison.Ordinal));
        Assert.True(rule.ClearRequiredCapability);
        Assert.Null(rule.RequiredCapabilityKey);

        Assert.Contains(snapshot.RestEndpointOverrides, item =>
            string.Equals(item.Id, "clear-public-capability", StringComparison.Ordinal) &&
            item.ClearRequiredCapability &&
            item.RequiredCapabilityKey is null);
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            string.Equals(item.AppliedOverrideId, "clear-public-capability", StringComparison.Ordinal) &&
            item.ProjectedEndpoint.RequiredCapabilityKey is null);
        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.Id, endpoint.Id, StringComparison.Ordinal) &&
            item.RequiredCapabilityKey is null &&
            string.Equals(item.OriginalRequiredCapabilityKey, "restricted.original", StringComparison.Ordinal) &&
            string.Equals(item.AppliedOverrideId, "clear-public-capability", StringComparison.Ordinal));

        var routeEndpoint = Assert.Single(
            ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(static dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>(),
            static item => string.Equals(item.RoutePattern.RawText, "/api/v4/tests/profile/runtime/clear/capability/orders/{orderId}", StringComparison.Ordinal));
        var capabilityMetadata = routeEndpoint.Metadata.OfType<RestEndpointCapabilityMetadata>().LastOrDefault();
        Assert.NotNull(capabilityMetadata);
        Assert.True(capabilityMetadata.ClearsExisting);
        Assert.Null(capabilityMetadata.CapabilityKey);

        var response = await client.GetAsync("/api/v4/tests/profile/runtime/clear/capability/orders/ord-42");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<GeneratedRuntimeOrderOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonDoesNotExposeAppliedOverrideIdForNoOpCapabilityClearOnPublishedEndpoint()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.Configuration["RestApi:Overrides:clear-public-capability-noop:Behaviors:0"] = "tests.profile.runtimeclear.noop.capability";
        builder.Configuration["RestApi:Overrides:clear-public-capability-noop:ClearRequiredCapability"] = "true";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileCapabilityClearNoOpRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.profile.runtimeclear.noop.capability", StringComparison.Ordinal));
        Assert.Equal("/api/v4/tests/profile/runtime/clear/noop/capability/orders/{orderId}", endpoint.RoutePattern);
        Assert.Null(endpoint.RequiredCapabilityKey);
        Assert.Null(endpoint.OriginalRequiredCapabilityKey);
        Assert.Null(endpoint.AppliedOverrideId);
        Assert.Equal("clear-public-capability-noop", endpoint.SelectedOverrideId);
        Assert.Contains("clear-public-capability-noop", endpoint.MatchedOverrideIds);

        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.Id, endpoint.Id, StringComparison.Ordinal) &&
            item.RequiredCapabilityKey is null &&
            item.OriginalRequiredCapabilityKey is null &&
            item.AppliedOverrideId is null &&
            string.Equals(item.SelectedOverrideId, "clear-public-capability-noop", StringComparison.Ordinal) &&
            item.MatchedOverrideIds.Contains("clear-public-capability-noop"));

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.profile.runtimeclear.noop.capability", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Null(candidate.AppliedOverrideId);
        Assert.Equal("clear-public-capability-noop", candidate.SelectedOverrideId);
        Assert.Null(candidate.ProjectedEndpoint.RequiredCapabilityKey);
        Assert.Contains("clear-public-capability-noop", candidate.MatchedOverrideIds);

        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            item.ProjectedEndpoint.RequiredCapabilityKey is null &&
            item.AppliedOverrideId is null &&
            string.Equals(item.SelectedOverrideId, "clear-public-capability-noop", StringComparison.Ordinal));

        var routeEndpoint = Assert.Single(
            ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(static dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>(),
            static item => string.Equals(item.RoutePattern.RawText, "/api/v4/tests/profile/runtime/clear/noop/capability/orders/{orderId}", StringComparison.Ordinal));
        Assert.Null(routeEndpoint.Metadata.GetMetadata<RestEndpointSourceCapabilityMetadata>()?.RequiredCapabilityKey);
        Assert.Null(routeEndpoint.Metadata.GetMetadata<RestEndpointAppliedOverrideMetadata>()?.OverrideId);

        var response = await client.GetAsync("/api/v4/tests/profile/runtime/clear/noop/capability/orders/ord-42");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<GeneratedRuntimeOrderOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonDoesNotExposeAppliedOverrideIdForNoOpCapabilityRewriteOnPublishedCandidate()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.Configuration["Engine:Trust:Capabilities:restricted.original"] = "Allowed";
        builder.Configuration["RestApi:Overrides:prefer-public-capability-same-key:Behaviors:0"] = "tests.profile.runtimeoverride.capability";
        builder.Configuration["RestApi:Overrides:prefer-public-capability-same-key:RequiredCapabilityKey"] = "restricted.original";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileCapabilityOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.profile.runtimeoverride.capability", StringComparison.Ordinal));
        Assert.Equal("/api/v4/tests/profile/runtime/override/capability/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal("restricted.original", endpoint.RequiredCapabilityKey);
        Assert.Equal("restricted.original", endpoint.OriginalRequiredCapabilityKey);
        Assert.Null(endpoint.AppliedOverrideId);
        Assert.Equal("prefer-public-capability-same-key", endpoint.SelectedOverrideId);
        Assert.Contains("prefer-public-capability-same-key", endpoint.MatchedOverrideIds);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.profile.runtimeoverride.capability", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Null(candidate.AppliedOverrideId);
        Assert.Equal("prefer-public-capability-same-key", candidate.SelectedOverrideId);
        Assert.Equal("restricted.original", candidate.ProjectedEndpoint.RequiredCapabilityKey);
        Assert.Contains("prefer-public-capability-same-key", candidate.MatchedOverrideIds);

        var rule = Assert.Single(overrides, static item =>
            string.Equals(item.Id, "prefer-public-capability-same-key", StringComparison.Ordinal));
        Assert.Equal("restricted.original", rule.RequiredCapabilityKey);

        Assert.Contains(snapshot.RestEndpointOverrides, item =>
            string.Equals(item.Id, "prefer-public-capability-same-key", StringComparison.Ordinal) &&
            string.Equals(item.RequiredCapabilityKey, "restricted.original", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            string.Equals(item.ProjectedEndpoint.RequiredCapabilityKey, "restricted.original", StringComparison.Ordinal) &&
            item.AppliedOverrideId is null &&
            string.Equals(item.SelectedOverrideId, "prefer-public-capability-same-key", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.Id, endpoint.Id, StringComparison.Ordinal) &&
            string.Equals(item.RequiredCapabilityKey, "restricted.original", StringComparison.Ordinal) &&
            string.Equals(item.OriginalRequiredCapabilityKey, "restricted.original", StringComparison.Ordinal) &&
            item.AppliedOverrideId is null &&
            string.Equals(item.SelectedOverrideId, "prefer-public-capability-same-key", StringComparison.Ordinal) &&
            item.MatchedOverrideIds.Contains("prefer-public-capability-same-key"));

        var routeEndpoint = Assert.Single(
            ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(static dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>(),
            static item => string.Equals(item.RoutePattern.RawText, "/api/v4/tests/profile/runtime/override/capability/orders/{orderId}", StringComparison.Ordinal));
        Assert.Equal(
            "restricted.original",
            routeEndpoint.Metadata.GetMetadata<RestEndpointSourceCapabilityMetadata>()?.RequiredCapabilityKey);
        Assert.Equal(
            "restricted.original",
            routeEndpoint.Metadata.OfType<RestEndpointCapabilityMetadata>().LastOrDefault()?.CapabilityKey);
        Assert.Null(routeEndpoint.Metadata.GetMetadata<RestEndpointAppliedOverrideMetadata>()?.OverrideId);

        var response = await client.GetAsync("/api/v4/tests/profile/runtime/override/capability/orders/ord-42");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<GeneratedRuntimeOrderOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonAppliesRestPatternOverridesAndExposesOverrideCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.Configuration["RestApi:Overrides:prefer-lookup-path:Behaviors:0"] = "tests.generated.runtimeoverride.lookup";
        builder.Configuration["RestApi:Overrides:prefer-lookup-path:Pattern"] = "/lookup/{orderId}";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedVersionOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal("GET", endpoint.Method);
        Assert.Equal("/api/v4/tests/generated/runtime/override/lookup/{orderId}", endpoint.RoutePattern);
        Assert.Equal("/lookup/{orderId}", endpoint.RelativePattern);
        Assert.Equal("v4", endpoint.OpenApiDocumentName);
        Assert.Equal(4, endpoint.ApiVersionMajor);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-lookup-path", candidate.AppliedOverrideId);
        Assert.Equal("/lookup/{orderId}", candidate.ProjectedEndpoint.RelativePattern);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "prefer-lookup-path", StringComparison.Ordinal));
        Assert.Equal("/lookup/{orderId}", rule.Pattern);
        Assert.Null(rule.ApiVersionMajor);
        Assert.Null(rule.Method);

        Assert.Contains(snapshot.RestEndpointOverrides, item =>
            string.Equals(item.Id, "prefer-lookup-path", StringComparison.Ordinal) &&
            string.Equals(item.Pattern, "/lookup/{orderId}", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            string.Equals(item.AppliedOverrideId, "prefer-lookup-path", StringComparison.Ordinal));

        var oldResponse = await client.GetAsync("/api/v4/tests/generated/runtime/override/orders/ord-42");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, oldResponse.StatusCode);

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v4/tests/generated/runtime/override/lookup/ord-42");
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonAppliesRestRouteGroupPrefixOverridesAndExposesOverrideCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.Configuration["RestApi:Overrides:prefer-remapped-group:Behaviors:0"] = "tests.generated.runtimeoverride.lookup";
        builder.Configuration["RestApi:Overrides:prefer-remapped-group:RouteGroupPrefix"] = "/api/v4/tests/generated/runtime/override/remapped";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedVersionOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal("GET", endpoint.Method);
        Assert.Equal("/api/v4/tests/generated/runtime/override/remapped/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, endpoint.AuthoringStyle);
        Assert.Equal("/api/v4/tests/generated/runtime/override/remapped", endpoint.RouteGroupPrefix);
        Assert.Equal("/orders/{orderId}", endpoint.RelativePattern);
        Assert.Equal("v4", endpoint.OpenApiDocumentName);
        Assert.Equal(4, endpoint.ApiVersionMajor);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal(candidate.Id, endpoint.CandidateId);
        Assert.Equal("prefer-remapped-group", candidate.AppliedOverrideId);
        Assert.Equal("/api/v4/tests/generated/runtime/override", candidate.OriginalProjection.RouteGroupPrefix);
        Assert.Equal("/api/v4/tests/generated/runtime/override/orders/{orderId}", candidate.OriginalProjection.RoutePattern);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);
        Assert.Equal(endpoint.AuthoringStyle, candidate.ProjectedEndpoint.AuthoringStyle);
        Assert.Equal(endpoint.RouteGroupPrefix, candidate.ProjectedEndpoint.RouteGroupPrefix);
        Assert.Equal(endpoint.RelativePattern, candidate.ProjectedEndpoint.RelativePattern);
        Assert.Equal(candidate.Id, candidate.ProjectedEndpoint.CandidateId);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "prefer-remapped-group", StringComparison.Ordinal));
        Assert.Equal("/api/v4/tests/generated/runtime/override/remapped", rule.RouteGroupPrefix);
        Assert.Null(rule.ApiVersionMajor);
        Assert.Null(rule.Method);
        Assert.Null(rule.Pattern);

        Assert.Contains(snapshot.RestEndpointOverrides, item =>
            string.Equals(item.Id, "prefer-remapped-group", StringComparison.Ordinal) &&
            string.Equals(item.RouteGroupPrefix, "/api/v4/tests/generated/runtime/override/remapped", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            string.Equals(item.AppliedOverrideId, "prefer-remapped-group", StringComparison.Ordinal) &&
            string.Equals(item.OriginalProjection.RouteGroupPrefix, "/api/v4/tests/generated/runtime/override", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.Id, endpoint.Id, StringComparison.Ordinal) &&
            string.Equals(item.AuthoringStyle, endpoint.AuthoringStyle, StringComparison.Ordinal) &&
            string.Equals(item.RouteGroupPrefix, endpoint.RouteGroupPrefix, StringComparison.Ordinal) &&
            string.Equals(item.RelativePattern, endpoint.RelativePattern, StringComparison.Ordinal) &&
            string.Equals(item.CandidateId, candidate.Id, StringComparison.Ordinal));

        var oldResponse = await client.GetAsync("/api/v4/tests/generated/runtime/override/orders/ord-42");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, oldResponse.StatusCode);

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v4/tests/generated/runtime/override/remapped/orders/ord-42");
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonAppliesRestRouteGroupPrefixOverridesAfterApiVersionRewrite()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-v6-remapped-group:Behaviors:0"] = "tests.generated.runtimeoverride.lookup";
        builder.Configuration["RestApi:Overrides:prefer-v6-remapped-group:ApiVersionMajor"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-v6-remapped-group:RouteGroupPrefix"] = "/api/v6/tests/generated/runtime/override/remapped";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedVersionOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/generated/runtime/override/remapped/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal("/api/v6/tests/generated/runtime/override/remapped", endpoint.RouteGroupPrefix);
        Assert.Equal("v6", endpoint.OpenApiDocumentName);
        Assert.Equal(6, endpoint.ApiVersionMajor);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal("prefer-v6-remapped-group", candidate.AppliedOverrideId);
        Assert.Equal(4, candidate.OriginalProjection.ApiVersionMajor);
        Assert.Equal("/api/v4/tests/generated/runtime/override", candidate.OriginalProjection.RouteGroupPrefix);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "prefer-v6-remapped-group", StringComparison.Ordinal));
        Assert.Equal(6, rule.ApiVersionMajor);
        Assert.Equal("/api/v6/tests/generated/runtime/override/remapped", rule.RouteGroupPrefix);

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v6/tests/generated/runtime/override/remapped/orders/ord-42");
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonSplitsRouteGroupMaterializationWhenRouteGroupPrefixOverrideTargetsOneCandidate()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.Configuration["RestApi:Overrides:details-remapped-group:Behaviors:0"] = "tests.rest.profile.split.details";
        builder.Configuration["RestApi:Overrides:details-remapped-group:RouteGroupPrefix"] = "/api/v4/tests/profile/runtime/split/remapped";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new SplitProfileVersionOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);

        var summaryEndpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.rest.profile.split.summary", StringComparison.Ordinal));
        Assert.Equal("/api/v4/tests/profile/runtime/split/orders/{orderId}", summaryEndpoint.RoutePattern);
        Assert.Equal("/api/v4/tests/profile/runtime/split", summaryEndpoint.RouteGroupPrefix);

        var detailsEndpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.rest.profile.split.details", StringComparison.Ordinal));
        Assert.Equal("/api/v4/tests/profile/runtime/split/remapped/orders/{orderId}/details", detailsEndpoint.RoutePattern);
        Assert.Equal("/api/v4/tests/profile/runtime/split/remapped", detailsEndpoint.RouteGroupPrefix);

        var detailsCandidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.split.details", StringComparison.Ordinal));
        Assert.Equal("details-remapped-group", detailsCandidate.AppliedOverrideId);
        Assert.Equal("/api/v4/tests/profile/runtime/split", detailsCandidate.OriginalProjection.RouteGroupPrefix);

        var summaryResponse = await client.GetFromJsonAsync<ProfileRuntimeOrderOutput>("/api/v4/tests/profile/runtime/split/orders/ord-42");
        Assert.NotNull(summaryResponse);
        Assert.Equal("ord-42", summaryResponse.OrderId);

        var oldDetailsResponse = await client.GetAsync("/api/v4/tests/profile/runtime/split/orders/ord-42/details");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, oldDetailsResponse.StatusCode);

        var remappedDetailsResponse = await client.GetFromJsonAsync<ProfileRuntimeOrderOutput>("/api/v4/tests/profile/runtime/split/remapped/orders/ord-42/details");
        Assert.NotNull(remappedDetailsResponse);
        Assert.Equal("ord-42-details", remappedDetailsResponse.OrderId);
    }

    [Fact]
    public async Task MapCephalonSplitsProfileDrivenGroupWhenApiVersionOverrideTargetsOnlyOneCandidate()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.Configuration["RestApi:Overrides:details-v6:Behaviors:0"] = "tests.rest.profile.split.details";
        builder.Configuration["RestApi:Overrides:details-v6:ApiVersionMajor"] = "6";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new SplitProfileVersionOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.Equal(2, endpoints.Length);

        var summaryEndpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.rest.profile.split.summary", StringComparison.Ordinal));
        Assert.Equal("/api/v4/tests/profile/runtime/split/orders/{orderId}", summaryEndpoint.RoutePattern);
        Assert.Equal(4, summaryEndpoint.ApiVersionMajor);

        var detailsEndpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.rest.profile.split.details", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile/runtime/split/orders/{orderId}/details", detailsEndpoint.RoutePattern);
        Assert.Equal(6, detailsEndpoint.ApiVersionMajor);

        var detailsCandidate = Assert.Single(candidates, static candidate =>
            string.Equals(candidate.ProjectedEndpoint.BehaviorId, "tests.rest.profile.split.details", StringComparison.Ordinal));
        Assert.Equal("details-v6", detailsCandidate.AppliedOverrideId);

        var summaryPayload = await client.GetFromJsonAsync<ProfileRuntimeOrderOutput>("/api/v4/tests/profile/runtime/split/orders/ord-11");
        Assert.NotNull(summaryPayload);
        Assert.Equal("ord-11", summaryPayload.OrderId);

        var detailsPayload = await client.GetFromJsonAsync<ProfileRuntimeOrderOutput>("/api/v6/tests/profile/runtime/split/orders/ord-11/details");
        Assert.NotNull(detailsPayload);
        Assert.Equal("ord-11-details", detailsPayload.OrderId);
    }

    [Fact]
    public async Task MapCephalonKeepsExplicitGroupApiVersionAuthoritativeOverRestApiVersionOverride()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "8";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-v6:Behaviors:0"] = "tests.generated.runtimeexplicitoverride.lookup";
        builder.Configuration["RestApi:Overrides:prefer-v6:ApiVersionMajor"] = "6";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ExplicitVersionOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.generated.runtimeexplicitoverride.lookup", StringComparison.Ordinal));
        Assert.Equal("/api/v8/tests/generated/runtime/explicit-override/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal(8, endpoint.ApiVersionMajor);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimeexplicitoverride.lookup", StringComparison.Ordinal));
        Assert.Null(candidate.AppliedOverrideId);

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v8/tests/generated/runtime/explicit-override/orders/ord-52");
        Assert.NotNull(payload);
        Assert.Equal("ord-52", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonKeepsExplicitGroupApiVersionAuthoritativeWhileApplyingRestMethodOverride()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "8";
        builder.Configuration["OpenApi:DefaultVersion"] = "8";
        builder.Configuration["RestApi:Overrides:prefer-delete-v6:Behaviors:0"] = "tests.generated.runtimeexplicitoverride.lookup";
        builder.Configuration["RestApi:Overrides:prefer-delete-v6:ApiVersionMajor"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-delete-v6:Method"] = "DELETE";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ExplicitVersionOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.generated.runtimeexplicitoverride.lookup", StringComparison.Ordinal));
        Assert.Equal("DELETE", endpoint.Method);
        Assert.Equal("/api/v8/tests/generated/runtime/explicit-override/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal(8, endpoint.ApiVersionMajor);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimeexplicitoverride.lookup", StringComparison.Ordinal));
        Assert.Equal("prefer-delete-v6", candidate.AppliedOverrideId);
        Assert.Equal("DELETE", candidate.ProjectedEndpoint.Method);
        Assert.Equal(8, candidate.ProjectedEndpoint.ApiVersionMajor);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "prefer-delete-v6", StringComparison.Ordinal));
        Assert.Equal("DELETE", rule.Method);
        Assert.Equal(6, rule.ApiVersionMajor);

        var getResponse = await client.GetAsync("/api/v8/tests/generated/runtime/explicit-override/orders/ord-52");
        Assert.Equal(System.Net.HttpStatusCode.MethodNotAllowed, getResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync("/api/v8/tests/generated/runtime/explicit-override/orders/ord-52");
        deleteResponse.EnsureSuccessStatusCode();
        var payload = await deleteResponse.Content.ReadFromJsonAsync<GeneratedRuntimeOrderOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-52", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonKeepsExplicitGroupApiVersionAuthoritativeWhileApplyingRestPatternOverride()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "8";
        builder.Configuration["OpenApi:DefaultVersion"] = "8";
        builder.Configuration["RestApi:Overrides:prefer-lookup-v6:Behaviors:0"] = "tests.generated.runtimeexplicitoverride.lookup";
        builder.Configuration["RestApi:Overrides:prefer-lookup-v6:ApiVersionMajor"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-lookup-v6:Pattern"] = "/lookup/{orderId}";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ExplicitVersionOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.generated.runtimeexplicitoverride.lookup", StringComparison.Ordinal));
        Assert.Equal("GET", endpoint.Method);
        Assert.Equal("/api/v8/tests/generated/runtime/explicit-override/lookup/{orderId}", endpoint.RoutePattern);
        Assert.Equal("/lookup/{orderId}", endpoint.RelativePattern);
        Assert.Equal(8, endpoint.ApiVersionMajor);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimeexplicitoverride.lookup", StringComparison.Ordinal));
        Assert.Equal("prefer-lookup-v6", candidate.AppliedOverrideId);
        Assert.Equal("/lookup/{orderId}", candidate.ProjectedEndpoint.RelativePattern);
        Assert.Equal(8, candidate.ProjectedEndpoint.ApiVersionMajor);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "prefer-lookup-v6", StringComparison.Ordinal));
        Assert.Equal("/lookup/{orderId}", rule.Pattern);
        Assert.Equal(6, rule.ApiVersionMajor);

        var oldResponse = await client.GetAsync("/api/v8/tests/generated/runtime/explicit-override/orders/ord-52");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, oldResponse.StatusCode);

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v8/tests/generated/runtime/explicit-override/lookup/ord-52");
        Assert.NotNull(payload);
        Assert.Equal("ord-52", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonKeepsExplicitGroupApiVersionAuthoritativeWhileApplyingRestRouteGroupPrefixOverride()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "8";
        builder.Configuration["OpenApi:DefaultVersion"] = "8";
        builder.Configuration["RestApi:Overrides:prefer-remapped-v6:Behaviors:0"] = "tests.generated.runtimeexplicitoverride.lookup";
        builder.Configuration["RestApi:Overrides:prefer-remapped-v6:ApiVersionMajor"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-remapped-v6:RouteGroupPrefix"] = "/api/v8/tests/generated/runtime/explicit-override/remapped";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ExplicitVersionOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.generated.runtimeexplicitoverride.lookup", StringComparison.Ordinal));
        Assert.Equal("GET", endpoint.Method);
        Assert.Equal("/api/v8/tests/generated/runtime/explicit-override/remapped/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal("/api/v8/tests/generated/runtime/explicit-override/remapped", endpoint.RouteGroupPrefix);
        Assert.Equal(8, endpoint.ApiVersionMajor);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimeexplicitoverride.lookup", StringComparison.Ordinal));
        Assert.Equal("prefer-remapped-v6", candidate.AppliedOverrideId);
        Assert.Equal(8, candidate.ProjectedEndpoint.ApiVersionMajor);
        Assert.Equal("/api/v8/tests/generated/runtime/explicit-override", candidate.OriginalProjection.RouteGroupPrefix);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "prefer-remapped-v6", StringComparison.Ordinal));
        Assert.Equal(6, rule.ApiVersionMajor);
        Assert.Equal("/api/v8/tests/generated/runtime/explicit-override/remapped", rule.RouteGroupPrefix);

        var oldResponse = await client.GetAsync("/api/v8/tests/generated/runtime/explicit-override/orders/ord-52");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, oldResponse.StatusCode);

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v8/tests/generated/runtime/explicit-override/remapped/orders/ord-52");
        Assert.NotNull(payload);
        Assert.Equal("ord-52", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonSuppressesGeneratedCandidatesThroughRestApiGovernanceAndExposesSuppressionCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "4";
        builder.Configuration["OpenApi:DefaultVersion"] = "4";
        builder.Configuration["RestApi:Suppressions:hide-generated-runtime:Modules:0"] = "tests.rest.generated-runtime";
        builder.Configuration["RestApi:Suppressions:hide-generated-runtime:AuthoringStyles:0"] = RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle;
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var suppressions = await client.GetFromJsonAsync<RestEndpointSuppressionDescriptor[]>("/engine/rest-endpoint-suppressions");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(suppressions);
        Assert.NotNull(snapshot);
        Assert.Empty(endpoints);
        Assert.Equal(2, candidates.Length);

        Assert.All(
            candidates,
            candidate =>
            {
                Assert.Equal(RestEndpointCandidateStatus.Suppressed, candidate.Status);
                Assert.Equal("hide-generated-runtime", candidate.SuppressedBySuppressionId);
                Assert.Null(candidate.SuppressedByCandidateId);
            });

        var suppression = Assert.Single(suppressions);
        Assert.Equal("hide-generated-runtime", suppression.Id);
        Assert.Contains("tests.rest.generated-runtime", suppression.SourceModuleIds);
        Assert.Contains(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, suppression.AuthoringStyles);

        Assert.Contains(snapshot.RestEndpointSuppressions, item =>
            string.Equals(item.Id, suppression.Id, StringComparison.Ordinal));
        Assert.All(
            snapshot.RestEndpointCandidates,
            candidate => Assert.Equal("hide-generated-runtime", candidate.SuppressedBySuppressionId));

        var response = await client.GetAsync("/api/v4/tests/generated/runtime/orders/ord-42");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MapCephalonLetsGeneratedCandidatePublishWhenProfileCandidateIsSuppressedByRestApiGovernance()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Suppressions:prefer-generated:Behaviors:0"] = "tests.rest.generated.threeway.lookup";
        builder.Configuration["RestApi:Suppressions:prefer-generated:AuthoringStyles:0"] = RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle;
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedProfileGovernanceRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var suppressions = await client.GetFromJsonAsync<RestEndpointSuppressionDescriptor[]>("/engine/rest-endpoint-suppressions");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(suppressions);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints);
        Assert.Equal("tests.rest.generated.threeway.lookup", endpoint.BehaviorId);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, endpoint.Metadata["authoringStyle"]);
        Assert.Equal("/api/v6/tests/generated/runtime/governed/orders/{orderId}", endpoint.RoutePattern);

        var published = Assert.Single(candidates, static candidate =>
            candidate.Status == RestEndpointCandidateStatus.Published);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, published.AuthoringStyle);
        Assert.Null(published.SuppressedBySuppressionId);

        var suppressed = Assert.Single(candidates, static candidate =>
            candidate.Status == RestEndpointCandidateStatus.Suppressed);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, suppressed.AuthoringStyle);
        Assert.Equal("prefer-generated", suppressed.SuppressedBySuppressionId);
        Assert.Null(suppressed.SuppressedByCandidateId);

        var suppression = Assert.Single(suppressions);
        Assert.Equal("prefer-generated", suppression.Id);
        Assert.Contains("tests.rest.generated.threeway.lookup", suppression.BehaviorIds);
        Assert.Contains(RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, suppression.AuthoringStyles);
        Assert.Contains(snapshot.RestEndpointSuppressions, item =>
            string.Equals(item.Id, "prefer-generated", StringComparison.Ordinal));

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v6/tests/generated/runtime/governed/orders/ord-99");
        Assert.NotNull(payload);
        Assert.Equal("ord-99", payload.OrderId);
    }

    [Fact]
    public async Task MapCephalonAppliesExpandedOverrideSelectorsOnlyToTheMatchingCandidate()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "7";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:secondary-only:Behaviors:0"] = "tests.rest.profile.selector.bindings";
        builder.Configuration["RestApi:Overrides:secondary-only:ApiVersionMajors:0"] = "7";
        builder.Configuration["RestApi:Overrides:secondary-only:Methods:0"] = "POST";
        builder.Configuration["RestApi:Overrides:secondary-only:RelativePatterns:0"] = "/{orderId}/items";
        builder.Configuration["RestApi:Overrides:secondary-only:RouteGroupPrefixes:0"] = "/api/v7/tests/profile-runtime/selectors/secondary/orders";
        builder.Configuration["RestApi:Overrides:secondary-only:Pattern"] = "/lookup/{orderId}/items";
        builder.Configuration["RestApi:Overrides:all-candidates:Behaviors:0"] = "tests.rest.profile.selector.bindings";
        builder.Configuration["RestApi:Overrides:all-candidates:Pattern"] = "/lookup/general/{orderId}/items";
        builder.Configuration["RestApi:Overrides:all-candidates:Bindings:0:PropertyName"] = "OrderId";
        builder.Configuration["RestApi:Overrides:all-candidates:Bindings:0:Source"] = "route";
        builder.Configuration["RestApi:Overrides:all-candidates:Bindings:0:Name"] = "orderId";
        builder.Configuration["RestApi:Overrides:all-candidates:Bindings:1:PropertyName"] = "Quantity";
        builder.Configuration["RestApi:Overrides:all-candidates:Bindings:1:Source"] = "query";
        builder.Configuration["RestApi:Overrides:all-candidates:Bindings:1:Name"] = "quantity";
        builder.Configuration["RestApi:Overrides:all-candidates:Bindings:2:PropertyName"] = "CorrelationId";
        builder.Configuration["RestApi:Overrides:all-candidates:Bindings:2:Source"] = "header";
        builder.Configuration["RestApi:Overrides:all-candidates:Bindings:2:Name"] = "X-Correlation-Id";
        builder.Configuration["RestApi:Overrides:all-candidates:Bindings:3:PropertyName"] = "Note";
        builder.Configuration["RestApi:Overrides:all-candidates:Bindings:3:Source"] = "body";
        builder.Configuration["RestApi:Overrides:all-candidates:Bindings:3:Name"] = "note";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileSelectorRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.Equal(2, endpoints.Length);
        Assert.Equal(2, candidates.Length);

        var primaryEndpoint = Assert.Single(endpoints, static item =>
            string.Equals(
                item.RoutePattern,
                "/api/v6/tests/profile-runtime/selectors/primary/orders/lookup/general/{orderId}/items",
                StringComparison.Ordinal));
        var secondaryEndpoint = Assert.Single(endpoints, static item =>
            string.Equals(
                item.RoutePattern,
                "/api/v7/tests/profile-runtime/selectors/secondary/orders/lookup/{orderId}/items",
                StringComparison.Ordinal));

        Assert.Equal("tests.rest.profile.selector.bindings", primaryEndpoint.BehaviorId);
        Assert.Equal("tests.rest.profile.selector.bindings", secondaryEndpoint.BehaviorId);
        Assert.Equal(
            RestEndpointGovernanceRuleSelectionBasis.SingleMatch,
            primaryEndpoint.OverrideSelectionBasis);
        Assert.Equal(
            RestEndpointGovernanceRuleSelectionBasis.MoreTargetDimensions,
            secondaryEndpoint.OverrideSelectionBasis);

        var primaryCandidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.RoutePattern, "/api/v6/tests/profile-runtime/selectors/primary/orders/lookup/general/{orderId}/items", StringComparison.Ordinal));
        var secondaryCandidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.RoutePattern, "/api/v7/tests/profile-runtime/selectors/secondary/orders/lookup/{orderId}/items", StringComparison.Ordinal));

        Assert.Equal("all-candidates", primaryCandidate.AppliedOverrideId);
        Assert.Equal(["all-candidates"], primaryCandidate.MatchedOverrideIds);
        Assert.Equal(
            RestEndpointGovernanceRuleSelectionBasis.SingleMatch,
            primaryCandidate.OverrideSelectionBasis);
        Assert.Equal("secondary-only", secondaryCandidate.AppliedOverrideId);
        Assert.Equal(
            ["secondary-only", "all-candidates"],
            secondaryCandidate.MatchedOverrideIds);
        Assert.Equal(
            RestEndpointGovernanceRuleSelectionBasis.MoreTargetDimensions,
            secondaryCandidate.OverrideSelectionBasis);
        Assert.Equal("POST", secondaryCandidate.OriginalProjection.Method);
        Assert.Equal(7, secondaryCandidate.OriginalProjection.ApiVersionMajor);
        Assert.Equal("/api/v7/tests/profile-runtime/selectors/secondary/orders", secondaryCandidate.OriginalProjection.RouteGroupPrefix);
        Assert.Equal("/{orderId}/items", secondaryCandidate.OriginalProjection.RelativePattern);
        Assert.Equal("/api/v7/tests/profile-runtime/selectors/secondary/orders/{orderId}/items", secondaryCandidate.OriginalProjection.RoutePattern);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "secondary-only", StringComparison.Ordinal));
        Assert.Contains("tests.rest.profile.selector.bindings", rule.BehaviorIds);
        Assert.Contains(7, rule.ApiVersionMajors);
        Assert.Contains("POST", rule.Methods);
        Assert.Contains("/{orderId}/items", rule.RelativePatterns);
        Assert.Contains("/api/v7/tests/profile-runtime/selectors/secondary/orders", rule.RouteGroupPrefixes);
        Assert.Equal("/lookup/{orderId}/items", rule.Pattern);

        using var primaryRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/selectors/primary/orders/lookup/general/ord-91/items?quantity=2");
        primaryRequest.Headers.Add("X-Correlation-Id", "corr-91");
        primaryRequest.Content = JsonContent.Create(new
        {
            note = "primary"
        });

        var primaryResponse = await client.SendAsync(primaryRequest);
        primaryResponse.EnsureSuccessStatusCode();

        using var secondaryRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v7/tests/profile-runtime/selectors/secondary/orders/lookup/ord-92/items?quantity=3");
        secondaryRequest.Headers.Add("X-Correlation-Id", "corr-92");
        secondaryRequest.Content = JsonContent.Create(new
        {
            note = "secondary"
        });

        var secondaryResponse = await client.SendAsync(secondaryRequest);
        secondaryResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task MapCephalonPrefersExactBehaviorOverrideOverPrefixTargetedRuleInRuntimeCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "7";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "8";
        builder.Configuration["OpenApi:DefaultVersion"] = "7";
        builder.Configuration["RestApi:Overrides:promote-grouped-orders:BehaviorIdPrefixes:0"] = "tests.generated.runtimegrouped.orders";
        builder.Configuration["RestApi:Overrides:promote-grouped-orders:TagName"] = "Generated Grouped Orders API";
        builder.Configuration["RestApi:Overrides:promote-grouped-orders-lookup:Behaviors:0"] = "tests.generated.runtimegrouped.orders.lookup";
        builder.Configuration["RestApi:Overrides:promote-grouped-orders-lookup:TagName"] = "Generated Grouped Orders Lookup API";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedGroupedRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();
        var overrideCatalog = app.Services.GetRequiredService<IRestEndpointOverrideRuntimeCatalog>();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");
        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var ordersLookupEndpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.generated.runtimegrouped.orders.lookup", StringComparison.Ordinal));
        Assert.Equal("promote-grouped-orders-lookup", ordersLookupEndpoint.AppliedOverrideId);
        Assert.Equal(
            ["promote-grouped-orders-lookup", "promote-grouped-orders"],
            ordersLookupEndpoint.MatchedOverrideIds);
        Assert.Equal(RestEndpointGovernanceRuleSelectionBasis.NarrowerBehaviorScope, ordersLookupEndpoint.OverrideSelectionBasis);
        Assert.Equal(["Generated Grouped Orders Lookup API"], ordersLookupEndpoint.Tags);

        var ordersCreateEndpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.generated.runtimegrouped.orders.create", StringComparison.Ordinal));
        Assert.Equal("promote-grouped-orders", ordersCreateEndpoint.AppliedOverrideId);
        Assert.Equal(["promote-grouped-orders"], ordersCreateEndpoint.MatchedOverrideIds);
        Assert.Equal(RestEndpointGovernanceRuleSelectionBasis.SingleMatch, ordersCreateEndpoint.OverrideSelectionBasis);
        Assert.Equal(["Generated Grouped Orders API"], ordersCreateEndpoint.Tags);

        var ordersLookupCandidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimegrouped.orders.lookup", StringComparison.Ordinal));
        Assert.Equal("promote-grouped-orders-lookup", ordersLookupCandidate.AppliedOverrideId);
        Assert.Equal(
            ["promote-grouped-orders-lookup", "promote-grouped-orders"],
            ordersLookupCandidate.MatchedOverrideIds);
        Assert.Equal(RestEndpointGovernanceRuleSelectionBasis.NarrowerBehaviorScope, ordersLookupCandidate.OverrideSelectionBasis);

        var ordersCreateCandidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimegrouped.orders.create", StringComparison.Ordinal));
        Assert.Equal("promote-grouped-orders", ordersCreateCandidate.AppliedOverrideId);
        Assert.Equal(RestEndpointGovernanceRuleSelectionBasis.SingleMatch, ordersCreateCandidate.OverrideSelectionBasis);

        var exactRule = Assert.Single(overrides, static item => string.Equals(item.Id, "promote-grouped-orders-lookup", StringComparison.Ordinal));
        Assert.Equal(["tests.generated.runtimegrouped.orders.lookup"], exactRule.BehaviorIds);
        Assert.Empty(exactRule.BehaviorIdPrefixes);
        Assert.Equal([ordersLookupCandidate.Id], exactRule.SelectedCandidateIds);
        Assert.Equal([ordersLookupCandidate.Id], exactRule.AppliedCandidateIds);
        Assert.Equal([RestEndpointGovernanceRuleSelectionBasis.NarrowerBehaviorScope], exactRule.SelectionBases);

        var prefixRule = Assert.Single(overrides, static item => string.Equals(item.Id, "promote-grouped-orders", StringComparison.Ordinal));
        Assert.Empty(prefixRule.BehaviorIds);
        Assert.Equal(["tests.generated.runtimegrouped.orders"], prefixRule.BehaviorIdPrefixes);
        Assert.Contains(ordersLookupCandidate.Id, prefixRule.MatchedCandidateIds);
        Assert.Contains(ordersCreateCandidate.Id, prefixRule.MatchedCandidateIds);
        Assert.Equal([ordersCreateCandidate.Id], prefixRule.SelectedCandidateIds);
        Assert.Equal([ordersCreateCandidate.Id], prefixRule.AppliedCandidateIds);

        Assert.Equal(
            ["promote-grouped-orders", "promote-grouped-orders-lookup"],
            overrideCatalog.GetByBehaviorId("tests.generated.runtimegrouped.orders.lookup").Select(static item => item.Id).ToArray());
        Assert.Equal(
            ["promote-grouped-orders"],
            overrideCatalog.GetByBehaviorId("tests.generated.runtimegrouped.orders.create").Select(static item => item.Id).ToArray());
        Assert.Empty(overrideCatalog.GetByBehaviorId("tests.generated.runtimegrouped.inventory.lookup"));

        Assert.Contains(snapshot.RestEndpointOverrides, item =>
            string.Equals(item.Id, "promote-grouped-orders-lookup", StringComparison.Ordinal) &&
            item.SelectionBases.SequenceEqual([RestEndpointGovernanceRuleSelectionBasis.NarrowerBehaviorScope]));

        var lookupPayload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>(
            "/api/v7/tests/generated/runtimegrouped/orders/ord-lookup");
        Assert.NotNull(lookupPayload);
        Assert.Equal("ord-lookup", lookupPayload.OrderId);

        using var createRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v7/tests/generated/runtimegrouped/orders/ord-create/items");
        createRequest.Content = JsonContent.Create(new
        {
            productId = "sku-1"
        });
        var createResponse = await client.SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createPayload = await createResponse.Content.ReadFromJsonAsync<GeneratedRuntimeOrderOutput>();
        Assert.NotNull(createPayload);
        Assert.Equal("ord-create", createPayload.OrderId);
    }

    [Fact]
    public async Task MapCephalonExposesBehaviorIdPrefixOverrideParityAcrossGroupedOperatorSurfaces()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "7";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "8";
        builder.Configuration["OpenApi:DefaultVersion"] = "7";
        builder.Configuration["RestApi:Overrides:promote-grouped-orders:BehaviorIdPrefixes:0"] = "tests.generated.runtimegrouped.orders";
        builder.Configuration["RestApi:Overrides:promote-grouped-orders:TagName"] = "Generated Grouped Orders API";
        builder.Configuration["RestApi:Overrides:promote-grouped-orders-lookup:Behaviors:0"] = "tests.generated.runtimegrouped.orders.lookup";
        builder.Configuration["RestApi:Overrides:promote-grouped-orders-lookup:TagName"] = "Generated Grouped Orders Lookup API";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedGroupedRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var groups = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor[]>("/engine/rest-endpoint-publication-groups");
        var lookupGroupByBehavior = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor>(
            "/engine/rest-endpoint-publication-groups/tests.generated.runtimegrouped.orders.lookup");
        var createGroupByBehavior = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor>(
            "/engine/rest-endpoint-publication-groups/tests.generated.runtimegrouped.orders.create");
        var policies = await client.GetFromJsonAsync<RestEndpointAuthoringPolicyDescriptor[]>("/engine/rest-endpoint-authoring-policies");
        var lookupPolicyByBehavior = await client.GetFromJsonAsync<RestEndpointAuthoringPolicyDescriptor>(
            "/engine/rest-endpoint-authoring-policies/tests.generated.runtimegrouped.orders.lookup");
        var createPolicyByBehavior = await client.GetFromJsonAsync<RestEndpointAuthoringPolicyDescriptor>(
            "/engine/rest-endpoint-authoring-policies/tests.generated.runtimegrouped.orders.create");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(candidates);
        Assert.NotNull(groups);
        Assert.NotNull(lookupGroupByBehavior);
        Assert.NotNull(createGroupByBehavior);
        Assert.NotNull(policies);
        Assert.NotNull(lookupPolicyByBehavior);
        Assert.NotNull(createPolicyByBehavior);
        Assert.NotNull(snapshot);

        var ordersLookupCandidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimegrouped.orders.lookup", StringComparison.Ordinal));
        var ordersCreateCandidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimegrouped.orders.create", StringComparison.Ordinal));
        Assert.Equal("promote-grouped-orders-lookup", ordersLookupCandidate.SelectedOverrideId);
        Assert.Equal("promote-grouped-orders-lookup", ordersLookupCandidate.AppliedOverrideId);
        Assert.Equal(
            ["promote-grouped-orders-lookup", "promote-grouped-orders"],
            ordersLookupCandidate.MatchedOverrideIds);
        Assert.Equal(
            RestEndpointGovernanceRuleSelectionBasis.NarrowerBehaviorScope,
            ordersLookupCandidate.OverrideSelectionBasis);
        Assert.Equal("promote-grouped-orders", ordersCreateCandidate.SelectedOverrideId);
        Assert.Equal("promote-grouped-orders", ordersCreateCandidate.AppliedOverrideId);
        Assert.Equal(["promote-grouped-orders"], ordersCreateCandidate.MatchedOverrideIds);
        Assert.Equal(RestEndpointGovernanceRuleSelectionBasis.SingleMatch, ordersCreateCandidate.OverrideSelectionBasis);

        var lookupGroup = Assert.Single(groups, static item =>
            string.Equals(item.BehaviorId, "tests.generated.runtimegrouped.orders.lookup", StringComparison.Ordinal));
        Assert.False(lookupGroup.AuthoringPolicy.IsConfigured);
        Assert.False(lookupGroup.AuthoringPolicy.AllowMultiplePublishedCandidates);
        Assert.Equal([ordersLookupCandidate.Id], lookupGroup.PublishedCandidateIds);
        Assert.Equal(2, lookupGroup.GovernanceOverrideSummaries.Count);
        var lookupExactOverride = Assert.Single(lookupGroup.GovernanceOverrideSummaries, static item =>
            string.Equals(item.RuleId, "promote-grouped-orders-lookup", StringComparison.Ordinal));
        Assert.Equal([ordersLookupCandidate.Id], lookupExactOverride.MatchedCandidateIds);
        Assert.Equal([ordersLookupCandidate.Id], lookupExactOverride.SelectedCandidateIds);
        Assert.Equal([ordersLookupCandidate.Id], lookupExactOverride.AppliedCandidateIds);
        Assert.Equal(
            RestEndpointGovernanceRuleSelectionBasis.NarrowerBehaviorScope,
            Assert.Single(lookupExactOverride.SelectionBasisSummaries).SelectionBasis);
        Assert.Equal(
            ordersLookupCandidate.SelectedOverrideActionKinds,
            lookupExactOverride.SelectedActionKindSummaries.Select(static item => item.ActionKind).ToArray());
        Assert.Equal(
            ordersLookupCandidate.AppliedOverrideActionKinds,
            lookupExactOverride.AppliedActionKindSummaries.Select(static item => item.ActionKind).ToArray());
        var lookupPrefixOverride = Assert.Single(lookupGroup.GovernanceOverrideSummaries, static item =>
            string.Equals(item.RuleId, "promote-grouped-orders", StringComparison.Ordinal));
        Assert.Equal([ordersLookupCandidate.Id], lookupPrefixOverride.MatchedCandidateIds);
        Assert.Empty(lookupPrefixOverride.SelectedCandidateIds);
        Assert.Empty(lookupPrefixOverride.AppliedCandidateIds);
        Assert.Empty(lookupPrefixOverride.SelectionBasisSummaries);
        Assert.Empty(lookupPrefixOverride.SelectedActionKindSummaries);
        Assert.Empty(lookupPrefixOverride.AppliedActionKindSummaries);
        var lookupGeneratedStyle = Assert.Single(lookupGroup.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal(2, lookupGeneratedStyle.GovernanceOverrideSummaries.Count);

        var createGroup = Assert.Single(groups, static item =>
            string.Equals(item.BehaviorId, "tests.generated.runtimegrouped.orders.create", StringComparison.Ordinal));
        Assert.False(createGroup.AuthoringPolicy.IsConfigured);
        Assert.False(createGroup.AuthoringPolicy.AllowMultiplePublishedCandidates);
        Assert.Equal([ordersCreateCandidate.Id], createGroup.PublishedCandidateIds);
        var createOverride = Assert.Single(createGroup.GovernanceOverrideSummaries);
        Assert.Equal("promote-grouped-orders", createOverride.RuleId);
        Assert.Equal([ordersCreateCandidate.Id], createOverride.MatchedCandidateIds);
        Assert.Equal([ordersCreateCandidate.Id], createOverride.SelectedCandidateIds);
        Assert.Equal([ordersCreateCandidate.Id], createOverride.AppliedCandidateIds);
        Assert.Equal(
            RestEndpointGovernanceRuleSelectionBasis.SingleMatch,
            Assert.Single(createOverride.SelectionBasisSummaries).SelectionBasis);
        Assert.Equal(
            ordersCreateCandidate.SelectedOverrideActionKinds,
            createOverride.SelectedActionKindSummaries.Select(static item => item.ActionKind).ToArray());
        Assert.Equal(
            ordersCreateCandidate.AppliedOverrideActionKinds,
            createOverride.AppliedActionKindSummaries.Select(static item => item.ActionKind).ToArray());
        var createGeneratedStyle = Assert.Single(createGroup.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Single(createGeneratedStyle.GovernanceOverrideSummaries);

        var lookupPolicy = Assert.Single(policies, static item =>
            string.Equals(item.BehaviorId, "tests.generated.runtimegrouped.orders.lookup", StringComparison.Ordinal));
        Assert.False(lookupPolicy.IsConfigured);
        Assert.False(lookupPolicy.AllowMultiplePublishedCandidates);
        Assert.Equal([ordersLookupCandidate.Id], lookupPolicy.PublishedCandidateIds);
        Assert.Equal(2, lookupPolicy.GovernanceOverrideSummaries.Count);
        var lookupExactPolicyOverride = Assert.Single(lookupPolicy.GovernanceOverrideSummaries, static item =>
            string.Equals(item.RuleId, "promote-grouped-orders-lookup", StringComparison.Ordinal));
        Assert.Equal([ordersLookupCandidate.Id], lookupExactPolicyOverride.MatchedCandidateIds);
        Assert.Equal([ordersLookupCandidate.Id], lookupExactPolicyOverride.SelectedCandidateIds);
        Assert.Equal([ordersLookupCandidate.Id], lookupExactPolicyOverride.AppliedCandidateIds);
        Assert.Equal(
            RestEndpointGovernanceRuleSelectionBasis.NarrowerBehaviorScope,
            Assert.Single(lookupExactPolicyOverride.SelectionBasisSummaries).SelectionBasis);
        var lookupPrefixPolicyOverride = Assert.Single(lookupPolicy.GovernanceOverrideSummaries, static item =>
            string.Equals(item.RuleId, "promote-grouped-orders", StringComparison.Ordinal));
        Assert.Equal([ordersLookupCandidate.Id], lookupPrefixPolicyOverride.MatchedCandidateIds);
        Assert.Empty(lookupPrefixPolicyOverride.SelectedCandidateIds);
        Assert.Empty(lookupPrefixPolicyOverride.AppliedCandidateIds);
        Assert.Empty(lookupPrefixPolicyOverride.SelectionBasisSummaries);
        var lookupPolicyGeneratedStyle = Assert.Single(lookupPolicy.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal(2, lookupPolicyGeneratedStyle.GovernanceOverrideSummaries.Count);

        var createPolicy = Assert.Single(policies, static item =>
            string.Equals(item.BehaviorId, "tests.generated.runtimegrouped.orders.create", StringComparison.Ordinal));
        Assert.False(createPolicy.IsConfigured);
        Assert.False(createPolicy.AllowMultiplePublishedCandidates);
        Assert.Equal([ordersCreateCandidate.Id], createPolicy.PublishedCandidateIds);
        var createPolicyOverride = Assert.Single(createPolicy.GovernanceOverrideSummaries);
        Assert.Equal("promote-grouped-orders", createPolicyOverride.RuleId);
        Assert.Equal([ordersCreateCandidate.Id], createPolicyOverride.MatchedCandidateIds);
        Assert.Equal([ordersCreateCandidate.Id], createPolicyOverride.SelectedCandidateIds);
        Assert.Equal([ordersCreateCandidate.Id], createPolicyOverride.AppliedCandidateIds);
        Assert.Equal(
            RestEndpointGovernanceRuleSelectionBasis.SingleMatch,
            Assert.Single(createPolicyOverride.SelectionBasisSummaries).SelectionBasis);
        var createPolicyGeneratedStyle = Assert.Single(createPolicy.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Single(createPolicyGeneratedStyle.GovernanceOverrideSummaries);

        Assert.Equal(lookupGroup.GovernanceOverrideSummaries.Count, lookupGroupByBehavior.GovernanceOverrideSummaries.Count);
        Assert.Equal(createGroup.GovernanceOverrideSummaries.Count, createGroupByBehavior.GovernanceOverrideSummaries.Count);
        Assert.Equal(lookupPolicy.GovernanceOverrideSummaries.Count, lookupPolicyByBehavior.GovernanceOverrideSummaries.Count);
        Assert.Equal(createPolicy.GovernanceOverrideSummaries.Count, createPolicyByBehavior.GovernanceOverrideSummaries.Count);

        Assert.Contains(snapshot.RestEndpointPublicationGroups, item =>
            string.Equals(item.BehaviorId, lookupGroup.BehaviorId, StringComparison.Ordinal) &&
            item.GovernanceOverrideSummaries.Count == 2 &&
            item.PublishedCandidateIds.SequenceEqual([ordersLookupCandidate.Id]));
        Assert.Contains(snapshot.RestEndpointPublicationGroups, item =>
            string.Equals(item.BehaviorId, createGroup.BehaviorId, StringComparison.Ordinal) &&
            item.GovernanceOverrideSummaries.Count == 1 &&
            item.PublishedCandidateIds.SequenceEqual([ordersCreateCandidate.Id]));
        Assert.Contains(snapshot.RestEndpointAuthoringPolicies, item =>
            string.Equals(item.BehaviorId, lookupPolicy.BehaviorId, StringComparison.Ordinal) &&
            item.GovernanceOverrideSummaries.Count == 2 &&
            item.PublishedCandidateIds.SequenceEqual([ordersLookupCandidate.Id]));
        Assert.Contains(snapshot.RestEndpointAuthoringPolicies, item =>
            string.Equals(item.BehaviorId, createPolicy.BehaviorId, StringComparison.Ordinal) &&
            item.GovernanceOverrideSummaries.Count == 1 &&
            item.PublishedCandidateIds.SequenceEqual([ordersCreateCandidate.Id]));
    }

    [Fact]
    public async Task MapCephalonAppliesExpandedSuppressionSelectorsOnlyToTheMatchingCandidate()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "7";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Suppressions:hide-secondary-only:Behaviors:0"] = "tests.rest.profile.selector.bindings";
        builder.Configuration["RestApi:Suppressions:hide-secondary-only:ApiVersionMajors:0"] = "7";
        builder.Configuration["RestApi:Suppressions:hide-secondary-only:Methods:0"] = "POST";
        builder.Configuration["RestApi:Suppressions:hide-secondary-only:RelativePatterns:0"] = "/{orderId}/items";
        builder.Configuration["RestApi:Suppressions:hide-secondary-only:RouteGroupPrefixes:0"] = "/api/v7/tests/profile-runtime/selectors/secondary/orders";
        builder.Configuration["RestApi:Suppressions:hide-secondary-group:Behaviors:0"] = "tests.rest.profile.selector.bindings";
        builder.Configuration["RestApi:Suppressions:hide-secondary-group:RouteGroupPrefixes:0"] = "/api/v7/tests/profile-runtime/selectors/secondary/orders";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileSelectorRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var suppressions = await client.GetFromJsonAsync<RestEndpointSuppressionDescriptor[]>("/engine/rest-endpoint-suppressions");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(suppressions);

        var endpoint = Assert.Single(endpoints);
        Assert.Equal("/api/v6/tests/profile-runtime/selectors/primary/orders/{orderId}/items", endpoint.RoutePattern);

        var published = Assert.Single(candidates, static item => item.Status == RestEndpointCandidateStatus.Published);
        Assert.Equal("/api/v6/tests/profile-runtime/selectors/primary/orders/{orderId}/items", published.ProjectedEndpoint.RoutePattern);
        Assert.Empty(published.MatchedSuppressionIds);

        var suppressed = Assert.Single(candidates, static item => item.Status == RestEndpointCandidateStatus.Suppressed);
        Assert.Equal("/api/v7/tests/profile-runtime/selectors/secondary/orders/{orderId}/items", suppressed.ProjectedEndpoint.RoutePattern);
        Assert.Equal("hide-secondary-only", suppressed.SuppressedBySuppressionId);
        Assert.Equal(
            ["hide-secondary-only", "hide-secondary-group"],
            suppressed.MatchedSuppressionIds);

        var rule = Assert.Single(suppressions, static item => string.Equals(item.Id, "hide-secondary-only", StringComparison.Ordinal));
        Assert.Contains("tests.rest.profile.selector.bindings", rule.BehaviorIds);
        Assert.Contains(7, rule.ApiVersionMajors);
        Assert.Contains("POST", rule.Methods);
        Assert.Contains("/{orderId}/items", rule.RelativePatterns);
        Assert.Contains("/api/v7/tests/profile-runtime/selectors/secondary/orders", rule.RouteGroupPrefixes);
        Assert.Equal([suppressed.Id], rule.MatchedCandidateIds);
        Assert.Equal([suppressed.Id], rule.SuppressedCandidateIds);
        Assert.Empty(rule.SkippedCandidateIds);
        Assert.Equal([RestEndpointGovernanceRuleSelectionBasis.MoreTargetDimensions], rule.SelectionBases);
        var matchedButLostRule = Assert.Single(suppressions, static item => string.Equals(item.Id, "hide-secondary-group", StringComparison.Ordinal));
        Assert.Equal([suppressed.Id], matchedButLostRule.MatchedCandidateIds);
        Assert.Empty(matchedButLostRule.SuppressedCandidateIds);
        Assert.Empty(matchedButLostRule.SkippedCandidateIds);
        Assert.Empty(matchedButLostRule.SelectionBases);

        using var publishedRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/selectors/primary/orders/ord-93/items?quantity=4");
        publishedRequest.Headers.Add("X-Correlation-Id", "corr-93");
        publishedRequest.Content = JsonContent.Create(new
        {
            note = "published"
        });

        var publishedResponse = await client.SendAsync(publishedRequest);
        publishedResponse.EnsureSuccessStatusCode();

        using var suppressedRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v7/tests/profile-runtime/selectors/secondary/orders/ord-94/items?quantity=5");
        suppressedRequest.Headers.Add("X-Correlation-Id", "corr-94");
        suppressedRequest.Content = JsonContent.Create(new
        {
            note = "suppressed"
        });

        var suppressedResponse = await client.SendAsync(suppressedRequest);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, suppressedResponse.StatusCode);
    }

    [Fact]
    public async Task MapCephalonAppliesDocumentAndTagOverrideSelectorsOnlyToTheMatchingCandidate()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "7";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:all-candidates:Behaviors:0"] = "tests.rest.profile.selector.bindings";
        builder.Configuration["RestApi:Overrides:all-candidates:Pattern"] = "/lookup/general/{orderId}/items";
        builder.Configuration["RestApi:Overrides:internal-only:Behaviors:0"] = "tests.rest.profile.selector.bindings";
        builder.Configuration["RestApi:Overrides:internal-only:OpenApiDocumentNames:0"] = "internal";
        builder.Configuration["RestApi:Overrides:internal-only:TagNames:0"] = "Profile Selector Secondary API";
        builder.Configuration["RestApi:Overrides:internal-only:Pattern"] = "/lookup/document-tag/{orderId}/items";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileSelectorRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.Equal(2, endpoints.Length);
        Assert.Equal(2, candidates.Length);

        var primaryEndpoint = Assert.Single(endpoints, static item =>
            string.Equals(
                item.RoutePattern,
                "/api/v6/tests/profile-runtime/selectors/primary/orders/lookup/general/{orderId}/items",
                StringComparison.Ordinal));
        var secondaryEndpoint = Assert.Single(endpoints, static item =>
            string.Equals(
                item.RoutePattern,
                "/api/v7/tests/profile-runtime/selectors/secondary/orders/lookup/document-tag/{orderId}/items",
                StringComparison.Ordinal));

        Assert.Equal("public", primaryEndpoint.OpenApiDocumentName);
        Assert.Equal(["Profile Selector Primary API"], primaryEndpoint.Tags);
        Assert.Equal("internal", secondaryEndpoint.OpenApiDocumentName);
        Assert.Equal(["Profile Selector Secondary API"], secondaryEndpoint.Tags);

        var primaryCandidate = Assert.Single(candidates, static item =>
            string.Equals(
                item.ProjectedEndpoint.RoutePattern,
                "/api/v6/tests/profile-runtime/selectors/primary/orders/lookup/general/{orderId}/items",
                StringComparison.Ordinal));
        var secondaryCandidate = Assert.Single(candidates, static item =>
            string.Equals(
                item.ProjectedEndpoint.RoutePattern,
                "/api/v7/tests/profile-runtime/selectors/secondary/orders/lookup/document-tag/{orderId}/items",
                StringComparison.Ordinal));

        Assert.Equal("all-candidates", primaryCandidate.AppliedOverrideId);
        Assert.Equal(["all-candidates"], primaryCandidate.MatchedOverrideIds);
        Assert.Equal("public", primaryCandidate.OriginalProjection.OpenApiDocumentName);
        Assert.Equal("Profile Selector Primary API", primaryCandidate.OriginalProjection.TagName);

        Assert.Equal("internal-only", secondaryCandidate.AppliedOverrideId);
        Assert.Equal(["internal-only", "all-candidates"], secondaryCandidate.MatchedOverrideIds);
        Assert.Equal("internal", secondaryCandidate.OriginalProjection.OpenApiDocumentName);
        Assert.Equal("Profile Selector Secondary API", secondaryCandidate.OriginalProjection.TagName);

        var allCandidatesRule = Assert.Single(overrides, static item => string.Equals(item.Id, "all-candidates", StringComparison.Ordinal));
        Assert.Equal([primaryCandidate.Id, secondaryCandidate.Id], allCandidatesRule.MatchedCandidateIds);
        Assert.Equal([primaryCandidate.Id], allCandidatesRule.SelectedCandidateIds);
        Assert.Equal([primaryCandidate.Id], allCandidatesRule.AppliedCandidateIds);
        Assert.Empty(allCandidatesRule.SkippedCandidateIds);
        Assert.Equal([primaryCandidate.OverrideSelectionBasis!.Value], allCandidatesRule.SelectionBases);
        var allCandidatesSelectionBasisSummary = Assert.Single(allCandidatesRule.SelectionBasisSummaries);
        Assert.Equal(primaryCandidate.OverrideSelectionBasis.Value, allCandidatesSelectionBasisSummary.SelectionBasis);
        Assert.Equal([primaryCandidate.Id], allCandidatesSelectionBasisSummary.CandidateIds);
        Assert.Equal(primaryCandidate.SelectedOverrideActionKinds, allCandidatesRule.SelectedActionKinds);
        Assert.Equal(primaryCandidate.AppliedOverrideActionKinds, allCandidatesRule.AppliedActionKinds);
        Assert.Equal(
            primaryCandidate.SelectedOverrideActionKinds,
            allCandidatesRule.SelectedActionKindSummaries.Select(static item => item.ActionKind).ToArray());
        Assert.All(
            allCandidatesRule.SelectedActionKindSummaries,
            summary => Assert.Equal([primaryCandidate.Id], summary.CandidateIds));
        Assert.Equal(
            primaryCandidate.AppliedOverrideActionKinds,
            allCandidatesRule.AppliedActionKindSummaries.Select(static item => item.ActionKind).ToArray());
        Assert.All(
            allCandidatesRule.AppliedActionKindSummaries,
            summary => Assert.Equal([primaryCandidate.Id], summary.CandidateIds));

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "internal-only", StringComparison.Ordinal));
        Assert.Contains("tests.rest.profile.selector.bindings", rule.BehaviorIds);
        Assert.Contains("internal", rule.OpenApiDocumentNames, StringComparer.Ordinal);
        Assert.Contains("Profile Selector Secondary API", rule.TagNames, StringComparer.Ordinal);
        Assert.Equal("/lookup/document-tag/{orderId}/items", rule.Pattern);
        Assert.Equal([secondaryCandidate.Id], rule.MatchedCandidateIds);
        Assert.Equal([secondaryCandidate.Id], rule.SelectedCandidateIds);
        Assert.Equal([secondaryCandidate.Id], rule.AppliedCandidateIds);
        Assert.Empty(rule.SkippedCandidateIds);
        Assert.Equal([secondaryCandidate.OverrideSelectionBasis!.Value], rule.SelectionBases);
        var internalOnlySelectionBasisSummary = Assert.Single(rule.SelectionBasisSummaries);
        Assert.Equal(secondaryCandidate.OverrideSelectionBasis.Value, internalOnlySelectionBasisSummary.SelectionBasis);
        Assert.Equal([secondaryCandidate.Id], internalOnlySelectionBasisSummary.CandidateIds);
        Assert.Equal(secondaryCandidate.SelectedOverrideActionKinds, rule.SelectedActionKinds);
        Assert.Equal(secondaryCandidate.AppliedOverrideActionKinds, rule.AppliedActionKinds);
        Assert.Equal(
            secondaryCandidate.SelectedOverrideActionKinds,
            rule.SelectedActionKindSummaries.Select(static item => item.ActionKind).ToArray());
        Assert.All(
            rule.SelectedActionKindSummaries,
            summary => Assert.Equal([secondaryCandidate.Id], summary.CandidateIds));
        Assert.Equal(
            secondaryCandidate.AppliedOverrideActionKinds,
            rule.AppliedActionKindSummaries.Select(static item => item.ActionKind).ToArray());
        Assert.All(
            rule.AppliedActionKindSummaries,
            summary => Assert.Equal([secondaryCandidate.Id], summary.CandidateIds));

        using var primaryRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/selectors/primary/orders/lookup/general/ord-111/items?quantity=2");
        primaryRequest.Headers.Add("X-Correlation-Id", "corr-111");
        primaryRequest.Content = JsonContent.Create(new
        {
            note = "primary"
        });

        var primaryResponse = await client.SendAsync(primaryRequest);
        primaryResponse.EnsureSuccessStatusCode();

        using var secondaryRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v7/tests/profile-runtime/selectors/secondary/orders/lookup/document-tag/ord-112/items?quantity=3");
        secondaryRequest.Headers.Add("X-Correlation-Id", "corr-112");
        secondaryRequest.Content = JsonContent.Create(new
        {
            note = "secondary"
        });

        var secondaryResponse = await client.SendAsync(secondaryRequest);
        secondaryResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task MapCephalonAppliesDocumentAndTagSuppressionSelectorsOnlyToTheMatchingCandidate()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "7";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Suppressions:hide-secondary-tag:Behaviors:0"] = "tests.rest.profile.selector.bindings";
        builder.Configuration["RestApi:Suppressions:hide-secondary-tag:TagNames:0"] = "Profile Selector Secondary API";
        builder.Configuration["RestApi:Suppressions:hide-secondary-only:Behaviors:0"] = "tests.rest.profile.selector.bindings";
        builder.Configuration["RestApi:Suppressions:hide-secondary-only:OpenApiDocumentNames:0"] = "internal";
        builder.Configuration["RestApi:Suppressions:hide-secondary-only:TagNames:0"] = "Profile Selector Secondary API";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileSelectorRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var suppressions = await client.GetFromJsonAsync<RestEndpointSuppressionDescriptor[]>("/engine/rest-endpoint-suppressions");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(suppressions);

        var endpoint = Assert.Single(endpoints);
        Assert.Equal("/api/v6/tests/profile-runtime/selectors/primary/orders/{orderId}/items", endpoint.RoutePattern);
        Assert.Equal("public", endpoint.OpenApiDocumentName);
        Assert.Equal(["Profile Selector Primary API"], endpoint.Tags);

        var published = Assert.Single(candidates, static item => item.Status == RestEndpointCandidateStatus.Published);
        Assert.Equal("/api/v6/tests/profile-runtime/selectors/primary/orders/{orderId}/items", published.ProjectedEndpoint.RoutePattern);
        Assert.Equal("public", published.OriginalProjection.OpenApiDocumentName);
        Assert.Equal("Profile Selector Primary API", published.OriginalProjection.TagName);
        Assert.Empty(published.MatchedSuppressionIds);

        var suppressed = Assert.Single(candidates, static item => item.Status == RestEndpointCandidateStatus.Suppressed);
        Assert.Equal("/api/v7/tests/profile-runtime/selectors/secondary/orders/{orderId}/items", suppressed.ProjectedEndpoint.RoutePattern);
        Assert.Equal("internal", suppressed.OriginalProjection.OpenApiDocumentName);
        Assert.Equal("Profile Selector Secondary API", suppressed.OriginalProjection.TagName);
        Assert.Equal("hide-secondary-only", suppressed.SuppressedBySuppressionId);
        Assert.Equal(["hide-secondary-only", "hide-secondary-tag"], suppressed.MatchedSuppressionIds);
        Assert.Equal(
            RestEndpointGovernanceRuleSelectionBasis.MoreTargetDimensions,
            suppressed.SuppressionSelectionBasis);

        var rule = Assert.Single(suppressions, static item => string.Equals(item.Id, "hide-secondary-only", StringComparison.Ordinal));
        Assert.Contains("tests.rest.profile.selector.bindings", rule.BehaviorIds);
        Assert.Contains("internal", rule.OpenApiDocumentNames, StringComparer.Ordinal);
        Assert.Contains("Profile Selector Secondary API", rule.TagNames, StringComparer.Ordinal);
        Assert.Equal([suppressed.Id], rule.MatchedCandidateIds);
        Assert.Equal([suppressed.Id], rule.SuppressedCandidateIds);
        Assert.Empty(rule.SkippedCandidateIds);
        Assert.Equal([suppressed.SuppressionSelectionBasis!.Value], rule.SelectionBases);
        var suppressionSelectionBasisSummary = Assert.Single(rule.SelectionBasisSummaries);
        Assert.Equal(suppressed.SuppressionSelectionBasis.Value, suppressionSelectionBasisSummary.SelectionBasis);
        Assert.Equal([suppressed.Id], suppressionSelectionBasisSummary.CandidateIds);

        var matchedButLosingRule = Assert.Single(suppressions, static item => string.Equals(item.Id, "hide-secondary-tag", StringComparison.Ordinal));
        Assert.Equal([suppressed.Id], matchedButLosingRule.MatchedCandidateIds);
        Assert.Empty(matchedButLosingRule.SuppressedCandidateIds);
        Assert.Empty(matchedButLosingRule.SelectionBases);
        Assert.Empty(matchedButLosingRule.SelectionBasisSummaries);

        using var publishedRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/selectors/primary/orders/ord-113/items?quantity=4");
        publishedRequest.Headers.Add("X-Correlation-Id", "corr-113");
        publishedRequest.Content = JsonContent.Create(new
        {
            note = "published"
        });

        var publishedResponse = await client.SendAsync(publishedRequest);
        publishedResponse.EnsureSuccessStatusCode();

        using var suppressedRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v7/tests/profile-runtime/selectors/secondary/orders/ord-114/items?quantity=5");
        suppressedRequest.Headers.Add("X-Correlation-Id", "corr-114");
        suppressedRequest.Content = JsonContent.Create(new
        {
            note = "suppressed"
        });

        var suppressedResponse = await client.SendAsync(suppressedRequest);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, suppressedResponse.StatusCode);
    }

    [Fact]
    public async Task MapCephalonAppliesEndpointNameOverrideSelectorsOnlyToTheMatchingCandidate()
    {
        const string moduleId = "tests.rest.profile-runtime.selectors";
        const string behaviorId = "tests.rest.profile.selector.bindings";
        const string primaryOriginalEndpointName = "tests_rest_profile_runtime_selectors.v6.tests_rest_profile_selector_bindings";
        const string secondaryOriginalEndpointName = "tests_rest_profile_runtime_selectors.v7.tests_rest_profile_selector_bindings";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "7";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:secondary-only:Modules:0"] = moduleId;
        builder.Configuration["RestApi:Overrides:secondary-only:EndpointNames:0"] = secondaryOriginalEndpointName;
        builder.Configuration["RestApi:Overrides:secondary-only:Pattern"] = "/lookup/original-endpoint-name/{orderId}/items";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileSelectorRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.Equal(2, endpoints.Length);
        Assert.Equal(2, candidates.Length);

        var primaryEndpoint = Assert.Single(endpoints, static item =>
            string.Equals(
                item.RoutePattern,
                "/api/v6/tests/profile-runtime/selectors/primary/orders/{orderId}/items",
                StringComparison.Ordinal));
        var secondaryEndpoint = Assert.Single(endpoints, static item =>
            string.Equals(
                item.RoutePattern,
                "/api/v7/tests/profile-runtime/selectors/secondary/orders/lookup/original-endpoint-name/{orderId}/items",
                StringComparison.Ordinal));

        Assert.Equal(primaryOriginalEndpointName, primaryEndpoint.OriginalEndpointName);
        Assert.Equal(secondaryOriginalEndpointName, secondaryEndpoint.OriginalEndpointName);

        var primaryCandidate = Assert.Single(candidates, static item =>
            string.Equals(
                item.ProjectedEndpoint.RoutePattern,
                "/api/v6/tests/profile-runtime/selectors/primary/orders/{orderId}/items",
                StringComparison.Ordinal));
        var secondaryCandidate = Assert.Single(candidates, static item =>
            string.Equals(
                item.ProjectedEndpoint.RoutePattern,
                "/api/v7/tests/profile-runtime/selectors/secondary/orders/lookup/original-endpoint-name/{orderId}/items",
                StringComparison.Ordinal));

        Assert.Equal(behaviorId, primaryCandidate.ProjectedEndpoint.BehaviorId);
        Assert.Equal(behaviorId, secondaryCandidate.ProjectedEndpoint.BehaviorId);
        Assert.Equal(primaryOriginalEndpointName, primaryCandidate.ProjectedEndpoint.OriginalEndpointName);
        Assert.Equal(secondaryOriginalEndpointName, secondaryCandidate.ProjectedEndpoint.OriginalEndpointName);
        Assert.Empty(primaryCandidate.MatchedOverrideIds);
        Assert.Null(primaryCandidate.AppliedOverrideId);
        Assert.Equal("secondary-only", secondaryCandidate.AppliedOverrideId);
        Assert.Equal(["secondary-only"], secondaryCandidate.MatchedOverrideIds);
        Assert.Equal([RestEndpointOverrideActionKind.Pattern], secondaryCandidate.SelectedOverrideActionKinds);
        Assert.Equal([RestEndpointOverrideActionKind.Pattern], secondaryCandidate.AppliedOverrideActionKinds);
        Assert.Equal(RestEndpointGovernanceRuleSelectionBasis.SingleMatch, secondaryCandidate.OverrideSelectionBasis);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "secondary-only", StringComparison.Ordinal));
        Assert.Contains(moduleId, rule.SourceModuleIds, StringComparer.Ordinal);
        Assert.Contains(secondaryOriginalEndpointName, rule.EndpointNames, StringComparer.Ordinal);
        Assert.Equal("/lookup/original-endpoint-name/{orderId}/items", rule.Pattern);
        Assert.Equal([secondaryCandidate.Id], rule.MatchedCandidateIds);
        Assert.Equal([secondaryCandidate.Id], rule.SelectedCandidateIds);
        Assert.Equal([secondaryCandidate.Id], rule.AppliedCandidateIds);
        Assert.Empty(rule.SkippedCandidateIds);
        Assert.Equal([RestEndpointGovernanceRuleSelectionBasis.SingleMatch], rule.SelectionBases);
        Assert.Equal([RestEndpointOverrideActionKind.Pattern], rule.SelectedActionKinds);
        Assert.Equal([RestEndpointOverrideActionKind.Pattern], rule.AppliedActionKinds);

        using var primaryRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/selectors/primary/orders/ord-115/items?quantity=4");
        primaryRequest.Headers.Add("X-Correlation-Id", "corr-115");
        primaryRequest.Content = JsonContent.Create(new
        {
            note = "primary"
        });

        var primaryResponse = await client.SendAsync(primaryRequest);
        primaryResponse.EnsureSuccessStatusCode();

        using var secondaryRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v7/tests/profile-runtime/selectors/secondary/orders/lookup/original-endpoint-name/ord-116/items?quantity=5");
        secondaryRequest.Headers.Add("X-Correlation-Id", "corr-116");
        secondaryRequest.Content = JsonContent.Create(new
        {
            note = "secondary"
        });

        var secondaryResponse = await client.SendAsync(secondaryRequest);
        secondaryResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task MapCephalonAppliesEndpointNameSuppressionSelectorsOnlyToTheMatchingCandidate()
    {
        const string moduleId = "tests.rest.profile-runtime.selectors";
        const string behaviorId = "tests.rest.profile.selector.bindings";
        const string primaryOriginalEndpointName = "tests_rest_profile_runtime_selectors.v6.tests_rest_profile_selector_bindings";
        const string secondaryOriginalEndpointName = "tests_rest_profile_runtime_selectors.v7.tests_rest_profile_selector_bindings";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "7";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Suppressions:hide-secondary-by-endpoint-name:Modules:0"] = moduleId;
        builder.Configuration["RestApi:Suppressions:hide-secondary-by-endpoint-name:EndpointNames:0"] = secondaryOriginalEndpointName;
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileSelectorRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var suppressions = await client.GetFromJsonAsync<RestEndpointSuppressionDescriptor[]>("/engine/rest-endpoint-suppressions");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(suppressions);

        var endpoint = Assert.Single(endpoints);
        Assert.Equal("/api/v6/tests/profile-runtime/selectors/primary/orders/{orderId}/items", endpoint.RoutePattern);
        Assert.Equal(primaryOriginalEndpointName, endpoint.OriginalEndpointName);

        var published = Assert.Single(candidates, static item => item.Status == RestEndpointCandidateStatus.Published);
        var suppressed = Assert.Single(candidates, static item => item.Status == RestEndpointCandidateStatus.Suppressed);

        Assert.Equal(behaviorId, published.ProjectedEndpoint.BehaviorId);
        Assert.Equal(behaviorId, suppressed.ProjectedEndpoint.BehaviorId);
        Assert.Equal(primaryOriginalEndpointName, published.ProjectedEndpoint.OriginalEndpointName);
        Assert.Equal(secondaryOriginalEndpointName, suppressed.ProjectedEndpoint.OriginalEndpointName);
        Assert.Equal("/api/v7/tests/profile-runtime/selectors/secondary/orders/{orderId}/items", suppressed.ProjectedEndpoint.RoutePattern);
        Assert.Equal("hide-secondary-by-endpoint-name", suppressed.SuppressedBySuppressionId);
        Assert.Equal(["hide-secondary-by-endpoint-name"], suppressed.MatchedSuppressionIds);
        Assert.Equal(RestEndpointGovernanceRuleSelectionBasis.SingleMatch, suppressed.SuppressionSelectionBasis);

        var rule = Assert.Single(suppressions, static item => string.Equals(item.Id, "hide-secondary-by-endpoint-name", StringComparison.Ordinal));
        Assert.Contains(moduleId, rule.SourceModuleIds, StringComparer.Ordinal);
        Assert.Contains(secondaryOriginalEndpointName, rule.EndpointNames, StringComparer.Ordinal);
        Assert.Equal([suppressed.Id], rule.MatchedCandidateIds);
        Assert.Equal([suppressed.Id], rule.SuppressedCandidateIds);
        Assert.Empty(rule.SkippedCandidateIds);
        Assert.Equal([RestEndpointGovernanceRuleSelectionBasis.SingleMatch], rule.SelectionBases);

        using var publishedRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/selectors/primary/orders/ord-117/items?quantity=4");
        publishedRequest.Headers.Add("X-Correlation-Id", "corr-117");
        publishedRequest.Content = JsonContent.Create(new
        {
            note = "published"
        });

        var publishedResponse = await client.SendAsync(publishedRequest);
        publishedResponse.EnsureSuccessStatusCode();

        using var suppressedRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v7/tests/profile-runtime/selectors/secondary/orders/ord-118/items?quantity=5");
        suppressedRequest.Headers.Add("X-Correlation-Id", "corr-118");
        suppressedRequest.Content = JsonContent.Create(new
        {
            note = "suppressed"
        });

        var suppressedResponse = await client.SendAsync(suppressedRequest);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, suppressedResponse.StatusCode);
    }

    [Fact]
    public async Task MapCephalonAppliesHostGovernanceScopeOverrideSelectorsOnlyToTheMatchingCandidate()
    {
        const string behaviorId = "tests.rest.profile.selector.bindings";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "7";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:secondary-scope-only:HostGovernanceScopes:0"] = "secondary-scope";
        builder.Configuration["RestApi:Overrides:secondary-scope-only:Pattern"] = "/lookup/host-governance-scope/{orderId}/items";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileSelectorRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.Equal(2, endpoints.Length);
        Assert.Equal(2, candidates.Length);

        var primaryEndpoint = Assert.Single(endpoints, static item =>
            string.Equals(
                item.RoutePattern,
                "/api/v6/tests/profile-runtime/selectors/primary/orders/{orderId}/items",
                StringComparison.Ordinal));
        var secondaryEndpoint = Assert.Single(endpoints, static item =>
            string.Equals(
                item.RoutePattern,
                "/api/v7/tests/profile-runtime/selectors/secondary/orders/lookup/host-governance-scope/{orderId}/items",
                StringComparison.Ordinal));

        Assert.NotNull(primaryEndpoint.OriginalProjection);
        Assert.NotNull(secondaryEndpoint.OriginalProjection);
        Assert.Equal("primary-scope", primaryEndpoint.OriginalProjection!.HostGovernanceScope);
        Assert.Equal("secondary-scope", secondaryEndpoint.OriginalProjection!.HostGovernanceScope);

        var primaryCandidate = Assert.Single(candidates, static item =>
            string.Equals(
                item.ProjectedEndpoint.RoutePattern,
                "/api/v6/tests/profile-runtime/selectors/primary/orders/{orderId}/items",
                StringComparison.Ordinal));
        var secondaryCandidate = Assert.Single(candidates, static item =>
            string.Equals(
                item.ProjectedEndpoint.RoutePattern,
                "/api/v7/tests/profile-runtime/selectors/secondary/orders/lookup/host-governance-scope/{orderId}/items",
                StringComparison.Ordinal));

        Assert.Equal(behaviorId, primaryCandidate.ProjectedEndpoint.BehaviorId);
        Assert.Equal(behaviorId, secondaryCandidate.ProjectedEndpoint.BehaviorId);
        Assert.Equal("primary-scope", primaryCandidate.OriginalProjection.HostGovernanceScope);
        Assert.Equal("secondary-scope", secondaryCandidate.OriginalProjection.HostGovernanceScope);
        Assert.Empty(primaryCandidate.MatchedOverrideIds);
        Assert.Null(primaryCandidate.AppliedOverrideId);
        Assert.Equal("secondary-scope-only", secondaryCandidate.AppliedOverrideId);
        Assert.Equal(["secondary-scope-only"], secondaryCandidate.MatchedOverrideIds);
        Assert.Equal([RestEndpointOverrideActionKind.Pattern], secondaryCandidate.SelectedOverrideActionKinds);
        Assert.Equal([RestEndpointOverrideActionKind.Pattern], secondaryCandidate.AppliedOverrideActionKinds);
        Assert.Equal(RestEndpointGovernanceRuleSelectionBasis.SingleMatch, secondaryCandidate.OverrideSelectionBasis);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "secondary-scope-only", StringComparison.Ordinal));
        Assert.Empty(rule.SourceModuleIds);
        Assert.Contains("secondary-scope", rule.HostGovernanceScopes, StringComparer.Ordinal);
        Assert.Equal("/lookup/host-governance-scope/{orderId}/items", rule.Pattern);
        Assert.Equal([secondaryCandidate.Id], rule.MatchedCandidateIds);
        Assert.Equal([secondaryCandidate.Id], rule.SelectedCandidateIds);
        Assert.Equal([secondaryCandidate.Id], rule.AppliedCandidateIds);
        Assert.Empty(rule.SkippedCandidateIds);
        Assert.Equal([RestEndpointGovernanceRuleSelectionBasis.SingleMatch], rule.SelectionBases);
        Assert.Equal([RestEndpointOverrideActionKind.Pattern], rule.SelectedActionKinds);
        Assert.Equal([RestEndpointOverrideActionKind.Pattern], rule.AppliedActionKinds);

        using var primaryRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/selectors/primary/orders/ord-119/items?quantity=4");
        primaryRequest.Headers.Add("X-Correlation-Id", "corr-119");
        primaryRequest.Content = JsonContent.Create(new
        {
            note = "primary"
        });

        var primaryResponse = await client.SendAsync(primaryRequest);
        primaryResponse.EnsureSuccessStatusCode();

        using var secondaryRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v7/tests/profile-runtime/selectors/secondary/orders/lookup/host-governance-scope/ord-120/items?quantity=5");
        secondaryRequest.Headers.Add("X-Correlation-Id", "corr-120");
        secondaryRequest.Content = JsonContent.Create(new
        {
            note = "secondary"
        });

        var secondaryResponse = await client.SendAsync(secondaryRequest);
        secondaryResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task MapCephalonAppliesHostGovernanceScopeSuppressionSelectorsOnlyToTheMatchingCandidate()
    {
        const string behaviorId = "tests.rest.profile.selector.bindings";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "7";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Suppressions:hide-secondary-by-scope:HostGovernanceScopes:0"] = "secondary-scope";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileSelectorRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var suppressions = await client.GetFromJsonAsync<RestEndpointSuppressionDescriptor[]>("/engine/rest-endpoint-suppressions");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(suppressions);

        var endpoint = Assert.Single(endpoints);
        Assert.Equal("/api/v6/tests/profile-runtime/selectors/primary/orders/{orderId}/items", endpoint.RoutePattern);
        Assert.NotNull(endpoint.OriginalProjection);
        Assert.Equal("primary-scope", endpoint.OriginalProjection!.HostGovernanceScope);

        var published = Assert.Single(candidates, static item => item.Status == RestEndpointCandidateStatus.Published);
        var suppressed = Assert.Single(candidates, static item => item.Status == RestEndpointCandidateStatus.Suppressed);

        Assert.Equal(behaviorId, published.ProjectedEndpoint.BehaviorId);
        Assert.Equal(behaviorId, suppressed.ProjectedEndpoint.BehaviorId);
        Assert.Equal("primary-scope", published.OriginalProjection.HostGovernanceScope);
        Assert.Equal("secondary-scope", suppressed.OriginalProjection.HostGovernanceScope);
        Assert.Equal("/api/v7/tests/profile-runtime/selectors/secondary/orders/{orderId}/items", suppressed.ProjectedEndpoint.RoutePattern);
        Assert.Equal("hide-secondary-by-scope", suppressed.SuppressedBySuppressionId);
        Assert.Equal(["hide-secondary-by-scope"], suppressed.MatchedSuppressionIds);
        Assert.Equal(RestEndpointGovernanceRuleSelectionBasis.SingleMatch, suppressed.SuppressionSelectionBasis);

        var rule = Assert.Single(suppressions, static item => string.Equals(item.Id, "hide-secondary-by-scope", StringComparison.Ordinal));
        Assert.Empty(rule.SourceModuleIds);
        Assert.Contains("secondary-scope", rule.HostGovernanceScopes, StringComparer.Ordinal);
        Assert.Equal([suppressed.Id], rule.MatchedCandidateIds);
        Assert.Equal([suppressed.Id], rule.SuppressedCandidateIds);
        Assert.Empty(rule.SkippedCandidateIds);
        Assert.Equal([RestEndpointGovernanceRuleSelectionBasis.SingleMatch], rule.SelectionBases);

        using var publishedRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/selectors/primary/orders/ord-121/items?quantity=4");
        publishedRequest.Headers.Add("X-Correlation-Id", "corr-121");
        publishedRequest.Content = JsonContent.Create(new
        {
            note = "published"
        });

        var publishedResponse = await client.SendAsync(publishedRequest);
        publishedResponse.EnsureSuccessStatusCode();

        using var suppressedRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v7/tests/profile-runtime/selectors/secondary/orders/ord-122/items?quantity=5");
        suppressedRequest.Headers.Add("X-Correlation-Id", "corr-122");
        suppressedRequest.Content = JsonContent.Create(new
        {
            note = "suppressed"
        });

        var suppressedResponse = await client.SendAsync(suppressedRequest);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, suppressedResponse.StatusCode);
    }

    [Fact]
    public async Task MapCephalonAppliesBindingFallbackOverrideSelectorsOnlyToTheMatchingCandidate()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:body-fallback-only:Modules:0"] = "tests.rest.profile-runtime.binding-fallback-selectors";
        builder.Configuration["RestApi:Overrides:body-fallback-only:BindingFallbackModes:0"] = "preserve-remaining-body-fallback";
        builder.Configuration["RestApi:Overrides:body-fallback-only:Pattern"] = "/lookup/{orderId}/items";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new BindingFallbackSelectorRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.Equal(2, endpoints.Length);
        Assert.Equal(2, candidates.Length);

        var writeEndpoint = Assert.Single(endpoints, static item =>
            string.Equals(
                item.RoutePattern,
                "/api/v6/tests/profile-runtime/binding-fallback-selectors/write/orders/lookup/{orderId}/items",
                StringComparison.Ordinal));
        var readEndpoint = Assert.Single(endpoints, static item =>
            string.Equals(
                item.RoutePattern,
                "/api/v6/tests/profile-runtime/binding-fallback-selectors/read/orders/{orderId}",
                StringComparison.Ordinal));

        Assert.Equal("tests.rest.profile.bindings", writeEndpoint.BehaviorId);
        Assert.Equal("tests.rest.profile.bindings.get", readEndpoint.BehaviorId);
        Assert.Equal("body-fallback-only", writeEndpoint.AppliedOverrideId);
        Assert.Null(readEndpoint.AppliedOverrideId);

        var writeCandidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        var readCandidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.get", StringComparison.Ordinal));

        Assert.Equal("body-fallback-only", writeCandidate.AppliedOverrideId);
        Assert.Equal(["body-fallback-only"], writeCandidate.MatchedOverrideIds);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback,
            writeCandidate.OriginalProjection.BindingFallbackMode);

        Assert.Null(readCandidate.AppliedOverrideId);
        Assert.Empty(readCandidate.MatchedOverrideIds);
        Assert.Null(readCandidate.OriginalProjection.BindingFallbackMode);

        var rule = Assert.Single(overrides, static item =>
            string.Equals(item.Id, "body-fallback-only", StringComparison.Ordinal));
        Assert.Contains(
            RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback,
            rule.BindingFallbackModes);
        Assert.Equal("/lookup/{orderId}/items", rule.Pattern);

        using var writeRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/binding-fallback-selectors/write/orders/lookup/ord-121/items?quantity=2");
        writeRequest.Headers.Add("X-Correlation-Id", "corr-121");
        writeRequest.Content = JsonContent.Create(new
        {
            note = "write"
        });

        var writeResponse = await client.SendAsync(writeRequest);
        writeResponse.EnsureSuccessStatusCode();

        var readResponse = await client.GetAsync("/api/v6/tests/profile-runtime/binding-fallback-selectors/read/orders/ord-122");
        readResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task MapCephalonAppliesBindingFallbackSuppressionSelectorsOnlyToTheMatchingCandidate()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Suppressions:hide-body-fallback:Modules:0"] = "tests.rest.profile-runtime.binding-fallback-selectors";
        builder.Configuration["RestApi:Suppressions:hide-body-fallback:BindingFallbackModes:0"] = "preserve-remaining-body-fallback";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new BindingFallbackSelectorRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var suppressions = await client.GetFromJsonAsync<RestEndpointSuppressionDescriptor[]>("/engine/rest-endpoint-suppressions");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(suppressions);

        var endpoint = Assert.Single(endpoints);
        Assert.Equal("/api/v6/tests/profile-runtime/binding-fallback-selectors/read/orders/{orderId}", endpoint.RoutePattern);

        var published = Assert.Single(candidates, static item => item.Status == RestEndpointCandidateStatus.Published);
        Assert.Equal("tests.rest.profile.bindings.get", published.ProjectedEndpoint.BehaviorId);
        Assert.Empty(published.MatchedSuppressionIds);

        var suppressed = Assert.Single(candidates, static item => item.Status == RestEndpointCandidateStatus.Suppressed);
        Assert.Equal("tests.rest.profile.bindings", suppressed.ProjectedEndpoint.BehaviorId);
        Assert.Equal("hide-body-fallback", suppressed.SuppressedBySuppressionId);
        Assert.Equal(["hide-body-fallback"], suppressed.MatchedSuppressionIds);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback,
            suppressed.OriginalProjection.BindingFallbackMode);

        var rule = Assert.Single(suppressions, static item =>
            string.Equals(item.Id, "hide-body-fallback", StringComparison.Ordinal));
        Assert.Contains(
            RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback,
            rule.BindingFallbackModes);

        var readResponse = await client.GetAsync("/api/v6/tests/profile-runtime/binding-fallback-selectors/read/orders/ord-123");
        readResponse.EnsureSuccessStatusCode();

        using var suppressedRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/binding-fallback-selectors/write/orders/ord-124/items?quantity=3");
        suppressedRequest.Headers.Add("X-Correlation-Id", "corr-124");
        suppressedRequest.Content = JsonContent.Create(new
        {
            note = "suppressed"
        });

        var suppressedResponse = await client.SendAsync(suppressedRequest);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, suppressedResponse.StatusCode);
    }

    [Fact]
    public void MapCephalonRejectsBindingFallbackOverrideSelectorsThatUseEnumMemberNames()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["RestApi:Overrides:body-fallback-only:Modules:0"] = "tests.rest.profile-runtime.binding-fallback-selectors";
        builder.Configuration["RestApi:Overrides:body-fallback-only:BindingFallbackModes:0"] = "PreserveRemainingBodyFallback";
        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddCephalon(engine =>
        {
            engine.AddModule(new BindingFallbackSelectorRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        }));

        Assert.Contains("RestApi:Overrides:body-fallback-only:BindingFallbackModes:0", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("stable binding fallback mode wire names", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback.GetWireName(), exception.Message, StringComparison.Ordinal);
        Assert.Contains(RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback.GetWireName(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MapCephalonRejectsBindingFallbackSuppressionSelectorsThatUseEnumMemberNames()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["RestApi:Suppressions:hide-body-fallback:Modules:0"] = "tests.rest.profile-runtime.binding-fallback-selectors";
        builder.Configuration["RestApi:Suppressions:hide-body-fallback:BindingFallbackModes:0"] = "PreserveRemainingBodyFallback";
        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddCephalon(engine =>
        {
            engine.AddModule(new BindingFallbackSelectorRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        }));

        Assert.Contains("RestApi:Suppressions:hide-body-fallback:BindingFallbackModes:0", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("stable binding fallback mode wire names", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback.GetWireName(), exception.Message, StringComparison.Ordinal);
        Assert.Contains(RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback.GetWireName(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MapCephalonRejectsOverrideBindingModeValuesThatUseEnumMemberNames()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-merge-route-quantity:Behaviors:0"] = "tests.rest.profile.bindings";
        builder.Configuration["RestApi:Overrides:prefer-merge-route-quantity:Pattern"] = "/lookup/{orderId}/items/{quantity}";
        builder.Configuration["RestApi:Overrides:prefer-merge-route-quantity:BindingMode"] = "MergeExplicit";
        builder.Configuration["RestApi:Overrides:prefer-merge-route-quantity:Bindings:0:PropertyName"] = "Quantity";
        builder.Configuration["RestApi:Overrides:prefer-merge-route-quantity:Bindings:0:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-merge-route-quantity:Bindings:0:Name"] = "quantity";
        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        }));

        Assert.Contains("RestApi:Overrides:prefer-merge-route-quantity:BindingMode", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("stable binding mode wire names", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(RestEndpointOverrideBindingMode.ReplaceExplicit.GetWireName(), exception.Message, StringComparison.Ordinal);
        Assert.Contains(RestEndpointOverrideBindingMode.MergeExplicit.GetWireName(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MapCephalonRejectsBindingSourceValuesThatUseEnumMemberNames()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Behaviors:0"] = "tests.rest.profile.bindings";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:0:PropertyName"] = "Quantity";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:0:Source"] = "Query";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:0:Name"] = "qty";

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        }));

        Assert.Contains("RestApi:Overrides:prefer-short-bindings:Bindings:0:Source", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("stable binding source wire names", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(RestEndpointBindingSource.Route.GetWireName(), exception.Message, StringComparison.Ordinal);
        Assert.Contains(RestEndpointBindingSource.Query.GetWireName(), exception.Message, StringComparison.Ordinal);
        Assert.Contains(RestEndpointBindingSource.Header.GetWireName(), exception.Message, StringComparison.Ordinal);
        Assert.Contains(RestEndpointBindingSource.Body.GetWireName(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonAppliesTargetBindingOverrideSelectorsOnlyToTheMatchingCandidate()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:route-only-target:Modules:0"] = "tests.rest.profile-runtime.binding-fallback-selectors";
        builder.Configuration["RestApi:Overrides:route-only-target:TargetBindings:0:PropertyName"] = "OrderId";
        builder.Configuration["RestApi:Overrides:route-only-target:TargetBindings:0:Source"] = "route";
        builder.Configuration["RestApi:Overrides:route-only-target:TargetBindings:0:Name"] = "orderId";
        builder.Configuration["RestApi:Overrides:route-only-target:Pattern"] = "/lookup/{orderId}";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new BindingFallbackSelectorRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var overrideById = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor>("/engine/rest-endpoint-overrides/route-only-target");
        var rawOverrides = await client.GetStringAsync("/engine/rest-endpoint-overrides");
        var rawOverride = await client.GetStringAsync("/engine/rest-endpoint-overrides/route-only-target");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(overrideById);
        Assert.Equal(2, endpoints.Length);
        Assert.Equal(2, candidates.Length);
        Assert.Contains("\"source\":\"route\"", rawOverrides, StringComparison.Ordinal);
        Assert.Contains("\"source\":\"route\"", rawOverride, StringComparison.Ordinal);

        var writeEndpoint = Assert.Single(endpoints, static item =>
            string.Equals(
                item.RoutePattern,
                "/api/v6/tests/profile-runtime/binding-fallback-selectors/write/orders/{orderId}",
                StringComparison.Ordinal));
        var readEndpoint = Assert.Single(endpoints, static item =>
            string.Equals(
                item.RoutePattern,
                "/api/v6/tests/profile-runtime/binding-fallback-selectors/read/orders/lookup/{orderId}",
                StringComparison.Ordinal));

        Assert.Null(writeEndpoint.AppliedOverrideId);
        Assert.Equal("route-only-target", readEndpoint.AppliedOverrideId);

        var writeCandidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        var readCandidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.get", StringComparison.Ordinal));

        Assert.Null(writeCandidate.AppliedOverrideId);
        Assert.Empty(writeCandidate.MatchedOverrideIds);
        Assert.Equal("route-only-target", readCandidate.AppliedOverrideId);
        Assert.Equal(["route-only-target"], readCandidate.MatchedOverrideIds);
        Assert.Single(readCandidate.OriginalProjection.BindingDescriptors);

        var rule = Assert.Single(overrides, static item =>
            string.Equals(item.Id, "route-only-target", StringComparison.Ordinal));
        Assert.Equal("/lookup/{orderId}", rule.Pattern);
        Assert.Single(rule.TargetBindings);
        Assert.Contains(rule.TargetBindings, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "orderId");
        Assert.Equal(rule.Id, overrideById.Id);
        Assert.Equal(rule.Pattern, overrideById.Pattern);
        Assert.Single(overrideById.TargetBindings);
        Assert.Equal(RestEndpointBindingSource.Route, overrideById.TargetBindings[0].Source);

        using var writeRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/binding-fallback-selectors/write/orders/ord-125?quantity=3");
        writeRequest.Headers.Add("X-Correlation-Id", "corr-125");
        writeRequest.Content = JsonContent.Create(new
        {
            note = "write"
        });

        var writeResponse = await client.SendAsync(writeRequest);
        writeResponse.EnsureSuccessStatusCode();

        var readResponse = await client.GetAsync("/api/v6/tests/profile-runtime/binding-fallback-selectors/read/orders/lookup/ord-126");
        readResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task MapCephalonAppliesTargetBindingSuppressionSelectorsOnlyToTheMatchingCandidate()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Suppressions:hide-route-only:Modules:0"] = "tests.rest.profile-runtime.binding-fallback-selectors";
        builder.Configuration["RestApi:Suppressions:hide-route-only:TargetBindings:0:PropertyName"] = "OrderId";
        builder.Configuration["RestApi:Suppressions:hide-route-only:TargetBindings:0:Source"] = "route";
        builder.Configuration["RestApi:Suppressions:hide-route-only:TargetBindings:0:Name"] = "orderId";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new BindingFallbackSelectorRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var suppressions = await client.GetFromJsonAsync<RestEndpointSuppressionDescriptor[]>("/engine/rest-endpoint-suppressions");
        var suppressionById = await client.GetFromJsonAsync<RestEndpointSuppressionDescriptor>("/engine/rest-endpoint-suppressions/hide-route-only");
        var rawSuppressions = await client.GetStringAsync("/engine/rest-endpoint-suppressions");
        var rawSuppression = await client.GetStringAsync("/engine/rest-endpoint-suppressions/hide-route-only");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(suppressions);
        Assert.NotNull(suppressionById);
        Assert.Contains("\"source\":\"route\"", rawSuppressions, StringComparison.Ordinal);
        Assert.Contains("\"source\":\"route\"", rawSuppression, StringComparison.Ordinal);

        var endpoint = Assert.Single(endpoints);
        Assert.Equal("/api/v6/tests/profile-runtime/binding-fallback-selectors/write/orders/{orderId}", endpoint.RoutePattern);

        var published = Assert.Single(candidates, static item => item.Status == RestEndpointCandidateStatus.Published);
        Assert.Equal("tests.rest.profile.bindings", published.ProjectedEndpoint.BehaviorId);
        Assert.Empty(published.MatchedSuppressionIds);

        var suppressed = Assert.Single(candidates, static item => item.Status == RestEndpointCandidateStatus.Suppressed);
        Assert.Equal("tests.rest.profile.bindings.get", suppressed.ProjectedEndpoint.BehaviorId);
        Assert.Equal("hide-route-only", suppressed.SuppressedBySuppressionId);
        Assert.Equal(["hide-route-only"], suppressed.MatchedSuppressionIds);
        Assert.Single(suppressed.OriginalProjection.BindingDescriptors);

        var rule = Assert.Single(suppressions, static item =>
            string.Equals(item.Id, "hide-route-only", StringComparison.Ordinal));
        Assert.Single(rule.TargetBindings);
        Assert.Contains(rule.TargetBindings, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "orderId");
        Assert.Equal(rule.Id, suppressionById.Id);
        Assert.Single(suppressionById.TargetBindings);
        Assert.Equal(RestEndpointBindingSource.Route, suppressionById.TargetBindings[0].Source);

        using var writeRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/binding-fallback-selectors/write/orders/ord-127?quantity=4");
        writeRequest.Headers.Add("X-Correlation-Id", "corr-127");
        writeRequest.Content = JsonContent.Create(new
        {
            note = "write"
        });

        var writeResponse = await client.SendAsync(writeRequest);
        writeResponse.EnsureSuccessStatusCode();

        var suppressedResponse = await client.GetAsync("/api/v6/tests/profile-runtime/binding-fallback-selectors/read/orders/ord-128");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, suppressedResponse.StatusCode);
    }

    [Fact]
    public async Task MapCephalonAppliesCandidateIdOverrideSelectorsUsingStableCandidateIds()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "7";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";

        var secondaryCandidateId = BuildBehaviorProjectionCandidateId(
            "tests.rest.profile-runtime.selectors",
            "tests.rest.profile.selector.bindings",
            RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle,
            "POST",
            "/api/v7/tests/profile-runtime/selectors/secondary/orders/{orderId}/items");
        builder.Configuration["RestApi:Overrides:secondary-only:CandidateIds:0"] = secondaryCandidateId;
        builder.Configuration["RestApi:Overrides:secondary-only:Pattern"] = "/lookup/{orderId}/items";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileSelectorRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.Equal(2, endpoints.Length);
        Assert.Equal(2, candidates.Length);

        var primaryCandidate = Assert.Single(candidates, static item =>
            string.Equals(
                item.OriginalProjection.RoutePattern,
                "/api/v6/tests/profile-runtime/selectors/primary/orders/{orderId}/items",
                StringComparison.Ordinal));
        var secondaryCandidate = Assert.Single(candidates, static item =>
            string.Equals(
                item.OriginalProjection.RoutePattern,
                "/api/v7/tests/profile-runtime/selectors/secondary/orders/{orderId}/items",
                StringComparison.Ordinal));

        Assert.Null(primaryCandidate.AppliedOverrideId);
        Assert.Equal("/api/v6/tests/profile-runtime/selectors/primary/orders/{orderId}/items", primaryCandidate.ProjectedEndpoint.RoutePattern);
        Assert.Equal(secondaryCandidateId, secondaryCandidate.Id);
        Assert.Equal("secondary-only", secondaryCandidate.AppliedOverrideId);
        Assert.Equal("/api/v7/tests/profile-runtime/selectors/secondary/orders/lookup/{orderId}/items", secondaryCandidate.ProjectedEndpoint.RoutePattern);

        var primaryEndpoint = Assert.Single(endpoints, static item =>
            string.Equals(
                item.RoutePattern,
                "/api/v6/tests/profile-runtime/selectors/primary/orders/{orderId}/items",
                StringComparison.Ordinal));
        var secondaryEndpoint = Assert.Single(endpoints, static item =>
            string.Equals(
                item.RoutePattern,
                "/api/v7/tests/profile-runtime/selectors/secondary/orders/lookup/{orderId}/items",
                StringComparison.Ordinal));
        Assert.Equal("tests.rest.profile.selector.bindings", primaryEndpoint.BehaviorId);
        Assert.Equal("tests.rest.profile.selector.bindings", secondaryEndpoint.BehaviorId);

        var rule = Assert.Single(overrides, static item => string.Equals(item.Id, "secondary-only", StringComparison.Ordinal));
        Assert.Contains(secondaryCandidateId, rule.CandidateIds, StringComparer.Ordinal);
        Assert.Equal("/lookup/{orderId}/items", rule.Pattern);

        using var primaryRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/selectors/primary/orders/ord-95/items?quantity=2");
        primaryRequest.Headers.Add("X-Correlation-Id", "corr-95");
        primaryRequest.Content = JsonContent.Create(new
        {
            note = "primary"
        });

        var primaryResponse = await client.SendAsync(primaryRequest);
        primaryResponse.EnsureSuccessStatusCode();

        using var secondaryRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v7/tests/profile-runtime/selectors/secondary/orders/lookup/ord-96/items?quantity=3");
        secondaryRequest.Headers.Add("X-Correlation-Id", "corr-96");
        secondaryRequest.Content = JsonContent.Create(new
        {
            note = "secondary"
        });

        var secondaryResponse = await client.SendAsync(secondaryRequest);
        secondaryResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task MapCephalonAppliesCandidateIdSuppressionSelectorsUsingStableCandidateIds()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "7";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";

        var secondaryCandidateId = BuildBehaviorProjectionCandidateId(
            "tests.rest.profile-runtime.selectors",
            "tests.rest.profile.selector.bindings",
            RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle,
            "POST",
            "/api/v7/tests/profile-runtime/selectors/secondary/orders/{orderId}/items");
        builder.Configuration["RestApi:Suppressions:hide-secondary-only:CandidateIds:0"] = secondaryCandidateId;
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileSelectorRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var suppressions = await client.GetFromJsonAsync<RestEndpointSuppressionDescriptor[]>("/engine/rest-endpoint-suppressions");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(suppressions);

        var endpoint = Assert.Single(endpoints);
        Assert.Equal("/api/v6/tests/profile-runtime/selectors/primary/orders/{orderId}/items", endpoint.RoutePattern);

        var published = Assert.Single(candidates, static item => item.Status == RestEndpointCandidateStatus.Published);
        Assert.Equal("/api/v6/tests/profile-runtime/selectors/primary/orders/{orderId}/items", published.ProjectedEndpoint.RoutePattern);

        var suppressed = Assert.Single(candidates, static item => item.Status == RestEndpointCandidateStatus.Suppressed);
        Assert.Equal(secondaryCandidateId, suppressed.Id);
        Assert.Equal("/api/v7/tests/profile-runtime/selectors/secondary/orders/{orderId}/items", suppressed.ProjectedEndpoint.RoutePattern);
        Assert.Equal("hide-secondary-only", suppressed.SuppressedBySuppressionId);

        var rule = Assert.Single(suppressions, static item => string.Equals(item.Id, "hide-secondary-only", StringComparison.Ordinal));
        Assert.Contains(secondaryCandidateId, rule.CandidateIds, StringComparer.Ordinal);

        using var publishedRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/selectors/primary/orders/ord-97/items?quantity=4");
        publishedRequest.Headers.Add("X-Correlation-Id", "corr-97");
        publishedRequest.Content = JsonContent.Create(new
        {
            note = "published"
        });

        var publishedResponse = await client.SendAsync(publishedRequest);
        publishedResponse.EnsureSuccessStatusCode();

        using var suppressedRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v7/tests/profile-runtime/selectors/secondary/orders/ord-98/items?quantity=5");
        suppressedRequest.Headers.Add("X-Correlation-Id", "corr-98");
        suppressedRequest.Content = JsonContent.Create(new
        {
            note = "suppressed"
        });

        var suppressedResponse = await client.SendAsync(suppressedRequest);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, suppressedResponse.StatusCode);
    }

    [Fact]
    public void AddCephalonRejectsRestApiSuppressionRulesWithoutPrimaryTargetsOrGovernanceScopes()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["RestApi:Suppressions:invalid:AuthoringStyles:0"] = RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle;

        var exception = Assert.Throws<ArgumentException>(() =>
            builder.AddCephalon(engine =>
            {
                engine.AddModule(new GeneratedRuntimeCatalogModule());
                engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
                {
                    behaviors.AddHttpBehaviorBindings();
                });
            }));

        Assert.Contains("candidate id, behavior id, behavior-id prefix, source module id, or host-governance scope", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddCephalonRejectsRestApiOverrideRulesWithoutOverrideActions()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["RestApi:Overrides:invalid:Behaviors:0"] = "tests.generated.runtimeoverride.lookup";

        var exception = Assert.Throws<ArgumentException>(() =>
            builder.AddCephalon(engine =>
            {
                engine.AddModule(new GeneratedVersionOverrideRuntimeCatalogModule());
                engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
                {
                    behaviors.AddHttpBehaviorBindings();
                });
            }));

        Assert.Contains("override action", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MapCephalonRejectsRestPatternOverridesThatChangeRoutePlaceholderSet()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Behaviors:0"] = "tests.generated.runtimeoverride.lookup";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Pattern"] = "/lookup/{id}";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedVersionOverrideRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        using var app = builder.Build();
        var exception = Assert.Throws<InvalidOperationException>(() => app.MapCephalon());

        Assert.Contains("explicit route-binding plan", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MapCephalonRejectsPlaceholderAdditionWhenEffectiveRouteCoverageIsIncomplete()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Behaviors:0"] = "tests.rest.profile.bindings";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Pattern"] = "/lookup/{orderId}/items/{quantity}";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:0:PropertyName"] = "OrderId";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:0:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:0:Name"] = "orderId";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:1:PropertyName"] = "CorrelationId";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:1:Source"] = "header";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:1:Name"] = "X-Correlation-Id";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:2:PropertyName"] = "Note";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:2:Source"] = "body";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:2:Name"] = "note";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        using var app = builder.Build();
        var exception = Assert.Throws<InvalidOperationException>(() => app.MapCephalon());

        Assert.Contains("adding placeholders", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("explicit route-binding plan", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MapCephalonRejectsPlaceholderAdditionWhenNewlyRouteBoundPropertiesWereNotExplicitOriginally()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Behaviors:0"] = "tests.rest.profile.bindings.inference";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Pattern"] = "/lookup/{orderId}/items/{quantity}";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:0:PropertyName"] = "OrderId";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:0:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:0:Name"] = "orderId";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:1:PropertyName"] = "Quantity";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:1:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:1:Name"] = "quantity";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:2:PropertyName"] = "CorrelationId";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:2:Source"] = "header";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:2:Name"] = "X-Correlation-Id";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:3:PropertyName"] = "Note";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:3:Source"] = "body";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:3:Name"] = "note";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingInferenceRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        using var app = builder.Build();
        var exception = Assert.Throws<InvalidOperationException>(() => app.MapCephalon());

        Assert.Contains("newly route-bound property", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("original projection", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MapCephalonRejectsImplicitBodyFallbackPromotionWhenOriginalProjectionDidNotAcceptBody()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Behaviors:0"] = "tests.rest.profile.bindings.get";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Pattern"] = "/lookup/{orderId}/{ignored}";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:0:PropertyName"] = "OrderId";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:0:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:0:Name"] = "orderId";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:1:PropertyName"] = "Ignored";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:1:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:1:Name"] = "ignored";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingGetRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        using var app = builder.Build();
        var exception = Assert.Throws<InvalidOperationException>(() => app.MapCephalon());

        Assert.Contains("deterministic implicit fallback surface", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("newly route-bound property", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MapCephalonAllowsImplicitQueryFallbackPromotionIntoAddedPlaceholderWhenOriginalProfileHadNoExplicitBindings()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-route-query:Behaviors:0"] = "tests.rest.profile.bindings.query";
        builder.Configuration["RestApi:Overrides:prefer-route-query:Pattern"] = "/lookup/{orderId}/{ignored}";
        builder.Configuration["RestApi:Overrides:prefer-route-query:Bindings:0:PropertyName"] = "OrderId";
        builder.Configuration["RestApi:Overrides:prefer-route-query:Bindings:0:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-route-query:Bindings:0:Name"] = "orderId";
        builder.Configuration["RestApi:Overrides:prefer-route-query:Bindings:1:PropertyName"] = "Ignored";
        builder.Configuration["RestApi:Overrides:prefer-route-query:Bindings:1:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-route-query:Bindings:1:Name"] = "ignored";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingQueryFallbackRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings.query", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile-runtime/query/orders/lookup/{orderId}/{ignored}", endpoint.RoutePattern);
        Assert.Equal("v6", endpoint.OpenApiDocumentName);
        Assert.Equal(6, endpoint.ApiVersionMajor);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, endpoint.Metadata["authoringStyle"]);
        Assert.Equal("/api/v6/tests/profile-runtime/query/orders", endpoint.RouteGroupPrefix);
        Assert.Equal("/lookup/{orderId}/{ignored}", endpoint.RelativePattern);
        Assert.Equal(2, endpoint.BindingDescriptors.Count);
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "orderId");
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "Ignored" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "ignored");

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.query", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-route-query", candidate.AppliedOverrideId);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);
        Assert.Empty(candidate.OriginalProjection.BindingDescriptors);
        Assert.Equal("/api/v6/tests/profile-runtime/query/orders/lookup/{orderId}/{ignored}", candidate.ProjectedEndpoint.RoutePattern);

        var rule = Assert.Single(overrides, static item =>
            string.Equals(item.Id, "prefer-route-query", StringComparison.Ordinal));
        Assert.Equal("/lookup/{orderId}/{ignored}", rule.Pattern);
        Assert.Equal(2, rule.Bindings.Count);
        Assert.Contains(rule.Bindings, static binding =>
            binding.PropertyName == "Ignored" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "ignored");

        var response = await client.GetAsync("/api/v6/tests/profile-runtime/query/orders/lookup/ord-79/route-query");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProfileBindingGetRuntimeOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-79", payload.OrderId);
        Assert.Equal("route-query", payload.Ignored);

        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");
        Assert.NotNull(snapshot);

        var snapshotEndpoint = Assert.Single(snapshot.RestEndpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings.query", StringComparison.Ordinal));
        Assert.Equal(endpoint.Id, snapshotEndpoint.Id);

        var snapshotCandidate = Assert.Single(snapshot.RestEndpointCandidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.query", StringComparison.Ordinal));
        Assert.Equal(candidate.Id, snapshotCandidate.Id);
    }

    [Fact]
    public async Task MapCephalonPreservesImplicitQueryFallbackWhenPartialExplicitBindingsOverrideImplicitSourceProfile()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-route-order:Behaviors:0"] = "tests.rest.profile.bindings.query.partial";
        builder.Configuration["RestApi:Overrides:prefer-route-order:Pattern"] = "/lookup/{orderId}";
        builder.Configuration["RestApi:Overrides:prefer-route-order:Bindings:0:PropertyName"] = "OrderId";
        builder.Configuration["RestApi:Overrides:prefer-route-order:Bindings:0:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-route-order:Bindings:0:Name"] = "orderId";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingPartialQueryFallbackRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings.query.partial", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile-runtime/query-partial/orders/lookup/{orderId}", endpoint.RoutePattern);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback,
            endpoint.BindingFallbackMode);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback.GetWireName(),
            endpoint.Metadata["bindingFallbackMode"]);
        Assert.Single(endpoint.BindingDescriptors);
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "orderId");

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.query.partial", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-route-order", candidate.AppliedOverrideId);
        Assert.Empty(candidate.OriginalProjection.BindingDescriptors);
        Assert.Null(candidate.OriginalProjection.BindingFallbackMode);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback,
            candidate.ProjectedEndpoint.BindingFallbackMode);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback.GetWireName(),
            candidate.ProjectedEndpoint.Metadata["bindingFallbackMode"]);

        var response = await client.GetAsync("/api/v6/tests/profile-runtime/query-partial/orders/lookup/ord-90?quantity=4");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProfileBindingQueryFallbackPartialRuntimeOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-90", payload.OrderId);
        Assert.Equal(4, payload.Quantity);

        Assert.Contains(snapshot.RestEndpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings.query.partial", StringComparison.Ordinal) &&
            item.BindingFallbackMode == RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback &&
            string.Equals(
                item.Metadata["bindingFallbackMode"],
                RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback.GetWireName(),
                StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointCandidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.query.partial", StringComparison.Ordinal) &&
            item.ProjectedEndpoint.BindingFallbackMode == RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback &&
            string.Equals(
                item.ProjectedEndpoint.Metadata["bindingFallbackMode"],
                RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback.GetWireName(),
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task MapCephalonPreservesImplicitQueryFallbackWhenProfileDeclaresItExplicitly()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingExplicitQueryFallbackRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings.query.explicit", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile-runtime/query-explicit/orders/lookup/{orderId}", endpoint.RoutePattern);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback,
            endpoint.BindingFallbackMode);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback.GetWireName(),
            endpoint.Metadata["bindingFallbackMode"]);
        Assert.Single(endpoint.BindingDescriptors);
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "orderId");

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.query.explicit", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Null(candidate.AppliedOverrideId);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback,
            candidate.OriginalProjection.BindingFallbackMode);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback,
            candidate.ProjectedEndpoint.BindingFallbackMode);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback.GetWireName(),
            candidate.ProjectedEndpoint.Metadata["bindingFallbackMode"]);
        Assert.Single(candidate.OriginalProjection.BindingDescriptors);
        Assert.Contains(candidate.OriginalProjection.BindingDescriptors, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "orderId");

        var response = await client.GetAsync("/api/v6/tests/profile-runtime/query-explicit/orders/lookup/ord-91?quantity=5");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProfileBindingQueryFallbackPartialRuntimeOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-91", payload.OrderId);
        Assert.Equal(5, payload.Quantity);

        Assert.Contains(snapshot.RestEndpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings.query.explicit", StringComparison.Ordinal) &&
            item.BindingFallbackMode == RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback &&
            string.Equals(
                item.Metadata["bindingFallbackMode"],
                RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback.GetWireName(),
                StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointCandidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.query.explicit", StringComparison.Ordinal) &&
            item.OriginalProjection.BindingFallbackMode == RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback &&
            item.ProjectedEndpoint.BindingFallbackMode == RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback &&
            string.Equals(
            item.ProjectedEndpoint.Metadata["bindingFallbackMode"],
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback.GetWireName(),
            StringComparison.Ordinal));
    }

    [Fact]
    public async Task MapCephalonAllowsPlaceholderAdditionWhenProfileExplicitlyPreservesImplicitQueryFallbackAndClearsFallbackModeAfterFullPromotion()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Behaviors:0"] = "tests.rest.profile.bindings.query.explicit";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Pattern"] = "/lookup/{orderId}/{quantity}";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:0:PropertyName"] = "OrderId";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:0:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:0:Name"] = "orderId";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:1:PropertyName"] = "Quantity";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:1:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:1:Name"] = "quantity";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingExplicitQueryFallbackRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings.query.explicit", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile-runtime/query-explicit/orders/lookup/{orderId}/{quantity}", endpoint.RoutePattern);
        Assert.Null(endpoint.BindingFallbackMode);
        Assert.DoesNotContain(endpoint.Metadata.Keys, static key => string.Equals(key, "bindingFallbackMode", StringComparison.Ordinal));
        Assert.Equal(2, endpoint.BindingDescriptors.Count);
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "orderId");
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "Quantity" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "quantity");

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.query.explicit", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-route-quantity", candidate.AppliedOverrideId);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback,
            candidate.OriginalProjection.BindingFallbackMode);
        Assert.Null(candidate.ProjectedEndpoint.BindingFallbackMode);
        Assert.DoesNotContain(candidate.ProjectedEndpoint.Metadata.Keys, static key =>
            string.Equals(key, "bindingFallbackMode", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile-runtime/query-explicit/orders/lookup/{orderId}/{quantity}", candidate.ProjectedEndpoint.RoutePattern);

        var rule = Assert.Single(overrides, static item =>
            string.Equals(item.Id, "prefer-route-quantity", StringComparison.Ordinal));
        Assert.Equal("/lookup/{orderId}/{quantity}", rule.Pattern);
        Assert.Equal(2, rule.Bindings.Count);
        Assert.Contains(rule.Bindings, static binding =>
            binding.PropertyName == "Quantity" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "quantity");

        var response = await client.GetAsync("/api/v6/tests/profile-runtime/query-explicit/orders/lookup/ord-91/5");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProfileBindingQueryFallbackPartialRuntimeOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-91", payload.OrderId);
        Assert.Equal(5, payload.Quantity);

        var snapshotEndpoint = Assert.Single(snapshot.RestEndpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings.query.explicit", StringComparison.Ordinal));
        Assert.Equal(endpoint.Id, snapshotEndpoint.Id);
        Assert.Null(snapshotEndpoint.BindingFallbackMode);
        Assert.DoesNotContain(snapshotEndpoint.Metadata.Keys, static key =>
            string.Equals(key, "bindingFallbackMode", StringComparison.Ordinal));

        var snapshotCandidate = Assert.Single(snapshot.RestEndpointCandidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.query.explicit", StringComparison.Ordinal));
        Assert.Equal(candidate.Id, snapshotCandidate.Id);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback,
            snapshotCandidate.OriginalProjection.BindingFallbackMode);
        Assert.Null(snapshotCandidate.ProjectedEndpoint.BindingFallbackMode);
        Assert.DoesNotContain(snapshotCandidate.ProjectedEndpoint.Metadata.Keys, static key =>
            string.Equals(key, "bindingFallbackMode", StringComparison.Ordinal));
    }

    [Fact]
    public void MapCephalonRejectsPlaceholderRemovalWhenOriginalRouteCoverageReliesOnInference()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Behaviors:0"] = "tests.rest.profile.bindings.inference";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Pattern"] = "/lookup";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:0:PropertyName"] = "OrderId";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:0:Source"] = "query";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:0:Name"] = "orderId";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:1:PropertyName"] = "Quantity";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:1:Source"] = "query";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:1:Name"] = "quantity";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:2:PropertyName"] = "CorrelationId";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:2:Source"] = "header";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:2:Name"] = "X-Correlation-Id";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:3:PropertyName"] = "Note";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:3:Source"] = "body";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:3:Name"] = "note";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingInferenceRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        using var app = builder.Build();
        var exception = Assert.Throws<InvalidOperationException>(() => app.MapCephalon());

        Assert.Contains("original projection", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("explicit route-binding plan", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MapCephalonAllowsPlaceholderAdditionWhenNewlyRouteBoundPropertiesWereExplicitlyBound()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Behaviors:0"] = "tests.rest.profile.bindings";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Pattern"] = "/lookup/{orderId}/items/{quantity}";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:0:PropertyName"] = "OrderId";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:0:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:0:Name"] = "orderId";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:1:PropertyName"] = "Quantity";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:1:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:1:Name"] = "quantity";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:2:PropertyName"] = "CorrelationId";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:2:Source"] = "header";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:2:Name"] = "X-Correlation-Id";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:3:PropertyName"] = "Note";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:3:Source"] = "body";
        builder.Configuration["RestApi:Overrides:prefer-route-quantity:Bindings:3:Name"] = "note";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders/lookup/{orderId}/items/{quantity}", endpoint.RoutePattern);
        Assert.Equal(4, endpoint.BindingDescriptors.Count);
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "Quantity" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "quantity");

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-route-quantity", candidate.AppliedOverrideId);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders/lookup/{orderId}/items/{quantity}", candidate.ProjectedEndpoint.RoutePattern);

        var rule = Assert.Single(overrides, static item =>
            string.Equals(item.Id, "prefer-route-quantity", StringComparison.Ordinal));
        Assert.Equal("/lookup/{orderId}/items/{quantity}", rule.Pattern);
        Assert.Equal(4, rule.Bindings.Count);
        Assert.Contains(rule.Bindings, static binding =>
            binding.PropertyName == "Quantity" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "quantity");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/bindings/orders/lookup/ord-75/items/5");
        request.Headers.Add("X-Correlation-Id", "corr-75");
        request.Content = JsonContent.Create(new
        {
            note = "route quantity",
            ignored = "body-fallback"
        });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProfileBindingRuntimeOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-75", payload.OrderId);
        Assert.Equal(5, payload.Quantity);
        Assert.Equal("corr-75", payload.CorrelationId);
        Assert.Equal("route quantity", payload.Note);
        Assert.Equal("body-fallback", payload.Ignored);
    }

    [Fact]
    public async Task MapCephalonAllowsImplicitBodyFallbackPromotionIntoAddedPlaceholder()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Behaviors:0"] = "tests.rest.profile.bindings";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Pattern"] = "/lookup/{orderId}/items/{ignored}";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:0:PropertyName"] = "OrderId";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:0:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:0:Name"] = "orderId";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:1:PropertyName"] = "Quantity";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:1:Source"] = "query";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:1:Name"] = "quantity";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:2:PropertyName"] = "CorrelationId";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:2:Source"] = "header";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:2:Name"] = "X-Correlation-Id";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:3:PropertyName"] = "Note";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:3:Source"] = "body";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:3:Name"] = "note";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:4:PropertyName"] = "Ignored";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:4:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-route-ignored:Bindings:4:Name"] = "ignored";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders/lookup/{orderId}/items/{ignored}", endpoint.RoutePattern);
        Assert.Equal(5, endpoint.BindingDescriptors.Count);
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "Ignored" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "ignored");

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-route-ignored", candidate.AppliedOverrideId);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders/lookup/{orderId}/items/{ignored}", candidate.ProjectedEndpoint.RoutePattern);

        var rule = Assert.Single(overrides, static item =>
            string.Equals(item.Id, "prefer-route-ignored", StringComparison.Ordinal));
        Assert.Equal("/lookup/{orderId}/items/{ignored}", rule.Pattern);
        Assert.Equal(5, rule.Bindings.Count);
        Assert.Contains(rule.Bindings, static binding =>
            binding.PropertyName == "Ignored" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "ignored");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/bindings/orders/lookup/ord-76/items/route-fallback?quantity=6");
        request.Headers.Add("X-Correlation-Id", "corr-76");
        request.Content = JsonContent.Create(new
        {
            note = "promoted from fallback"
        });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProfileBindingRuntimeOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-76", payload.OrderId);
        Assert.Equal(6, payload.Quantity);
        Assert.Equal("corr-76", payload.CorrelationId);
        Assert.Equal("promoted from fallback", payload.Note);
        Assert.Equal("route-fallback", payload.Ignored);
    }

    [Fact]
    public async Task MapCephalonExposesRestEndpointCandidatesAndSuppressesGeneratedAndProfileMappingsWhenExplicitDslExists()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "12";
        builder.Configuration["OpenApi:DefaultVersion"] = "12";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedThreeWaySuppressionRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.rest.generated.threeway.lookup", StringComparison.Ordinal));
        Assert.Equal("/api/v12/tests/generated/runtime/threeway/orders/explicit/{orderId}", endpoint.RoutePattern);
        Assert.Equal("v12", endpoint.OpenApiDocumentName);
        Assert.Equal(12, endpoint.ApiVersionMajor);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, endpoint.Metadata["authoringStyle"]);

        var behaviorCandidates = candidates
            .Where(static candidate => string.Equals(candidate.ProjectedEndpoint.BehaviorId, "tests.rest.generated.threeway.lookup", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(3, behaviorCandidates.Length);

        var published = Assert.Single(behaviorCandidates, static candidate =>
            candidate.Status == RestEndpointCandidateStatus.Published);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, published.AuthoringStyle);
        Assert.Equal(endpoint.Id, published.ProjectedEndpoint.Id);

        var suppressedProfile = Assert.Single(behaviorCandidates, static candidate =>
            candidate.Status == RestEndpointCandidateStatus.Suppressed &&
            string.Equals(candidate.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal(published.Id, suppressedProfile.SuppressedByCandidateId);

        var suppressedGenerated = Assert.Single(behaviorCandidates, static candidate =>
            candidate.Status == RestEndpointCandidateStatus.Suppressed &&
            string.Equals(candidate.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal(published.Id, suppressedGenerated.SuppressedByCandidateId);

        Assert.Contains(snapshot.RestEndpointCandidates, candidate =>
            string.Equals(candidate.Id, suppressedProfile.Id, StringComparison.Ordinal) &&
            candidate.Status == RestEndpointCandidateStatus.Suppressed);
        Assert.Contains(snapshot.RestEndpointCandidates, candidate =>
            string.Equals(candidate.Id, suppressedGenerated.Id, StringComparison.Ordinal) &&
            candidate.Status == RestEndpointCandidateStatus.Suppressed);

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>(
            "/api/v12/tests/generated/runtime/threeway/orders/explicit/ord-42");
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);

        var suppressedResponse = await client.GetAsync("/api/v12/tests/generated/runtime/threeway/orders/ord-42");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, suppressedResponse.StatusCode);
    }

    [Fact]
    public async Task MapCephalonExposesRestEndpointPublicationGroupsForThreeWayPrecedence()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "12";
        builder.Configuration["OpenApi:DefaultVersion"] = "12";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedThreeWaySuppressionRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var groups = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor[]>("/engine/rest-endpoint-publication-groups");
        var groupByBehavior = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor>(
            "/engine/rest-endpoint-publication-groups/tests.rest.generated.threeway.lookup");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(candidates);
        Assert.NotNull(groups);
        Assert.NotNull(groupByBehavior);
        Assert.NotNull(snapshot);

        var behaviorCandidates = candidates
            .Where(static candidate => string.Equals(candidate.ProjectedEndpoint.BehaviorId, "tests.rest.generated.threeway.lookup", StringComparison.Ordinal))
            .ToArray();
        var published = Assert.Single(behaviorCandidates, static candidate => candidate.Status == RestEndpointCandidateStatus.Published);
        var suppressed = behaviorCandidates
            .Where(static candidate => candidate.Status == RestEndpointCandidateStatus.Suppressed)
            .ToArray();

        var group = Assert.Single(groups, static item =>
            string.Equals(item.BehaviorId, "tests.rest.generated.threeway.lookup", StringComparison.Ordinal));
        Assert.False(group.AuthoringPolicy.IsConfigured);
        Assert.False(group.AuthoringPolicy.AllowMultiplePublishedCandidates);
        Assert.Equal(group.BehaviorId, group.AuthoringPolicy.BehaviorId);
        Assert.Null(group.AuthoringPolicy.PreferredAuthoringStyle);
        Assert.Empty(group.AuthoringPolicy.AllowedAuthoringStyles);
        Assert.Empty(group.AuthoringPolicy.DisallowedAuthoringStyles);
        Assert.Equal(2, group.WinningPrecedenceRank);
        Assert.Single(group.PublishedCandidateIds);
        Assert.Equal(published.Id, group.PublishedCandidateIds[0]);
        Assert.Equal(2, group.PrecedenceSuppressedCandidateIds.Count);
        Assert.Empty(group.GovernanceSuppressedCandidateIds);
        Assert.Equal(3, group.Candidates.Count);
        Assert.Equal(3, group.AuthoringStyleSummaries.Count);
        var explicitStyle = Assert.Single(group.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([published.Id], explicitStyle.CandidateIds);
        Assert.Equal([published.Id], explicitStyle.PublishedCandidateIds);
        Assert.Equal([RestEndpointRuntimeMetadata.BehaviorModuleDslPrecedenceRank], explicitStyle.PrecedenceRanks);
        Assert.Empty(explicitStyle.PrecedenceSuppressedCandidateIds);
        Assert.Empty(explicitStyle.GovernanceSuppressedCandidateIds);
        var profileSuppressed = Assert.Single(suppressed, static candidate =>
            string.Equals(candidate.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, StringComparison.Ordinal));
        var profileStyle = Assert.Single(group.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([profileSuppressed.Id], profileStyle.CandidateIds);
        Assert.Equal([profileSuppressed.Id], profileStyle.PrecedenceSuppressedCandidateIds);
        Assert.Equal([RestEndpointRuntimeMetadata.BehaviorModuleProfilePrecedenceRank], profileStyle.PrecedenceRanks);
        Assert.Empty(profileStyle.PublishedCandidateIds);
        Assert.Empty(profileStyle.GovernanceSuppressedCandidateIds);
        var generatedSuppressed = Assert.Single(suppressed, static candidate =>
            string.Equals(candidate.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        var generatedStyle = Assert.Single(group.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([generatedSuppressed.Id], generatedStyle.CandidateIds);
        Assert.Equal([generatedSuppressed.Id], generatedStyle.PrecedenceSuppressedCandidateIds);
        Assert.Equal([RestEndpointRuntimeMetadata.BehaviorModuleGeneratedPrecedenceRank], generatedStyle.PrecedenceRanks);
        Assert.Empty(generatedStyle.PublishedCandidateIds);
        Assert.Empty(generatedStyle.GovernanceSuppressedCandidateIds);
        Assert.Equal(group.BehaviorId, groupByBehavior.BehaviorId);
        Assert.Equal(group.PublishedCandidateIds, groupByBehavior.PublishedCandidateIds);
        Assert.Equal(group.PrecedenceSuppressedCandidateIds, groupByBehavior.PrecedenceSuppressedCandidateIds);
        Assert.Equal(group.AuthoringStyleSummaries.Count, groupByBehavior.AuthoringStyleSummaries.Count);
        Assert.False(groupByBehavior.AuthoringPolicy.IsConfigured);
        Assert.False(groupByBehavior.AuthoringPolicy.AllowMultiplePublishedCandidates);
        Assert.Contains(suppressed, candidate =>
            group.PrecedenceSuppressedCandidateIds.Contains(candidate.Id, StringComparer.Ordinal));
        Assert.Contains(snapshot.RestEndpointPublicationGroups, item =>
            string.Equals(item.BehaviorId, group.BehaviorId, StringComparison.Ordinal) &&
            item.AuthoringPolicy.IsConfigured == false &&
            item.AuthoringStyleSummaries.Count == 3 &&
            item.PublishedCandidateIds.Count == 1 &&
            string.Equals(item.PublishedCandidateIds[0], published.Id, StringComparison.Ordinal));
    }

    [Fact]
    public async Task MapCephalonExposesConfiguredRestEndpointPublicationGroupAuthoringPolicy()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "12";
        builder.Configuration["OpenApi:DefaultVersion"] = "12";
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:AllowMultiplePublishedCandidates"] = "false";
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:PreferredAuthoringStyle"] =
            RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:AllowedAuthoringStyles:0"] =
            RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:AllowedAuthoringStyles:1"] =
            RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle;
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:AllowedAuthoringStyles:2"] =
            RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle;
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:DisallowedAuthoringStyles:0"] =
            RestEndpointRuntimeMetadata.BehaviorHelperAuthoringStyle;
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedThreeWaySuppressionRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var groups = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor[]>("/engine/rest-endpoint-publication-groups");
        var groupByBehavior = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor>(
            "/engine/rest-endpoint-publication-groups/tests.rest.generated.threeway.lookup");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(candidates);
        Assert.NotNull(groups);
        Assert.NotNull(groupByBehavior);
        Assert.NotNull(snapshot);

        var published = Assert.Single(candidates, static item =>
            item.Status == RestEndpointCandidateStatus.Published);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, published.AuthoringStyle);
        Assert.Null(published.SuppressedByAuthoringPolicyKind);

        var authoringPolicySuppressed = candidates
            .Where(static item => item.Status == RestEndpointCandidateStatus.Suppressed)
            .OrderBy(static item => item.AuthoringStyle, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(2, authoringPolicySuppressed.Length);
        Assert.All(authoringPolicySuppressed, static item =>
        {
            Assert.Equal(
                RestEndpointAuthoringPolicySuppressionKind.PreferredAuthoringStyleSelected,
                item.SuppressedByAuthoringPolicyKind);
            Assert.Null(item.SuppressedByCandidateId);
            Assert.Null(item.SuppressedBySuppressionId);
        });

        var group = Assert.Single(groups, static item =>
            string.Equals(item.BehaviorId, "tests.rest.generated.threeway.lookup", StringComparison.Ordinal));
        Assert.True(group.AuthoringPolicy.IsConfigured);
        Assert.False(group.AuthoringPolicy.AllowMultiplePublishedCandidates);
        Assert.Equal(group.BehaviorId, group.AuthoringPolicy.BehaviorId);
        Assert.Equal(
            RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle,
            group.AuthoringPolicy.PreferredAuthoringStyle);
        Assert.Equal(
            [
                RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle,
                RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle,
                RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle
            ],
            group.AuthoringPolicy.AllowedAuthoringStyles);
        Assert.Equal(
            [RestEndpointRuntimeMetadata.BehaviorHelperAuthoringStyle],
            group.AuthoringPolicy.DisallowedAuthoringStyles);
        Assert.Single(group.PublishedCandidateIds);
        Assert.Empty(group.PrecedenceSuppressedCandidateIds);
        Assert.Empty(group.GovernanceSuppressedCandidateIds);
        Assert.Equal(2, group.AuthoringPolicySuppressedCandidateIds.Count);
        Assert.True(groupByBehavior.AuthoringPolicy.IsConfigured);
        Assert.False(groupByBehavior.AuthoringPolicy.AllowMultiplePublishedCandidates);
        Assert.Equal(group.AuthoringPolicy.PreferredAuthoringStyle, groupByBehavior.AuthoringPolicy.PreferredAuthoringStyle);
        Assert.Equal(group.AuthoringPolicy.AllowedAuthoringStyles, groupByBehavior.AuthoringPolicy.AllowedAuthoringStyles);
        Assert.Equal(group.AuthoringPolicy.DisallowedAuthoringStyles, groupByBehavior.AuthoringPolicy.DisallowedAuthoringStyles);
        Assert.Equal(group.AuthoringPolicySuppressedCandidateIds, groupByBehavior.AuthoringPolicySuppressedCandidateIds);
        var explicitStyle = Assert.Single(group.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, StringComparison.Ordinal));
        Assert.Single(explicitStyle.PublishedCandidateIds);
        Assert.Empty(explicitStyle.AuthoringPolicySuppressedCandidateIds);
        var profileStyle = Assert.Single(group.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([authoringPolicySuppressed[1].Id], profileStyle.AuthoringPolicySuppressedCandidateIds);
        Assert.Empty(profileStyle.PrecedenceSuppressedCandidateIds);
        Assert.Empty(profileStyle.GovernanceSuppressedCandidateIds);
        var generatedStyle = Assert.Single(group.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([authoringPolicySuppressed[0].Id], generatedStyle.AuthoringPolicySuppressedCandidateIds);
        Assert.Empty(generatedStyle.PrecedenceSuppressedCandidateIds);
        Assert.Empty(generatedStyle.GovernanceSuppressedCandidateIds);
        Assert.Contains(snapshot.RestEndpointPublicationGroups, item =>
            string.Equals(item.BehaviorId, group.BehaviorId, StringComparison.Ordinal) &&
            item.AuthoringPolicy.IsConfigured &&
            item.AuthoringPolicy.AllowMultiplePublishedCandidates == false &&
            string.Equals(
                item.AuthoringPolicy.PreferredAuthoringStyle,
                RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle,
                StringComparison.Ordinal) &&
            item.AuthoringPolicy.AllowedAuthoringStyles.Count == 3 &&
            item.AuthoringPolicy.DisallowedAuthoringStyles.Count == 1 &&
            item.AuthoringPolicySuppressedCandidateIds.Count == 2);
    }

    [Fact]
    public async Task MapCephalonExposesPublicationGroupAuthoringPolicySuppressionSummaries()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "12";
        builder.Configuration["OpenApi:DefaultVersion"] = "12";
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:AllowedAuthoringStyles:0"] =
            RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:DisallowedAuthoringStyles:0"] =
            RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle;
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedThreeWaySuppressionRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var groups = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor[]>("/engine/rest-endpoint-publication-groups");
        var groupByBehavior = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor>(
            "/engine/rest-endpoint-publication-groups/tests.rest.generated.threeway.lookup");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");
        var groupsJson = await client.GetStringAsync("/engine/rest-endpoint-publication-groups");
        var groupByBehaviorJson = await client.GetStringAsync(
            "/engine/rest-endpoint-publication-groups/tests.rest.generated.threeway.lookup");
        var snapshotJson = await client.GetStringAsync("/engine/snapshot");

        Assert.NotNull(candidates);
        Assert.NotNull(groups);
        Assert.NotNull(groupByBehavior);
        Assert.NotNull(snapshot);

        var behaviorCandidates = candidates
            .Where(static candidate => string.Equals(candidate.ProjectedEndpoint.BehaviorId, "tests.rest.generated.threeway.lookup", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(3, behaviorCandidates.Length);

        var published = Assert.Single(behaviorCandidates, static candidate => candidate.Status == RestEndpointCandidateStatus.Published);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, published.AuthoringStyle);

        var generatedSuppressed = Assert.Single(behaviorCandidates, static candidate =>
            candidate.Status == RestEndpointCandidateStatus.Suppressed &&
            string.Equals(candidate.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal(
            RestEndpointAuthoringPolicySuppressionKind.DisallowedAuthoringStyle,
            generatedSuppressed.SuppressedByAuthoringPolicyKind);

        var profileSuppressed = Assert.Single(behaviorCandidates, static candidate =>
            candidate.Status == RestEndpointCandidateStatus.Suppressed &&
            string.Equals(candidate.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal(
            RestEndpointAuthoringPolicySuppressionKind.NotAllowedAuthoringStyle,
            profileSuppressed.SuppressedByAuthoringPolicyKind);

        var group = Assert.Single(groups, static item =>
            string.Equals(item.BehaviorId, "tests.rest.generated.threeway.lookup", StringComparison.Ordinal));
        Assert.True(group.AuthoringPolicy.IsConfigured);
        Assert.Single(group.PublishedCandidateIds);
        Assert.Equal(published.Id, group.PublishedCandidateIds[0]);
        Assert.Empty(group.PrecedenceSuppressedCandidateIds);
        Assert.Empty(group.GovernanceSuppressedCandidateIds);
        Assert.Equal(2, group.AuthoringPolicySuppressedCandidateIds.Count);
        Assert.Contains(generatedSuppressed.Id, group.AuthoringPolicySuppressedCandidateIds, StringComparer.Ordinal);
        Assert.Contains(profileSuppressed.Id, group.AuthoringPolicySuppressedCandidateIds, StringComparer.Ordinal);

        var disallowedSummary = Assert.Single(group.AuthoringPolicySuppressionSummaries, static item =>
            item.Kind == RestEndpointAuthoringPolicySuppressionKind.DisallowedAuthoringStyle);
        Assert.Equal([generatedSuppressed.Id], disallowedSummary.CandidateIds);
        var notAllowedSummary = Assert.Single(group.AuthoringPolicySuppressionSummaries, static item =>
            item.Kind == RestEndpointAuthoringPolicySuppressionKind.NotAllowedAuthoringStyle);
        Assert.Equal([profileSuppressed.Id], notAllowedSummary.CandidateIds);

        var explicitStyle = Assert.Single(group.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([published.Id], explicitStyle.PublishedCandidateIds);
        Assert.Empty(explicitStyle.AuthoringPolicySuppressedCandidateIds);
        Assert.Empty(explicitStyle.AuthoringPolicySuppressionSummaries);

        var generatedStyle = Assert.Single(group.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([generatedSuppressed.Id], generatedStyle.AuthoringPolicySuppressedCandidateIds);
        Assert.Empty(generatedStyle.PublishedCandidateIds);
        var generatedStyleSuppression = Assert.Single(generatedStyle.AuthoringPolicySuppressionSummaries);
        Assert.Equal(RestEndpointAuthoringPolicySuppressionKind.DisallowedAuthoringStyle, generatedStyleSuppression.Kind);
        Assert.Equal([generatedSuppressed.Id], generatedStyleSuppression.CandidateIds);

        var profileStyle = Assert.Single(group.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([profileSuppressed.Id], profileStyle.AuthoringPolicySuppressedCandidateIds);
        Assert.Empty(profileStyle.PublishedCandidateIds);
        var profileStyleSuppression = Assert.Single(profileStyle.AuthoringPolicySuppressionSummaries);
        Assert.Equal(RestEndpointAuthoringPolicySuppressionKind.NotAllowedAuthoringStyle, profileStyleSuppression.Kind);
        Assert.Equal([profileSuppressed.Id], profileStyleSuppression.CandidateIds);

        Assert.Equal(group.AuthoringPolicySuppressedCandidateIds, groupByBehavior.AuthoringPolicySuppressedCandidateIds);
        Assert.Equal(group.AuthoringPolicySuppressionSummaries.Count, groupByBehavior.AuthoringPolicySuppressionSummaries.Count);
        Assert.Contains(groupByBehavior.AuthoringPolicySuppressionSummaries, static item =>
            item.Kind == RestEndpointAuthoringPolicySuppressionKind.DisallowedAuthoringStyle);
        Assert.Contains(groupByBehavior.AuthoringPolicySuppressionSummaries, static item =>
            item.Kind == RestEndpointAuthoringPolicySuppressionKind.NotAllowedAuthoringStyle);

        var snapshotGroup = Assert.Single(snapshot.RestEndpointPublicationGroups, static item =>
            string.Equals(item.BehaviorId, "tests.rest.generated.threeway.lookup", StringComparison.Ordinal));
        Assert.Equal(group.AuthoringPolicySuppressionSummaries.Count, snapshotGroup.AuthoringPolicySuppressionSummaries.Count);
        Assert.Contains(snapshotGroup.AuthoringPolicySuppressionSummaries, static item =>
            item.Kind == RestEndpointAuthoringPolicySuppressionKind.DisallowedAuthoringStyle);
        Assert.Contains(snapshotGroup.AuthoringPolicySuppressionSummaries, static item =>
            item.Kind == RestEndpointAuthoringPolicySuppressionKind.NotAllowedAuthoringStyle);

        Assert.Contains("\"kind\":\"disallowed-authoring-style\"", groupsJson, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"not-allowed-authoring-style\"", groupsJson, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"disallowed-authoring-style\"", groupByBehaviorJson, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"not-allowed-authoring-style\"", groupByBehaviorJson, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"disallowed-authoring-style\"", snapshotJson, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"not-allowed-authoring-style\"", snapshotJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonSerializesAuthoringPolicySuppressionKindsUsingCanonicalWireNames()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "12";
        builder.Configuration["OpenApi:DefaultVersion"] = "12";
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:AllowMultiplePublishedCandidates"] = "false";
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:PreferredAuthoringStyle"] =
            RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:AllowedAuthoringStyles:0"] =
            RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:AllowedAuthoringStyles:1"] =
            RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle;
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:AllowedAuthoringStyles:2"] =
            RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle;
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:DisallowedAuthoringStyles:0"] =
            RestEndpointRuntimeMetadata.BehaviorHelperAuthoringStyle;
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedThreeWaySuppressionRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");
        var candidatesJson = await client.GetStringAsync("/engine/rest-endpoint-candidates");
        var snapshotJson = await client.GetStringAsync("/engine/snapshot");

        Assert.NotNull(candidates);
        Assert.NotNull(snapshot);

        var suppressedCandidates = candidates
            .Where(static item =>
                item.Status == RestEndpointCandidateStatus.Suppressed &&
                item.SuppressedByAuthoringPolicyKind == RestEndpointAuthoringPolicySuppressionKind.PreferredAuthoringStyleSelected)
            .OrderBy(static item => item.AuthoringStyle, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(2, suppressedCandidates.Length);

        var suppressed = suppressedCandidates[0];

        var candidateById = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor>(
            $"/engine/rest-endpoint-candidates/{suppressed.Id}");
        var candidateByIdJson = await client.GetStringAsync($"/engine/rest-endpoint-candidates/{suppressed.Id}");

        Assert.NotNull(candidateById);
        Assert.Equal(suppressed.Id, candidateById.Id);
        Assert.Equal(
            RestEndpointAuthoringPolicySuppressionKind.PreferredAuthoringStyleSelected,
            candidateById.SuppressedByAuthoringPolicyKind);

        var expectedWireName = RestEndpointAuthoringPolicySuppressionKind.PreferredAuthoringStyleSelected.GetWireName();
        var expectedJsonFragment = $"\"suppressedByAuthoringPolicyKind\":\"{expectedWireName}\"";
        Assert.Contains(expectedJsonFragment, candidatesJson, StringComparison.Ordinal);
        Assert.Contains(expectedJsonFragment, candidateByIdJson, StringComparison.Ordinal);
        Assert.Contains(expectedJsonFragment, snapshotJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\"suppressedByAuthoringPolicyKind\":\"PreferredAuthoringStyleSelected\"", candidatesJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\"suppressedByAuthoringPolicyKind\":3", candidatesJson, StringComparison.Ordinal);

        var snapshotSuppressed = Assert.Single(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, suppressed.Id, StringComparison.Ordinal));
        Assert.Equal(
            RestEndpointAuthoringPolicySuppressionKind.PreferredAuthoringStyleSelected,
            snapshotSuppressed.SuppressedByAuthoringPolicyKind);
    }

    [Fact]
    public async Task MapCephalonExposesRestEndpointAuthoringPolicies()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "12";
        builder.Configuration["OpenApi:DefaultVersion"] = "12";
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:AllowMultiplePublishedCandidates"] = "false";
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:PreferredAuthoringStyle"] =
            RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:AllowedAuthoringStyles:0"] =
            RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:AllowedAuthoringStyles:1"] =
            RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle;
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:AllowedAuthoringStyles:2"] =
            RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle;
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:DisallowedAuthoringStyles:0"] =
            RestEndpointRuntimeMetadata.BehaviorHelperAuthoringStyle;
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedThreeWaySuppressionRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var policies = await client.GetFromJsonAsync<RestEndpointAuthoringPolicyDescriptor[]>("/engine/rest-endpoint-authoring-policies");
        var policyByBehavior = await client.GetFromJsonAsync<RestEndpointAuthoringPolicyDescriptor>(
            "/engine/rest-endpoint-authoring-policies/tests.rest.generated.threeway.lookup");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");
        var policiesJson = await client.GetStringAsync("/engine/rest-endpoint-authoring-policies");
        var policyByBehaviorJson = await client.GetStringAsync(
            "/engine/rest-endpoint-authoring-policies/tests.rest.generated.threeway.lookup");
        var snapshotJson = await client.GetStringAsync("/engine/snapshot");

        Assert.NotNull(candidates);
        Assert.NotNull(policies);
        Assert.NotNull(policyByBehavior);
        Assert.NotNull(snapshot);

        var published = Assert.Single(candidates, static item =>
            item.Status == RestEndpointCandidateStatus.Published);
        var authoringPolicySuppressed = candidates
            .Where(static item => item.Status == RestEndpointCandidateStatus.Suppressed)
            .OrderBy(static item => item.AuthoringStyle, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(2, authoringPolicySuppressed.Length);

        var policy = Assert.Single(policies, static item =>
            string.Equals(item.BehaviorId, "tests.rest.generated.threeway.lookup", StringComparison.Ordinal));
        Assert.True(policy.IsConfigured);
        Assert.False(policy.AllowMultiplePublishedCandidates);
        Assert.Equal(
            RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle,
            policy.PreferredAuthoringStyle);
        Assert.Equal(
            [
                RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle,
                RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle,
                RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle
            ],
            policy.AllowedAuthoringStyles);
        Assert.Equal(
            [RestEndpointRuntimeMetadata.BehaviorHelperAuthoringStyle],
            policy.DisallowedAuthoringStyles);
        Assert.Equal(3, policy.CandidateIds.Count);
        Assert.Equal([published.Id], policy.RetainedCandidateIds);
        Assert.Equal([published.Id], policy.PublishedCandidateIds);
        Assert.Empty(policy.PrecedenceSuppressedCandidateIds);
        Assert.Empty(policy.GovernanceSuppressedCandidateIds);
        Assert.Equal(2, policy.SuppressedCandidateIds.Count);
        Assert.All(authoringPolicySuppressed, candidate =>
            Assert.Contains(candidate.Id, policy.SuppressedCandidateIds, StringComparer.Ordinal));
        Assert.Equal(
            [RestEndpointAuthoringPolicySuppressionKind.PreferredAuthoringStyleSelected],
            policy.SuppressionKinds);
        var suppressionSummary = Assert.Single(policy.SuppressionSummaries);
        Assert.Equal(
            RestEndpointAuthoringPolicySuppressionKind.PreferredAuthoringStyleSelected,
            suppressionSummary.Kind);
        Assert.Equal(authoringPolicySuppressed.Select(static candidate => candidate.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray(),
            suppressionSummary.CandidateIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray());
        Assert.Equal(
            authoringPolicySuppressed.Select(static candidate => candidate.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray(),
            policy.HostGovernanceEligibleCandidateIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray());
        Assert.Equal([published.Id], policy.HostGovernanceIneligibleCandidateIds);
        Assert.Empty(policy.SkippedSuppressionIds);
        Assert.Empty(policy.SkippedOverrideIds);
        Assert.Equal(3, policy.AuthoringStyleSummaries.Count);
        var explicitStyle = Assert.Single(policy.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([published.Id], explicitStyle.CandidateIds);
        Assert.Equal([published.Id], explicitStyle.RetainedCandidateIds);
        Assert.Equal([published.Id], explicitStyle.PublishedCandidateIds);
        Assert.Empty(explicitStyle.PrecedenceSuppressedCandidateIds);
        Assert.Empty(explicitStyle.GovernanceSuppressedCandidateIds);
        Assert.Empty(explicitStyle.SuppressedCandidateIds);
        Assert.Empty(explicitStyle.SuppressionKinds);
        Assert.Empty(explicitStyle.SuppressionSummaries);
        Assert.Empty(explicitStyle.HostGovernanceEligibleCandidateIds);
        Assert.Equal([published.Id], explicitStyle.HostGovernanceIneligibleCandidateIds);
        Assert.Empty(explicitStyle.SkippedSuppressionIds);
        Assert.Empty(explicitStyle.SkippedOverrideIds);

        var generatedSuppressed = Assert.Single(authoringPolicySuppressed, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        var generatedStyle = Assert.Single(policy.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([generatedSuppressed.Id], generatedStyle.CandidateIds);
        Assert.Empty(generatedStyle.RetainedCandidateIds);
        Assert.Empty(generatedStyle.PublishedCandidateIds);
        Assert.Empty(generatedStyle.PrecedenceSuppressedCandidateIds);
        Assert.Empty(generatedStyle.GovernanceSuppressedCandidateIds);
        Assert.Equal([generatedSuppressed.Id], generatedStyle.SuppressedCandidateIds);
        Assert.Equal(
            [RestEndpointAuthoringPolicySuppressionKind.PreferredAuthoringStyleSelected],
            generatedStyle.SuppressionKinds);
        var generatedSuppressionSummary = Assert.Single(generatedStyle.SuppressionSummaries);
        Assert.Equal(
            RestEndpointAuthoringPolicySuppressionKind.PreferredAuthoringStyleSelected,
            generatedSuppressionSummary.Kind);
        Assert.Equal([generatedSuppressed.Id], generatedSuppressionSummary.CandidateIds);
        Assert.Equal([generatedSuppressed.Id], generatedStyle.HostGovernanceEligibleCandidateIds);
        Assert.Empty(generatedStyle.HostGovernanceIneligibleCandidateIds);
        Assert.Empty(generatedStyle.SkippedSuppressionIds);
        Assert.Empty(generatedStyle.SkippedOverrideIds);

        var profileSuppressed = Assert.Single(authoringPolicySuppressed, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, StringComparison.Ordinal));
        var profileStyle = Assert.Single(policy.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([profileSuppressed.Id], profileStyle.CandidateIds);
        Assert.Empty(profileStyle.RetainedCandidateIds);
        Assert.Empty(profileStyle.PublishedCandidateIds);
        Assert.Empty(profileStyle.PrecedenceSuppressedCandidateIds);
        Assert.Empty(profileStyle.GovernanceSuppressedCandidateIds);
        Assert.Equal([profileSuppressed.Id], profileStyle.SuppressedCandidateIds);
        Assert.Equal(
            [RestEndpointAuthoringPolicySuppressionKind.PreferredAuthoringStyleSelected],
            profileStyle.SuppressionKinds);
        var profileSuppressionSummary = Assert.Single(profileStyle.SuppressionSummaries);
        Assert.Equal(
            RestEndpointAuthoringPolicySuppressionKind.PreferredAuthoringStyleSelected,
            profileSuppressionSummary.Kind);
        Assert.Equal([profileSuppressed.Id], profileSuppressionSummary.CandidateIds);
        Assert.Equal([profileSuppressed.Id], profileStyle.HostGovernanceEligibleCandidateIds);
        Assert.Empty(profileStyle.HostGovernanceIneligibleCandidateIds);
        Assert.Empty(profileStyle.SkippedSuppressionIds);
        Assert.Empty(profileStyle.SkippedOverrideIds);

        Assert.Equal(policy.CandidateIds, policyByBehavior.CandidateIds);
        Assert.Equal(policy.RetainedCandidateIds, policyByBehavior.RetainedCandidateIds);
        Assert.Equal(policy.PublishedCandidateIds, policyByBehavior.PublishedCandidateIds);
        Assert.Equal(policy.SuppressedCandidateIds, policyByBehavior.SuppressedCandidateIds);
        Assert.Equal(policy.SuppressionKinds, policyByBehavior.SuppressionKinds);
        Assert.Equal(policy.SuppressionSummaries.Count, policyByBehavior.SuppressionSummaries.Count);
        Assert.Equal(policy.HostGovernanceEligibleCandidateIds, policyByBehavior.HostGovernanceEligibleCandidateIds);
        Assert.Equal(policy.HostGovernanceIneligibleCandidateIds, policyByBehavior.HostGovernanceIneligibleCandidateIds);
        Assert.Equal(policy.SkippedSuppressionIds, policyByBehavior.SkippedSuppressionIds);
        Assert.Equal(policy.SkippedOverrideIds, policyByBehavior.SkippedOverrideIds);
        Assert.Equal(policy.AuthoringStyleSummaries.Count, policyByBehavior.AuthoringStyleSummaries.Count);

        var snapshotPolicy = Assert.Single(snapshot.RestEndpointAuthoringPolicies, static item =>
            string.Equals(item.BehaviorId, "tests.rest.generated.threeway.lookup", StringComparison.Ordinal));
        Assert.Equal(policy.CandidateIds, snapshotPolicy.CandidateIds);
        Assert.Equal(policy.SuppressedCandidateIds, snapshotPolicy.SuppressedCandidateIds);
        Assert.Equal(policy.SuppressionKinds, snapshotPolicy.SuppressionKinds);
        Assert.Equal(policy.HostGovernanceEligibleCandidateIds, snapshotPolicy.HostGovernanceEligibleCandidateIds);
        Assert.Equal(policy.HostGovernanceIneligibleCandidateIds, snapshotPolicy.HostGovernanceIneligibleCandidateIds);
        Assert.Equal(policy.SkippedSuppressionIds, snapshotPolicy.SkippedSuppressionIds);
        Assert.Equal(policy.SkippedOverrideIds, snapshotPolicy.SkippedOverrideIds);
        Assert.Equal(policy.AuthoringStyleSummaries.Count, snapshotPolicy.AuthoringStyleSummaries.Count);

        Assert.Contains("\"suppressionKinds\":[\"preferred-authoring-style-selected\"]", policiesJson, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"preferred-authoring-style-selected\"", policiesJson, StringComparison.Ordinal);
        Assert.Contains("\"authoringStyleSummaries\":[", policiesJson, StringComparison.Ordinal);
        Assert.Contains("\"suppressionKinds\":[\"preferred-authoring-style-selected\"]", policyByBehaviorJson, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"preferred-authoring-style-selected\"", policyByBehaviorJson, StringComparison.Ordinal);
        Assert.Contains("\"authoringStyleSummaries\":[", policyByBehaviorJson, StringComparison.Ordinal);
        Assert.Contains("\"suppressionKinds\":[\"preferred-authoring-style-selected\"]", snapshotJson, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"preferred-authoring-style-selected\"", snapshotJson, StringComparison.Ordinal);
        Assert.Contains("\"authoringStyleSummaries\":[", snapshotJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonKeepsConfiguredRestEndpointAuthoringPoliciesWithoutCandidatesVisible()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "12";
        builder.Configuration["OpenApi:DefaultVersion"] = "12";
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:AllowMultiplePublishedCandidates"] = "false";
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.missing.lookup:AllowedAuthoringStyles:0"] =
            RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle;
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.missing.lookup:DisallowedAuthoringStyles:0"] =
            RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle;
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedThreeWaySuppressionRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var policies = await client.GetFromJsonAsync<RestEndpointAuthoringPolicyDescriptor[]>("/engine/rest-endpoint-authoring-policies");
        var missingPolicy = await client.GetFromJsonAsync<RestEndpointAuthoringPolicyDescriptor>(
            "/engine/rest-endpoint-authoring-policies/tests.rest.generated.missing.lookup");
        var groups = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor[]>("/engine/rest-endpoint-publication-groups");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(policies);
        Assert.NotNull(missingPolicy);
        Assert.NotNull(groups);
        Assert.NotNull(snapshot);

        var policy = Assert.Single(policies, static item =>
            string.Equals(item.BehaviorId, "tests.rest.generated.missing.lookup", StringComparison.Ordinal));
        Assert.True(policy.IsConfigured);
        Assert.False(policy.AllowMultiplePublishedCandidates);
        Assert.Equal(
            [RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle],
            policy.AllowedAuthoringStyles);
        Assert.Equal(
            [RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle],
            policy.DisallowedAuthoringStyles);
        Assert.Empty(policy.CandidateIds);
        Assert.Empty(policy.RetainedCandidateIds);
        Assert.Empty(policy.PublishedCandidateIds);
        Assert.Empty(policy.PrecedenceSuppressedCandidateIds);
        Assert.Empty(policy.GovernanceSuppressedCandidateIds);
        Assert.Empty(policy.SuppressedCandidateIds);
        Assert.Empty(policy.SuppressionKinds);
        Assert.Empty(policy.SuppressionSummaries);
        Assert.Empty(policy.HostGovernanceEligibleCandidateIds);
        Assert.Empty(policy.HostGovernanceIneligibleCandidateIds);
        Assert.Empty(policy.SkippedSuppressionIds);
        Assert.Empty(policy.SkippedOverrideIds);
        Assert.Empty(policy.AuthoringStyleSummaries);

        Assert.Equal(policy.BehaviorId, missingPolicy.BehaviorId);
        Assert.Empty(missingPolicy.CandidateIds);
        Assert.Empty(missingPolicy.HostGovernanceEligibleCandidateIds);
        Assert.Empty(missingPolicy.HostGovernanceIneligibleCandidateIds);
        Assert.Empty(missingPolicy.SkippedSuppressionIds);
        Assert.Empty(missingPolicy.SkippedOverrideIds);
        Assert.Empty(missingPolicy.AuthoringStyleSummaries);
        Assert.DoesNotContain(groups, static item =>
            string.Equals(item.BehaviorId, "tests.rest.generated.missing.lookup", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointAuthoringPolicies, static item =>
            string.Equals(item.BehaviorId, "tests.rest.generated.missing.lookup", StringComparison.Ordinal) &&
            item.IsConfigured &&
            item.CandidateIds.Count == 0 &&
            item.HostGovernanceEligibleCandidateIds.Count == 0 &&
            item.HostGovernanceIneligibleCandidateIds.Count == 0 &&
            item.SkippedSuppressionIds.Count == 0 &&
            item.SkippedOverrideIds.Count == 0 &&
            item.AuthoringStyleSummaries.Count == 0);
    }

    [Fact]
    public async Task MapCephalonExposesSeparateRestEndpointAuthoringPolicyRetainedPrecedenceAndGovernanceBuckets()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "12";
        builder.Configuration["OpenApi:DefaultVersion"] = "12";
        builder.Configuration["RestApi:Suppressions:hide-generated:Behaviors:0"] = "tests.rest.generated.threeway.lookup";
        builder.Configuration["RestApi:Suppressions:hide-generated:AuthoringStyles:0"] =
            RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle;
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedThreeWaySuppressionRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var policies = await client.GetFromJsonAsync<RestEndpointAuthoringPolicyDescriptor[]>("/engine/rest-endpoint-authoring-policies");
        var policyByBehavior = await client.GetFromJsonAsync<RestEndpointAuthoringPolicyDescriptor>(
            "/engine/rest-endpoint-authoring-policies/tests.rest.generated.threeway.lookup");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(policies);
        Assert.NotNull(policyByBehavior);
        Assert.NotNull(candidates);
        Assert.NotNull(snapshot);

        var behaviorCandidates = candidates
            .Where(static item =>
                string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.generated.threeway.lookup", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(3, behaviorCandidates.Length);

        var published = Assert.Single(behaviorCandidates, static item =>
            item.Status == RestEndpointCandidateStatus.Published);
        var precedenceSuppressed = Assert.Single(behaviorCandidates, static item =>
            item.Status == RestEndpointCandidateStatus.Suppressed &&
            !string.IsNullOrWhiteSpace(item.SuppressedByCandidateId));
        var governanceSuppressed = Assert.Single(behaviorCandidates, static item =>
            item.Status == RestEndpointCandidateStatus.Suppressed &&
            !string.IsNullOrWhiteSpace(item.SuppressedBySuppressionId));

        var policy = Assert.Single(policies, static item =>
            string.Equals(item.BehaviorId, "tests.rest.generated.threeway.lookup", StringComparison.Ordinal));
        Assert.False(policy.IsConfigured);
        Assert.Equal(3, policy.CandidateIds.Count);
        Assert.Equal(3, policy.RetainedCandidateIds.Count);
        Assert.Empty(policy.SuppressedCandidateIds);
        Assert.Empty(policy.SuppressionKinds);
        Assert.Empty(policy.SuppressionSummaries);
        Assert.Equal([precedenceSuppressed.Id, governanceSuppressed.Id], policy.HostGovernanceEligibleCandidateIds);
        Assert.Equal([published.Id], policy.HostGovernanceIneligibleCandidateIds);
        Assert.Empty(policy.SkippedSuppressionIds);
        Assert.Empty(policy.SkippedOverrideIds);
        Assert.Empty(policy.GovernanceOverrideSummaries);
        Assert.Empty(policy.SkippedSuppressionSummaries);
        Assert.Empty(policy.SkippedOverrideSummaries);
        Assert.Equal([published.Id], policy.PublishedCandidateIds);
        Assert.Equal([precedenceSuppressed.Id], policy.PrecedenceSuppressedCandidateIds);
        Assert.Equal([governanceSuppressed.Id], policy.GovernanceSuppressedCandidateIds);
        var governanceSuppressionSummary = Assert.Single(policy.GovernanceSuppressionSummaries);
        Assert.Equal("hide-generated", governanceSuppressionSummary.RuleId);
        Assert.Equal([governanceSuppressed.Id], governanceSuppressionSummary.MatchedCandidateIds);
        Assert.Equal([governanceSuppressed.Id], governanceSuppressionSummary.SuppressedCandidateIds);
        Assert.Equal(
            governanceSuppressed.SuppressionSelectionBasis!.Value,
            Assert.Single(governanceSuppressionSummary.SelectionBasisSummaries).SelectionBasis);
        Assert.Equal(3, policy.AuthoringStyleSummaries.Count);
        var explicitStyle = Assert.Single(policy.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([published.Id], explicitStyle.CandidateIds);
        Assert.Equal([published.Id], explicitStyle.RetainedCandidateIds);
        Assert.Equal([published.Id], explicitStyle.PublishedCandidateIds);
        Assert.Empty(explicitStyle.PrecedenceSuppressedCandidateIds);
        Assert.Empty(explicitStyle.GovernanceSuppressedCandidateIds);
        Assert.Empty(explicitStyle.SuppressedCandidateIds);
        Assert.Empty(explicitStyle.HostGovernanceEligibleCandidateIds);
        Assert.Equal([published.Id], explicitStyle.HostGovernanceIneligibleCandidateIds);
        Assert.Empty(explicitStyle.SkippedSuppressionIds);
        Assert.Empty(explicitStyle.SkippedOverrideIds);
        Assert.Empty(explicitStyle.GovernanceSuppressionSummaries);
        Assert.Empty(explicitStyle.GovernanceOverrideSummaries);
        Assert.Empty(explicitStyle.SkippedSuppressionSummaries);
        Assert.Empty(explicitStyle.SkippedOverrideSummaries);

        var profileStyle = Assert.Single(policy.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([precedenceSuppressed.Id], profileStyle.CandidateIds);
        Assert.Equal([precedenceSuppressed.Id], profileStyle.RetainedCandidateIds);
        Assert.Empty(profileStyle.PublishedCandidateIds);
        Assert.Equal([precedenceSuppressed.Id], profileStyle.PrecedenceSuppressedCandidateIds);
        Assert.Empty(profileStyle.GovernanceSuppressedCandidateIds);
        Assert.Empty(profileStyle.SuppressedCandidateIds);
        Assert.Equal([precedenceSuppressed.Id], profileStyle.HostGovernanceEligibleCandidateIds);
        Assert.Empty(profileStyle.HostGovernanceIneligibleCandidateIds);
        Assert.Empty(profileStyle.SkippedSuppressionIds);
        Assert.Empty(profileStyle.SkippedOverrideIds);
        Assert.Empty(profileStyle.GovernanceSuppressionSummaries);
        Assert.Empty(profileStyle.GovernanceOverrideSummaries);
        Assert.Empty(profileStyle.SkippedSuppressionSummaries);
        Assert.Empty(profileStyle.SkippedOverrideSummaries);

        var generatedStyle = Assert.Single(policy.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([governanceSuppressed.Id], generatedStyle.CandidateIds);
        Assert.Equal([governanceSuppressed.Id], generatedStyle.RetainedCandidateIds);
        Assert.Empty(generatedStyle.PublishedCandidateIds);
        Assert.Empty(generatedStyle.PrecedenceSuppressedCandidateIds);
        Assert.Equal([governanceSuppressed.Id], generatedStyle.GovernanceSuppressedCandidateIds);
        Assert.Empty(generatedStyle.SuppressedCandidateIds);
        Assert.Equal([governanceSuppressed.Id], generatedStyle.HostGovernanceEligibleCandidateIds);
        Assert.Empty(generatedStyle.HostGovernanceIneligibleCandidateIds);
        Assert.Empty(generatedStyle.SkippedSuppressionIds);
        Assert.Empty(generatedStyle.SkippedOverrideIds);
        var generatedGovernanceSuppressionSummary = Assert.Single(generatedStyle.GovernanceSuppressionSummaries);
        Assert.Equal("hide-generated", generatedGovernanceSuppressionSummary.RuleId);
        Assert.Equal([governanceSuppressed.Id], generatedGovernanceSuppressionSummary.MatchedCandidateIds);
        Assert.Equal([governanceSuppressed.Id], generatedGovernanceSuppressionSummary.SuppressedCandidateIds);
        Assert.Empty(generatedStyle.GovernanceOverrideSummaries);
        Assert.Empty(generatedStyle.SkippedSuppressionSummaries);
        Assert.Empty(generatedStyle.SkippedOverrideSummaries);

        Assert.Equal(policy.RetainedCandidateIds, policyByBehavior.RetainedCandidateIds);
        Assert.Equal(policy.PrecedenceSuppressedCandidateIds, policyByBehavior.PrecedenceSuppressedCandidateIds);
        Assert.Equal(policy.GovernanceSuppressedCandidateIds, policyByBehavior.GovernanceSuppressedCandidateIds);
        Assert.Equal(policy.HostGovernanceEligibleCandidateIds, policyByBehavior.HostGovernanceEligibleCandidateIds);
        Assert.Equal(policy.HostGovernanceIneligibleCandidateIds, policyByBehavior.HostGovernanceIneligibleCandidateIds);
        Assert.Equal(policy.SkippedSuppressionIds, policyByBehavior.SkippedSuppressionIds);
        Assert.Equal(policy.SkippedOverrideIds, policyByBehavior.SkippedOverrideIds);
        Assert.Equal(policy.GovernanceSuppressionSummaries.Count, policyByBehavior.GovernanceSuppressionSummaries.Count);
        Assert.Equal(policy.GovernanceOverrideSummaries.Count, policyByBehavior.GovernanceOverrideSummaries.Count);
        Assert.Equal(policy.SkippedSuppressionSummaries.Count, policyByBehavior.SkippedSuppressionSummaries.Count);
        Assert.Equal(policy.SkippedOverrideSummaries.Count, policyByBehavior.SkippedOverrideSummaries.Count);
        Assert.Equal(policy.AuthoringStyleSummaries.Count, policyByBehavior.AuthoringStyleSummaries.Count);
        Assert.Contains(snapshot.RestEndpointAuthoringPolicies, item =>
            string.Equals(item.BehaviorId, policy.BehaviorId, StringComparison.Ordinal) &&
            item.RetainedCandidateIds.Count == 3 &&
            item.PrecedenceSuppressedCandidateIds.Count == 1 &&
            item.GovernanceSuppressedCandidateIds.Count == 1 &&
            item.SuppressedCandidateIds.Count == 0 &&
            item.HostGovernanceEligibleCandidateIds.Count == 2 &&
            item.HostGovernanceIneligibleCandidateIds.Count == 1 &&
            item.SkippedSuppressionIds.Count == 0 &&
            item.SkippedOverrideIds.Count == 0 &&
            item.GovernanceSuppressionSummaries.Count == 1 &&
            item.GovernanceOverrideSummaries.Count == 0 &&
            item.SkippedSuppressionSummaries.Count == 0 &&
            item.SkippedOverrideSummaries.Count == 0 &&
            item.AuthoringStyleSummaries.Count == 3);
    }

    [Fact]
    public async Task MapCephalonExposesRestEndpointAuthoringPolicyHostGovernanceEligibilityAndSkippedRuleVisibility()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "9";
        builder.Configuration["OpenApi:DefaultVersion"] = "9";
        builder.Configuration["RestApi:Suppressions:skip-disabled-explicit:HostGovernanceScopes:0"] = "explicit-disabled";
        builder.Configuration["RestApi:Suppressions:skip-disabled-explicit:AuthoringStyles:0"] = RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:Overrides:rewrite-disabled-explicit:HostGovernanceScopes:0"] = "explicit-disabled";
        builder.Configuration["RestApi:Overrides:rewrite-disabled-explicit:AuthoringStyles:0"] = RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:Overrides:rewrite-disabled-explicit:Pattern"] = "/governed/{orderId}";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ExplicitDslHostGovernanceDisabledRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var policies = await client.GetFromJsonAsync<RestEndpointAuthoringPolicyDescriptor[]>("/engine/rest-endpoint-authoring-policies");
        var policyByBehavior = await client.GetFromJsonAsync<RestEndpointAuthoringPolicyDescriptor>(
            "/engine/rest-endpoint-authoring-policies/tests.dsl.runtimeoverride.disabled.lookup");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var suppressions = await client.GetFromJsonAsync<RestEndpointSuppressionDescriptor[]>("/engine/rest-endpoint-suppressions");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");
        var policiesJson = await client.GetStringAsync("/engine/rest-endpoint-authoring-policies");

        Assert.NotNull(policies);
        Assert.NotNull(policyByBehavior);
        Assert.NotNull(candidates);
        Assert.NotNull(suppressions);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.dsl.runtimeoverride.disabled.lookup", StringComparison.Ordinal));
        var policy = Assert.Single(policies, static item =>
            string.Equals(item.BehaviorId, "tests.dsl.runtimeoverride.disabled.lookup", StringComparison.Ordinal));
        Assert.Equal([candidate.Id], policy.CandidateIds);
        Assert.Equal([candidate.Id], policy.RetainedCandidateIds);
        Assert.Equal([candidate.Id], policy.PublishedCandidateIds);
        Assert.Empty(policy.PrecedenceSuppressedCandidateIds);
        Assert.Empty(policy.GovernanceSuppressedCandidateIds);
        Assert.Empty(policy.SuppressedCandidateIds);
        Assert.Empty(policy.SuppressionKinds);
        Assert.Empty(policy.SuppressionSummaries);
        Assert.Empty(policy.HostGovernanceEligibleCandidateIds);
        Assert.Equal([candidate.Id], policy.HostGovernanceIneligibleCandidateIds);
        Assert.Equal(["skip-disabled-explicit"], policy.SkippedSuppressionIds);
        Assert.Equal(["rewrite-disabled-explicit"], policy.SkippedOverrideIds);
        Assert.Empty(policy.GovernanceSuppressionSummaries);
        Assert.Empty(policy.GovernanceOverrideSummaries);
        var skippedSuppressionSummary = Assert.Single(policy.SkippedSuppressionSummaries);
        Assert.Equal("skip-disabled-explicit", skippedSuppressionSummary.RuleId);
        Assert.Equal([candidate.Id], skippedSuppressionSummary.CandidateIds);
        var skippedOverrideSummary = Assert.Single(policy.SkippedOverrideSummaries);
        Assert.Equal("rewrite-disabled-explicit", skippedOverrideSummary.RuleId);
        Assert.Equal([candidate.Id], skippedOverrideSummary.CandidateIds);
        var explicitStyle = Assert.Single(policy.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([candidate.Id], explicitStyle.CandidateIds);
        Assert.Equal([candidate.Id], explicitStyle.PublishedCandidateIds);
        Assert.Empty(explicitStyle.HostGovernanceEligibleCandidateIds);
        Assert.Equal([candidate.Id], explicitStyle.HostGovernanceIneligibleCandidateIds);
        Assert.Equal(["skip-disabled-explicit"], explicitStyle.SkippedSuppressionIds);
        Assert.Equal(["rewrite-disabled-explicit"], explicitStyle.SkippedOverrideIds);
        Assert.Empty(explicitStyle.GovernanceSuppressionSummaries);
        Assert.Empty(explicitStyle.GovernanceOverrideSummaries);
        Assert.Equal("skip-disabled-explicit", Assert.Single(explicitStyle.SkippedSuppressionSummaries).RuleId);
        Assert.Equal("rewrite-disabled-explicit", Assert.Single(explicitStyle.SkippedOverrideSummaries).RuleId);
        var skippedSuppressionRule = Assert.Single(suppressions, static item =>
            string.Equals(item.Id, "skip-disabled-explicit", StringComparison.Ordinal));
        Assert.Empty(skippedSuppressionRule.BehaviorIds);
        Assert.Equal(["explicit-disabled"], skippedSuppressionRule.HostGovernanceScopes);
        var skippedOverrideRule = Assert.Single(overrides, static item =>
            string.Equals(item.Id, "rewrite-disabled-explicit", StringComparison.Ordinal));
        Assert.Empty(skippedOverrideRule.BehaviorIds);
        Assert.Equal(["explicit-disabled"], skippedOverrideRule.HostGovernanceScopes);

        Assert.Equal(policy.HostGovernanceEligibleCandidateIds, policyByBehavior.HostGovernanceEligibleCandidateIds);
        Assert.Equal(policy.HostGovernanceIneligibleCandidateIds, policyByBehavior.HostGovernanceIneligibleCandidateIds);
        Assert.Equal(policy.SkippedSuppressionIds, policyByBehavior.SkippedSuppressionIds);
        Assert.Equal(policy.SkippedOverrideIds, policyByBehavior.SkippedOverrideIds);
        Assert.Collection(
            snapshot.RestEndpointAuthoringPolicies,
            item =>
            {
                Assert.Equal(policy.BehaviorId, item.BehaviorId);
                Assert.Equal(policy.HostGovernanceEligibleCandidateIds, item.HostGovernanceEligibleCandidateIds);
                Assert.Equal(policy.HostGovernanceIneligibleCandidateIds, item.HostGovernanceIneligibleCandidateIds);
                Assert.Equal(policy.SkippedSuppressionIds, item.SkippedSuppressionIds);
                Assert.Equal(policy.SkippedOverrideIds, item.SkippedOverrideIds);
                Assert.Equal(policy.SkippedSuppressionSummaries.Count, item.SkippedSuppressionSummaries.Count);
                Assert.Equal(policy.SkippedOverrideSummaries.Count, item.SkippedOverrideSummaries.Count);
            });
        Assert.Contains("\"hostGovernanceEligibleCandidateIds\":[]", policiesJson, StringComparison.Ordinal);
        Assert.Contains("\"hostGovernanceIneligibleCandidateIds\":[\"", policiesJson, StringComparison.Ordinal);
        Assert.Contains("\"skippedSuppressionIds\":[\"skip-disabled-explicit\"]", policiesJson, StringComparison.Ordinal);
        Assert.Contains("\"skippedOverrideIds\":[\"rewrite-disabled-explicit\"]", policiesJson, StringComparison.Ordinal);
        Assert.Contains("\"skippedSuppressionSummaries\":[", policiesJson, StringComparison.Ordinal);
        Assert.Contains("\"skippedOverrideSummaries\":[", policiesJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonPublishesMultipleRestEndpointCandidatesWhenAllowMultipleAuthoringPolicyIsEnabled()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:AllowMultiplePublishedCandidates"] = "true";
        builder.Configuration["RestApi:Overrides:split-generated:HostGovernanceScopes:0"] = "generated-governance";
        builder.Configuration["RestApi:Overrides:split-generated:AuthoringStyles:0"] =
            RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle;
        builder.Configuration["RestApi:Overrides:split-generated:Pattern"] = "/generated/{orderId}";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedProfileGovernanceRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var groups = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor[]>("/engine/rest-endpoint-publication-groups");
        var policies = await client.GetFromJsonAsync<RestEndpointAuthoringPolicyDescriptor[]>("/engine/rest-endpoint-authoring-policies");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(groups);
        Assert.NotNull(policies);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);
        Assert.Equal(2, endpoints.Length);
        Assert.Equal(2, candidates.Length);
        Assert.All(candidates, static candidate => Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status));

        var profileEndpoint = Assert.Single(endpoints, static endpoint =>
            string.Equals(endpoint.RoutePattern, "/api/v6/tests/generated/runtime/governed/orders/{orderId}", StringComparison.Ordinal));
        var generatedEndpoint = Assert.Single(endpoints, static endpoint =>
            string.Equals(endpoint.RoutePattern, "/api/v6/tests/generated/runtime/governed/orders/generated/{orderId}", StringComparison.Ordinal));
        var profileCandidate = Assert.Single(candidates, static candidate =>
            string.Equals(candidate.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, StringComparison.Ordinal));
        var generatedCandidate = Assert.Single(candidates, static candidate =>
            string.Equals(candidate.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal("tests.rest.generated.threeway.lookup", profileEndpoint.BehaviorId);
        Assert.Equal("tests.rest.generated.threeway.lookup", generatedEndpoint.BehaviorId);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, profileEndpoint.AuthoringStyle);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, generatedEndpoint.AuthoringStyle);
        Assert.NotNull(profileEndpoint.EndpointName);
        Assert.NotNull(generatedEndpoint.EndpointName);
        Assert.NotEqual(profileEndpoint.EndpointName, generatedEndpoint.EndpointName);
        Assert.EndsWith(".behavior_module_profile", profileEndpoint.EndpointName, StringComparison.Ordinal);
        Assert.EndsWith(".behavior_module_generated", generatedEndpoint.EndpointName, StringComparison.Ordinal);
        Assert.Equal("split-generated", generatedEndpoint.AppliedOverrideId);
        Assert.Equal("split-generated", generatedCandidate.SelectedOverrideId);
        Assert.Equal("split-generated", generatedCandidate.AppliedOverrideId);
        var policy = Assert.Single(policies, static item =>
            string.Equals(item.BehaviorId, "tests.rest.generated.threeway.lookup", StringComparison.Ordinal));
        Assert.Empty(policy.GovernanceSuppressionSummaries);
        Assert.Empty(policy.SkippedSuppressionSummaries);
        Assert.Empty(policy.SkippedOverrideSummaries);
        var policyOverrideSummary = Assert.Single(policy.GovernanceOverrideSummaries);
        Assert.Equal("split-generated", policyOverrideSummary.RuleId);
        Assert.Equal([generatedCandidate.Id], policyOverrideSummary.MatchedCandidateIds);
        Assert.Equal([generatedCandidate.Id], policyOverrideSummary.SelectedCandidateIds);
        Assert.Equal([generatedCandidate.Id], policyOverrideSummary.AppliedCandidateIds);
        Assert.Equal(
            generatedCandidate.OverrideSelectionBasis!.Value,
            Assert.Single(policyOverrideSummary.SelectionBasisSummaries).SelectionBasis);
        Assert.Equal(
            generatedCandidate.SelectedOverrideActionKinds,
            policyOverrideSummary.SelectedActionKindSummaries.Select(static item => item.ActionKind).ToArray());
        Assert.Equal(
            generatedCandidate.AppliedOverrideActionKinds,
            policyOverrideSummary.AppliedActionKindSummaries.Select(static item => item.ActionKind).ToArray());
        Assert.Equal(2, policy.AuthoringStyleSummaries.Count);
        var policyProfileStyle = Assert.Single(policy.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, StringComparison.Ordinal));
        Assert.Empty(policyProfileStyle.GovernanceSuppressionSummaries);
        Assert.Empty(policyProfileStyle.GovernanceOverrideSummaries);
        Assert.Empty(policyProfileStyle.SkippedSuppressionSummaries);
        Assert.Empty(policyProfileStyle.SkippedOverrideSummaries);
        var policyGeneratedStyle = Assert.Single(policy.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        var policyGeneratedOverrideSummary = Assert.Single(policyGeneratedStyle.GovernanceOverrideSummaries);
        Assert.Equal("split-generated", policyGeneratedOverrideSummary.RuleId);
        Assert.Equal([generatedCandidate.Id], policyGeneratedOverrideSummary.MatchedCandidateIds);
        Assert.Equal([generatedCandidate.Id], policyGeneratedOverrideSummary.SelectedCandidateIds);
        Assert.Equal([generatedCandidate.Id], policyGeneratedOverrideSummary.AppliedCandidateIds);

        var group = Assert.Single(groups, static item =>
            string.Equals(item.BehaviorId, "tests.rest.generated.threeway.lookup", StringComparison.Ordinal));
        Assert.True(group.AuthoringPolicy.IsConfigured);
        Assert.True(group.AuthoringPolicy.AllowMultiplePublishedCandidates);
        Assert.Equal(2, group.PublishedCandidateIds.Count);
        Assert.Empty(group.PrecedenceSuppressedCandidateIds);
        Assert.Empty(group.GovernanceSuppressedCandidateIds);
        Assert.Empty(group.AuthoringPolicySuppressedCandidateIds);
        Assert.Single(group.GovernanceOverrideSummaries);
        var overrideSummary = Assert.Single(group.GovernanceOverrideSummaries, static item =>
            string.Equals(item.RuleId, "split-generated", StringComparison.Ordinal));
        Assert.Equal([generatedCandidate.Id], overrideSummary.MatchedCandidateIds);
        Assert.Equal([generatedCandidate.Id], overrideSummary.SelectedCandidateIds);
        Assert.Equal([generatedCandidate.Id], overrideSummary.AppliedCandidateIds);
        Assert.NotNull(generatedCandidate.OverrideSelectionBasis);
        var generatedSelectionBasisSummary = Assert.Single(overrideSummary.SelectionBasisSummaries);
        Assert.Equal(generatedCandidate.OverrideSelectionBasis.Value, generatedSelectionBasisSummary.SelectionBasis);
        Assert.Equal([generatedCandidate.Id], generatedSelectionBasisSummary.CandidateIds);
        Assert.Equal(generatedCandidate.SelectedOverrideActionKinds, overrideSummary.SelectedActionKindSummaries.Select(static item => item.ActionKind).ToArray());
        Assert.All(
            overrideSummary.SelectedActionKindSummaries,
            summary => Assert.Equal([generatedCandidate.Id], summary.CandidateIds));
        Assert.Equal(generatedCandidate.AppliedOverrideActionKinds, overrideSummary.AppliedActionKindSummaries.Select(static item => item.ActionKind).ToArray());
        Assert.All(
            overrideSummary.AppliedActionKindSummaries,
            summary => Assert.Equal([generatedCandidate.Id], summary.CandidateIds));
        Assert.Equal(2, group.AuthoringStyleSummaries.Count);
        var profileStyle = Assert.Single(group.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, StringComparison.Ordinal));
        Assert.Single(profileStyle.PublishedCandidateIds);
        Assert.Empty(profileStyle.PrecedenceSuppressedCandidateIds);
        Assert.Empty(profileStyle.AuthoringPolicySuppressedCandidateIds);
        Assert.Empty(profileStyle.GovernanceSuppressionSummaries);
        Assert.Empty(profileStyle.GovernanceOverrideSummaries);
        var generatedStyle = Assert.Single(group.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Single(generatedStyle.PublishedCandidateIds);
        Assert.Empty(generatedStyle.PrecedenceSuppressedCandidateIds);
        Assert.Empty(generatedStyle.AuthoringPolicySuppressedCandidateIds);
        var generatedOverrideSummary = Assert.Single(generatedStyle.GovernanceOverrideSummaries);
        Assert.Equal("split-generated", generatedOverrideSummary.RuleId);
        Assert.Equal([generatedCandidate.Id], generatedOverrideSummary.MatchedCandidateIds);
        Assert.Equal([generatedCandidate.Id], generatedOverrideSummary.SelectedCandidateIds);
        Assert.Equal([generatedCandidate.Id], generatedOverrideSummary.AppliedCandidateIds);
        Assert.Equal(generatedCandidate.OverrideSelectionBasis.Value, Assert.Single(generatedOverrideSummary.SelectionBasisSummaries).SelectionBasis);
        Assert.Equal(generatedCandidate.SelectedOverrideActionKinds, generatedOverrideSummary.SelectedActionKindSummaries.Select(static item => item.ActionKind).ToArray());
        Assert.Equal(generatedCandidate.AppliedOverrideActionKinds, generatedOverrideSummary.AppliedActionKindSummaries.Select(static item => item.ActionKind).ToArray());
        var overrideRule = Assert.Single(overrides, static item => string.Equals(item.Id, "split-generated", StringComparison.Ordinal));
        Assert.Empty(overrideRule.BehaviorIds);
        Assert.Equal(["generated-governance"], overrideRule.HostGovernanceScopes);
        Assert.Equal([generatedCandidate.Id], overrideRule.MatchedCandidateIds);
        Assert.Equal([generatedCandidate.Id], overrideRule.SelectedCandidateIds);
        Assert.Equal([generatedCandidate.Id], overrideRule.AppliedCandidateIds);
        Assert.Empty(overrideRule.SkippedCandidateIds);
        Assert.Equal([generatedCandidate.OverrideSelectionBasis!.Value], overrideRule.SelectionBases);
        Assert.Equal(generatedCandidate.SelectedOverrideActionKinds, overrideRule.SelectedActionKinds);
        Assert.Equal(generatedCandidate.AppliedOverrideActionKinds, overrideRule.AppliedActionKinds);
        Assert.Contains(snapshot.RestEndpointPublicationGroups, item =>
            string.Equals(item.BehaviorId, group.BehaviorId, StringComparison.Ordinal) &&
            item.AuthoringPolicy.AllowMultiplePublishedCandidates &&
            item.PublishedCandidateIds.Count == 2 &&
            item.PrecedenceSuppressedCandidateIds.Count == 0 &&
            item.AuthoringPolicySuppressedCandidateIds.Count == 0 &&
            item.GovernanceOverrideSummaries.Count == 1 &&
            string.Equals(item.GovernanceOverrideSummaries[0].RuleId, "split-generated", StringComparison.Ordinal) &&
            item.GovernanceOverrideSummaries[0].SelectionBasisSummaries.Count == 1 &&
            item.GovernanceOverrideSummaries[0].SelectedActionKindSummaries.Count == generatedCandidate.SelectedOverrideActionKinds.Count &&
            item.GovernanceOverrideSummaries[0].AppliedActionKindSummaries.Count == generatedCandidate.AppliedOverrideActionKinds.Count);
        Assert.Contains(snapshot.RestEndpointOverrides, item =>
            string.Equals(item.Id, "split-generated", StringComparison.Ordinal) &&
            item.MatchedCandidateIds.Count == 1 &&
            string.Equals(item.MatchedCandidateIds[0], generatedCandidate.Id, StringComparison.Ordinal) &&
            item.SelectedCandidateIds.Count == 1 &&
            string.Equals(item.SelectedCandidateIds[0], generatedCandidate.Id, StringComparison.Ordinal) &&
            item.AppliedCandidateIds.Count == 1 &&
            string.Equals(item.AppliedCandidateIds[0], generatedCandidate.Id, StringComparison.Ordinal) &&
            item.SkippedCandidateIds.Count == 0 &&
            item.SelectionBases.Count == 1 &&
            item.SelectionBases[0] == generatedCandidate.OverrideSelectionBasis &&
            item.SelectedActionKinds.Count == generatedCandidate.SelectedOverrideActionKinds.Count &&
            item.AppliedActionKinds.Count == generatedCandidate.AppliedOverrideActionKinds.Count);
        Assert.Contains(snapshot.RestEndpointAuthoringPolicies, item =>
            string.Equals(item.BehaviorId, policy.BehaviorId, StringComparison.Ordinal) &&
            item.GovernanceSuppressionSummaries.Count == 0 &&
            item.GovernanceOverrideSummaries.Count == 1 &&
            string.Equals(item.GovernanceOverrideSummaries[0].RuleId, "split-generated", StringComparison.Ordinal) &&
            item.SkippedSuppressionSummaries.Count == 0 &&
            item.SkippedOverrideSummaries.Count == 0 &&
            item.AuthoringStyleSummaries.Count == 2);

        var profilePayload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>(
            "/api/v6/tests/generated/runtime/governed/orders/ord-61");
        var generatedPayload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>(
            "/api/v6/tests/generated/runtime/governed/orders/generated/ord-62");
        Assert.NotNull(profilePayload);
        Assert.NotNull(generatedPayload);
        Assert.Equal("ord-61", profilePayload.OrderId);
        Assert.Equal("ord-62", generatedPayload.OrderId);
    }

    [Fact]
    public async Task MapCephalonExposesRestEndpointPublicationGroupsForGovernanceSuppression()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Suppressions:prefer-generated:HostGovernanceScopes:0"] = "generated-governance";
        builder.Configuration["RestApi:Suppressions:prefer-generated:AuthoringStyles:0"] = RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle;
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedProfileGovernanceRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var groups = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor[]>("/engine/rest-endpoint-publication-groups");
        var suppressions = await client.GetFromJsonAsync<RestEndpointSuppressionDescriptor[]>("/engine/rest-endpoint-suppressions");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(candidates);
        Assert.NotNull(groups);
        Assert.NotNull(suppressions);
        Assert.NotNull(snapshot);

        var behaviorCandidates = candidates
            .Where(static candidate => string.Equals(candidate.ProjectedEndpoint.BehaviorId, "tests.rest.generated.threeway.lookup", StringComparison.Ordinal))
            .ToArray();
        var published = Assert.Single(behaviorCandidates, static candidate => candidate.Status == RestEndpointCandidateStatus.Published);
        var governanceSuppressed = Assert.Single(behaviorCandidates, static candidate =>
            candidate.Status == RestEndpointCandidateStatus.Suppressed &&
            string.Equals(candidate.SuppressedBySuppressionId, "prefer-generated", StringComparison.Ordinal));

        var group = Assert.Single(groups, static item =>
            string.Equals(item.BehaviorId, "tests.rest.generated.threeway.lookup", StringComparison.Ordinal));
        Assert.False(group.AuthoringPolicy.IsConfigured);
        Assert.False(group.AuthoringPolicy.AllowMultiplePublishedCandidates);
        Assert.Equal(4, group.WinningPrecedenceRank);
        Assert.Single(group.PublishedCandidateIds);
        Assert.Equal(published.Id, group.PublishedCandidateIds[0]);
        Assert.Empty(group.PrecedenceSuppressedCandidateIds);
        Assert.Single(group.GovernanceSuppressedCandidateIds);
        Assert.Equal(governanceSuppressed.Id, group.GovernanceSuppressedCandidateIds[0]);
        Assert.Single(group.GovernanceSuppressionSummaries);
        var suppressionSummary = Assert.Single(group.GovernanceSuppressionSummaries, static item =>
            string.Equals(item.RuleId, "prefer-generated", StringComparison.Ordinal));
        Assert.Equal([governanceSuppressed.Id], suppressionSummary.MatchedCandidateIds);
        Assert.Equal([governanceSuppressed.Id], suppressionSummary.SuppressedCandidateIds);
        Assert.NotNull(governanceSuppressed.SuppressionSelectionBasis);
        var suppressionSelectionBasisSummary = Assert.Single(suppressionSummary.SelectionBasisSummaries);
        Assert.Equal(governanceSuppressed.SuppressionSelectionBasis.Value, suppressionSelectionBasisSummary.SelectionBasis);
        Assert.Equal([governanceSuppressed.Id], suppressionSelectionBasisSummary.CandidateIds);
        var suppressionRule = Assert.Single(suppressions, static item =>
            string.Equals(item.Id, "prefer-generated", StringComparison.Ordinal));
        Assert.Empty(suppressionRule.BehaviorIds);
        Assert.Equal(["generated-governance"], suppressionRule.HostGovernanceScopes);
        Assert.Equal([governanceSuppressed.Id], suppressionRule.MatchedCandidateIds);
        Assert.Equal([governanceSuppressed.Id], suppressionRule.SuppressedCandidateIds);
        Assert.Equal(2, group.HostGovernanceEligibleCandidateIds.Count);
        Assert.Empty(group.HostGovernanceIneligibleCandidateIds);
        Assert.Empty(group.SkippedSuppressionIds);
        Assert.Empty(group.SkippedOverrideIds);
        Assert.Empty(group.SkippedSuppressionSummaries);
        Assert.Empty(group.SkippedOverrideSummaries);
        Assert.Equal(2, group.Candidates.Count);
        Assert.Equal(2, group.AuthoringStyleSummaries.Count);
        var profileStyle = Assert.Single(group.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([governanceSuppressed.Id], profileStyle.CandidateIds);
        Assert.Equal([governanceSuppressed.Id], profileStyle.GovernanceSuppressedCandidateIds);
        Assert.Equal([RestEndpointRuntimeMetadata.BehaviorModuleProfilePrecedenceRank], profileStyle.PrecedenceRanks);
        Assert.Equal([governanceSuppressed.Id], profileStyle.HostGovernanceEligibleCandidateIds);
        Assert.Empty(profileStyle.HostGovernanceIneligibleCandidateIds);
        Assert.Empty(profileStyle.PublishedCandidateIds);
        Assert.Empty(profileStyle.PrecedenceSuppressedCandidateIds);
        Assert.Empty(profileStyle.SkippedSuppressionIds);
        Assert.Empty(profileStyle.SkippedOverrideIds);
        Assert.Empty(profileStyle.SkippedSuppressionSummaries);
        Assert.Empty(profileStyle.SkippedOverrideSummaries);
        var profileSuppressionSummary = Assert.Single(profileStyle.GovernanceSuppressionSummaries);
        Assert.Equal("prefer-generated", profileSuppressionSummary.RuleId);
        Assert.Equal([governanceSuppressed.Id], profileSuppressionSummary.MatchedCandidateIds);
        Assert.Equal([governanceSuppressed.Id], profileSuppressionSummary.SuppressedCandidateIds);
        Assert.Equal(governanceSuppressed.SuppressionSelectionBasis.Value, Assert.Single(profileSuppressionSummary.SelectionBasisSummaries).SelectionBasis);
        Assert.Empty(profileStyle.GovernanceOverrideSummaries);
        var generatedStyle = Assert.Single(group.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([published.Id], generatedStyle.CandidateIds);
        Assert.Equal([published.Id], generatedStyle.PublishedCandidateIds);
        Assert.Equal([RestEndpointRuntimeMetadata.BehaviorModuleGeneratedPrecedenceRank], generatedStyle.PrecedenceRanks);
        Assert.Equal([published.Id], generatedStyle.HostGovernanceEligibleCandidateIds);
        Assert.Empty(generatedStyle.HostGovernanceIneligibleCandidateIds);
        Assert.Empty(generatedStyle.PrecedenceSuppressedCandidateIds);
        Assert.Empty(generatedStyle.GovernanceSuppressedCandidateIds);
        Assert.Empty(generatedStyle.SkippedSuppressionIds);
        Assert.Empty(generatedStyle.SkippedOverrideIds);
        Assert.Empty(generatedStyle.SkippedSuppressionSummaries);
        Assert.Empty(generatedStyle.SkippedOverrideSummaries);
        Assert.Empty(generatedStyle.GovernanceSuppressionSummaries);
        Assert.Empty(generatedStyle.GovernanceOverrideSummaries);
        Assert.Contains(snapshot.RestEndpointPublicationGroups, item =>
            string.Equals(item.BehaviorId, group.BehaviorId, StringComparison.Ordinal) &&
            item.AuthoringPolicy.IsConfigured == false &&
            item.AuthoringStyleSummaries.Count == 2 &&
            item.GovernanceSuppressedCandidateIds.Count == 1 &&
            item.GovernanceSuppressionSummaries.Count == 1 &&
            item.GovernanceSuppressionSummaries[0].SelectionBasisSummaries.Count == 1 &&
            item.HostGovernanceEligibleCandidateIds.Count == 2 &&
            item.HostGovernanceIneligibleCandidateIds.Count == 0 &&
            item.SkippedSuppressionIds.Count == 0 &&
            item.SkippedOverrideIds.Count == 0 &&
            item.SkippedSuppressionSummaries.Count == 0 &&
            item.SkippedOverrideSummaries.Count == 0 &&
            string.Equals(item.GovernanceSuppressedCandidateIds[0], governanceSuppressed.Id, StringComparison.Ordinal));
    }

    [Fact]
    public async Task MapCephalonExposesRestEndpointPublicationGroupsForExplicitDslGovernanceSkippedVisibility()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "9";
        builder.Configuration["OpenApi:DefaultVersion"] = "9";
        builder.Configuration["RestApi:Suppressions:skip-disabled-explicit:HostGovernanceScopes:0"] = "explicit-disabled";
        builder.Configuration["RestApi:Suppressions:skip-disabled-explicit:AuthoringStyles:0"] = RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:Overrides:rewrite-disabled-explicit:HostGovernanceScopes:0"] = "explicit-disabled";
        builder.Configuration["RestApi:Overrides:rewrite-disabled-explicit:AuthoringStyles:0"] = RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:Overrides:rewrite-disabled-explicit:Pattern"] = "/governed/{orderId}";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ExplicitDslHostGovernanceDisabledRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var groups = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor[]>("/engine/rest-endpoint-publication-groups");
        var suppressions = await client.GetFromJsonAsync<RestEndpointSuppressionDescriptor[]>("/engine/rest-endpoint-suppressions");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var groupByBehavior = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor>(
            "/engine/rest-endpoint-publication-groups/tests.dsl.runtimeoverride.disabled.lookup");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(candidates);
        Assert.NotNull(groups);
        Assert.NotNull(suppressions);
        Assert.NotNull(overrides);
        Assert.NotNull(groupByBehavior);
        Assert.NotNull(snapshot);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.dsl.runtimeoverride.disabled.lookup", StringComparison.Ordinal));
        var group = Assert.Single(groups, static item =>
            string.Equals(item.BehaviorId, "tests.dsl.runtimeoverride.disabled.lookup", StringComparison.Ordinal));
        Assert.Equal([candidate.Id], group.PublishedCandidateIds);
        Assert.Empty(group.PrecedenceSuppressedCandidateIds);
        Assert.Empty(group.GovernanceSuppressedCandidateIds);
        Assert.Empty(group.AuthoringPolicySuppressedCandidateIds);
        Assert.Empty(group.HostGovernanceEligibleCandidateIds);
        Assert.Equal([candidate.Id], group.HostGovernanceIneligibleCandidateIds);
        Assert.Equal(["skip-disabled-explicit"], group.SkippedSuppressionIds);
        Assert.Equal(["rewrite-disabled-explicit"], group.SkippedOverrideIds);
        var skippedSuppressionSummary = Assert.Single(group.SkippedSuppressionSummaries, static item =>
            string.Equals(item.RuleId, "skip-disabled-explicit", StringComparison.Ordinal));
        Assert.Equal([candidate.Id], skippedSuppressionSummary.CandidateIds);
        var skippedOverrideSummary = Assert.Single(group.SkippedOverrideSummaries, static item =>
            string.Equals(item.RuleId, "rewrite-disabled-explicit", StringComparison.Ordinal));
        Assert.Equal([candidate.Id], skippedOverrideSummary.CandidateIds);
        Assert.Single(group.AuthoringStyleSummaries);

        var explicitStyle = Assert.Single(group.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal([candidate.Id], explicitStyle.CandidateIds);
        Assert.Equal([candidate.Id], explicitStyle.PublishedCandidateIds);
        Assert.Empty(explicitStyle.PrecedenceSuppressedCandidateIds);
        Assert.Empty(explicitStyle.GovernanceSuppressedCandidateIds);
        Assert.Empty(explicitStyle.AuthoringPolicySuppressedCandidateIds);
        Assert.Empty(explicitStyle.HostGovernanceEligibleCandidateIds);
        Assert.Equal([candidate.Id], explicitStyle.HostGovernanceIneligibleCandidateIds);
        Assert.Equal(["skip-disabled-explicit"], explicitStyle.SkippedSuppressionIds);
        Assert.Equal(["rewrite-disabled-explicit"], explicitStyle.SkippedOverrideIds);
        var explicitStyleSkippedSuppressionSummary = Assert.Single(explicitStyle.SkippedSuppressionSummaries, static item =>
            string.Equals(item.RuleId, "skip-disabled-explicit", StringComparison.Ordinal));
        Assert.Equal([candidate.Id], explicitStyleSkippedSuppressionSummary.CandidateIds);
        var explicitStyleSkippedOverrideSummary = Assert.Single(explicitStyle.SkippedOverrideSummaries, static item =>
            string.Equals(item.RuleId, "rewrite-disabled-explicit", StringComparison.Ordinal));
        Assert.Equal([candidate.Id], explicitStyleSkippedOverrideSummary.CandidateIds);
        var skippedSuppressionRule = Assert.Single(suppressions, static item =>
            string.Equals(item.Id, "skip-disabled-explicit", StringComparison.Ordinal));
        Assert.Empty(skippedSuppressionRule.BehaviorIds);
        Assert.Equal(["explicit-disabled"], skippedSuppressionRule.HostGovernanceScopes);
        Assert.Empty(skippedSuppressionRule.MatchedCandidateIds);
        Assert.Empty(skippedSuppressionRule.SuppressedCandidateIds);
        Assert.Equal([candidate.Id], skippedSuppressionRule.SkippedCandidateIds);
        Assert.Empty(skippedSuppressionRule.SelectionBases);
        var skippedOverrideRule = Assert.Single(overrides, static item =>
            string.Equals(item.Id, "rewrite-disabled-explicit", StringComparison.Ordinal));
        Assert.Empty(skippedOverrideRule.BehaviorIds);
        Assert.Equal(["explicit-disabled"], skippedOverrideRule.HostGovernanceScopes);
        Assert.Empty(skippedOverrideRule.MatchedCandidateIds);
        Assert.Empty(skippedOverrideRule.SelectedCandidateIds);
        Assert.Empty(skippedOverrideRule.AppliedCandidateIds);
        Assert.Equal([candidate.Id], skippedOverrideRule.SkippedCandidateIds);
        Assert.Empty(skippedOverrideRule.SelectionBases);
        Assert.Empty(skippedOverrideRule.SelectedActionKinds);
        Assert.Empty(skippedOverrideRule.AppliedActionKinds);

        Assert.Equal(group.HostGovernanceEligibleCandidateIds, groupByBehavior.HostGovernanceEligibleCandidateIds);
        Assert.Equal(group.HostGovernanceIneligibleCandidateIds, groupByBehavior.HostGovernanceIneligibleCandidateIds);
        Assert.Equal(group.SkippedSuppressionIds, groupByBehavior.SkippedSuppressionIds);
        Assert.Equal(group.SkippedOverrideIds, groupByBehavior.SkippedOverrideIds);
        Assert.Collection(
            groupByBehavior.SkippedSuppressionSummaries,
            summary =>
            {
                Assert.Equal("skip-disabled-explicit", summary.RuleId);
                Assert.Equal([candidate.Id], summary.CandidateIds);
            });
        Assert.Collection(
            groupByBehavior.SkippedOverrideSummaries,
            summary =>
            {
                Assert.Equal("rewrite-disabled-explicit", summary.RuleId);
                Assert.Equal([candidate.Id], summary.CandidateIds);
            });
        Assert.Contains(snapshot.RestEndpointPublicationGroups, item =>
            string.Equals(item.BehaviorId, group.BehaviorId, StringComparison.Ordinal) &&
            item.HostGovernanceEligibleCandidateIds.Count == 0 &&
            item.HostGovernanceIneligibleCandidateIds.Count == 1 &&
            item.SkippedSuppressionIds.SequenceEqual(["skip-disabled-explicit"]) &&
            item.SkippedOverrideIds.SequenceEqual(["rewrite-disabled-explicit"]) &&
            item.SkippedSuppressionSummaries.Count == 1 &&
            item.SkippedOverrideSummaries.Count == 1 &&
            item.SkippedSuppressionSummaries[0].CandidateIds.SequenceEqual([candidate.Id]) &&
            item.SkippedOverrideSummaries[0].CandidateIds.SequenceEqual([candidate.Id]));
        Assert.Contains(snapshot.RestEndpointSuppressions, item =>
            string.Equals(item.Id, "skip-disabled-explicit", StringComparison.Ordinal) &&
            item.MatchedCandidateIds.Count == 0 &&
            item.SuppressedCandidateIds.Count == 0 &&
            item.SkippedCandidateIds.Count == 1 &&
            string.Equals(item.SkippedCandidateIds[0], candidate.Id, StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointOverrides, item =>
            string.Equals(item.Id, "rewrite-disabled-explicit", StringComparison.Ordinal) &&
            item.MatchedCandidateIds.Count == 0 &&
            item.SelectedCandidateIds.Count == 0 &&
            item.AppliedCandidateIds.Count == 0 &&
            item.SkippedCandidateIds.Count == 1 &&
            string.Equals(item.SkippedCandidateIds[0], candidate.Id, StringComparison.Ordinal) &&
            item.SelectionBases.Count == 0 &&
            item.SelectedActionKinds.Count == 0 &&
            item.AppliedActionKinds.Count == 0);
    }

    [Fact]
    public async Task MapCephalonExposesBehaviorIdPrefixSkippedGovernanceParityAcrossGroupedOperatorSurfaces()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "9";
        builder.Configuration["OpenApi:DefaultVersion"] = "9";
        builder.Configuration["RestApi:Suppressions:skip-prefix-disabled-explicit:BehaviorIdPrefixes:0"] = "tests.dsl.runtimeoverride.disabled";
        builder.Configuration["RestApi:Suppressions:skip-prefix-disabled-explicit:AuthoringStyles:0"] =
            RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:Overrides:rewrite-prefix-disabled-explicit:BehaviorIdPrefixes:0"] = "tests.dsl.runtimeoverride.disabled";
        builder.Configuration["RestApi:Overrides:rewrite-prefix-disabled-explicit:AuthoringStyles:0"] =
            RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:Overrides:rewrite-prefix-disabled-explicit:Pattern"] = "/governed/{orderId}";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ExplicitDslHostGovernanceDisabledRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();
        var publicationGroupCatalog = app.Services.GetRequiredService<IRestEndpointPublicationGroupRuntimeCatalog>();
        var authoringPolicyCatalog = app.Services.GetRequiredService<IRestEndpointAuthoringPolicyRuntimeCatalog>();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var groups = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor[]>("/engine/rest-endpoint-publication-groups");
        var groupByBehavior = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor>(
            "/engine/rest-endpoint-publication-groups/tests.dsl.runtimeoverride.disabled.lookup");
        var policies = await client.GetFromJsonAsync<RestEndpointAuthoringPolicyDescriptor[]>("/engine/rest-endpoint-authoring-policies");
        var policyByBehavior = await client.GetFromJsonAsync<RestEndpointAuthoringPolicyDescriptor>(
            "/engine/rest-endpoint-authoring-policies/tests.dsl.runtimeoverride.disabled.lookup");
        var suppressions = await client.GetFromJsonAsync<RestEndpointSuppressionDescriptor[]>("/engine/rest-endpoint-suppressions");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(groups);
        Assert.NotNull(groupByBehavior);
        Assert.NotNull(policies);
        Assert.NotNull(policyByBehavior);
        Assert.NotNull(suppressions);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.dsl.runtimeoverride.disabled.lookup", StringComparison.Ordinal));
        Assert.Equal(["skip-prefix-disabled-explicit"], endpoint.SkippedSuppressionIds);
        Assert.Equal(["rewrite-prefix-disabled-explicit"], endpoint.SkippedOverrideIds);
        Assert.NotNull(endpoint.OriginalProjection);
        Assert.False(endpoint.OriginalProjection!.AllowsHostGovernance);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.dsl.runtimeoverride.disabled.lookup", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Empty(candidate.MatchedSuppressionIds);
        Assert.Empty(candidate.MatchedOverrideIds);
        Assert.Equal(["skip-prefix-disabled-explicit"], candidate.SkippedSuppressionIds);
        Assert.Equal(["rewrite-prefix-disabled-explicit"], candidate.SkippedOverrideIds);
        Assert.Equal(["skip-prefix-disabled-explicit"], candidate.ProjectedEndpoint.SkippedSuppressionIds);
        Assert.Equal(["rewrite-prefix-disabled-explicit"], candidate.ProjectedEndpoint.SkippedOverrideIds);

        var suppressionRule = Assert.Single(suppressions, static item =>
            string.Equals(item.Id, "skip-prefix-disabled-explicit", StringComparison.Ordinal));
        Assert.Empty(suppressionRule.BehaviorIds);
        Assert.Equal(["tests.dsl.runtimeoverride.disabled"], suppressionRule.BehaviorIdPrefixes);
        Assert.Empty(suppressionRule.MatchedCandidateIds);
        Assert.Empty(suppressionRule.SuppressedCandidateIds);
        Assert.Equal([candidate.Id], suppressionRule.SkippedCandidateIds);
        Assert.Empty(suppressionRule.SelectionBases);

        var overrideRule = Assert.Single(overrides, static item =>
            string.Equals(item.Id, "rewrite-prefix-disabled-explicit", StringComparison.Ordinal));
        Assert.Empty(overrideRule.BehaviorIds);
        Assert.Equal(["tests.dsl.runtimeoverride.disabled"], overrideRule.BehaviorIdPrefixes);
        Assert.Empty(overrideRule.MatchedCandidateIds);
        Assert.Empty(overrideRule.SelectedCandidateIds);
        Assert.Empty(overrideRule.AppliedCandidateIds);
        Assert.Equal([candidate.Id], overrideRule.SkippedCandidateIds);
        Assert.Empty(overrideRule.SelectionBases);
        Assert.Empty(overrideRule.SelectedActionKinds);
        Assert.Empty(overrideRule.AppliedActionKinds);

        var group = Assert.Single(groups, static item =>
            string.Equals(item.BehaviorId, "tests.dsl.runtimeoverride.disabled.lookup", StringComparison.Ordinal));
        Assert.Equal([candidate.Id], group.PublishedCandidateIds);
        Assert.Empty(group.HostGovernanceEligibleCandidateIds);
        Assert.Equal([candidate.Id], group.HostGovernanceIneligibleCandidateIds);
        Assert.Equal(["skip-prefix-disabled-explicit"], group.SkippedSuppressionIds);
        Assert.Equal(["rewrite-prefix-disabled-explicit"], group.SkippedOverrideIds);
        Assert.Equal("skip-prefix-disabled-explicit", Assert.Single(group.SkippedSuppressionSummaries).RuleId);
        Assert.Equal([candidate.Id], Assert.Single(group.SkippedSuppressionSummaries).CandidateIds);
        Assert.Equal("rewrite-prefix-disabled-explicit", Assert.Single(group.SkippedOverrideSummaries).RuleId);
        Assert.Equal([candidate.Id], Assert.Single(group.SkippedOverrideSummaries).CandidateIds);
        var groupExplicitStyle = Assert.Single(group.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal(["skip-prefix-disabled-explicit"], groupExplicitStyle.SkippedSuppressionIds);
        Assert.Equal(["rewrite-prefix-disabled-explicit"], groupExplicitStyle.SkippedOverrideIds);
        Assert.Equal("skip-prefix-disabled-explicit", Assert.Single(groupExplicitStyle.SkippedSuppressionSummaries).RuleId);
        Assert.Equal("rewrite-prefix-disabled-explicit", Assert.Single(groupExplicitStyle.SkippedOverrideSummaries).RuleId);

        var policy = Assert.Single(policies, static item =>
            string.Equals(item.BehaviorId, "tests.dsl.runtimeoverride.disabled.lookup", StringComparison.Ordinal));
        Assert.Equal([candidate.Id], policy.PublishedCandidateIds);
        Assert.Empty(policy.HostGovernanceEligibleCandidateIds);
        Assert.Equal([candidate.Id], policy.HostGovernanceIneligibleCandidateIds);
        Assert.Equal(["skip-prefix-disabled-explicit"], policy.SkippedSuppressionIds);
        Assert.Equal(["rewrite-prefix-disabled-explicit"], policy.SkippedOverrideIds);
        Assert.Empty(policy.GovernanceSuppressionSummaries);
        Assert.Empty(policy.GovernanceOverrideSummaries);
        Assert.Equal("skip-prefix-disabled-explicit", Assert.Single(policy.SkippedSuppressionSummaries).RuleId);
        Assert.Equal([candidate.Id], Assert.Single(policy.SkippedSuppressionSummaries).CandidateIds);
        Assert.Equal("rewrite-prefix-disabled-explicit", Assert.Single(policy.SkippedOverrideSummaries).RuleId);
        Assert.Equal([candidate.Id], Assert.Single(policy.SkippedOverrideSummaries).CandidateIds);
        var policyExplicitStyle = Assert.Single(policy.AuthoringStyleSummaries, static item =>
            string.Equals(item.AuthoringStyle, RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, StringComparison.Ordinal));
        Assert.Equal(["skip-prefix-disabled-explicit"], policyExplicitStyle.SkippedSuppressionIds);
        Assert.Equal(["rewrite-prefix-disabled-explicit"], policyExplicitStyle.SkippedOverrideIds);
        Assert.Equal("skip-prefix-disabled-explicit", Assert.Single(policyExplicitStyle.SkippedSuppressionSummaries).RuleId);
        Assert.Equal("rewrite-prefix-disabled-explicit", Assert.Single(policyExplicitStyle.SkippedOverrideSummaries).RuleId);

        var groupFromCatalog = publicationGroupCatalog.GetByBehaviorId("tests.dsl.runtimeoverride.disabled.lookup");
        Assert.NotNull(groupFromCatalog);
        Assert.Equal(group.SkippedSuppressionIds, groupFromCatalog!.SkippedSuppressionIds);
        Assert.Equal(group.SkippedOverrideIds, groupFromCatalog.SkippedOverrideIds);
        var policyFromCatalog = authoringPolicyCatalog.GetByBehaviorId("tests.dsl.runtimeoverride.disabled.lookup");
        Assert.NotNull(policyFromCatalog);
        Assert.Equal(policy.SkippedSuppressionIds, policyFromCatalog!.SkippedSuppressionIds);
        Assert.Equal(policy.SkippedOverrideIds, policyFromCatalog.SkippedOverrideIds);

        Assert.Equal(group.SkippedSuppressionIds, groupByBehavior.SkippedSuppressionIds);
        Assert.Equal(group.SkippedOverrideIds, groupByBehavior.SkippedOverrideIds);
        Assert.Equal(policy.SkippedSuppressionIds, policyByBehavior.SkippedSuppressionIds);
        Assert.Equal(policy.SkippedOverrideIds, policyByBehavior.SkippedOverrideIds);

        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.Id, endpoint.Id, StringComparison.Ordinal) &&
            item.SkippedSuppressionIds.SequenceEqual(["skip-prefix-disabled-explicit"]) &&
            item.SkippedOverrideIds.SequenceEqual(["rewrite-prefix-disabled-explicit"]));
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            item.SkippedSuppressionIds.SequenceEqual(["skip-prefix-disabled-explicit"]) &&
            item.SkippedOverrideIds.SequenceEqual(["rewrite-prefix-disabled-explicit"]));
        Assert.Contains(snapshot.RestEndpointPublicationGroups, item =>
            string.Equals(item.BehaviorId, group.BehaviorId, StringComparison.Ordinal) &&
            item.SkippedSuppressionIds.SequenceEqual(["skip-prefix-disabled-explicit"]) &&
            item.SkippedOverrideIds.SequenceEqual(["rewrite-prefix-disabled-explicit"]) &&
            item.SkippedSuppressionSummaries.Count == 1 &&
            item.SkippedOverrideSummaries.Count == 1);
        Assert.Contains(snapshot.RestEndpointAuthoringPolicies, item =>
            string.Equals(item.BehaviorId, policy.BehaviorId, StringComparison.Ordinal) &&
            item.SkippedSuppressionIds.SequenceEqual(["skip-prefix-disabled-explicit"]) &&
            item.SkippedOverrideIds.SequenceEqual(["rewrite-prefix-disabled-explicit"]) &&
            item.SkippedSuppressionSummaries.Count == 1 &&
            item.SkippedOverrideSummaries.Count == 1);
        Assert.Contains(snapshot.RestEndpointSuppressions, item =>
            string.Equals(item.Id, "skip-prefix-disabled-explicit", StringComparison.Ordinal) &&
            item.BehaviorIdPrefixes.SequenceEqual(["tests.dsl.runtimeoverride.disabled"]) &&
            item.SkippedCandidateIds.SequenceEqual([candidate.Id]));
        Assert.Contains(snapshot.RestEndpointOverrides, item =>
            string.Equals(item.Id, "rewrite-prefix-disabled-explicit", StringComparison.Ordinal) &&
            item.BehaviorIdPrefixes.SequenceEqual(["tests.dsl.runtimeoverride.disabled"]) &&
            item.SkippedCandidateIds.SequenceEqual([candidate.Id]) &&
            item.SelectedCandidateIds.Count == 0 &&
            item.AppliedCandidateIds.Count == 0);
    }

    [Fact]
    public async Task MapCephalonExposesBehaviorHttpGovernanceDiagnosticsAndLogsSuppressionOutcomes()
    {
        var loggerProvider = new TestLoggerProvider();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "8";
        builder.Configuration["OpenApi:DefaultVersion"] = "8";
        builder.Configuration["RestApi:Suppressions:prefer-generated:Behaviors:0"] = "tests.rest.generated.threeway.lookup";
        builder.Configuration["RestApi:Suppressions:prefer-generated:AuthoringStyles:0"] = RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle;
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedProfileGovernanceRuntimeCatalogModule());
            engine.AddModule(new ProfileSuppressionRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();
        var diagnostics = await client.GetFromJsonAsync<DiagnosticsSurface>("/engine/diagnostics");
        var candidates = await client.GetFromJsonAsync<List<RestEndpointCandidateRuntimeDescriptor>>("/engine/rest-endpoint-candidates");

        Assert.NotNull(diagnostics);
        Assert.NotNull(candidates);

        var convention = Assert.Single(diagnostics.Conventions, static item =>
            string.Equals(item.Source, "Cephalon.Behaviors.Http", StringComparison.Ordinal));
        Assert.Equal(5200, convention.MinimumEventId);
        Assert.Equal(5206, convention.MaximumEventId);
        var governanceSuppressed = Assert.Single(convention.Events, static item => item.Id == 5200 && item.Name == "RestEndpointGovernanceSuppressed");
        Assert.Contains("{SuppressionSelectionBasis}", governanceSuppressed.MessageTemplate, StringComparison.Ordinal);
        Assert.Contains(convention.Events, static item => item.Id == 5201 && item.Name == "RestEndpointPrecedenceSuppressed");
        var overrideApplied = Assert.Single(convention.Events, static item => item.Id == 5202 && item.Name == "RestEndpointOverrideApplied");
        Assert.Contains("{OverrideSelectionBasis}", overrideApplied.MessageTemplate, StringComparison.Ordinal);
        Assert.Contains("{SelectedOverrideActionKinds}", overrideApplied.MessageTemplate, StringComparison.Ordinal);
        Assert.Contains("{AppliedOverrideActionKinds}", overrideApplied.MessageTemplate, StringComparison.Ordinal);
        var overrideNoOp = Assert.Single(convention.Events, static item => item.Id == 5203 && item.Name == "RestEndpointOverrideNoOp");
        Assert.Contains("{OverrideSelectionBasis}", overrideNoOp.MessageTemplate, StringComparison.Ordinal);
        Assert.Contains("{SelectedOverrideActionKinds}", overrideNoOp.MessageTemplate, StringComparison.Ordinal);
        Assert.Contains("{AppliedOverrideActionKinds}", overrideNoOp.MessageTemplate, StringComparison.Ordinal);
        Assert.Contains(convention.Events, static item => item.Id == 5204 && item.Name == "RestEndpointBindingFallbackPreserved");
        Assert.Contains(convention.Events, static item => item.Id == 5205 && item.Name == "RestEndpointAuthoringPolicySuppressed");
        Assert.Contains(convention.Events, static item => item.Id == 5206 && item.Name == "RestEndpointGovernanceSkipped");

        var suppressedCandidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.generated.threeway.lookup", StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(item.SuppressedBySuppressionId));
        var suppressionSelectionBasis = JoinSelectionBasis(suppressedCandidate.SuppressionSelectionBasis);

        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 5200 &&
            entry.Message.Contains("tests.rest.generated.threeway.lookup", StringComparison.Ordinal) &&
            entry.Message.Contains(suppressionSelectionBasis, StringComparison.Ordinal) &&
            entry.Message.Contains("prefer-generated", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 5201 &&
            entry.Message.Contains("tests.rest.profile.suppression", StringComparison.Ordinal) &&
            entry.Message.Contains(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, StringComparison.Ordinal));
    }

    [Fact]
    public async Task MapCephalonLogsAuthoringPolicySuppressionOutcomes()
    {
        var loggerProvider = new TestLoggerProvider();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "12";
        builder.Configuration["OpenApi:DefaultVersion"] = "12";
        builder.Configuration["RestApi:AuthoringPolicies:tests.rest.generated.threeway.lookup:PreferredAuthoringStyle"] =
            RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new GeneratedThreeWaySuppressionRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 5205 &&
            entry.Message.Contains("tests.rest.generated.threeway.lookup", StringComparison.Ordinal) &&
            entry.Message.Contains("preferred-authoring-style-selected", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MapCephalonLogsSkippedExplicitGovernanceOutcomes()
    {
        var loggerProvider = new TestLoggerProvider();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "9";
        builder.Configuration["OpenApi:DefaultVersion"] = "9";
        builder.Configuration["RestApi:Suppressions:skip-disabled-explicit:Behaviors:0"] = "tests.dsl.runtimeoverride.disabled.lookup";
        builder.Configuration["RestApi:Suppressions:skip-disabled-explicit:AuthoringStyles:0"] = RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:Overrides:rewrite-disabled-explicit:Behaviors:0"] = "tests.dsl.runtimeoverride.disabled.lookup";
        builder.Configuration["RestApi:Overrides:rewrite-disabled-explicit:AuthoringStyles:0"] = RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle;
        builder.Configuration["RestApi:Overrides:rewrite-disabled-explicit:Pattern"] = "/governed/{orderId}";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ExplicitDslHostGovernanceDisabledRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 5206 &&
            entry.Message.Contains("tests.dsl.runtimeoverride.disabled.lookup", StringComparison.Ordinal) &&
            entry.Message.Contains(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, StringComparison.Ordinal) &&
            entry.Message.Contains("did not allow host governance", StringComparison.Ordinal) &&
            entry.Message.Contains("skip-disabled-explicit", StringComparison.Ordinal) &&
            entry.Message.Contains("rewrite-disabled-explicit", StringComparison.Ordinal));
        Assert.DoesNotContain(loggerProvider.Entries, entry =>
            (entry.EventId.Id == 5200 || entry.EventId.Id == 5202 || entry.EventId.Id == 5203) &&
            entry.Message.Contains("tests.dsl.runtimeoverride.disabled.lookup", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MapCephalonExposesRestEndpointPublicationGroupsWhenSameRankCandidatesRemainPublished()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "7";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileSelectorRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var groups = await client.GetFromJsonAsync<RestEndpointPublicationGroupDescriptor[]>("/engine/rest-endpoint-publication-groups");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(candidates);
        Assert.NotNull(groups);
        Assert.NotNull(snapshot);

        var behaviorCandidates = candidates
            .Where(static candidate => string.Equals(candidate.ProjectedEndpoint.BehaviorId, "tests.rest.profile.selector.bindings", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(2, behaviorCandidates.Length);
        Assert.All(behaviorCandidates, static candidate => Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status));

        var group = Assert.Single(groups, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.selector.bindings", StringComparison.Ordinal));
        Assert.False(group.AuthoringPolicy.IsConfigured);
        Assert.False(group.AuthoringPolicy.AllowMultiplePublishedCandidates);
        Assert.Equal(3, group.WinningPrecedenceRank);
        Assert.Equal(2, group.PublishedCandidateIds.Count);
        Assert.Empty(group.PrecedenceSuppressedCandidateIds);
        Assert.Empty(group.GovernanceSuppressedCandidateIds);
        Assert.Equal(2, group.Candidates.Count);
        var authoringStyle = Assert.Single(group.AuthoringStyleSummaries);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, authoringStyle.AuthoringStyle);
        Assert.Equal([RestEndpointRuntimeMetadata.BehaviorModuleProfilePrecedenceRank], authoringStyle.PrecedenceRanks);
        Assert.Equal(2, authoringStyle.CandidateIds.Count);
        Assert.Equal(2, authoringStyle.PublishedCandidateIds.Count);
        Assert.Empty(authoringStyle.PrecedenceSuppressedCandidateIds);
        Assert.Empty(authoringStyle.GovernanceSuppressedCandidateIds);
        Assert.All(
            behaviorCandidates,
            candidate =>
            {
                Assert.Contains(candidate.Id, group.PublishedCandidateIds, StringComparer.Ordinal);
                Assert.Contains(candidate.Id, authoringStyle.CandidateIds, StringComparer.Ordinal);
                Assert.Contains(candidate.Id, authoringStyle.PublishedCandidateIds, StringComparer.Ordinal);
            });
        Assert.Contains(snapshot.RestEndpointPublicationGroups, item =>
            string.Equals(item.BehaviorId, group.BehaviorId, StringComparison.Ordinal) &&
            item.AuthoringPolicy.IsConfigured == false &&
            item.AuthoringStyleSummaries.Count == 1 &&
            item.PublishedCandidateIds.Count == 2);
    }

    [Fact]
    public async Task MapCephalonExposesRestEndpointCandidatesAndSuppressesLowerPrecedenceProfileMappings()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "8";
        builder.Configuration["OpenApi:DefaultVersion"] = "8";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileSuppressionRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");
        var candidatesJson = await client.GetStringAsync("/engine/rest-endpoint-candidates");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.rest.profile.suppression", StringComparison.Ordinal));
        Assert.Equal("/api/v8/tests/profile-runtime/precedence/orders/explicit/{orderId}", endpoint.RoutePattern);
        Assert.Equal("v8", endpoint.OpenApiDocumentName);
        Assert.Equal(8, endpoint.ApiVersionMajor);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, endpoint.Metadata["authoringStyle"]);

        var behaviorCandidates = candidates
            .Where(static candidate => string.Equals(candidate.ProjectedEndpoint.BehaviorId, "tests.rest.profile.suppression", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(2, behaviorCandidates.Length);

        var published = Assert.Single(behaviorCandidates, static candidate =>
            candidate.Status == RestEndpointCandidateStatus.Published);
        Assert.Equal(endpoint.Id, published.ProjectedEndpoint.Id);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle, published.AuthoringStyle);
        Assert.Equal("/api/v8/tests/profile-runtime/precedence/orders/explicit/{orderId}", published.ProjectedEndpoint.RoutePattern);

        var suppressed = Assert.Single(behaviorCandidates, static candidate =>
            candidate.Status == RestEndpointCandidateStatus.Suppressed);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, suppressed.AuthoringStyle);
        Assert.Equal("/api/v8/tests/profile-runtime/precedence/orders/{orderId}", suppressed.ProjectedEndpoint.RoutePattern);
        Assert.Equal(published.Id, suppressed.SuppressedByCandidateId);
        Assert.Contains("higher-precedence authoring style", suppressed.SuppressionReason, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("\"status\":\"published\"", candidatesJson, StringComparison.Ordinal);
        Assert.Contains("\"status\":\"suppressed\"", candidatesJson, StringComparison.Ordinal);

        var candidateById = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor>(
            $"/engine/rest-endpoint-candidates/{suppressed.Id}");
        var candidateByIdJson = await client.GetStringAsync($"/engine/rest-endpoint-candidates/{suppressed.Id}");
        Assert.NotNull(candidateById);
        Assert.Equal(suppressed.Id, candidateById.Id);
        Assert.Contains("\"status\":\"suppressed\"", candidateByIdJson, StringComparison.Ordinal);

        var snapshotJson = await client.GetStringAsync("/engine/snapshot");

        var snapshotPublished = Assert.Single(snapshot.RestEndpointCandidates, candidate =>
            string.Equals(candidate.Id, published.Id, StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, snapshotPublished.Status);

        var snapshotSuppressed = Assert.Single(snapshot.RestEndpointCandidates, candidate =>
            string.Equals(candidate.Id, suppressed.Id, StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Suppressed, snapshotSuppressed.Status);
        Assert.Equal(published.Id, snapshotSuppressed.SuppressedByCandidateId);
        Assert.Contains("\"status\":\"published\"", snapshotJson, StringComparison.Ordinal);
        Assert.Contains("\"status\":\"suppressed\"", snapshotJson, StringComparison.Ordinal);

        var payload = await client.GetFromJsonAsync<ProfileRuntimeOrderOutput>(
            "/api/v8/tests/profile-runtime/precedence/orders/explicit/ord-42");
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);

        var suppressedResponse = await client.GetAsync("/api/v8/tests/profile-runtime/precedence/orders/ord-42");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, suppressedResponse.StatusCode);
    }

    [Fact]
    public async Task MapCephalonAppliesExplicitProfileBindingsAndExposesThemInRuntimeCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var rawEndpoints = await client.GetStringAsync("/engine/rest-endpoints");

        Assert.NotNull(endpoints);
        Assert.Contains("\"source\":\"route\"", rawEndpoints, StringComparison.Ordinal);
        Assert.Contains("\"source\":\"query\"", rawEndpoints, StringComparison.Ordinal);
        Assert.Contains("\"source\":\"header\"", rawEndpoints, StringComparison.Ordinal);
        Assert.Contains("\"source\":\"body\"", rawEndpoints, StringComparison.Ordinal);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        var rawEndpoint = await client.GetStringAsync($"/engine/rest-endpoints/{Uri.EscapeDataString(endpoint.Id)}");
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, endpoint.Metadata["authoringStyle"]);
        Assert.False(endpoint.Metadata.ContainsKey("bindingDescriptors"));
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback,
            endpoint.BindingFallbackMode);
        Assert.Contains("\"source\":\"route\"", rawEndpoint, StringComparison.Ordinal);
        Assert.Contains("\"source\":\"query\"", rawEndpoint, StringComparison.Ordinal);
        Assert.Contains("\"source\":\"header\"", rawEndpoint, StringComparison.Ordinal);
        Assert.Contains("\"source\":\"body\"", rawEndpoint, StringComparison.Ordinal);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback.GetWireName(),
            endpoint.Metadata["bindingFallbackMode"]);
        Assert.Equal(4, endpoint.BindingDescriptors.Count);
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "orderId");
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "Quantity" &&
            binding.Source == RestEndpointBindingSource.Query &&
            binding.Name == "quantity");
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "CorrelationId" &&
            binding.Source == RestEndpointBindingSource.Header &&
            binding.Name == "X-Correlation-Id");
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "Note" &&
            binding.Source == RestEndpointBindingSource.Body &&
            binding.Name == "note");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/bindings/orders/ord-42?quantity=3");
        request.Headers.Add("X-Correlation-Id", "corr-42");
        request.Content = JsonContent.Create(new
        {
            note = "gift wrap",
            ignored = "body-fallback"
        });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProfileBindingRuntimeOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
        Assert.Equal(3, payload.Quantity);
        Assert.Equal("corr-42", payload.CorrelationId);
        Assert.Equal("gift wrap", payload.Note);
        Assert.Equal("body-fallback", payload.Ignored);

        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");
        var rawSnapshot = await client.GetStringAsync("/engine/snapshot");
        Assert.NotNull(snapshot);
        Assert.Contains("\"source\":\"route\"", rawSnapshot, StringComparison.Ordinal);
        Assert.Contains("\"source\":\"query\"", rawSnapshot, StringComparison.Ordinal);
        Assert.Contains("\"source\":\"header\"", rawSnapshot, StringComparison.Ordinal);
        Assert.Contains("\"source\":\"body\"", rawSnapshot, StringComparison.Ordinal);

        var snapshotEndpoint = Assert.Single(snapshot.RestEndpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.False(snapshotEndpoint.Metadata.ContainsKey("bindingDescriptors"));
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback,
            snapshotEndpoint.BindingFallbackMode);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback.GetWireName(),
            snapshotEndpoint.Metadata["bindingFallbackMode"]);
        Assert.Equal(4, snapshotEndpoint.BindingDescriptors.Count);
        Assert.Contains(snapshotEndpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "orderId");
    }

    [Fact]
    public async Task MapCephalonAppliesRestBindingOverridesAndExposesOverrideCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Behaviors:0"] = "tests.rest.profile.bindings";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:0:PropertyName"] = "Quantity";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:0:Source"] = "query";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:0:Name"] = "qty";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:1:PropertyName"] = "CorrelationId";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:1:Source"] = "header";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:1:Name"] = "X-Trace-Id";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:2:PropertyName"] = "Note";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:2:Source"] = "body";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:2:Name"] = "memo";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal(3, endpoint.BindingDescriptors.Count);
        Assert.DoesNotContain(endpoint.BindingDescriptors, static binding =>
            string.Equals(binding.PropertyName, "OrderId", StringComparison.Ordinal));
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "Quantity" &&
            binding.Source == RestEndpointBindingSource.Query &&
            binding.Name == "qty");
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "CorrelationId" &&
            binding.Source == RestEndpointBindingSource.Header &&
            binding.Name == "X-Trace-Id");
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "Note" &&
            binding.Source == RestEndpointBindingSource.Body &&
            binding.Name == "memo");

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-short-bindings", candidate.AppliedOverrideId);
        Assert.Equal(3, candidate.ProjectedEndpoint.BindingDescriptors.Count);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);

        var rule = Assert.Single(overrides, static item =>
            string.Equals(item.Id, "prefer-short-bindings", StringComparison.Ordinal));
        Assert.Equal(3, rule.Bindings.Count);
        Assert.Contains(rule.Bindings, static binding =>
            binding.PropertyName == "Quantity" &&
            binding.Source == RestEndpointBindingSource.Query &&
            binding.Name == "qty");
        Assert.Contains(rule.Bindings, static binding =>
            binding.PropertyName == "CorrelationId" &&
            binding.Source == RestEndpointBindingSource.Header &&
            binding.Name == "X-Trace-Id");
        Assert.Contains(rule.Bindings, static binding =>
            binding.PropertyName == "Note" &&
            binding.Source == RestEndpointBindingSource.Body &&
            binding.Name == "memo");

        Assert.Contains(snapshot.RestEndpointOverrides, static item =>
            string.Equals(item.Id, "prefer-short-bindings", StringComparison.Ordinal) &&
            item.Bindings.Count == 3);
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            string.Equals(item.AppliedOverrideId, "prefer-short-bindings", StringComparison.Ordinal));

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/bindings/orders/ord-64?qty=7");
        request.Headers.Add("X-Trace-Id", "trace-64");
        request.Content = JsonContent.Create(new
        {
            memo = "override memo",
            ignored = "body-fallback"
        });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProfileBindingRuntimeOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-64", payload.OrderId);
        Assert.Equal(7, payload.Quantity);
        Assert.Equal("trace-64", payload.CorrelationId);
        Assert.Equal("override memo", payload.Note);
        Assert.Equal("body-fallback", payload.Ignored);
    }

    [Fact]
    public async Task MapCephalonAppliesMergeBindingOverridesAndExposesBindingModeInOverrideCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-merge-route-quantity:Behaviors:0"] = "tests.rest.profile.bindings";
        builder.Configuration["RestApi:Overrides:prefer-merge-route-quantity:Pattern"] = "/lookup/{orderId}/items/{quantity}";
        builder.Configuration["RestApi:Overrides:prefer-merge-route-quantity:BindingMode"] = "merge-explicit";
        builder.Configuration["RestApi:Overrides:prefer-merge-route-quantity:Bindings:0:PropertyName"] = "Quantity";
        builder.Configuration["RestApi:Overrides:prefer-merge-route-quantity:Bindings:0:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-merge-route-quantity:Bindings:0:Name"] = "quantity";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders/lookup/{orderId}/items/{quantity}", endpoint.RoutePattern);
        Assert.Equal(4, endpoint.BindingDescriptors.Count);
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "orderId");
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "Quantity" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "quantity");
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "CorrelationId" &&
            binding.Source == RestEndpointBindingSource.Header &&
            binding.Name == "X-Correlation-Id");
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "Note" &&
            binding.Source == RestEndpointBindingSource.Body &&
            binding.Name == "note");

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-merge-route-quantity", candidate.AppliedOverrideId);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);

        var rule = Assert.Single(overrides, static item =>
            string.Equals(item.Id, "prefer-merge-route-quantity", StringComparison.Ordinal));
        Assert.Equal(RestEndpointOverrideBindingMode.MergeExplicit, rule.BindingMode);
        Assert.Equal("/lookup/{orderId}/items/{quantity}", rule.Pattern);
        Assert.Single(rule.Bindings);
        Assert.Contains(rule.Bindings, static binding =>
            binding.PropertyName == "Quantity" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "quantity");

        Assert.Contains(snapshot.RestEndpointOverrides, static item =>
            string.Equals(item.Id, "prefer-merge-route-quantity", StringComparison.Ordinal) &&
            item.BindingMode == RestEndpointOverrideBindingMode.MergeExplicit);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/bindings/orders/lookup/ord-65/items/9");
        request.Headers.Add("X-Correlation-Id", "corr-65");
        request.Content = JsonContent.Create(new
        {
            note = "merge binding mode",
            ignored = "body-fallback"
        });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProfileBindingRuntimeOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-65", payload.OrderId);
        Assert.Equal(9, payload.Quantity);
        Assert.Equal("corr-65", payload.CorrelationId);
        Assert.Equal("merge binding mode", payload.Note);
        Assert.Equal("body-fallback", payload.Ignored);
    }

    [Fact]
    public async Task MapCephalonAppliesMergeBindingRemovalsAndExposesRemovedPropertiesInOverrideCatalog()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:withdraw-query-quantity:Behaviors:0"] = "tests.rest.profile.bindings";
        builder.Configuration["RestApi:Overrides:withdraw-query-quantity:RemovedBindingProperties:0"] = "Quantity";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal(3, endpoint.BindingDescriptors.Count);
        Assert.DoesNotContain(endpoint.BindingDescriptors, static binding =>
            string.Equals(binding.PropertyName, nameof(ProfileBindingRuntimeInput.Quantity), StringComparison.Ordinal));
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "orderId");
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "CorrelationId" &&
            binding.Source == RestEndpointBindingSource.Header &&
            binding.Name == "X-Correlation-Id");
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "Note" &&
            binding.Source == RestEndpointBindingSource.Body &&
            binding.Name == "note");

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("withdraw-query-quantity", candidate.AppliedOverrideId);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);

        var rule = Assert.Single(overrides, static item =>
            string.Equals(item.Id, "withdraw-query-quantity", StringComparison.Ordinal));
        Assert.Equal(RestEndpointOverrideBindingMode.MergeExplicit, rule.BindingMode);
        Assert.Empty(rule.Bindings);
        Assert.Single(rule.RemovedBindingProperties);
        Assert.Contains(nameof(ProfileBindingRuntimeInput.Quantity), rule.RemovedBindingProperties);

        Assert.Contains(snapshot.RestEndpointOverrides, static item =>
            string.Equals(item.Id, "withdraw-query-quantity", StringComparison.Ordinal) &&
            item.BindingMode == RestEndpointOverrideBindingMode.MergeExplicit &&
            item.RemovedBindingProperties.Contains(nameof(ProfileBindingRuntimeInput.Quantity), StringComparer.Ordinal));

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/bindings/orders/ord-66");
        request.Headers.Add("X-Correlation-Id", "corr-66");
        request.Content = JsonContent.Create(new
        {
            quantity = 11,
            note = "quantity from body",
            ignored = "body-fallback"
        });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProfileBindingRuntimeOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-66", payload.OrderId);
        Assert.Equal(11, payload.Quantity);
        Assert.Equal("corr-66", payload.CorrelationId);
        Assert.Equal("quantity from body", payload.Note);
        Assert.Equal("body-fallback", payload.Ignored);
    }

    [Fact]
    public async Task MapCephalonAppliesMergeBindingRemovalAndPreservesRemainingBodyFallbackVisibility()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:withdraw-body-note:Behaviors:0"] = "tests.rest.profile.bindings.inference";
        builder.Configuration["RestApi:Overrides:withdraw-body-note:RemovedBindingProperties:0"] = "Note";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingInferenceRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings.inference", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile-runtime/inference/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback,
            endpoint.BindingFallbackMode);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback.GetWireName(),
            endpoint.Metadata["bindingFallbackMode"]);
        Assert.Equal(2, endpoint.BindingDescriptors.Count);
        Assert.DoesNotContain(endpoint.BindingDescriptors, static binding =>
            string.Equals(binding.PropertyName, nameof(ProfileBindingInferenceRuntimeInput.Note), StringComparison.Ordinal));
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "Quantity" &&
            binding.Source == RestEndpointBindingSource.Query &&
            binding.Name == "quantity");
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "CorrelationId" &&
            binding.Source == RestEndpointBindingSource.Header &&
            binding.Name == "X-Correlation-Id");

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.inference", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("withdraw-body-note", candidate.AppliedOverrideId);
        Assert.Null(candidate.OriginalProjection.BindingFallbackMode);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback,
            candidate.ProjectedEndpoint.BindingFallbackMode);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback.GetWireName(),
            candidate.ProjectedEndpoint.Metadata["bindingFallbackMode"]);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/inference/orders/ord-88?quantity=9");
        request.Headers.Add("X-Correlation-Id", "corr-88");
        request.Content = JsonContent.Create(new
        {
            note = "body fallback note"
        });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProfileBindingInferenceRuntimeOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-88", payload.OrderId);
        Assert.Equal(9, payload.Quantity);
        Assert.Equal("corr-88", payload.CorrelationId);
        Assert.Equal("body fallback note", payload.Note);

        Assert.Contains(snapshot.RestEndpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings.inference", StringComparison.Ordinal) &&
            item.BindingFallbackMode == RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback &&
            string.Equals(
                item.Metadata["bindingFallbackMode"],
                RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback.GetWireName(),
                StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointCandidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.inference", StringComparison.Ordinal) &&
            item.ProjectedEndpoint.BindingFallbackMode == RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback &&
            string.Equals(
                item.ProjectedEndpoint.Metadata["bindingFallbackMode"],
                RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback.GetWireName(),
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task MapCephalonAppliesMergeBindingRemovalAndPreservesImplicitQueryFallbackVisibilityForExplicitProfile()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:withdraw-query-quantity:Behaviors:0"] = "tests.rest.profile.bindings.query.explicit.withdraw.preserved";
        builder.Configuration["RestApi:Overrides:withdraw-query-quantity:RemovedBindingProperties:0"] = "Quantity";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingExplicitQueryWithdrawalPreservedRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();
        var runtimeCandidateCatalog = app.Services.GetService(typeof(IRestEndpointCandidateRuntimeCatalog)) as IRestEndpointCandidateRuntimeCatalog;

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings.query.explicit.withdraw.preserved", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile-runtime/query-explicit-withdraw/orders/lookup/{orderId}", endpoint.RoutePattern);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback,
            endpoint.BindingFallbackMode);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback.GetWireName(),
            endpoint.Metadata["bindingFallbackMode"]);
        Assert.Single(endpoint.BindingDescriptors);
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "orderId");

        Assert.NotNull(runtimeCandidateCatalog);
        var runtimeCandidate = Assert.Single(runtimeCandidateCatalog.Candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.query.explicit.withdraw.preserved", StringComparison.Ordinal));
        Assert.Null(runtimeCandidate.OriginalProjection.BindingFallbackMode);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.query.explicit.withdraw.preserved", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("withdraw-query-quantity", candidate.AppliedOverrideId);
        Assert.Null(candidate.OriginalProjection.BindingFallbackMode);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback,
            candidate.ProjectedEndpoint.BindingFallbackMode);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback.GetWireName(),
            candidate.ProjectedEndpoint.Metadata["bindingFallbackMode"]);

        var response = await client.GetAsync("/api/v6/tests/profile-runtime/query-explicit-withdraw/orders/lookup/ord-71?quantity=14");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProfileBindingQueryFallbackPartialRuntimeOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-71", payload.OrderId);
        Assert.Equal(14, payload.Quantity);

        Assert.Contains(snapshot.RestEndpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings.query.explicit.withdraw.preserved", StringComparison.Ordinal) &&
            item.BindingFallbackMode == RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback &&
            string.Equals(
                item.Metadata["bindingFallbackMode"],
                RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback.GetWireName(),
                StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointCandidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.query.explicit.withdraw.preserved", StringComparison.Ordinal) &&
            item.ProjectedEndpoint.BindingFallbackMode == RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback &&
            string.Equals(
                item.ProjectedEndpoint.Metadata["bindingFallbackMode"],
                RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback.GetWireName(),
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task MapCephalonAllowsHostGovernanceToOptExplicitQueryWithdrawalsIntoPreservedImplicitQueryFallback()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:withdraw-query-quantity:Behaviors:0"] = "tests.rest.profile.bindings.query.explicit.withdraw";
        builder.Configuration["RestApi:Overrides:withdraw-query-quantity:RemovedBindingProperties:0"] = "Quantity";
        builder.Configuration["RestApi:Overrides:withdraw-query-quantity:PreserveImplicitQueryFallback"] = "true";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingExplicitQueryWithdrawalRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();
        var runtimeCandidateCatalog = app.Services.GetService(typeof(IRestEndpointCandidateRuntimeCatalog)) as IRestEndpointCandidateRuntimeCatalog;

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings.query.explicit.withdraw", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile-runtime/query-explicit-withdraw/orders/lookup/{orderId}", endpoint.RoutePattern);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback,
            endpoint.BindingFallbackMode);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback.GetWireName(),
            endpoint.Metadata["bindingFallbackMode"]);
        Assert.Single(endpoint.BindingDescriptors);
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "orderId");
        Assert.Contains(
            endpoint.SelectedOverrideActionKinds,
            static actionKind => actionKind == RestEndpointOverrideActionKind.PreserveImplicitQueryFallback);
        Assert.Contains(
            endpoint.AppliedOverrideActionKinds,
            static actionKind => actionKind == RestEndpointOverrideActionKind.PreserveImplicitQueryFallback);

        Assert.NotNull(runtimeCandidateCatalog);
        var runtimeCandidate = Assert.Single(runtimeCandidateCatalog.Candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.query.explicit.withdraw", StringComparison.Ordinal));
        Assert.Null(runtimeCandidate.OriginalProjection.BindingFallbackMode);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.query.explicit.withdraw", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("withdraw-query-quantity", candidate.AppliedOverrideId);
        Assert.Null(candidate.OriginalProjection.BindingFallbackMode);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback,
            candidate.ProjectedEndpoint.BindingFallbackMode);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback.GetWireName(),
            candidate.ProjectedEndpoint.Metadata["bindingFallbackMode"]);
        Assert.Contains(
            candidate.SelectedOverrideActionKinds,
            static actionKind => actionKind == RestEndpointOverrideActionKind.PreserveImplicitQueryFallback);
        Assert.Contains(
            candidate.SelectedOverrideActionKinds,
            static actionKind => actionKind == RestEndpointOverrideActionKind.RemoveBindingProperties);
        Assert.Contains(
            candidate.AppliedOverrideActionKinds,
            static actionKind => actionKind == RestEndpointOverrideActionKind.PreserveImplicitQueryFallback);
        Assert.Contains(
            candidate.AppliedOverrideActionKinds,
            static actionKind => actionKind == RestEndpointOverrideActionKind.RemoveBindingProperties);

        var rule = Assert.Single(overrides, static item =>
            string.Equals(item.Id, "withdraw-query-quantity", StringComparison.Ordinal));
        Assert.True(rule.PreserveImplicitQueryFallback);
        Assert.Single(rule.RemovedBindingProperties);
        Assert.Contains(nameof(ProfileBindingQueryFallbackPartialRuntimeInput.Quantity), rule.RemovedBindingProperties);
        Assert.Contains(
            RestEndpointOverrideActionKind.PreserveImplicitQueryFallback,
            rule.ActionKinds);
        Assert.Contains(
            RestEndpointOverrideActionKind.PreserveImplicitQueryFallback,
            rule.SelectedActionKinds);
        Assert.Contains(
            RestEndpointOverrideActionKind.PreserveImplicitQueryFallback,
            rule.AppliedActionKinds);

        var response = await client.GetAsync("/api/v6/tests/profile-runtime/query-explicit-withdraw/orders/lookup/ord-71?quantity=14");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProfileBindingQueryFallbackPartialRuntimeOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-71", payload.OrderId);
        Assert.Equal(14, payload.Quantity);

        Assert.Contains(snapshot.RestEndpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings.query.explicit.withdraw", StringComparison.Ordinal) &&
            item.BindingFallbackMode == RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback &&
            item.AppliedOverrideActionKinds.Contains(RestEndpointOverrideActionKind.PreserveImplicitQueryFallback));
        Assert.Contains(snapshot.RestEndpointCandidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.query.explicit.withdraw", StringComparison.Ordinal) &&
            item.ProjectedEndpoint.BindingFallbackMode == RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback &&
            item.AppliedOverrideActionKinds.Contains(RestEndpointOverrideActionKind.PreserveImplicitQueryFallback));
        Assert.Contains(snapshot.RestEndpointOverrides, static item =>
            string.Equals(item.Id, "withdraw-query-quantity", StringComparison.Ordinal) &&
            item.PreserveImplicitQueryFallback &&
            item.AppliedActionKinds.Contains(RestEndpointOverrideActionKind.PreserveImplicitQueryFallback));
    }

    [Fact]
    public void MapCephalonRejectsMergeBindingRemovalWhenExplicitQueryBindingWouldStopBindingWithoutPreserveFlag()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:withdraw-query-quantity:Behaviors:0"] = "tests.rest.profile.bindings.query.explicit.withdraw";
        builder.Configuration["RestApi:Overrides:withdraw-query-quantity:RemovedBindingProperties:0"] = "Quantity";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingExplicitQueryWithdrawalRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        using var app = builder.Build();
        var exception = Assert.Throws<InvalidOperationException>(() => app.MapCephalon());

        Assert.Contains("withdraw-query-quantity", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(nameof(ProfileBindingQueryFallbackPartialRuntimeInput.Quantity), exception.Message, StringComparison.Ordinal);
        Assert.Contains("PreserveImplicitQueryFallback", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ClearBindings", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonAppliesClearBindingsOverridesAndReturnsToImplicitRequestBindingBaseline()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:clear-explicit-bindings:Behaviors:0"] = "tests.rest.profile.bindings";
        builder.Configuration["RestApi:Overrides:clear-explicit-bindings:ClearBindings"] = "true";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");
        var rawOverrides = await client.GetStringAsync("/engine/rest-endpoint-overrides");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");
        var rawSnapshot = await client.GetStringAsync("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal("clear-explicit-bindings", endpoint.AppliedOverrideId);
        Assert.Equal(["clear-explicit-bindings"], endpoint.MatchedOverrideIds);
        Assert.NotNull(endpoint.OriginalProjection);
        Assert.Equal(4, endpoint.OriginalProjection!.BindingDescriptors.Count);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback,
            endpoint.OriginalProjection.BindingFallbackMode);
        Assert.Empty(endpoint.BindingDescriptors);
        Assert.Null(endpoint.BindingFallbackMode);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("clear-explicit-bindings", candidate.AppliedOverrideId);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);
        Assert.Equal(4, candidate.OriginalProjection.BindingDescriptors.Count);
        Assert.Equal(
            RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback,
            candidate.OriginalProjection.BindingFallbackMode);
        Assert.Empty(candidate.ProjectedEndpoint.BindingDescriptors);
        Assert.Null(candidate.ProjectedEndpoint.BindingFallbackMode);

        var rule = Assert.Single(overrides, static item =>
            string.Equals(item.Id, "clear-explicit-bindings", StringComparison.Ordinal));
        Assert.True(rule.ClearBindings);
        Assert.Equal(RestEndpointOverrideBindingMode.Unspecified, rule.BindingMode);
        Assert.Empty(rule.Bindings);
        Assert.Empty(rule.RemovedBindingProperties);
        Assert.Contains("\"bindingMode\":\"unspecified\"", rawOverrides, StringComparison.Ordinal);

        Assert.Contains(snapshot.RestEndpointOverrides, static item =>
            string.Equals(item.Id, "clear-explicit-bindings", StringComparison.Ordinal) &&
            item.ClearBindings);
        Assert.Contains("\"bindingMode\":\"unspecified\"", rawSnapshot, StringComparison.Ordinal);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/bindings/orders/ord-68");
        request.Content = JsonContent.Create(new
        {
            quantity = 12,
            correlationId = "corr-68",
            note = "implicit baseline",
            ignored = "body-fallback"
        });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProfileBindingRuntimeOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-68", payload.OrderId);
        Assert.Equal(12, payload.Quantity);
        Assert.Equal("corr-68", payload.CorrelationId);
        Assert.Equal("implicit baseline", payload.Note);
        Assert.Equal("body-fallback", payload.Ignored);
    }

    [Fact]
    public async Task MapCephalonDoesNotExposeAppliedOverrideIdWhenBindingsOnlyReorderTheSourcePlan()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Behaviors:0"] = "tests.rest.profile.bindings";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:0:PropertyName"] = "CorrelationId";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:0:Source"] = "header";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:0:Name"] = "X-Correlation-Id";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:1:PropertyName"] = "Note";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:1:Source"] = "body";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:1:Name"] = "note";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:2:PropertyName"] = "OrderId";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:2:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:2:Name"] = "orderId";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:3:PropertyName"] = "Quantity";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:3:Source"] = "query";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:3:Name"] = "quantity";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Null(endpoint.AppliedOverrideId);
        Assert.Equal("prefer-current-bindings", endpoint.SelectedOverrideId);
        Assert.Equal(["prefer-current-bindings"], endpoint.MatchedOverrideIds);
        Assert.Equal(
            RestEndpointGovernanceRuleSelectionBasis.SingleMatch,
            endpoint.OverrideSelectionBasis);
        Assert.NotNull(endpoint.OriginalProjection);
        Assert.Collection(
            endpoint.BindingDescriptors,
            orderId =>
            {
                Assert.Equal("OrderId", orderId.PropertyName);
                Assert.Equal(RestEndpointBindingSource.Route, orderId.Source);
                Assert.Equal("orderId", orderId.Name);
            },
            quantity =>
            {
                Assert.Equal("Quantity", quantity.PropertyName);
                Assert.Equal(RestEndpointBindingSource.Query, quantity.Source);
                Assert.Equal("quantity", quantity.Name);
            },
            correlationId =>
            {
                Assert.Equal("CorrelationId", correlationId.PropertyName);
                Assert.Equal(RestEndpointBindingSource.Header, correlationId.Source);
                Assert.Equal("X-Correlation-Id", correlationId.Name);
            },
            note =>
            {
                Assert.Equal("Note", note.PropertyName);
                Assert.Equal(RestEndpointBindingSource.Body, note.Source);
                Assert.Equal("note", note.Name);
            });

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Null(candidate.AppliedOverrideId);
        Assert.Equal("prefer-current-bindings", candidate.SelectedOverrideId);
        Assert.Equal(["prefer-current-bindings"], candidate.MatchedOverrideIds);
        Assert.Equal(
            RestEndpointGovernanceRuleSelectionBasis.SingleMatch,
            candidate.OverrideSelectionBasis);
        Assert.Collection(
            candidate.ProjectedEndpoint.BindingDescriptors,
            orderId =>
            {
                Assert.Equal("OrderId", orderId.PropertyName);
                Assert.Equal(RestEndpointBindingSource.Route, orderId.Source);
                Assert.Equal("orderId", orderId.Name);
            },
            quantity =>
            {
                Assert.Equal("Quantity", quantity.PropertyName);
                Assert.Equal(RestEndpointBindingSource.Query, quantity.Source);
                Assert.Equal("quantity", quantity.Name);
            },
            correlationId =>
            {
                Assert.Equal("CorrelationId", correlationId.PropertyName);
                Assert.Equal(RestEndpointBindingSource.Header, correlationId.Source);
                Assert.Equal("X-Correlation-Id", correlationId.Name);
            },
            note =>
            {
                Assert.Equal("Note", note.PropertyName);
                Assert.Equal(RestEndpointBindingSource.Body, note.Source);
                Assert.Equal("note", note.Name);
            });

        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.Id, endpoint.Id, StringComparison.Ordinal) &&
            item.AppliedOverrideId is null &&
            string.Equals(item.SelectedOverrideId, "prefer-current-bindings", StringComparison.Ordinal) &&
            item.OverrideSelectionBasis == RestEndpointGovernanceRuleSelectionBasis.SingleMatch &&
            item.MatchedOverrideIds.SequenceEqual(["prefer-current-bindings"]));
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            item.AppliedOverrideId is null &&
            string.Equals(item.SelectedOverrideId, "prefer-current-bindings", StringComparison.Ordinal) &&
            item.OverrideSelectionBasis == RestEndpointGovernanceRuleSelectionBasis.SingleMatch &&
            item.MatchedOverrideIds.SequenceEqual(["prefer-current-bindings"]));

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/bindings/orders/ord-69?quantity=13");
        request.Headers.Add("X-Correlation-Id", "corr-69");
        request.Content = JsonContent.Create(new
        {
            note = "same binding semantics",
            ignored = "body-fallback"
        });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProfileBindingRuntimeOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-69", payload.OrderId);
        Assert.Equal(13, payload.Quantity);
        Assert.Equal("corr-69", payload.CorrelationId);
        Assert.Equal("same binding semantics", payload.Note);
        Assert.Equal("body-fallback", payload.Ignored);
    }

    [Fact]
    public async Task MapCephalonLogsBehaviorHttpGovernanceOverrideNoOpAndFallbackOutcomes()
    {
        var loggerProvider = new TestLoggerProvider();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Behaviors:0"] = "tests.rest.profile.bindings";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:0:PropertyName"] = "CorrelationId";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:0:Source"] = "header";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:0:Name"] = "X-Correlation-Id";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:1:PropertyName"] = "Note";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:1:Source"] = "body";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:1:Name"] = "note";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:2:PropertyName"] = "OrderId";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:2:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:2:Name"] = "orderId";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:3:PropertyName"] = "Quantity";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:3:Source"] = "query";
        builder.Configuration["RestApi:Overrides:prefer-current-bindings:Bindings:3:Name"] = "quantity";
        builder.Configuration["RestApi:Overrides:prefer-route-order:Behaviors:0"] = "tests.rest.profile.bindings.query.partial";
        builder.Configuration["RestApi:Overrides:prefer-route-order:Pattern"] = "/lookup/{orderId}";
        builder.Configuration["RestApi:Overrides:prefer-route-order:Bindings:0:PropertyName"] = "OrderId";
        builder.Configuration["RestApi:Overrides:prefer-route-order:Bindings:0:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-route-order:Bindings:0:Name"] = "orderId";
        builder.Configuration["RestApi:Overrides:withdraw-body-note:Behaviors:0"] = "tests.rest.profile.bindings.inference";
        builder.Configuration["RestApi:Overrides:withdraw-body-note:RemovedBindingProperties:0"] = "Note";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingRuntimeCatalogModule());
            engine.AddModule(new ProfileBindingPartialQueryFallbackRuntimeCatalogModule());
            engine.AddModule(new ProfileBindingInferenceRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();
        var candidates = await client.GetFromJsonAsync<List<RestEndpointCandidateRuntimeDescriptor>>("/engine/rest-endpoint-candidates");

        Assert.NotNull(candidates);

        var appliedCandidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings.query.partial", StringComparison.Ordinal));
        var noOpCandidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        var appliedSelectedActionKinds = JoinActionKinds(appliedCandidate.SelectedOverrideActionKinds);
        var appliedActionKinds = JoinActionKinds(appliedCandidate.AppliedOverrideActionKinds);
        var appliedSelectionBasis = JoinSelectionBasis(appliedCandidate.OverrideSelectionBasis);
        var noOpSelectedActionKinds = JoinActionKinds(noOpCandidate.SelectedOverrideActionKinds);
        var noOpAppliedActionKinds = JoinActionKinds(noOpCandidate.AppliedOverrideActionKinds);
        var noOpSelectionBasis = JoinSelectionBasis(noOpCandidate.OverrideSelectionBasis);
        var preservedImplicitFallbackWireName = RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback.GetWireName();
        var preservedRemainingBodyFallbackWireName = RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback.GetWireName();

        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 5202 &&
            entry.Message.Contains("tests.rest.profile.bindings.query.partial", StringComparison.Ordinal) &&
            entry.Message.Contains("prefer-route-order", StringComparison.Ordinal) &&
            entry.Message.Contains(appliedSelectionBasis, StringComparison.Ordinal) &&
            entry.Message.Contains(appliedSelectedActionKinds, StringComparison.Ordinal) &&
            entry.Message.Contains(appliedActionKinds, StringComparison.Ordinal) &&
            entry.Message.Contains("/api/v6/tests/profile-runtime/query-partial/orders/lookup/{orderId}", StringComparison.Ordinal));
        Assert.DoesNotContain(loggerProvider.Entries, entry =>
            entry.EventId.Id == 5202 &&
            entry.Message.Contains("tests.rest.profile.bindings", StringComparison.Ordinal) &&
            entry.Message.Contains("prefer-current-bindings", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 5203 &&
            entry.Message.Contains("tests.rest.profile.bindings", StringComparison.Ordinal) &&
            entry.Message.Contains("prefer-current-bindings", StringComparison.Ordinal) &&
            entry.Message.Contains(noOpSelectionBasis, StringComparison.Ordinal) &&
            entry.Message.Contains(noOpSelectedActionKinds, StringComparison.Ordinal) &&
            entry.Message.Contains(noOpAppliedActionKinds, StringComparison.Ordinal) &&
            entry.Message.Contains("selected governance override", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 5204 &&
            entry.Message.Contains("tests.rest.profile.bindings.query.partial", StringComparison.Ordinal) &&
            entry.Message.Contains(preservedImplicitFallbackWireName, StringComparison.Ordinal) &&
            entry.Message.Contains("prefer-route-order", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 5204 &&
            entry.Message.Contains("tests.rest.profile.bindings.inference", StringComparison.Ordinal) &&
            entry.Message.Contains(preservedRemainingBodyFallbackWireName, StringComparison.Ordinal) &&
            entry.Message.Contains("withdraw-body-note", StringComparison.Ordinal));
        Assert.DoesNotContain(loggerProvider.Entries, entry =>
            entry.EventId.Id == 5204 &&
            entry.Message.Contains("tests.rest.profile.bindings.query.partial", StringComparison.Ordinal) &&
            entry.Message.Contains("PreserveSourceImplicitFallback", StringComparison.Ordinal));
        Assert.DoesNotContain(loggerProvider.Entries, entry =>
            entry.EventId.Id == 5204 &&
            entry.Message.Contains("tests.rest.profile.bindings.inference", StringComparison.Ordinal) &&
            entry.Message.Contains("PreserveRemainingBodyFallback", StringComparison.Ordinal));
    }

    private static string JoinActionKinds(IReadOnlyList<RestEndpointOverrideActionKind> actionKinds)
    {
        ArgumentNullException.ThrowIfNull(actionKinds);

        return actionKinds.Count == 0
            ? "(none)"
            : string.Join(", ", actionKinds.Select(static item => item.GetWireName()));
    }

    private static string JoinSelectionBasis(RestEndpointGovernanceRuleSelectionBasis? selectionBasis)
    {
        return selectionBasis.HasValue
            ? selectionBasis.Value.GetWireName()
            : "(none)";
    }

    [Fact]
    public void MapCephalonRejectsClearBindingsWhenRoutePlaceholdersRequireExplicitAliases()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:clear-explicit-bindings:Behaviors:0"] = "tests.rest.profile.bindings.alias";
        builder.Configuration["RestApi:Overrides:clear-explicit-bindings:ClearBindings"] = "true";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingAliasRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        var app = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(() => app.MapCephalon());

        Assert.Contains("clear-explicit-bindings", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ClearBindings", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(nameof(ProfileBindingAliasRuntimeInput.OrderId), exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MapCephalonAllowsPlaceholderRenameWhenOverrideBindingsCoverTheRenamedRouteSet()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Behaviors:0"] = "tests.rest.profile.bindings";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Pattern"] = "/lookup/{id}";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:0:PropertyName"] = "OrderId";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:0:Source"] = "route";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:0:Name"] = "id";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:1:PropertyName"] = "Quantity";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:1:Source"] = "query";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:1:Name"] = "quantity";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:2:PropertyName"] = "CorrelationId";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:2:Source"] = "header";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:2:Name"] = "X-Correlation-Id";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:3:PropertyName"] = "Note";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:3:Source"] = "body";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:3:Name"] = "note";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders/lookup/{id}", endpoint.RoutePattern);
        Assert.NotNull(endpoint.OriginalProjection);
        Assert.Equal(6, endpoint.OriginalProjection!.ApiVersionMajor);
        Assert.Equal("v6", endpoint.OriginalProjection.OpenApiDocumentName);
        Assert.Equal("POST", endpoint.OriginalProjection.Method);
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders", endpoint.OriginalProjection.RouteGroupPrefix);
        Assert.Equal("/{orderId}", endpoint.OriginalProjection.RelativePattern);
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders/{orderId}", endpoint.OriginalProjection.RoutePattern);
        Assert.Equal(4, endpoint.BindingDescriptors.Count);
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "id");

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-renamed-placeholder", candidate.AppliedOverrideId);
        Assert.Equal(6, candidate.OriginalProjection.ApiVersionMajor);
        Assert.Equal("v6", candidate.OriginalProjection.OpenApiDocumentName);
        Assert.Equal("POST", candidate.OriginalProjection.Method);
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders", candidate.OriginalProjection.RouteGroupPrefix);
        Assert.Equal("/{orderId}", candidate.OriginalProjection.RelativePattern);
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders/{orderId}", candidate.OriginalProjection.RoutePattern);
        Assert.Equal(4, candidate.OriginalProjection.BindingDescriptors.Count);
        Assert.Contains(candidate.OriginalProjection.BindingDescriptors, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "orderId");
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders/lookup/{id}", candidate.ProjectedEndpoint.RoutePattern);
        Assert.Equal(candidate.OriginalProjection.RoutePattern, endpoint.OriginalProjection.RoutePattern);
        Assert.Equal(candidate.OriginalProjection.RouteGroupPrefix, endpoint.OriginalProjection.RouteGroupPrefix);
        Assert.Equal(candidate.OriginalProjection.RelativePattern, endpoint.OriginalProjection.RelativePattern);

        var rule = Assert.Single(overrides, static item =>
            string.Equals(item.Id, "prefer-renamed-placeholder", StringComparison.Ordinal));
        Assert.Equal("/lookup/{id}", rule.Pattern);
        Assert.Equal(4, rule.Bindings.Count);
        Assert.Contains(rule.Bindings, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "id");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/bindings/orders/lookup/ord-88?quantity=8");
        request.Headers.Add("X-Correlation-Id", "corr-88");
        request.Content = JsonContent.Create(new
        {
            note = "renamed placeholder",
            ignored = "body-fallback"
        });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProfileBindingRuntimeOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-88", payload.OrderId);
        Assert.Equal(8, payload.Quantity);
        Assert.Equal("corr-88", payload.CorrelationId);
        Assert.Equal("renamed placeholder", payload.Note);
        Assert.Equal("body-fallback", payload.Ignored);
    }

    [Fact]
    public async Task MapCephalonAllowsPlaceholderRemovalWhenAffectedPropertiesStayExplicitlyBound()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Behaviors:0"] = "tests.rest.profile.bindings";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Pattern"] = "/lookup";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:0:PropertyName"] = "OrderId";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:0:Source"] = "query";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:0:Name"] = "orderId";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:1:PropertyName"] = "Quantity";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:1:Source"] = "query";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:1:Name"] = "quantity";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:2:PropertyName"] = "CorrelationId";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:2:Source"] = "header";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:2:Name"] = "X-Correlation-Id";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:3:PropertyName"] = "Note";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:3:Source"] = "body";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:3:Name"] = "note";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var candidates = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor[]>("/engine/rest-endpoint-candidates");
        var overrides = await client.GetFromJsonAsync<RestEndpointOverrideDescriptor[]>("/engine/rest-endpoint-overrides");

        Assert.NotNull(endpoints);
        Assert.NotNull(candidates);
        Assert.NotNull(overrides);

        var endpoint = Assert.Single(endpoints, static item =>
            string.Equals(item.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders/lookup", endpoint.RoutePattern);
        Assert.Equal(4, endpoint.BindingDescriptors.Count);
        Assert.DoesNotContain(endpoint.BindingDescriptors, static binding =>
            binding.Source == RestEndpointBindingSource.Route);
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Query &&
            binding.Name == "orderId");

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-query-identity", candidate.AppliedOverrideId);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders/lookup", candidate.ProjectedEndpoint.RoutePattern);

        var rule = Assert.Single(overrides, static item =>
            string.Equals(item.Id, "prefer-query-identity", StringComparison.Ordinal));
        Assert.Equal("/lookup", rule.Pattern);
        Assert.Equal(4, rule.Bindings.Count);
        Assert.Contains(rule.Bindings, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Query &&
            binding.Name == "orderId");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/bindings/orders/lookup?orderId=ord-74&quantity=5");
        request.Headers.Add("X-Correlation-Id", "corr-74");
        request.Content = JsonContent.Create(new
        {
            note = "removed placeholder",
            ignored = "body-fallback"
        });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProfileBindingRuntimeOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-74", payload.OrderId);
        Assert.Equal(5, payload.Quantity);
        Assert.Equal("corr-74", payload.CorrelationId);
        Assert.Equal("removed placeholder", payload.Note);
        Assert.Equal("body-fallback", payload.Ignored);
    }

    [Fact]
    public async Task MapCephalonRejectsBodyConflictsWithExplicitProfileBindings()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/bindings/orders/ord-42?quantity=3");
        request.Headers.Add("X-Correlation-Id", "corr-42");
        request.Content = JsonContent.Create(new
        {
            note = "gift wrap",
            quantity = 99
        });

        var response = await client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("Quantity", payload, StringComparison.Ordinal);
        Assert.Contains("conflicts", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MapCephalonInfersRouteValuesForUnboundProfileBindingProperties()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingInferenceRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v6/tests/profile-runtime/inference/orders/ord-77?quantity=8");
        request.Headers.Add("X-Correlation-Id", "corr-77");
        request.Content = JsonContent.Create(new
        {
            note = "route inference"
        });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProfileBindingInferenceRuntimeOutput>();
        Assert.NotNull(payload);
        Assert.Equal("ord-77", payload.OrderId);
        Assert.Equal(8, payload.Quantity);
        Assert.Equal("corr-77", payload.CorrelationId);
        Assert.Equal("route inference", payload.Note);
    }

    [Fact]
    public void MapCephalonRejectsMethodOverridesThatLeaveInvalidBindingPlans()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "6";
        builder.Configuration["OpenApi:DefaultVersion"] = "6";
        builder.Configuration["RestApi:Overrides:prefer-get:Behaviors:0"] = "tests.rest.profile.bindings";
        builder.Configuration["RestApi:Overrides:prefer-get:Method"] = "GET";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileBindingRuntimeCatalogModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        var app = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(() => app.MapCephalon());

        Assert.Contains("prefer-get", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not accept a request body", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MapCephalonDoesNotPublishProfileMetadataWithoutExplicitModuleConsumption()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new ProfileMetadataOnlyRuntimeModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");

        Assert.NotNull(endpoints);
        Assert.Empty(endpoints);
    }

    [Fact]
    public void MapCephalonRejectsCollidingDslAndManualRestEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new DslCollisionRuntimeModule());
            engine.AddModule(new ManualCollisionRuntimeModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        var app = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(() => app.MapCephalon());

        Assert.Contains("Resolved public REST endpoint collision detected", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GET /api/v1/tests/runtime-catalog/collisions/manual/{itemId}", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tests.rest.runtime-collision.dsl", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tests.rest.runtime-collision.manual", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("source kind 'module-dsl'", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("source kind 'manual'", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CatalogRestModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.runtime-catalog",
            "Runtime Catalog Module",
            "Publishes projection-backed REST endpoints for runtime catalog tests.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            var group = behaviors.Group("/tests/runtime-catalog/cart")
                .WithTagName("Runtime Catalog API")
                .ApiVersion(2);

            group.MapGet<GetCatalogCartBehavior>("/{cartId}");
            group.MapPost<AddCatalogCartItemBehavior>("/{cartId}/items");
        }
    }

    private sealed class CollidingLookupModuleOne : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.runtime-collision.one",
            "Runtime Collision One",
            "First colliding REST module.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/runtime-catalog/collisions/items")
                .MapGet<LookupCollisionOneBehavior>("/{itemId}");
        }
    }

    private sealed class CollidingLookupModuleTwo : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.runtime-collision.two",
            "Runtime Collision Two",
            "Second colliding REST module.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/runtime-catalog/collisions/items")
                .MapGet<LookupCollisionTwoBehavior>("/{itemId}");
        }
    }

    private sealed class ManualRuntimeCatalogModule : ModuleBase, IEndpointModule
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.manual-runtime",
            "Manual Runtime Module",
            "Publishes a legacy manual REST endpoint for runtime catalog coverage.",
            version: "1.4.0");

        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/tests/manual-runtime");
            group.WithTags("Manual Runtime API");
            group.WithGroupName("v4");
            group.MapGet("/orders/{orderId}", static (string orderId) => TypedResults.Ok(new ManualRuntimeOrderOutput(orderId)))
                .WithName("tests.manual-runtime.orders.get")
                .WithSummary("Gets a manual runtime order.")
                .WithDescription("Publishes a legacy Minimal API route into the runtime catalog.");
        }
    }

    private sealed class ManualBehaviorHelperRuntimeModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.manual-helper",
            "Manual Helper Module",
            "Uses MapAdditionalEndpoints for behavior-aware REST helper coverage.",
            version: "1.1.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Internal<GetManualHelperOrderBehavior>();
        }

        protected override void MapAdditionalEndpoints(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapBehaviorRestGroup(this, "/tests/manual-helper/orders")
                .ApiVersion(3)
                .WithTagName("Manual Helper API");
            group.MapBehaviorGet<GetManualHelperOrderBehavior>("/{orderId}");
        }
    }

    private sealed class DslCollisionRuntimeModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.runtime-collision.dsl",
            "Runtime Collision DSL",
            "Publishes a projection-backed runtime-collision endpoint.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/runtime-catalog/collisions/manual")
                .MapGet<GetDslCollisionOrderBehavior>("/{itemId}");
        }
    }

    private sealed class ManualCollisionRuntimeModule : ModuleBase, IEndpointModule
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.runtime-collision.manual",
            "Runtime Collision Manual",
            "Publishes a manual runtime-collision endpoint.",
            version: "1.0.0");

        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet(
                "/v1/tests/runtime-catalog/collisions/manual/{itemId}",
                static (string itemId) => TypedResults.Ok(new ManualCollisionOutput(itemId)));
        }
    }

    private sealed class ProfileDrivenRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime",
            "Profile Runtime Module",
            "Publishes profile-driven REST endpoints for runtime catalog coverage.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile-runtime/orders")
                .WithTagName("Profile Runtime API")
                .MapProfile<GetProfileRuntimeOrderBehavior>();
        }
    }

    private sealed class ProfileVersionOverrideRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.override",
            "Profile Runtime Override Module",
            "Overrides a profile-declared version from the owning module DSL.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile-runtime/override/orders")
                .ApiVersion(5)
                .WithTagName("Profile Runtime Override API")
                .MapProfile<GetProfileOverrideOrderBehavior>();
        }
    }

    private sealed class GeneratedRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.generated-runtime",
            "Generated Runtime Module",
            "Publishes generated module-owned REST endpoints for runtime catalog coverage.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/generated/runtime")
                .WithTagName("Generated Runtime API")
                .MapGeneratedProfiles();
        }
    }

    private sealed class GeneratedGroupedRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.generated-grouped-runtime",
            "Generated Grouped Runtime Module",
            "Publishes generated REST profiles across multiple derived route groups while preserving module ownership.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.MapGeneratedProfileGroups(
                "tests.generated.runtimegrouped",
                group => group
                    .WithTagName("Generated Grouped Runtime API")
                    .WithHostGovernanceScope("generated-grouped"));
        }
    }

    private sealed class GeneratedVersionOverrideRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.generated-runtime.override",
            "Generated Runtime Override Module",
            "Publishes generated shorthand so REST governance can override the effective API version.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/generated/runtime/override")
                .WithTagName("Generated Override API")
                .MapGeneratedProfiles("tests.generated.runtimeoverride");
        }
    }

    private sealed class ExplicitDslHostGovernanceDisabledRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.dsl-runtime.override-disabled",
            "Explicit DSL Governance Disabled Module",
            "Publishes an explicit module-DSL route that remains authoritative when host governance is not opted in.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/dsl/runtime/override-disabled/orders")
                .ApiVersion(9)
                .WithTagName("Explicit DSL Governance Disabled API")
                .WithHostGovernanceScope("explicit-disabled")
                .MapGet<GetExplicitDslHostGovernanceDisabledRuntimeOrderBehavior>("/{orderId}");
        }
    }

    private sealed class ExplicitDslHostGovernanceEnabledRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.dsl-runtime.override-enabled",
            "Explicit DSL Governance Enabled Module",
            "Publishes an explicit module-DSL route that opts into host governance.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/dsl/runtime/override-enabled/orders")
                .ApiVersion(9)
                .WithTagName("Explicit DSL Governance Enabled API")
                .WithHostGovernanceScope("explicit-enabled")
                .AllowHostGovernance()
                .MapGet<GetExplicitDslHostGovernanceEnabledRuntimeOrderBehavior>("/{orderId}");
        }
    }

    private sealed class ExplicitDslHostGovernanceSuppressionRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.dsl-runtime.suppression-enabled",
            "Explicit DSL Governance Suppression Module",
            "Publishes an explicit module-DSL route that opts into host suppression governance.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/dsl/runtime/suppression-enabled/orders")
                .ApiVersion(9)
                .WithTagName("Explicit DSL Governance Suppression API")
                .WithHostGovernanceScope("explicit-suppression")
                .AllowHostGovernance()
                .MapGet<GetExplicitDslHostGovernanceSuppressedRuntimeOrderBehavior>("/{orderId}");
        }
    }

    private sealed class ProfileCapabilityOverrideRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.override.capability",
            "Profile Runtime Capability Override Module",
            "Publishes a profile-backed route whose REST capability boundary is governed by a host override.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile/runtime/override/capability/orders")
                .WithTagName("Profile Capability Override API")
                .MapProfile<GetProfileCapabilityOverrideRuntimeOrderBehavior>(builder =>
                    builder.RequireCapability("restricted.original"));
        }
    }

    private sealed class ProfileSeededDocumentVersionOverrideRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.document-pinned",
            "Profile Document Pinned Runtime Module",
            "Publishes a profile-backed shorthand route beneath an explicit OpenAPI document name so profile-seeded route-version overrides can keep document truth pinned.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile/runtime/document-pinned")
                .WithOpenApiDocumentName("public")
                .WithTagName("Profile Document Pinned API")
                .MapProfile<GetProfileDocumentPinnedRuntimeOrderBehavior>();
        }
    }

    private sealed class ProfileCapabilityClearRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.clear.capability",
            "Profile Runtime Capability Clear Module",
            "Publishes a profile-backed route whose REST capability boundary is cleared by a host override.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile/runtime/clear/capability/orders")
                .WithTagName("Profile Capability Clear API")
                .MapProfile<GetProfileCapabilityClearRuntimeOrderBehavior>(builder =>
                    builder.RequireCapability("restricted.original"));
        }
    }

    private sealed class ProfileCapabilityClearNoOpRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.clear.noop.capability",
            "Profile Runtime Capability Clear No-Op Module",
            "Publishes a profile-backed route whose host clear rule leaves the capability boundary unchanged.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile/runtime/clear/noop/capability/orders")
                .WithTagName("Profile Capability Clear No-Op API")
                .MapProfile<GetProfileCapabilityClearNoOpRuntimeOrderBehavior>();
        }
    }

    private sealed class ProfileMetadataRewriteNoOpRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.noop.metadata",
            "Profile Runtime Metadata Rewrite No-Op Module",
            "Publishes a profile-backed route whose explicit module metadata already matches the host metadata rule.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile/runtime/noop/metadata/orders")
                .WithTagName("Profile Metadata No-Op API")
                .MapProfile<GetProfileMetadataRewriteNoOpRuntimeOrderBehavior>(builder =>
                {
                    builder.WithName("tests.profile.runtimeoverride.noop.metadata.lookup");
                    builder.Add(endpointBuilder =>
                    {
                        endpointBuilder.Metadata.Add(new RuntimeCatalogTestEndpointSummaryMetadata(
                            "Gets a profile-backed runtime order through explicit module metadata."));
                        endpointBuilder.Metadata.Add(new RuntimeCatalogTestEndpointDescriptionMetadata(
                            "Publishes explicit module metadata so a matching host rule becomes a runtime no-op."));
                    });
                });
        }
    }

    private sealed class ProfileMetadataClearNoOpRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.noop.clear.metadata",
            "Profile Runtime Metadata Clear No-Op Module",
            "Publishes a profile-backed route whose explicit module configuration already clears description metadata before host governance runs.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile/runtime/noop/clear/metadata/orders")
                .WithTagName("Profile Metadata Clear No-Op API")
                .MapProfile<GetProfileMetadataClearNoOpRuntimeOrderBehavior>(builder =>
                    builder.Add(endpointBuilder =>
                    {
                        for (var index = endpointBuilder.Metadata.Count - 1; index >= 0; index--)
                        {
                            if (endpointBuilder.Metadata[index] is IEndpointDescriptionMetadata)
                            {
                                endpointBuilder.Metadata.RemoveAt(index);
                            }
                        }
                    }));
        }
    }

    private sealed class ExplicitVersionOverrideRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.generated-runtime.explicit-override",
            "Generated Runtime Explicit Override Module",
            "Publishes generated shorthand beneath an explicitly versioned group.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/generated/runtime/explicit-override")
                .ApiVersion(8)
                .WithTagName("Generated Explicit Override API")
                .MapGeneratedProfiles("tests.generated.runtimeexplicitoverride");
        }
    }

    private sealed class GeneratedThreeWaySuppressionRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.generated-runtime.threeway",
            "Generated Three-Way Runtime Module",
            "Publishes explicit, profile, and generated routes for precedence visibility coverage.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            var group = behaviors.Group("/tests/generated/runtime/threeway/orders")
                .ApiVersion(12)
                .WithTagName("Generated Three-Way API");

            group.MapGeneratedProfiles("tests.rest.generated.threeway");
            group.MapProfile<GetGeneratedThreeWayRuntimeOrderBehavior>();
            group.MapGet<GetGeneratedThreeWayRuntimeOrderBehavior>("/explicit/{orderId}");
        }
    }

    private sealed class GeneratedProfileGovernanceRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.generated-runtime.governance",
            "Generated Profile Governance Runtime Module",
            "Publishes generated and profile shorthand so REST governance can suppress the profile candidate.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            var group = behaviors.Group("/tests/generated/runtime/governed/orders")
                .WithTagName("Generated Governance API")
                .WithHostGovernanceScope("generated-governance");

            group.MapGeneratedProfiles("tests.rest.generated.threeway");
            group.MapProfile<GetGeneratedThreeWayRuntimeOrderBehavior>();
        }
    }

    private sealed class SplitProfileVersionOverrideRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.split",
            "Profile Split Override Module",
            "Publishes multiple profile-driven shorthand endpoints beneath one group so governance can split their effective versions.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            var group = behaviors.Group("/tests/profile/runtime/split")
                .WithTagName("Profile Split Override API");

            group.MapProfile<GetSplitProfileRuntimeOrderBehavior>();
            group.MapProfile<GetSplitProfileRuntimeOrderDetailsBehavior>();
        }
    }

    private sealed class ProfileMetadataOnlyRuntimeModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.metadata-only",
            "Profile Metadata Only Module",
            "Owns a behavior with profile metadata without consuming it as public REST.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Internal<GetProfileMetadataOnlyOrderBehavior>();
        }
    }

    private sealed class ProfileBindingRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.bindings",
            "Profile Runtime Binding Module",
            "Publishes profile-driven REST endpoints with explicit input bindings.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile-runtime/bindings/orders")
                .WithTagName("Profile Runtime Binding API")
                .MapProfile<PostProfileBindingRuntimeOrderBehavior>();
        }
    }

    private sealed class ProfileBindingInferenceRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.bindings.inference",
            "Profile Runtime Binding Inference Module",
            "Publishes profile-driven REST endpoints that infer unbound route values.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile-runtime/inference/orders")
                .WithTagName("Profile Runtime Binding Inference API")
                .MapProfile<PostProfileBindingInferenceRuntimeOrderBehavior>();
        }
    }

    private sealed class ProfileBindingGetRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.bindings.get",
            "Profile Runtime Binding GET Module",
            "Publishes a GET profile so implicit body-fallback promotion remains rejected.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile-runtime/get/orders")
                .WithTagName("Profile Runtime Binding GET API")
                .MapProfile<GetProfileBindingRuntimeOrderBehavior>();
        }
    }

    private sealed class ProfileBindingQueryFallbackRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.bindings.query",
            "Profile Runtime Binding Query Fallback Module",
            "Publishes a shorthand GET profile with no explicit binding plan so bounded implicit query-fallback promotion remains visible.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile-runtime/query/orders")
                .WithTagName("Profile Runtime Binding Query API")
                .MapProfile<GetProfileBindingQueryFallbackRuntimeOrderBehavior>();
        }
    }

    private sealed class ProfileBindingAliasRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.bindings.alias",
            "Profile Runtime Binding Alias Module",
            "Publishes a profile-driven REST endpoint whose route placeholder currently relies on an explicit alias binding.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile-runtime/alias/orders")
                .WithTagName("Profile Runtime Binding Alias API")
                .MapProfile<GetProfileBindingAliasRuntimeOrderBehavior>();
        }
    }

    private sealed class ProfileBindingPartialQueryFallbackRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.bindings.query.partial",
            "Profile Runtime Partial Query Fallback Module",
            "Publishes a shorthand GET profile with no explicit binding plan so partial explicit overrides can preserve the remaining implicit query fallback surface.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile-runtime/query-partial/orders")
                .WithTagName("Profile Runtime Partial Query API")
                .MapProfile<GetProfileBindingPartialQueryFallbackRuntimeOrderBehavior>();
        }
    }

    private sealed class ProfileBindingExplicitQueryFallbackRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.bindings.query.explicit",
            "Profile Runtime Explicit Query Fallback Module",
            "Publishes a profile-driven REST endpoint with explicit bindings that still preserve the remaining implicit query fallback surface.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile-runtime/query-explicit/orders")
                .WithTagName("Profile Runtime Explicit Query API")
                .MapProfile<GetProfileBindingExplicitQueryFallbackRuntimeOrderBehavior>();
        }
    }

    private sealed class ProfileBindingExplicitQueryWithdrawalRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.bindings.query.explicit.withdraw",
            "Profile Runtime Explicit Query Withdraw Module",
            "Publishes a profile-driven REST endpoint whose host binding rewrite would otherwise stop binding a source explicit query property without preserve intent.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile-runtime/query-explicit-withdraw/orders")
                .WithTagName("Profile Runtime Explicit Query Withdraw API")
                .MapProfile<GetProfileBindingExplicitQueryWithdrawalRuntimeOrderBehavior>();
        }
    }

    private sealed class ProfileBindingExplicitQueryWithdrawalPreservedRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.bindings.query.explicit.withdraw.preserved",
            "Profile Runtime Explicit Query Withdraw Preserved Module",
            "Publishes a profile-driven REST endpoint whose host binding rewrite keeps preserved implicit query fallback truthful after one explicit query binding is withdrawn.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile-runtime/query-explicit-withdraw/orders")
                .WithTagName("Profile Runtime Explicit Query Withdraw API")
                .MapProfile<GetProfileBindingExplicitQueryWithdrawalPreservedRuntimeOrderBehavior>();
        }
    }

    private static string BuildBehaviorProjectionCandidateId(
        string sourceModuleId,
        string behaviorId,
        string authoringStyle,
        string method,
        string routePattern)
    {
        return RestEndpointRuntimeDescriptorFactory.BuildEndpointId(
            $"{sourceModuleId}:{behaviorId}:{authoringStyle}:{method}:{routePattern}");
    }

    private sealed class ProfileSuppressionRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.suppression",
            "Profile Runtime Suppression Module",
            "Publishes both explicit and profile-backed routes for precedence visibility coverage.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            var group = behaviors.Group("/tests/profile-runtime/precedence/orders")
                .ApiVersion(8)
                .WithTagName("Profile Precedence API");

            group.MapProfile<GetProfileSuppressionOrderBehavior>();
            group.MapGet<GetProfileSuppressionOrderBehavior>("/explicit/{orderId}");
        }
    }

    private sealed class ProfileSelectorRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.selectors",
            "Profile Runtime Selector Module",
            "Publishes the same shorthand behavior beneath multiple route groups so governance selectors can target one candidate precisely.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile-runtime/selectors/primary/orders")
                .ApiVersion(6)
                .WithOpenApiDocumentName("public")
                .WithTagName("Profile Selector Primary API")
                .WithHostGovernanceScope("primary-scope")
                .MapProfile<PostProfileSelectorRuntimeOrderBehavior>();

            behaviors.Group("/tests/profile-runtime/selectors/secondary/orders")
                .ApiVersion(7)
                .WithOpenApiDocumentName("internal")
                .WithTagName("Profile Selector Secondary API")
                .WithHostGovernanceScope("secondary-scope")
                .MapProfile<PostProfileSelectorRuntimeOrderBehavior>();
        }
    }

    private sealed class BindingFallbackSelectorRuntimeCatalogModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            "tests.rest.profile-runtime.binding-fallback-selectors",
            "Binding Fallback Selector Runtime Module",
            "Publishes mixed shorthand binding-fallback shapes so governance selectors can target only the matching original fallback mode.",
            version: "1.0.0");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/profile-runtime/binding-fallback-selectors/write/orders")
                .ApiVersion(6)
                .WithTagName("Binding Fallback Selector Write API")
                .MapProfile<PostProfileBindingRuntimeOrderBehavior>();

            behaviors.Group("/tests/profile-runtime/binding-fallback-selectors/read/orders")
                .ApiVersion(6)
                .WithTagName("Binding Fallback Selector Read API")
                .MapProfile<GetProfileBindingRuntimeOrderBehavior>();
        }
    }

    [AppBehavior("tests.rest.runtime-catalog.get")]
    private sealed class GetCatalogCartBehavior : IAppBehavior<GetCatalogCartInput, GetCatalogCartOutput>
    {
        public Task<GetCatalogCartOutput> HandleAsync(
            GetCatalogCartInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new GetCatalogCartOutput(input.CartId));
        }
    }

    [AppBehavior("tests.rest.runtime-catalog.add-item")]
    private sealed class AddCatalogCartItemBehavior : IAppBehavior<AddCatalogCartItemInput, AddCatalogCartItemOutput>
    {
        public Task<AddCatalogCartItemOutput> HandleAsync(
            AddCatalogCartItemInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new AddCatalogCartItemOutput(input.CartId, input.ProductId));
        }
    }

    [AppBehavior("tests.rest.runtime-collision.lookup-one")]
    private sealed class LookupCollisionOneBehavior : IAppBehavior<LookupCollisionInput, LookupCollisionOutput>
    {
        public Task<LookupCollisionOutput> HandleAsync(
            LookupCollisionInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new LookupCollisionOutput(input.ItemId));
        }
    }

    [AppBehavior("tests.rest.runtime-collision.lookup-two")]
    private sealed class LookupCollisionTwoBehavior : IAppBehavior<LookupCollisionInput, LookupCollisionOutput>
    {
        public Task<LookupCollisionOutput> HandleAsync(
            LookupCollisionInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new LookupCollisionOutput(input.ItemId));
        }
    }

    [AppBehavior("tests.rest.manual-helper.get")]
    private sealed class GetManualHelperOrderBehavior : IAppBehavior<ManualHelperOrderInput, ManualHelperOrderOutput>
    {
        public Task<ManualHelperOrderOutput> HandleAsync(
            ManualHelperOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ManualHelperOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.rest.runtime-collision.dsl.get")]
    private sealed class GetDslCollisionOrderBehavior : IAppBehavior<LookupCollisionInput, LookupCollisionOutput>
    {
        public Task<LookupCollisionOutput> HandleAsync(
            LookupCollisionInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new LookupCollisionOutput(input.ItemId));
        }
    }

    [AppBehavior("tests.rest.profile.lookup")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 3)]
    private sealed class GetProfileRuntimeOrderBehavior : IAppBehavior<ProfileRuntimeOrderInput, ProfileRuntimeOrderOutput>
    {
        public Task<ProfileRuntimeOrderOutput> HandleAsync(
            ProfileRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProfileRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.rest.profile.override")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 2)]
    private sealed class GetProfileOverrideOrderBehavior : IAppBehavior<ProfileRuntimeOrderInput, ProfileRuntimeOrderOutput>
    {
        public Task<ProfileRuntimeOrderOutput> HandleAsync(
            ProfileRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProfileRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.rest.profile.documentpinned.lookup")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 4)]
    private sealed class GetProfileDocumentPinnedRuntimeOrderBehavior : IAppBehavior<ProfileRuntimeOrderInput, ProfileRuntimeOrderOutput>
    {
        public Task<ProfileRuntimeOrderOutput> HandleAsync(
            ProfileRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProfileRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.generated.runtime.lookup")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/orders/{orderId}", ApiVersionMajor = 4)]
    private sealed class GetGeneratedRuntimeOrderBehavior : IAppBehavior<GeneratedRuntimeOrderInput, GeneratedRuntimeOrderOutput>
    {
        public Task<GeneratedRuntimeOrderOutput> HandleAsync(
            GeneratedRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new GeneratedRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.generated.runtimeoverride.lookup")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/orders/{orderId}", ApiVersionMajor = 4)]
    private sealed class GetGeneratedVersionOverrideRuntimeOrderBehavior : IAppBehavior<GeneratedRuntimeOrderInput, GeneratedRuntimeOrderOutput>
    {
        public Task<GeneratedRuntimeOrderOutput> HandleAsync(
            GeneratedRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new GeneratedRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.dsl.runtimeoverride.disabled.lookup")]
    private sealed class GetExplicitDslHostGovernanceDisabledRuntimeOrderBehavior : IAppBehavior<GeneratedRuntimeOrderInput, GeneratedRuntimeOrderOutput>
    {
        public Task<GeneratedRuntimeOrderOutput> HandleAsync(
            GeneratedRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new GeneratedRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.dsl.runtimeoverride.enabled.lookup")]
    private sealed class GetExplicitDslHostGovernanceEnabledRuntimeOrderBehavior : IAppBehavior<GeneratedRuntimeOrderInput, GeneratedRuntimeOrderOutput>
    {
        public Task<GeneratedRuntimeOrderOutput> HandleAsync(
            GeneratedRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new GeneratedRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.dsl.runtimesuppression.enabled.lookup")]
    private sealed class GetExplicitDslHostGovernanceSuppressedRuntimeOrderBehavior : IAppBehavior<GeneratedRuntimeOrderInput, GeneratedRuntimeOrderOutput>
    {
        public Task<GeneratedRuntimeOrderOutput> HandleAsync(
            GeneratedRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new GeneratedRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.profile.runtimeoverride.capability")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 4)]
    private sealed class GetProfileCapabilityOverrideRuntimeOrderBehavior : IAppBehavior<GeneratedRuntimeOrderInput, GeneratedRuntimeOrderOutput>
    {
        public Task<GeneratedRuntimeOrderOutput> HandleAsync(
            GeneratedRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new GeneratedRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.profile.runtimeclear.capability")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 4)]
    private sealed class GetProfileCapabilityClearRuntimeOrderBehavior : IAppBehavior<GeneratedRuntimeOrderInput, GeneratedRuntimeOrderOutput>
    {
        public Task<GeneratedRuntimeOrderOutput> HandleAsync(
            GeneratedRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new GeneratedRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.profile.runtimeclear.noop.capability")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 4)]
    private sealed class GetProfileCapabilityClearNoOpRuntimeOrderBehavior : IAppBehavior<GeneratedRuntimeOrderInput, GeneratedRuntimeOrderOutput>
    {
        public Task<GeneratedRuntimeOrderOutput> HandleAsync(
            GeneratedRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new GeneratedRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.profile.runtimenoop.metadata")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 4)]
    private sealed class GetProfileMetadataRewriteNoOpRuntimeOrderBehavior : IAppBehavior<GeneratedRuntimeOrderInput, GeneratedRuntimeOrderOutput>
    {
        public Task<GeneratedRuntimeOrderOutput> HandleAsync(
            GeneratedRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new GeneratedRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.profile.runtimenoop.clear.metadata")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 4)]
    private sealed class GetProfileMetadataClearNoOpRuntimeOrderBehavior : IAppBehavior<GeneratedRuntimeOrderInput, GeneratedRuntimeOrderOutput>
    {
        public Task<GeneratedRuntimeOrderOutput> HandleAsync(
            GeneratedRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new GeneratedRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.generated.runtimeexplicitoverride.lookup")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/orders/{orderId}", ApiVersionMajor = 4)]
    private sealed class GetGeneratedExplicitOverrideRuntimeOrderBehavior : IAppBehavior<GeneratedRuntimeOrderInput, GeneratedRuntimeOrderOutput>
    {
        public Task<GeneratedRuntimeOrderOutput> HandleAsync(
            GeneratedRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new GeneratedRuntimeOrderOutput(input.OrderId));
        }
    }

    private sealed record RuntimeCatalogTestEndpointSummaryMetadata(string Summary) : IEndpointSummaryMetadata;

    private sealed record RuntimeCatalogTestEndpointDescriptionMetadata(string Description) : IEndpointDescriptionMetadata;

    [AppBehavior("tests.generated.runtime.create")]
    [BehaviorRestProfile(BehaviorRestMethod.Post, "/orders/{orderId}/items", ApiVersionMajor = 4)]
    private sealed class CreateGeneratedRuntimeOrderItemBehavior : IAppBehavior<GeneratedRuntimeOrderItemInput, GeneratedRuntimeOrderOutput>
    {
        public Task<GeneratedRuntimeOrderOutput> HandleAsync(
            GeneratedRuntimeOrderItemInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new GeneratedRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.generated.runtimegrouped.orders.lookup")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 7)]
    private sealed class GetGeneratedGroupedRuntimeOrderBehavior : IAppBehavior<GeneratedRuntimeOrderInput, GeneratedRuntimeOrderOutput>
    {
        public Task<GeneratedRuntimeOrderOutput> HandleAsync(
            GeneratedRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new GeneratedRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.generated.runtimegrouped.orders.create")]
    [BehaviorRestProfile(BehaviorRestMethod.Post, "/{orderId}/items", ApiVersionMajor = 7)]
    private sealed class CreateGeneratedGroupedRuntimeOrderItemBehavior : IAppBehavior<GeneratedRuntimeOrderItemInput, GeneratedRuntimeOrderOutput>
    {
        public Task<GeneratedRuntimeOrderOutput> HandleAsync(
            GeneratedRuntimeOrderItemInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new GeneratedRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.generated.runtimegrouped.inventory.lookup")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 8)]
    private sealed class GetGeneratedGroupedRuntimeInventoryBehavior : IAppBehavior<GeneratedRuntimeOrderInput, GeneratedRuntimeOrderOutput>
    {
        public Task<GeneratedRuntimeOrderOutput> HandleAsync(
            GeneratedRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new GeneratedRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.rest.generated.threeway.lookup")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 6)]
    private sealed class GetGeneratedThreeWayRuntimeOrderBehavior : IAppBehavior<GeneratedRuntimeOrderInput, GeneratedRuntimeOrderOutput>
    {
        public Task<GeneratedRuntimeOrderOutput> HandleAsync(
            GeneratedRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new GeneratedRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.rest.profile.metadata-only")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 4)]
    private sealed class GetProfileMetadataOnlyOrderBehavior : IAppBehavior<ProfileRuntimeOrderInput, ProfileRuntimeOrderOutput>
    {
        public Task<ProfileRuntimeOrderOutput> HandleAsync(
            ProfileRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProfileRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.rest.profile.split.summary")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/orders/{orderId}", ApiVersionMajor = 4)]
    private sealed class GetSplitProfileRuntimeOrderBehavior : IAppBehavior<ProfileRuntimeOrderInput, ProfileRuntimeOrderOutput>
    {
        public Task<ProfileRuntimeOrderOutput> HandleAsync(
            ProfileRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProfileRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.rest.profile.split.details")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/orders/{orderId}/details", ApiVersionMajor = 4)]
    private sealed class GetSplitProfileRuntimeOrderDetailsBehavior : IAppBehavior<ProfileRuntimeOrderInput, ProfileRuntimeOrderOutput>
    {
        public Task<ProfileRuntimeOrderOutput> HandleAsync(
            ProfileRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProfileRuntimeOrderOutput($"{input.OrderId}-details"));
        }
    }

    [AppBehavior("tests.rest.profile.suppression")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 6)]
    private sealed class GetProfileSuppressionOrderBehavior : IAppBehavior<ProfileRuntimeOrderInput, ProfileRuntimeOrderOutput>
    {
        public Task<ProfileRuntimeOrderOutput> HandleAsync(
            ProfileRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProfileRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.rest.profile.bindings")]
    [BehaviorRestProfile(BehaviorRestMethod.Post, "/{orderId}", ApiVersionMajor = 6)]
    [BehaviorRestBinding(nameof(ProfileBindingRuntimeInput.OrderId), BehaviorRestBindingSource.Route, Name = "orderId")]
    [BehaviorRestBinding(nameof(ProfileBindingRuntimeInput.Quantity), BehaviorRestBindingSource.Query, Name = "quantity")]
    [BehaviorRestBinding(nameof(ProfileBindingRuntimeInput.CorrelationId), BehaviorRestBindingSource.Header, Name = "X-Correlation-Id")]
    [BehaviorRestBinding(nameof(ProfileBindingRuntimeInput.Note), BehaviorRestBindingSource.Body, Name = "note")]
    private sealed class PostProfileBindingRuntimeOrderBehavior : IAppBehavior<ProfileBindingRuntimeInput, ProfileBindingRuntimeOutput>
    {
        public Task<ProfileBindingRuntimeOutput> HandleAsync(
            ProfileBindingRuntimeInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProfileBindingRuntimeOutput(
                input.OrderId,
                input.Quantity,
                input.CorrelationId,
                input.Note,
                input.Ignored));
        }
    }

    [AppBehavior("tests.rest.profile.bindings.inference")]
    [BehaviorRestProfile(BehaviorRestMethod.Post, "/{orderId}", ApiVersionMajor = 6)]
    [BehaviorRestBinding(nameof(ProfileBindingInferenceRuntimeInput.Quantity), BehaviorRestBindingSource.Query, Name = "quantity")]
    [BehaviorRestBinding(nameof(ProfileBindingInferenceRuntimeInput.CorrelationId), BehaviorRestBindingSource.Header, Name = "X-Correlation-Id")]
    [BehaviorRestBinding(nameof(ProfileBindingInferenceRuntimeInput.Note), BehaviorRestBindingSource.Body, Name = "note")]
    private sealed class PostProfileBindingInferenceRuntimeOrderBehavior : IAppBehavior<ProfileBindingInferenceRuntimeInput, ProfileBindingInferenceRuntimeOutput>
    {
        public Task<ProfileBindingInferenceRuntimeOutput> HandleAsync(
            ProfileBindingInferenceRuntimeInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProfileBindingInferenceRuntimeOutput(
                input.OrderId,
                input.Quantity,
                input.CorrelationId,
                input.Note));
        }
    }

    [AppBehavior("tests.rest.profile.bindings.get")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 6)]
    [BehaviorRestBinding(nameof(ProfileBindingGetRuntimeInput.OrderId), BehaviorRestBindingSource.Route, Name = "orderId")]
    private sealed class GetProfileBindingRuntimeOrderBehavior : IAppBehavior<ProfileBindingGetRuntimeInput, ProfileBindingGetRuntimeOutput>
    {
        public Task<ProfileBindingGetRuntimeOutput> HandleAsync(
            ProfileBindingGetRuntimeInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProfileBindingGetRuntimeOutput(
                input.OrderId,
                input.Ignored));
        }
    }

    [AppBehavior("tests.rest.profile.bindings.alias")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{id}", ApiVersionMajor = 6)]
    [BehaviorRestBinding(nameof(ProfileBindingAliasRuntimeInput.OrderId), BehaviorRestBindingSource.Route, Name = "id")]
    private sealed class GetProfileBindingAliasRuntimeOrderBehavior : IAppBehavior<ProfileBindingAliasRuntimeInput, ProfileRuntimeOrderOutput>
    {
        public Task<ProfileRuntimeOrderOutput> HandleAsync(
            ProfileBindingAliasRuntimeInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProfileRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.rest.profile.bindings.query")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/lookup", ApiVersionMajor = 6)]
    private sealed class GetProfileBindingQueryFallbackRuntimeOrderBehavior : IAppBehavior<ProfileBindingGetRuntimeInput, ProfileBindingGetRuntimeOutput>
    {
        public Task<ProfileBindingGetRuntimeOutput> HandleAsync(
            ProfileBindingGetRuntimeInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProfileBindingGetRuntimeOutput(
                input.OrderId,
                input.Ignored));
        }
    }

    [AppBehavior("tests.rest.profile.bindings.query.partial")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/lookup", ApiVersionMajor = 6)]
    private sealed class GetProfileBindingPartialQueryFallbackRuntimeOrderBehavior : IAppBehavior<ProfileBindingQueryFallbackPartialRuntimeInput, ProfileBindingQueryFallbackPartialRuntimeOutput>
    {
        public Task<ProfileBindingQueryFallbackPartialRuntimeOutput> HandleAsync(
            ProfileBindingQueryFallbackPartialRuntimeInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProfileBindingQueryFallbackPartialRuntimeOutput(
                input.OrderId,
                input.Quantity));
        }
    }

    [AppBehavior("tests.rest.profile.bindings.query.explicit")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/lookup/{orderId}", ApiVersionMajor = 6, PreserveImplicitQueryFallback = true)]
    [BehaviorRestBinding(nameof(ProfileBindingQueryFallbackPartialRuntimeInput.OrderId), BehaviorRestBindingSource.Route, Name = "orderId")]
    private sealed class GetProfileBindingExplicitQueryFallbackRuntimeOrderBehavior : IAppBehavior<ProfileBindingQueryFallbackPartialRuntimeInput, ProfileBindingQueryFallbackPartialRuntimeOutput>
    {
        public Task<ProfileBindingQueryFallbackPartialRuntimeOutput> HandleAsync(
            ProfileBindingQueryFallbackPartialRuntimeInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProfileBindingQueryFallbackPartialRuntimeOutput(
                input.OrderId,
                input.Quantity));
        }
    }

    [AppBehavior("tests.rest.profile.bindings.query.explicit.withdraw")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/lookup/{orderId}", ApiVersionMajor = 6)]
    [BehaviorRestBinding(nameof(ProfileBindingQueryFallbackPartialRuntimeInput.OrderId), BehaviorRestBindingSource.Route, Name = "orderId")]
    [BehaviorRestBinding(nameof(ProfileBindingQueryFallbackPartialRuntimeInput.Quantity), BehaviorRestBindingSource.Query, Name = "quantity")]
    private sealed class GetProfileBindingExplicitQueryWithdrawalRuntimeOrderBehavior : IAppBehavior<ProfileBindingQueryFallbackPartialRuntimeInput, ProfileBindingQueryFallbackPartialRuntimeOutput>
    {
        public Task<ProfileBindingQueryFallbackPartialRuntimeOutput> HandleAsync(
            ProfileBindingQueryFallbackPartialRuntimeInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProfileBindingQueryFallbackPartialRuntimeOutput(
                input.OrderId,
                input.Quantity));
        }
    }

    [AppBehavior("tests.rest.profile.bindings.query.explicit.withdraw.preserved")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/lookup/{orderId}", ApiVersionMajor = 6, PreserveImplicitQueryFallback = true)]
    [BehaviorRestBinding(nameof(ProfileBindingQueryFallbackPartialRuntimeInput.OrderId), BehaviorRestBindingSource.Route, Name = "orderId")]
    [BehaviorRestBinding(nameof(ProfileBindingQueryFallbackPartialRuntimeInput.Quantity), BehaviorRestBindingSource.Query, Name = "quantity")]
    private sealed class GetProfileBindingExplicitQueryWithdrawalPreservedRuntimeOrderBehavior : IAppBehavior<ProfileBindingQueryFallbackPartialRuntimeInput, ProfileBindingQueryFallbackPartialRuntimeOutput>
    {
        public Task<ProfileBindingQueryFallbackPartialRuntimeOutput> HandleAsync(
            ProfileBindingQueryFallbackPartialRuntimeInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProfileBindingQueryFallbackPartialRuntimeOutput(
                input.OrderId,
                input.Quantity));
        }
    }

    [AppBehavior("tests.rest.profile.selector.bindings")]
    [BehaviorRestProfile(BehaviorRestMethod.Post, "/{orderId}/items", ApiVersionMajor = 6)]
    [BehaviorRestBinding(nameof(ProfileBindingRuntimeInput.OrderId), BehaviorRestBindingSource.Route, Name = "orderId")]
    [BehaviorRestBinding(nameof(ProfileBindingRuntimeInput.Quantity), BehaviorRestBindingSource.Query, Name = "quantity")]
    [BehaviorRestBinding(nameof(ProfileBindingRuntimeInput.CorrelationId), BehaviorRestBindingSource.Header, Name = "X-Correlation-Id")]
    [BehaviorRestBinding(nameof(ProfileBindingRuntimeInput.Note), BehaviorRestBindingSource.Body, Name = "note")]
    private sealed class PostProfileSelectorRuntimeOrderBehavior : IAppBehavior<ProfileBindingRuntimeInput, ProfileBindingRuntimeOutput>
    {
        public Task<ProfileBindingRuntimeOutput> HandleAsync(
            ProfileBindingRuntimeInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new ProfileBindingRuntimeOutput(
                input.OrderId,
                input.Quantity,
                input.CorrelationId,
                input.Note,
                input.Ignored));
        }
    }

    private sealed record GetCatalogCartInput(string CartId);

    private sealed record GetCatalogCartOutput(string CartId);

    private sealed record AddCatalogCartItemInput(string CartId, string ProductId);

    private sealed record AddCatalogCartItemOutput(string CartId, string ProductId);

    private sealed record LookupCollisionInput(string ItemId);

    private sealed record LookupCollisionOutput(string ItemId);

    private sealed record ManualRuntimeOrderOutput(string OrderId);

    private sealed record ManualHelperOrderInput(string OrderId);

    private sealed record ManualHelperOrderOutput(string OrderId);

    private sealed record ManualCollisionOutput(string ItemId);

    private sealed record ProfileRuntimeOrderInput(string OrderId);

    private sealed record ProfileRuntimeOrderOutput(string OrderId);

    private sealed record GeneratedRuntimeOrderInput(string OrderId);

    private sealed record GeneratedRuntimeOrderItemInput(string OrderId, string? ProductId);

    private sealed record GeneratedRuntimeOrderOutput(string OrderId);

    private sealed record ProfileBindingRuntimeInput(
        string OrderId,
        int Quantity,
        string? CorrelationId,
        string? Note,
        string? Ignored = null);

    private sealed record ProfileBindingRuntimeOutput(
        string OrderId,
        int Quantity,
        string? CorrelationId,
        string? Note,
        string? Ignored);

    private sealed record ProfileBindingInferenceRuntimeInput(
        string OrderId,
        int Quantity,
        string? CorrelationId,
        string? Note);

    private sealed record ProfileBindingInferenceRuntimeOutput(
        string OrderId,
        int Quantity,
        string? CorrelationId,
        string? Note);

    private sealed record ProfileBindingGetRuntimeInput(
        string OrderId,
        string? Ignored = null);

    private sealed record ProfileBindingGetRuntimeOutput(
        string OrderId,
        string? Ignored);

    private sealed record ProfileBindingAliasRuntimeInput(string OrderId);

    private sealed record ProfileBindingQueryFallbackPartialRuntimeInput(
        string OrderId,
        int Quantity);

    private sealed record ProfileBindingQueryFallbackPartialRuntimeOutput(
        string OrderId,
        int Quantity);
}
