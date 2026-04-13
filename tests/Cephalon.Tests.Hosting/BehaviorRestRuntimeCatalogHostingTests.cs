using System.Net.Http.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Modules;
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
}
