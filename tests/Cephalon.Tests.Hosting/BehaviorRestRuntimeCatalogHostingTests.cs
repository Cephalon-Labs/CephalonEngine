using System.Net.Http.Json;
using System.Text.Json;
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
    public async Task MapCephalonAppliesExplicitProfileBindingsAndExposesThemInRuntimeMetadata()
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
        Assert.True(endpoint.Metadata.TryGetValue("bindingDescriptors", out var bindingDescriptorJson));

        var bindings = JsonSerializer.Deserialize<BehaviorRestBindingDescriptor[]>(bindingDescriptorJson!);
        Assert.NotNull(bindings);
        Assert.Equal(4, bindings.Length);
        Assert.Contains(bindings, static binding =>
            binding.PropertyName == "OrderId" &&
            binding.Source == BehaviorRestBindingSource.Route &&
            binding.Name == "orderId");
        Assert.Contains(bindings, static binding =>
            binding.PropertyName == "Quantity" &&
            binding.Source == BehaviorRestBindingSource.Query &&
            binding.Name == "quantity");
        Assert.Contains(bindings, static binding =>
            binding.PropertyName == "CorrelationId" &&
            binding.Source == BehaviorRestBindingSource.Header &&
            binding.Name == "X-Correlation-Id");
        Assert.Contains(bindings, static binding =>
            binding.PropertyName == "Note" &&
            binding.Source == BehaviorRestBindingSource.Body &&
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
