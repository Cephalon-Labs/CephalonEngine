using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Http.Bindings;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Behaviors.Http.Registry;
using Cephalon.Behaviors.Services;
using Cephalon.AspNetCore.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Behaviors.Http;

/// <summary>
/// Integration and unit tests for the ABT M2 HTTP Transport Pack.
/// Covers all 7 transport bindings, the lazy-init wrapper, the registry, and DI wiring.
/// </summary>
public sealed class HttpBehaviorBindingTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Test fixtures
    // ─────────────────────────────────────────────────────────────────────────

    [AppBehavior("echo.direct")]
    private sealed class EchoBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult($"echo:{input}");
    }

    /// <summary>
    /// Object-in / object-out behavior used for HTTP binding tests where the input
    /// arrives as a deserialized <see cref="System.Text.Json.JsonElement" />.
    /// </summary>
    [AppBehavior("object.echo")]
    private sealed class ObjectEchoBehavior : IAppBehavior<object, object>
    {
        public Task<object> HandleAsync(object input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(input);
    }

    [AppBehavior("rest.helper.echo")]
    private sealed class RestHelperEchoBehavior : IAppBehavior<RestHelperEchoInput, RestHelperEchoOutput>
    {
        public Task<RestHelperEchoOutput> HandleAsync(
            RestHelperEchoInput input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RestHelperEchoOutput(
                input.CartId,
                input.ProductName,
                input.Quantity,
                context.EventStore is not null));
        }
    }

    private sealed record RestHelperEchoInput(string CartId, string ProductName, int Quantity);

    private sealed record RestHelperEchoOutput(
        string CartId,
        string ProductName,
        int Quantity,
        bool HasEventStore);

    private sealed class RestHelperModule : ModuleBase, IEndpointModule
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.cart",
            displayName: "Test Cart",
            description: "Test module for behavior-aware REST endpoint helpers.",
            version: "2.4.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapBehaviorRestGroup(this, "/tests/cart");
            group.MapBehaviorPost<RestHelperEchoBehavior>("/{cartId}/items");
        }
    }

    private sealed class VersionedRestHelperModule : ModuleBase, IEndpointModule
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.cart",
            displayName: "Test Cart",
            description: "Test module for behavior-aware REST endpoint helpers.",
            version: "2.4.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapBehaviorRestGroup(this, "/tests/cart/versioned")
                .ApiVersion(1);
            group.MapBehaviorPost<RestHelperEchoBehavior>("/{cartId}/items");
        }
    }

    private sealed class StubEventStore : IEventStore
    {
        public Task AppendAsync(
            string streamId,
            IReadOnlyCollection<IDomainEvent> events,
            long expectedVersion,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<long> GetVersionAsync(string streamId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(-1L);
        }

        public async IAsyncEnumerable<IDomainEvent> ReadStreamAsync(
            string streamId,
            long fromVersion = 0,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper: build a minimal WebApplication with specified bindings mapped
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task<(WebApplication App, HttpClient Client)> BuildAppAsync(
        BehaviorTopologyDescriptor descriptor,
        params IHttpBehaviorBinding[] bindings)
        => await BuildAppAsync<ObjectEchoBehavior>(descriptor, bindings);

    private static async Task<(WebApplication App, HttpClient Client)> BuildAppAsync<TBehavior>(
        BehaviorTopologyDescriptor descriptor,
        IHttpBehaviorBinding[] bindings)
        where TBehavior : class
    {
        var services = new ServiceCollection();
        services.AddTransient<TBehavior>();

        var typeRegistry = new BehaviorTypeRegistry();
        typeRegistry.Register(descriptor.Id, typeof(TBehavior));

        var contributor = new FluentBehaviorContributor(descriptor);
        services.AddSingleton<IBehaviorContributor>(contributor);
        services.AddSingleton<IBehaviorTypeRegistry>(typeRegistry);
        services.AddSingleton<IBehaviorCatalog>(sp =>
            new BehaviorCatalog(sp.GetServices<IBehaviorContributor>()));

        var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IBehaviorCatalog>();
        var dispatcher = new BehaviorDispatcher(catalog, typeRegistry, provider);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();

        foreach (var binding in bindings)
        {
            await binding.MapAsync(app, descriptor, dispatcher);
        }

        await app.StartAsync();
        return (app, app.GetTestClient());
    }

    private static async Task<(WebApplication App, HttpClient Client)> BuildBehaviorRestHelperAppAsync()
        => await BuildBehaviorRestHelperAppAsync(new RestHelperModule());

    private static async Task<(WebApplication App, HttpClient Client)> BuildBehaviorRestHelperAppAsync(IEndpointModule module)
    {
        var descriptor = new BehaviorTopologyDescriptor("rest.helper.echo", "direct", ["http.rest"]);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddTransient<RestHelperEchoBehavior>();
        builder.Services.AddSingleton<IEventStore, StubEventStore>();

        var typeRegistry = new BehaviorTypeRegistry();
        typeRegistry.Register(descriptor.Id, typeof(RestHelperEchoBehavior));
        builder.Services.AddSingleton<IBehaviorContributor>(new FluentBehaviorContributor(descriptor));
        builder.Services.AddSingleton<IBehaviorTypeRegistry>(typeRegistry);
        builder.Services.AddSingleton<IBehaviorCatalog>(sp =>
            new BehaviorCatalog(sp.GetServices<IBehaviorContributor>()));
        builder.Services.AddSingleton<BehaviorDispatcher>();

        var app = builder.Build();
        module.MapEndpoints(app);

        await app.StartAsync();
        return (app, app.GetTestClient());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. RestBindingMapsPostRoute
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RestBindingMapsPostRoute()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.rest"]);
        var (app, client) = await BuildAppAsync(descriptor, new RestHttpBehaviorBinding());

        var response = await client.PostAsJsonAsync("/api/v1/object/echo", "World");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent,
            $"Expected 200/204 but got {response.StatusCode}");

        await app.StopAsync();
    }

    [Fact]
    public async Task RestBindingMapsGetRoute()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.rest"]);
        var (app, client) = await BuildAppAsync(descriptor, new RestHttpBehaviorBinding());

        var response = await client.GetAsync("/api/v1/object/echo?q=hello");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent,
            $"Expected 200/204 but got {response.StatusCode}");

        await app.StopAsync();
    }

    [Fact]
    public async Task RestBindingMapsCanonicalApiSurfaceRoute()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.rest"]);
        var (app, client) = await BuildAppAsync(descriptor, new RestHttpBehaviorBinding());

        var response = await client.GetAsync("/api/v1/object/echo?q=hello");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent,
            $"Expected 200/204 but got {response.StatusCode}");

        await app.StopAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. JsonRpcBindingParsesRequestReturnsResult
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task JsonRpcBindingParsesRequestReturnsResult()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.jsonrpc"]);
        var (app, client) = await BuildAppAsync(descriptor, new JsonRpcHttpBehaviorBinding());

        var requestBody = new
        {
            jsonrpc = "2.0",
            method = "handle",
            @params = "test-input",
            id = 42
        };

        var response = await client.PostAsJsonAsync("/json-rpc/v1/object/echo", requestBody);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2.0", json.GetProperty("jsonrpc").GetString());
        Assert.Equal(42, json.GetProperty("id").GetInt32());

        await app.StopAsync();
    }

    [Fact]
    public async Task JsonRpcBindingReturnsErrorForUnknownMethod()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.jsonrpc"]);
        var (app, client) = await BuildAppAsync(descriptor, new JsonRpcHttpBehaviorBinding());

        var requestBody = new { jsonrpc = "2.0", method = "unknown", id = 1 };
        var response = await client.PostAsJsonAsync("/json-rpc/v1/object/echo", requestBody);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("error", out var error));
        Assert.Equal(-32601, error.GetProperty("code").GetInt32());

        await app.StopAsync();
    }

    [Fact]
    public async Task JsonRpcBindingMapsCanonicalApiSurfaceRoute()
    {
        var descriptor = new BehaviorTopologyDescriptor(
            "object.echo",
            "direct",
            ["http.jsonrpc"],
            apiSurface: new BehaviorApiSurfaceDescriptor("catalog/items", "lookup"));
        var (app, client) = await BuildAppAsync(descriptor, new JsonRpcHttpBehaviorBinding());

        var requestBody = new
        {
            jsonrpc = "2.0",
            method = "handle",
            @params = "test-input",
            id = 7
        };

        var response = await client.PostAsJsonAsync("/json-rpc/v1/catalog/items/lookup", requestBody);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await app.StopAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. GraphqlBindingParsesQueryReturnsData
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GraphqlBindingParsesQueryReturnsData()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.graphql"]);
        var (app, client) = await BuildAppAsync(descriptor, new GraphqlHttpBehaviorBinding());

        var requestBody = new { query = "{ echo }", variables = (object?)null };
        var response = await client.PostAsJsonAsync("/graphql/v1/object/echo", requestBody);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(
            json.TryGetProperty("data", out _) || json.TryGetProperty("errors", out _),
            "Response should contain 'data' or 'errors'");

        await app.StopAsync();
    }

    [Fact]
    public async Task GraphqlBindingMapsCanonicalApiSurfaceRoute()
    {
        var descriptor = new BehaviorTopologyDescriptor(
            "object.echo",
            "direct",
            ["http.graphql"],
            apiSurface: new BehaviorApiSurfaceDescriptor("catalog/items", "lookup"));
        var (app, client) = await BuildAppAsync(descriptor, new GraphqlHttpBehaviorBinding());

        var requestBody = new { query = "{ echo }", variables = (object?)null };
        var response = await client.PostAsJsonAsync("/graphql/v1/catalog/items/lookup", requestBody);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await app.StopAsync();
    }

    [Fact]
    public async Task GraphqlSseBindingMapsCanonicalApiSurfaceRoute()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.graphql-sse"]);
        var (app, client) = await BuildAppAsync(descriptor, new GraphqlSseBehaviorBinding());

        var response = await client.PostAsJsonAsync("/graphql-sse/v1/object/echo", new { query = "{ echo }" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await app.StopAsync();
    }

    [Fact]
    public async Task GraphqlWsBindingMapsCanonicalApiSurfaceRoute()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.graphql-ws"]);
        var (app, client) = await BuildAppAsync(descriptor, new GraphqlWsBehaviorBinding());

        var response = await client.GetAsync("/graphql-ws/v1/object/echo");

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await app.StopAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. LazyBindingCallsMapAsyncOnlyOnce
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LazyBindingCallsMapAsyncOnlyOnce()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.rest"]);
        var mapCallCount = 0;

        var trackingBinding = new TrackingBinding("http.test", (_, _, _) =>
        {
            mapCallCount++;
            return Task.CompletedTask;
        });

        var services = new ServiceCollection();
        services.AddTransient<EchoBehavior>();
        var typeRegistry = new BehaviorTypeRegistry();
        typeRegistry.Register(descriptor.Id, typeof(EchoBehavior));
        services.AddSingleton<IBehaviorContributor>(new FluentBehaviorContributor(descriptor));
        services.AddSingleton<IBehaviorTypeRegistry>(typeRegistry);
        services.AddSingleton<IBehaviorCatalog>(sp =>
            new BehaviorCatalog(sp.GetServices<IBehaviorContributor>()));
        var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IBehaviorCatalog>();
        var dispatcher = new BehaviorDispatcher(catalog, typeRegistry, provider);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();

        using var lazy = new Cephalon.Behaviors.Http.LazyTransportBinding();

        // Three concurrent callers — MapAsync must fire only once.
        await Task.WhenAll(
            lazy.EnsureMappedAsync(app, descriptor, dispatcher, trackingBinding),
            lazy.EnsureMappedAsync(app, descriptor, dispatcher, trackingBinding),
            lazy.EnsureMappedAsync(app, descriptor, dispatcher, trackingBinding));

        Assert.Equal(1, mapCallCount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. RegistryGetBindingByTransportId
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void RegistryGetBindingByTransportId()
    {
        var bindings = new IHttpBehaviorBinding[]
        {
            new RestHttpBehaviorBinding(),
            new JsonRpcHttpBehaviorBinding(),
            new GraphqlHttpBehaviorBinding()
        };

        var registry = new HttpBehaviorBindingRegistry(bindings);

        Assert.NotNull(registry.GetBinding("http.rest"));
        Assert.NotNull(registry.GetBinding("http.jsonrpc"));
        Assert.NotNull(registry.GetBinding("http.graphql"));
        Assert.Null(registry.GetBinding("http.unknown"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. AllBindingsHaveUniqueTransportIds
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AllBindingsHaveUniqueTransportIds()
    {
        IHttpBehaviorBinding[] all =
        [
            new RestHttpBehaviorBinding(),
            new JsonRpcHttpBehaviorBinding(),
            new GraphqlHttpBehaviorBinding(),
            new GraphqlSseBehaviorBinding(),
            new GraphqlWsBehaviorBinding(),
            new SseBehaviorBinding(),
            new WebSocketBehaviorBinding()
        ];

        var ids = all.Select(b => b.TransportId).ToList();
        var distinct = ids.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        Assert.Equal(7, ids.Count);
        Assert.Equal(ids.Count, distinct.Count);
    }

    [Fact]
    public void AllBindingsHaveExpectedTransportIds()
    {
        Assert.Equal("http.rest", new RestHttpBehaviorBinding().TransportId);
        Assert.Equal("http.jsonrpc", new JsonRpcHttpBehaviorBinding().TransportId);
        Assert.Equal("http.graphql", new GraphqlHttpBehaviorBinding().TransportId);
        Assert.Equal("http.graphql-sse", new GraphqlSseBehaviorBinding().TransportId);
        Assert.Equal("http.graphql-ws", new GraphqlWsBehaviorBinding().TransportId);
        Assert.Equal("http.sse", new SseBehaviorBinding().TransportId);
        Assert.Equal("http.ws", new WebSocketBehaviorBinding().TransportId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 7. SseBindingHasCorrectRoute
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task JsonRpcBindingDoesNotMapLegacyAliasByDefault()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.jsonrpc"]);
        var (app, client) = await BuildAppAsync(descriptor, new JsonRpcHttpBehaviorBinding());

        var response = await client.PostAsJsonAsync(
            "/behaviors/object.echo/jsonrpc",
            new { jsonrpc = "2.0", method = "handle", @params = "test-input", id = 42 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await app.StopAsync();
    }

    [Fact]
    public async Task SseBindingDoesNotMapLegacyAliasByDefault()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.sse"]);
        var (app, client) = await BuildAppAsync(descriptor, new SseBehaviorBinding());

        using var request = new HttpRequestMessage(HttpMethod.Get, "/behaviors/object.echo/events");
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await app.StopAsync();
    }

    [Fact]
    public async Task SseBindingHasCanonicalApiSurfaceRoute()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.sse"]);
        var (app, client) = await BuildAppAsync(descriptor, new SseBehaviorBinding());

        using var request = new HttpRequestMessage(HttpMethod.Get, "/sse/v1/object/echo");
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);

        await app.StopAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 8. WebSocketBindingHasCorrectRoute
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task WebSocketBindingDoesNotMapLegacyAliasByDefault()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.ws"]);
        var (app, client) = await BuildAppAsync(descriptor, new WebSocketBehaviorBinding());

        var response = await client.GetAsync("/behaviors/object.echo/ws");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await app.StopAsync();
    }

    [Fact]
    public async Task WebSocketBindingHasCanonicalApiSurfaceRoute()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.ws"]);
        var (app, client) = await BuildAppAsync(descriptor, new WebSocketBehaviorBinding());

        var response = await client.GetAsync("/ws/v1/object/echo");

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await app.StopAsync();
    }

    [Fact]
    public async Task CanonicalBehaviorTransportRoutesRespectConfiguredPrefixesAndDefaultVersion()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiRoutes:Prefixes:Rest"] = "/service-api",
                ["ApiRoutes:Prefixes:GraphQL"] = "/behavior-graphql",
                ["ApiRoutes:Prefixes:JsonRpc"] = "/invoke",
                ["ApiRoutes:Prefixes:GraphQLSse"] = "/graphql-stream",
                ["ApiRoutes:Prefixes:GraphQLWs"] = "/graphql-socket",
                ["ApiRoutes:Prefixes:Sse"] = "/stream",
                ["ApiRoutes:Prefixes:Ws"] = "/socket",
                ["OpenApi:DefaultVersion"] = "2"
            })
            .Build();

        var restDescriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.rest"]);
        var (restApp, restClient) = await BuildAppAsync(restDescriptor, new RestHttpBehaviorBinding(configuration));
        var restResponse = await restClient.GetAsync("/service-api/v2/object/echo?q=hello");
        Assert.True(restResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);
        await restApp.StopAsync();

        var graphQlDescriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.graphql"]);
        var (graphQlApp, graphQlClient) = await BuildAppAsync(graphQlDescriptor, new GraphqlHttpBehaviorBinding(configuration));
        var graphQlResponse = await graphQlClient.PostAsJsonAsync(
            "/behavior-graphql/v2/object/echo",
            new { query = "{ echo }", variables = (object?)null });
        Assert.Equal(HttpStatusCode.OK, graphQlResponse.StatusCode);
        await graphQlApp.StopAsync();

        var rpcDescriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.jsonrpc"]);
        var (rpcApp, rpcClient) = await BuildAppAsync(rpcDescriptor, new JsonRpcHttpBehaviorBinding(configuration));
        var rpcResponse = await rpcClient.PostAsJsonAsync(
            "/invoke/v2/object/echo",
            new { jsonrpc = "2.0", method = "handle", @params = "hello", id = 11 });
        Assert.Equal(HttpStatusCode.OK, rpcResponse.StatusCode);
        var legacyRpcAliasResponse = await rpcClient.PostAsJsonAsync(
            "/behaviors/object.echo/jsonrpc",
            new { jsonrpc = "2.0", method = "handle", @params = "hello", id = 12 });
        Assert.Equal(HttpStatusCode.NotFound, legacyRpcAliasResponse.StatusCode);
        await rpcApp.StopAsync();

        var sseDescriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.sse"]);
        var (sseApp, sseClient) = await BuildAppAsync(sseDescriptor, new SseBehaviorBinding(configuration));
        using var sseRequest = new HttpRequestMessage(HttpMethod.Get, "/stream/v2/object/echo");
        var sseResponse = await sseClient.SendAsync(sseRequest, HttpCompletionOption.ResponseHeadersRead);
        Assert.NotEqual(HttpStatusCode.NotFound, sseResponse.StatusCode);
        await sseApp.StopAsync();

        var graphQlSseDescriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.graphql-sse"]);
        var (graphQlSseApp, graphQlSseClient) = await BuildAppAsync(graphQlSseDescriptor, new GraphqlSseBehaviorBinding(configuration));
        var graphQlSseResponse = await graphQlSseClient.PostAsJsonAsync(
            "/graphql-stream/v2/object/echo",
            new { query = "{ echo }" });
        Assert.Equal(HttpStatusCode.OK, graphQlSseResponse.StatusCode);
        await graphQlSseApp.StopAsync();

        var wsDescriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.ws"]);
        var (wsApp, wsClient) = await BuildAppAsync(wsDescriptor, new WebSocketBehaviorBinding(configuration));
        var wsResponse = await wsClient.GetAsync("/socket/v2/object/echo");
        Assert.Equal(HttpStatusCode.BadRequest, wsResponse.StatusCode);
        var legacyWsAliasResponse = await wsClient.GetAsync("/behaviors/object.echo/ws");
        Assert.Equal(HttpStatusCode.NotFound, legacyWsAliasResponse.StatusCode);
        await wsApp.StopAsync();

        var graphQlWsDescriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.graphql-ws"]);
        var (graphQlWsApp, graphQlWsClient) = await BuildAppAsync(graphQlWsDescriptor, new GraphqlWsBehaviorBinding(configuration));
        var graphQlWsResponse = await graphQlWsClient.GetAsync("/graphql-socket/v2/object/echo");
        Assert.Equal(HttpStatusCode.BadRequest, graphQlWsResponse.StatusCode);
        await graphQlWsApp.StopAsync();
    }

    [Fact]
    public void ApiRoutesOptionsUsesCanonicalPrefixesContract()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiRoutes:RestPrefix"] = "/legacy-api",
                ["ApiRoutes:GraphQLPrefix"] = "/legacy-graphql",
                ["ApiRoutes:JsonRpcPrefix"] = "/legacy-json-rpc",
                ["ApiRoutes:GrpcPrefix"] = "/legacy-grpc",
                ["ApiRoutes:WsPrefix"] = "/legacy-ws",
                ["ApiRoutes:SsePrefix"] = "/legacy-sse",
                ["ApiRoutes:GraphQLWsPrefix"] = "/legacy-graphql-ws",
                ["ApiRoutes:GraphQLSsePrefix"] = "/legacy-graphql-sse"
            })
            .Build();

        var options = ApiRoutesOptions.FromConfiguration(configuration);

        Assert.Equal("/api", options.RestPrefix);
        Assert.Equal("/graphql", options.GraphQLPrefix);
        Assert.Equal("/json-rpc", options.JsonRpcPrefix);
        Assert.Equal("/grpc", options.GrpcPrefix);
        Assert.Equal("/ws", options.WsPrefix);
        Assert.Equal("/sse", options.SsePrefix);
        Assert.Equal("/graphql-ws", options.GraphQLWsPrefix);
        Assert.Equal("/graphql-sse", options.GraphQLSsePrefix);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 9. AddHttpBehaviorBindingsRegistersAllBindingsInDI
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddHttpBehaviorBindingsRegistersAllBindingsInDI()
    {
        var services = new ServiceCollection();
        var typeRegistry = new BehaviorTypeRegistry();
        var builder = new BehaviorCollectionBuilder(services, typeRegistry);

        builder.AddHttpBehaviorBindings();

        var provider = services.BuildServiceProvider();
        var bindings = provider.GetServices<IHttpBehaviorBinding>().ToList();
        var registry = provider.GetService<IHttpBehaviorBindingRegistry>();

        Assert.Equal(7, bindings.Count);
        Assert.NotNull(registry);
        Assert.Equal(7, registry!.All.Count);
    }

    [Fact]
    public async Task BehaviorRestEndpointGroupBindsRouteQueryAndBodyAndUsesModuleVersionedNameByDefault()
    {
        var (app, client) = await BuildBehaviorRestHelperAppAsync();

        var response = await client.PostAsJsonAsync(
            "/v2/tests/cart/cart-001/items?quantity=3",
            new { productName = "Widget" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RestHelperEchoOutput>();
        Assert.NotNull(body);
        Assert.Equal("cart-001", body!.CartId);
        Assert.Equal("Widget", body.ProductName);
        Assert.Equal(3, body.Quantity);
        Assert.True(body.HasEventStore);

        var endpoint = app.Services.GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Single(static candidate =>
                string.Equals(candidate.RoutePattern.RawText, "/v2/tests/cart/{cartId}/items", StringComparison.Ordinal));

        var endpointName = endpoint.Metadata.GetMetadata<EndpointNameMetadata>();
        Assert.NotNull(endpointName);
        Assert.Equal("tests_cart.v2.rest_helper_echo", endpointName!.EndpointName);

        var groupName = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
        Assert.NotNull(groupName);
        Assert.Equal("v2", groupName!.EndpointGroupName);

        await app.StopAsync();
    }

    [Fact]
    public async Task BehaviorRestEndpointGroupApiVersionOverridesOperationNameVersionSegment()
    {
        var (app, client) = await BuildBehaviorRestHelperAppAsync(new VersionedRestHelperModule());

        var response = await client.PostAsJsonAsync(
            "/v1/tests/cart/versioned/cart-001/items?quantity=3",
            new { productName = "Widget" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var endpoint = app.Services.GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Single(static candidate =>
                string.Equals(candidate.RoutePattern.RawText, "/v1/tests/cart/versioned/{cartId}/items", StringComparison.Ordinal));

        var endpointName = endpoint.Metadata.GetMetadata<EndpointNameMetadata>();
        Assert.NotNull(endpointName);
        Assert.Equal("tests_cart.v1.rest_helper_echo", endpointName!.EndpointName);

        var groupName = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
        Assert.NotNull(groupName);
        Assert.Equal("v1", groupName!.EndpointGroupName);

        await app.StopAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper: tracking binding stub
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class TrackingBinding : IHttpBehaviorBinding
    {
        private readonly Func<WebApplication, BehaviorTopologyDescriptor, BehaviorDispatcher, Task> _callback;

        internal TrackingBinding(
            string transportId,
            Func<WebApplication, BehaviorTopologyDescriptor, BehaviorDispatcher, Task> callback)
        {
            TransportId = transportId;
            _callback = callback;
        }

        public string TransportId { get; }

        public Task MapAsync(WebApplication app, BehaviorTopologyDescriptor descriptor, BehaviorDispatcher dispatcher)
            => _callback(app, descriptor, dispatcher);
    }
}
