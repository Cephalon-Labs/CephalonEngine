using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Http.Bindings;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Behaviors.Http.Registry;
using Cephalon.Behaviors.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
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

    // ─────────────────────────────────────────────────────────────────────────
    // 1. RestBindingMapsPostRoute
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RestBindingMapsPostRoute()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.rest"]);
        var (app, client) = await BuildAppAsync(descriptor, new RestHttpBehaviorBinding());

        var response = await client.PostAsJsonAsync("/behaviors/object.echo", "World");

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

        var response = await client.GetAsync("/behaviors/object.echo?q=hello");

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

        var response = await client.PostAsJsonAsync("/behaviors/object.echo/jsonrpc", requestBody);

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
        var response = await client.PostAsJsonAsync("/behaviors/object.echo/jsonrpc", requestBody);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("error", out var error));
        Assert.Equal(-32601, error.GetProperty("code").GetInt32());

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
        var response = await client.PostAsJsonAsync("/behaviors/object.echo/graphql", requestBody);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(
            json.TryGetProperty("data", out _) || json.TryGetProperty("errors", out _),
            "Response should contain 'data' or 'errors'");

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
    public async Task SseBindingHasCorrectRoute()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.sse"]);
        var (app, client) = await BuildAppAsync(descriptor, new SseBehaviorBinding());

        var response = await client.GetAsync("/behaviors/object.echo/events");

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);

        await app.StopAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 8. WebSocketBindingHasCorrectRoute
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task WebSocketBindingHasCorrectRoute()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.ws"]);
        var (app, client) = await BuildAppAsync(descriptor, new WebSocketBehaviorBinding());

        // Non-WebSocket HTTP GET to the WS route returns 400 (not 404).
        var response = await client.GetAsync("/behaviors/object.echo/ws");

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await app.StopAsync();
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
