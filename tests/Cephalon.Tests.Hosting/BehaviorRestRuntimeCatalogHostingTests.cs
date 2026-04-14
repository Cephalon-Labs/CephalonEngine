using System.Net.Http.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Modules;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;

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
        Assert.Equal("/api/v2/tests/runtime-catalog/cart", getEndpoint.Metadata["routeGroupPrefix"]);
        Assert.Equal("/{cartId}", getEndpoint.Metadata["relativePattern"]);
        Assert.Contains("Runtime Catalog API", getEndpoint.Tags);
        Assert.Equal("v2", getEndpoint.OpenApiDocumentName);
        Assert.Equal(2, getEndpoint.ApiVersionMajor);
        Assert.Equal("GET", getEndpoint.Metadata["method"]);
        Assert.Contains("GetCatalogCartBehavior", getEndpoint.Metadata["behaviorType"], StringComparison.Ordinal);
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
        Assert.Equal("minimal-api", manualEndpoint.Metadata["authoringStyle"]);
        Assert.Equal("tests.rest.manual-runtime:GET:/api/tests/manual-runtime/orders/{orderId}", manualEndpoint.Metadata["sourceId"]);
        Assert.Empty(manualEndpoint.BindingDescriptors);

        var behaviorHelperEndpoint = Assert.Single(endpoints, static endpoint =>
            string.Equals(endpoint.BehaviorId, "tests.rest.manual-helper.get", StringComparison.Ordinal));
        Assert.Equal("manual", behaviorHelperEndpoint.SourceKind);
        Assert.Equal("GET", behaviorHelperEndpoint.Method);
        Assert.Equal("/api/v3/tests/manual-helper/orders/{orderId}", behaviorHelperEndpoint.RoutePattern);
        Assert.Equal("v3", behaviorHelperEndpoint.OpenApiDocumentName);
        Assert.Equal(3, behaviorHelperEndpoint.ApiVersionMajor);
        Assert.Contains("Manual Helper API", behaviorHelperEndpoint.Tags);
        Assert.Equal("/api/v3/tests/manual-helper/orders", behaviorHelperEndpoint.Metadata["routeGroupPrefix"]);
        Assert.Equal("/{orderId}", behaviorHelperEndpoint.Metadata["relativePattern"]);
        Assert.Equal("behavior-helper", behaviorHelperEndpoint.Metadata["authoringStyle"]);
        Assert.Contains("GetManualHelperOrderBehavior", behaviorHelperEndpoint.Metadata["behaviorType"], StringComparison.Ordinal);
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

        Assert.NotNull(endpoints);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.rest.profile.lookup", StringComparison.Ordinal));
        Assert.Equal("module-dsl", endpoint.SourceKind);
        Assert.Equal("/api/v3/tests/profile-runtime/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal("v3", endpoint.OpenApiDocumentName);
        Assert.Equal(3, endpoint.ApiVersionMajor);
        Assert.Contains("Profile Runtime API", endpoint.Tags);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, endpoint.Metadata["authoringStyle"]);
        Assert.Equal("/api/v3/tests/profile-runtime/orders", endpoint.Metadata["routeGroupPrefix"]);
        Assert.Equal("/{orderId}", endpoint.Metadata["relativePattern"]);
        Assert.Empty(endpoint.BindingDescriptors);

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
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, endpoint.Metadata["authoringStyle"]);
        Assert.Equal("/api/v4/tests/generated/runtime", endpoint.Metadata["routeGroupPrefix"]);
        Assert.Equal("/orders/{orderId}", endpoint.Metadata["relativePattern"]);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtime.lookup", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, candidate.AuthoringStyle);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);

        Assert.Contains(snapshot.RestEndpoints, item =>
            string.Equals(item.Id, endpoint.Id, StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            item.Status == RestEndpointCandidateStatus.Published);

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v4/tests/generated/runtime/orders/ord-42");
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
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
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle, endpoint.Metadata["authoringStyle"]);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-v6", candidate.AppliedOverrideId);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);

        var rule = Assert.Single(overrides);
        Assert.Equal("prefer-v6", rule.Id);
        Assert.Equal(6, rule.ApiVersionMajor);
        Assert.Contains("tests.generated.runtimeoverride.lookup", rule.BehaviorIds);

        Assert.Contains(snapshot.RestEndpointOverrides, item =>
            string.Equals(item.Id, "prefer-v6", StringComparison.Ordinal));
        Assert.Contains(snapshot.RestEndpointCandidates, item =>
            string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) &&
            string.Equals(item.AppliedOverrideId, "prefer-v6", StringComparison.Ordinal));

        var payload = await client.GetFromJsonAsync<GeneratedRuntimeOrderOutput>("/api/v6/tests/generated/runtime/override/orders/ord-42");
        Assert.NotNull(payload);
        Assert.Equal("ord-42", payload.OrderId);
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
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

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
        Assert.Equal("/lookup/{orderId}", endpoint.Metadata["relativePattern"]);
        Assert.Equal("v4", endpoint.OpenApiDocumentName);
        Assert.Equal(4, endpoint.ApiVersionMajor);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimeoverride.lookup", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-lookup-path", candidate.AppliedOverrideId);
        Assert.Equal("/lookup/{orderId}", candidate.ProjectedEndpoint.Metadata["relativePattern"]);
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
        builder.Configuration["OpenApi:EnabledVersions:0"] = "8";
        builder.Configuration["OpenApi:DefaultVersion"] = "8";
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
        Assert.Equal("/lookup/{orderId}", endpoint.Metadata["relativePattern"]);
        Assert.Equal(8, endpoint.ApiVersionMajor);

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.generated.runtimeexplicitoverride.lookup", StringComparison.Ordinal));
        Assert.Equal("prefer-lookup-v6", candidate.AppliedOverrideId);
        Assert.Equal("/lookup/{orderId}", candidate.ProjectedEndpoint.Metadata["relativePattern"]);
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
    public void AddCephalonRejectsRestApiSuppressionRulesWithoutBehaviorOrModuleTargets()
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

        Assert.Contains("behavior id or source module id", exception.Message, StringComparison.OrdinalIgnoreCase);
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
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:0:Source"] = "Query";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:0:Name"] = "orderId";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:1:PropertyName"] = "Quantity";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:1:Source"] = "Query";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:1:Name"] = "quantity";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:2:PropertyName"] = "CorrelationId";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:2:Source"] = "Header";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:2:Name"] = "X-Correlation-Id";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:3:PropertyName"] = "Note";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:3:Source"] = "Body";
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

        var candidateById = await client.GetFromJsonAsync<RestEndpointCandidateRuntimeDescriptor>(
            $"/engine/rest-endpoint-candidates/{suppressed.Id}");
        Assert.NotNull(candidateById);
        Assert.Equal(suppressed.Id, candidateById.Id);

        var snapshotPublished = Assert.Single(snapshot.RestEndpointCandidates, candidate =>
            string.Equals(candidate.Id, published.Id, StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, snapshotPublished.Status);

        var snapshotSuppressed = Assert.Single(snapshot.RestEndpointCandidates, candidate =>
            string.Equals(candidate.Id, suppressed.Id, StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Suppressed, snapshotSuppressed.Status);
        Assert.Equal(published.Id, snapshotSuppressed.SuppressedByCandidateId);

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

        Assert.NotNull(endpoints);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders/{orderId}", endpoint.RoutePattern);
        Assert.Equal(RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle, endpoint.Metadata["authoringStyle"]);
        Assert.False(endpoint.Metadata.ContainsKey("bindingDescriptors"));
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
        Assert.NotNull(snapshot);

        var snapshotEndpoint = Assert.Single(snapshot.RestEndpoints, static candidate =>
            string.Equals(candidate.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.False(snapshotEndpoint.Metadata.ContainsKey("bindingDescriptors"));
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
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:0:Source"] = "Query";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:0:Name"] = "qty";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:1:PropertyName"] = "CorrelationId";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:1:Source"] = "Header";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:1:Name"] = "X-Trace-Id";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:2:PropertyName"] = "Note";
        builder.Configuration["RestApi:Overrides:prefer-short-bindings:Bindings:2:Source"] = "Body";
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
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:0:Source"] = "Route";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:0:Name"] = "id";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:1:PropertyName"] = "Quantity";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:1:Source"] = "Query";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:1:Name"] = "quantity";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:2:PropertyName"] = "CorrelationId";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:2:Source"] = "Header";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:2:Name"] = "X-Correlation-Id";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:3:PropertyName"] = "Note";
        builder.Configuration["RestApi:Overrides:prefer-renamed-placeholder:Bindings:3:Source"] = "Body";
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
        Assert.Equal(4, endpoint.BindingDescriptors.Count);
        Assert.Contains(endpoint.BindingDescriptors, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == RestEndpointBindingSource.Route &&
            binding.Name == "id");

        var candidate = Assert.Single(candidates, static item =>
            string.Equals(item.ProjectedEndpoint.BehaviorId, "tests.rest.profile.bindings", StringComparison.Ordinal));
        Assert.Equal(RestEndpointCandidateStatus.Published, candidate.Status);
        Assert.Equal("prefer-renamed-placeholder", candidate.AppliedOverrideId);
        Assert.Equal(endpoint.Id, candidate.ProjectedEndpoint.Id);
        Assert.Equal("/api/v6/tests/profile-runtime/bindings/orders/lookup/{id}", candidate.ProjectedEndpoint.RoutePattern);

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
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:0:Source"] = "Query";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:0:Name"] = "orderId";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:1:PropertyName"] = "Quantity";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:1:Source"] = "Query";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:1:Name"] = "quantity";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:2:PropertyName"] = "CorrelationId";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:2:Source"] = "Header";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:2:Name"] = "X-Correlation-Id";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:3:PropertyName"] = "Note";
        builder.Configuration["RestApi:Overrides:prefer-query-identity:Bindings:3:Source"] = "Body";
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
                .WithTagName("Generated Governance API");

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
}
