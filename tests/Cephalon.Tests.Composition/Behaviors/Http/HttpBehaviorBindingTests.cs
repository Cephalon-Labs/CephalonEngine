using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Modules;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Http.Bindings;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Behaviors.Http.Registry;
using Cephalon.Behaviors.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Behaviors.Http;

public sealed partial class HttpBehaviorBindingTests
{
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

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(RestHelperEchoInput))]
    private sealed partial class HttpBehaviorBindingJsonSerializerContext : JsonSerializerContext;

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

    private static async Task<(WebApplication App, HttpClient Client)> BuildAppAsync(
        BehaviorTopologyDescriptor descriptor,
        params IHttpBehaviorBinding[] bindings)
        => await BuildAppAsync<ObjectEchoBehavior>(descriptor, bindings);

    private static async Task<(WebApplication App, HttpClient Client)> BuildAppAsync<TBehavior>(
        BehaviorTopologyDescriptor descriptor,
        params IHttpBehaviorBinding[] bindings)
        where TBehavior : class, new()
    {
        var services = new ServiceCollection();
        services.AddTransient<TBehavior>();
        services.AddSingleton(new BehaviorImplementationDescriptor(descriptor.Id, typeof(TBehavior)));
        var slotRegistry = new BehaviorExecutionSlotRegistry();
        slotRegistry.Register(
            descriptor.Id,
            typeof(TBehavior),
            BehaviorExecutionTestSlots.For(new TBehavior()));

        services.AddSingleton<IBehaviorContributor>(new FluentBehaviorContributor(descriptor));
        services.AddSingleton(slotRegistry);
        services.AddSingleton<IBehaviorCatalog>(sp =>
            new BehaviorCatalog(sp.GetServices<IBehaviorContributor>()));

        var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IBehaviorCatalog>();
        var dispatcher = new BehaviorDispatcher(catalog, provider.GetServices<BehaviorImplementationDescriptor>(), provider);

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

    private static async Task<(WebApplication App, HttpClient Client)> BuildBehaviorRestHelperAppAsync(
        IEndpointModule? module = null,
        IDictionary<string, string?>? configurationValues = null)
    {
        module ??= new RestHelperModule();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        if (configurationValues is not null)
        {
            builder.Configuration.AddInMemoryCollection(configurationValues);
        }

        var descriptor = new BehaviorTopologyDescriptor("rest.helper.echo", "direct", []);
        builder.Services.AddTransient<RestHelperEchoBehavior>();
        builder.Services.AddSingleton<IEventStore, StubEventStore>();

        builder.Services.AddSingleton(new BehaviorImplementationDescriptor(descriptor.Id, typeof(RestHelperEchoBehavior)));
        var slotRegistry = new BehaviorExecutionSlotRegistry();
        slotRegistry.Register(
            descriptor.Id,
            typeof(RestHelperEchoBehavior),
            BehaviorExecutionSlot.For<RestHelperEchoBehavior, RestHelperEchoInput, RestHelperEchoOutput>(
                HttpBehaviorBindingJsonSerializerContext.Default.RestHelperEchoInput));
        builder.Services.AddSingleton<IBehaviorContributor>(new FluentBehaviorContributor(descriptor));
        builder.Services.AddSingleton(slotRegistry);
        builder.Services.AddSingleton<IBehaviorCatalog>(sp =>
            new BehaviorCatalog(sp.GetServices<IBehaviorContributor>()));
        builder.Services.AddSingleton<BehaviorDispatcher>();

        var app = builder.Build();
        var routeOptions = ApiRoutesOptions.FromConfiguration(app.Configuration);
        IEndpointRouteBuilder endpoints = string.IsNullOrWhiteSpace(routeOptions.RestPrefix)
            ? app
            : app.MapGroup(routeOptions.RestPrefix);
        module.MapEndpoints(endpoints);

        await app.StartAsync();
        return (app, app.GetTestClient());
    }

    [Fact]
    public async Task JsonRpcBindingParsesRequestReturnsResult()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.jsonrpc"]);
        var (app, client) = await BuildAppAsync(descriptor, new JsonRpcHttpBehaviorBinding());

        var response = await client.PostAsJsonAsync(
            "/json-rpc/v1/object/echo",
            new { jsonrpc = "2.0", method = "handle", @params = "test-input", id = 42 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2.0", json.GetProperty("jsonrpc").GetString());
        Assert.Equal(42, json.GetProperty("id").GetInt32());

        await app.StopAsync();
    }

    [Fact]
    public async Task JsonRpcBindingReturnsParseErrorWithNullIdForMalformedPayload()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.jsonrpc"]);
        var (app, client) = await BuildAppAsync(descriptor, new JsonRpcHttpBehaviorBinding());

        using var request = new HttpRequestMessage(HttpMethod.Post, "/json-rpc/v1/object/echo")
        {
            Content = new StringContent("{\"jsonrpc\":\"2.0\",\"method\":", System.Text.Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2.0", payload.GetProperty("jsonrpc").GetString());
        Assert.Equal(-32700, payload.GetProperty("error").GetProperty("code").GetInt32());
        Assert.Equal("Parse error", payload.GetProperty("error").GetProperty("message").GetString());
        Assert.True(payload.TryGetProperty("id", out var id));
        Assert.Equal(JsonValueKind.Null, id.ValueKind);

        await app.StopAsync();
    }

    [Fact]
    public async Task JsonRpcBindingReturnsInvalidRequestAndEchoesIdWhenVersionIsNot20()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.jsonrpc"]);
        var (app, client) = await BuildAppAsync(descriptor, new JsonRpcHttpBehaviorBinding());

        var response = await client.PostAsJsonAsync(
            "/json-rpc/v1/object/echo",
            new { jsonrpc = "1.0", method = "handle", @params = "test-input", id = "req-9" });
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2.0", payload.GetProperty("jsonrpc").GetString());
        Assert.Equal(-32600, payload.GetProperty("error").GetProperty("code").GetInt32());
        Assert.Equal("Invalid Request", payload.GetProperty("error").GetProperty("message").GetString());
        Assert.Contains("jsonrpc must be", payload.GetProperty("error").GetProperty("data").GetString(), StringComparison.Ordinal);
        Assert.Equal("req-9", payload.GetProperty("id").GetString());

        await app.StopAsync();
    }

    [Fact]
    public async Task GraphqlBindingMapsCanonicalApiSurfaceRoute()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.graphql"]);
        var (app, client) = await BuildAppAsync(descriptor, new GraphqlHttpBehaviorBinding());

        var response = await client.PostAsJsonAsync(
            "/graphql/v1/object/echo",
            new { query = "query Echo($value: String!) { echo(value: $value) }", variables = new { value = "hello" } });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(payload.TryGetProperty("data", out _));

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

    [Fact]
    public async Task WebSocketBindingHasCanonicalApiSurfaceRoute()
    {
        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.ws"]);
        var (app, client) = await BuildAppAsync(descriptor, new WebSocketBehaviorBinding());

        var response = await client.GetAsync("/ws/v1/object/echo");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await app.StopAsync();
    }

    [Fact]
    public async Task CanonicalBehaviorTransportRoutesRespectConfiguredPrefixesAndDefaultVersion()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiRoutes:Prefixes:GraphQL"] = "/behavior-graphql",
                ["ApiRoutes:Prefixes:JsonRpc"] = "/invoke",
                ["ApiRoutes:Prefixes:GraphQLSse"] = "/graphql-stream",
                ["ApiRoutes:Prefixes:GraphQLWs"] = "/graphql-socket",
                ["ApiRoutes:Prefixes:Sse"] = "/stream",
                ["ApiRoutes:Prefixes:Ws"] = "/socket",
                ["OpenApi:DefaultVersion"] = "2",
                ["OpenApi:EnabledVersions:0"] = "3"
            })
            .Build();

        var rpcDescriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.jsonrpc"]);
        var (rpcApp, rpcClient) = await BuildAppAsync(rpcDescriptor, new JsonRpcHttpBehaviorBinding(configuration));
        var rpcResponse = await rpcClient.PostAsJsonAsync(
            "/invoke/v2/object/echo",
            new { jsonrpc = "2.0", method = "handle", @params = "hello", id = 11 });
        Assert.Equal(HttpStatusCode.OK, rpcResponse.StatusCode);
        await rpcApp.StopAsync();

        var graphQlDescriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.graphql"]);
        var (graphQlApp, graphQlClient) = await BuildAppAsync(graphQlDescriptor, new GraphqlHttpBehaviorBinding(configuration));
        var graphQlResponse = await graphQlClient.PostAsJsonAsync(
            "/behavior-graphql/v2/object/echo",
            new { query = "{ echo }", variables = new { value = "hello" } });
        Assert.Equal(HttpStatusCode.OK, graphQlResponse.StatusCode);
        await graphQlApp.StopAsync();

        var graphQlSseDescriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.graphql-sse"]);
        var (graphQlSseApp, graphQlSseClient) = await BuildAppAsync(graphQlSseDescriptor, new GraphqlSseBehaviorBinding(configuration));
        var graphQlSseResponse = await graphQlSseClient.PostAsJsonAsync(
            "/graphql-stream/v2/object/echo",
            new { query = "{ echo }", variables = new { value = "hello" } });
        Assert.Equal(HttpStatusCode.OK, graphQlSseResponse.StatusCode);
        Assert.Equal("text/event-stream", graphQlSseResponse.Content.Headers.ContentType?.MediaType);
        await graphQlSseApp.StopAsync();

        var graphQlWsDescriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.graphql-ws"]);
        var (graphQlWsApp, graphQlWsClient) = await BuildAppAsync(graphQlWsDescriptor, new GraphqlWsBehaviorBinding(configuration));
        var graphQlWsResponse = await graphQlWsClient.GetAsync("/graphql-socket/v2/object/echo");
        Assert.Equal(HttpStatusCode.BadRequest, graphQlWsResponse.StatusCode);
        await graphQlWsApp.StopAsync();

        var sseDescriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.sse"]);
        var (sseApp, sseClient) = await BuildAppAsync(sseDescriptor, new SseBehaviorBinding(configuration));
        using var sseRequest = new HttpRequestMessage(HttpMethod.Get, "/stream/v2/object/echo");
        var sseResponse = await sseClient.SendAsync(sseRequest, HttpCompletionOption.ResponseHeadersRead);
        Assert.NotEqual(HttpStatusCode.NotFound, sseResponse.StatusCode);
        await sseApp.StopAsync();

        var wsDescriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.ws"]);
        var (wsApp, wsClient) = await BuildAppAsync(wsDescriptor, new WebSocketBehaviorBinding(configuration));
        var wsResponse = await wsClient.GetAsync("/socket/v2/object/echo");
        Assert.Equal(HttpStatusCode.BadRequest, wsResponse.StatusCode);
        await wsApp.StopAsync();
    }

    [Fact]
    public async Task CanonicalBehaviorTransportRoutesPreferExplicitDefaultBehaviorDocumentName()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiRoutes:Prefixes:JsonRpc"] = "/invoke",
                ["ApiRoutes:DefaultBehaviorDocumentName"] = "preview",
                ["OpenApi:DefaultVersion"] = "2",
                ["OpenApi:EnabledVersions:0"] = "3"
            })
            .Build();

        var descriptor = new BehaviorTopologyDescriptor("object.echo", "direct", ["http.jsonrpc"]);
        var (app, client) = await BuildAppAsync(descriptor, new JsonRpcHttpBehaviorBinding(configuration));

        var preferredResponse = await client.PostAsJsonAsync(
            "/invoke/preview/object/echo",
            new { jsonrpc = "2.0", method = "handle", @params = "hello", id = 12 });
        Assert.Equal(HttpStatusCode.OK, preferredResponse.StatusCode);

        var fallbackResponse = await client.PostAsJsonAsync(
            "/invoke/v2/object/echo",
            new { jsonrpc = "2.0", method = "handle", @params = "hello", id = 13 });
        Assert.Equal(HttpStatusCode.NotFound, fallbackResponse.StatusCode);

        await app.StopAsync();
    }

    [Fact]
    public void RegistryGetBindingByTransportId()
    {
        var bindings = new IHttpBehaviorBinding[]
        {
            new JsonRpcHttpBehaviorBinding(),
            new GraphqlHttpBehaviorBinding(),
            new GraphqlSseBehaviorBinding(),
            new GraphqlWsBehaviorBinding(),
            new SseBehaviorBinding(),
            new WebSocketBehaviorBinding()
        };

        var registry = new HttpBehaviorBindingRegistry(bindings);

        Assert.Null(registry.GetBinding("http.rest"));
        Assert.NotNull(registry.GetBinding("http.jsonrpc"));
        Assert.NotNull(registry.GetBinding("http.graphql"));
        Assert.NotNull(registry.GetBinding("http.graphql-sse"));
        Assert.NotNull(registry.GetBinding("http.graphql-ws"));
        Assert.NotNull(registry.GetBinding("http.sse"));
        Assert.NotNull(registry.GetBinding("http.ws"));
    }

    [Fact]
    public void AllBindingsHaveExpectedTransportIds()
    {
        IHttpBehaviorBinding[] all =
        [
            new JsonRpcHttpBehaviorBinding(),
            new GraphqlHttpBehaviorBinding(),
            new GraphqlSseBehaviorBinding(),
            new GraphqlWsBehaviorBinding(),
            new SseBehaviorBinding(),
            new WebSocketBehaviorBinding()
        ];

        var ids = all.Select(binding => binding.TransportId).ToList();
        Assert.Equal(6, ids.Count);
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.DoesNotContain("http.rest", ids);
    }

    [Fact]
    public void AddHttpBehaviorBindingsRegistersAllBindingsInDi()
    {
        var services = new ServiceCollection();
        var builder = new BehaviorCollectionBuilder(services);

        builder.AddHttpBehaviorBindings();

        var provider = services.BuildServiceProvider();
        var bindings = provider.GetServices<IHttpBehaviorBinding>().ToList();
        var registry = provider.GetService<IHttpBehaviorBindingRegistry>();

        Assert.Equal(6, bindings.Count);
        Assert.NotNull(registry);
        Assert.Equal(6, registry!.All.Count);
        Assert.DoesNotContain(registry.All, binding => string.Equals(binding.TransportId, "http.rest", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BehaviorRestEndpointGroupUsesConfiguredRestPrefixByDefault()
    {
        var (app, client) = await BuildBehaviorRestHelperAppAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v2/tests/cart/cart-001/items?quantity=3",
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
                string.Equals(candidate.RoutePattern.RawText, "/api/v2/tests/cart/{cartId}/items", StringComparison.Ordinal));

        var endpointName = endpoint.Metadata.GetMetadata<EndpointNameMetadata>();
        Assert.NotNull(endpointName);
        Assert.Equal("tests_cart.v2.rest_helper_echo", endpointName!.EndpointName);

        var groupName = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
        Assert.NotNull(groupName);
        Assert.Equal("v2", groupName!.EndpointGroupName);

        await app.StopAsync();
    }

    [Fact]
    public async Task BehaviorRestEndpointGroupRespectsEmptyRestPrefix()
    {
        var (app, client) = await BuildBehaviorRestHelperAppAsync(
            configurationValues: new Dictionary<string, string?>
            {
                ["ApiRoutes:Prefixes:Rest"] = string.Empty
            });

        var response = await client.PostAsJsonAsync(
            "/v2/tests/cart/cart-001/items?quantity=3",
            new { productName = "Widget" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var endpoint = app.Services.GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Single(static candidate =>
                string.Equals(candidate.RoutePattern.RawText, "/v2/tests/cart/{cartId}/items", StringComparison.Ordinal));

        Assert.NotNull(endpoint);
        await app.StopAsync();
    }

    [Fact]
    public async Task BehaviorRestEndpointGroupApiVersionOverridesOperationNameVersionSegment()
    {
        var (app, client) = await BuildBehaviorRestHelperAppAsync(new VersionedRestHelperModule());

        var response = await client.PostAsJsonAsync(
            "/api/v1/tests/cart/versioned/cart-001/items?quantity=3",
            new { productName = "Widget" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var endpoint = app.Services.GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Single(static candidate =>
                string.Equals(candidate.RoutePattern.RawText, "/api/v1/tests/cart/versioned/{cartId}/items", StringComparison.Ordinal));

        var endpointName = endpoint.Metadata.GetMetadata<EndpointNameMetadata>();
        Assert.NotNull(endpointName);
        Assert.Equal("tests_cart.v1.rest_helper_echo", endpointName!.EndpointName);

        var groupName = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
        Assert.NotNull(groupName);
        Assert.Equal("v1", groupName!.EndpointGroupName);

        await app.StopAsync();
    }
}
