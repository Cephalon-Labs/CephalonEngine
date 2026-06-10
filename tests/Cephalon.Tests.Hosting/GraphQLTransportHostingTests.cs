using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using Cephalon.Abstractions.Resilience;
using Cephalon.AspNetCore.GraphQL.Hosting;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

public sealed class GraphQLTransportHostingTests
{
    [Fact]
    public async Task MapCephalonSplitsBuiltInGraphQlRoutesAcrossConfiguredPrefixes()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "GraphQL";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Algorithm"] = "FixedWindow";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:PermitLimit"] = "20";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:QueueLimit"] = "0";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:WindowSeconds"] = "60";
        builder.Configuration["ApiRoutes:Prefixes:GraphQL"] = "/graph-http";
        builder.Configuration["ApiRoutes:Prefixes:GraphQLSse"] = "/graph-stream";
        builder.Configuration["ApiRoutes:Prefixes:GraphQLWs"] = "/graph-socket";
        builder.AddGraphQLTransport();
        builder.ConfigureGraphQLTransport(graphql => graphql.AddInMemorySubscriptions());
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var graphQlResponse = await client.PostAsJsonAsync("/graph-http", CreateGreetingRequest("Http"));
        var graphQlPayload = await graphQlResponse.Content.ReadAsStringAsync();
        var schemaResponse = await client.GetAsync("/graph-http/schema");
        var schemaPayload = await schemaResponse.Content.ReadAsStringAsync();
        var legacyGraphQlResponse = await client.PostAsJsonAsync("/graphql", CreateGreetingRequest("Legacy"));
        var legacySchemaResponse = await client.GetAsync("/graphql/schema");
        var policies = await client.GetFromJsonAsync<RateLimitingRuntimeDescriptor[]>("/engine/rate-limiting");

        Assert.True(graphQlResponse.IsSuccessStatusCode);
        Assert.Contains("Hello, Http", graphQlPayload, StringComparison.Ordinal);
        Assert.True(schemaResponse.IsSuccessStatusCode);
        Assert.Contains("type Query", schemaPayload, StringComparison.Ordinal);
        Assert.Contains("type Mutation", schemaPayload, StringComparison.Ordinal);
        Assert.Contains("type Subscription", schemaPayload, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, legacyGraphQlResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, legacySchemaResponse.StatusCode);
        Assert.NotNull(policies);
        var policy = Assert.Single(policies);
        Assert.Contains("graphql", policy.TransportIds);
        Assert.Contains("graphql-sse", policy.TransportIds);
        Assert.Contains("graphql-ws", policy.TransportIds);
        Assert.Equal("mixed-request-and-long-lived", policy.Metadata["transportKind"]);
        Assert.Equal("mixed-request-and-session-entry-rate", policy.Metadata["transportSemantics"]);
        Assert.Equal("checked-on-request-or-session-entry", policy.Metadata["enforcementMoment"]);
    }

    [Fact]
    public async Task MapCephalonServesGraphQlSubscriptionsOverConfiguredSsePrefix()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "GraphQL";
        builder.Configuration["ApiRoutes:Prefixes:GraphQL"] = "/graph-http";
        builder.Configuration["ApiRoutes:Prefixes:GraphQLSse"] = "/graph-stream";
        builder.AddGraphQLTransport();
        builder.ConfigureGraphQLTransport(graphql => graphql.AddInMemorySubscriptions());
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graph-stream");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Content = JsonContent.Create(CreateGreetingSubscriptionRequest("Stream"));

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);
        var publishResponse = await client.PostAsJsonAsync(
            "/graph-http",
            CreatePublishGreetingRequest("Stream", delayMilliseconds: 250),
            cts.Token);
        var payload = await response.Content.ReadAsStringAsync(cts.Token);

        Assert.True(publishResponse.IsSuccessStatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("data:", payload, StringComparison.Ordinal);
        Assert.Contains("Hello, Stream", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonServesGraphQlSubscriptionsOverConfiguredWebSocketPrefix()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "GraphQL";
        builder.Configuration["ApiRoutes:Prefixes:GraphQL"] = "/graph-http";
        builder.Configuration["ApiRoutes:Prefixes:GraphQLWs"] = "/graph-socket";
        builder.AddGraphQLTransport();
        builder.ConfigureGraphQLTransport(graphql => graphql.AddInMemorySubscriptions());
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();
        var webSocketClient = app.GetTestServer().CreateWebSocketClient();
        webSocketClient.SubProtocols.Add("graphql-transport-ws");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var socket = await webSocketClient.ConnectAsync(
            new Uri("ws://localhost/graph-socket"),
            cts.Token);

        await SendWebSocketJsonAsync(socket, new { type = "connection_init" }, cts.Token);
        var ackMessage = await ReceiveWebSocketMessageMatchingAsync(
            socket,
            message => message.Contains("\"type\":\"connection_ack\"", StringComparison.Ordinal),
            cts.Token);

        await SendWebSocketJsonAsync(
            socket,
            new
            {
                id = "sub-1",
                type = "subscribe",
                payload = CreateGreetingSubscriptionRequest("Socket")
            },
            cts.Token);
        var publishResponse = await client.PostAsJsonAsync(
            "/graph-http",
            CreatePublishGreetingRequest("Socket", delayMilliseconds: 250),
            cts.Token);
        var nextMessage = await ReceiveWebSocketMessageMatchingAsync(
            socket,
            message => message.Contains("\"type\":\"next\"", StringComparison.Ordinal),
            cts.Token);
        var completeMessage = await ReceiveWebSocketMessageMatchingAsync(
            socket,
            message => message.Contains("\"type\":\"complete\"", StringComparison.Ordinal),
            cts.Token);

        Assert.True(publishResponse.IsSuccessStatusCode);
        Assert.Equal(WebSocketState.Open, socket.State);
        Assert.Contains("\"type\":\"connection_ack\"", ackMessage, StringComparison.Ordinal);
        Assert.Contains("\"id\":\"sub-1\"", nextMessage, StringComparison.Ordinal);
        Assert.Contains("Hello, Socket", nextMessage, StringComparison.Ordinal);
        Assert.Contains("\"id\":\"sub-1\"", completeMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonMergesGraphQlContributionsFromMultipleModulesIntoSharedRoots()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "GraphQL";
        builder.Configuration["ApiRoutes:Prefixes:GraphQL"] = "/graph-http";
        builder.AddGraphQLTransport();
        builder.ConfigureGraphQLTransport(graphql => graphql.AddInMemorySubscriptions());
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
            engine.AddModule(new AdditionalGraphQLContributionModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var schemaResponse = await client.GetAsync("/graph-http/schema");
        var schemaPayload = await schemaResponse.Content.ReadAsStringAsync();

        Assert.True(schemaResponse.IsSuccessStatusCode);
        Assert.Contains("hello(", schemaPayload, StringComparison.Ordinal);
        Assert.Contains("farewell(", schemaPayload, StringComparison.Ordinal);
        Assert.Contains("publishGreeting(", schemaPayload, StringComparison.Ordinal);
        Assert.Contains("publishFarewell(", schemaPayload, StringComparison.Ordinal);
        Assert.Contains("greetingPublished(", schemaPayload, StringComparison.Ordinal);
        Assert.Contains("farewellPublished(", schemaPayload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonReturnsGraphQlErrorEnvelopeForMalformedQuery()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "GraphQL";
        builder.Configuration["ApiRoutes:Prefixes:GraphQL"] = "/graph-http";
        builder.AddGraphQLTransport();
        builder.ConfigureGraphQLTransport(graphql => graphql.AddInMemorySubscriptions());
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/graph-http", new
        {
            query = "query { hello( }"
        });
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(payload.TryGetProperty("errors", out var errors));
        Assert.True(errors.ValueKind == JsonValueKind.Array);
        Assert.True(errors.GetArrayLength() > 0);
        var message = errors[0].GetProperty("message").GetString();
        Assert.False(string.IsNullOrWhiteSpace(message));
        if (payload.TryGetProperty("data", out var dataElement))
        {
            Assert.Equal(JsonValueKind.Null, dataElement.ValueKind);
        }
    }

    [Fact]
    public async Task MapCephalonAppliesGraphQlWebSocketConcurrencyLimitAsActiveConnectionConcurrency()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "GraphQL";
        builder.Configuration["ApiRoutes:Prefixes:GraphQL"] = "/graph-http";
        builder.Configuration["ApiRoutes:Prefixes:GraphQLWs"] = "/graph-socket";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Overrides:graphql-ws-only:Transports:0"] = "graphql-ws";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Overrides:graphql-ws-only:Algorithm"] = "ConcurrencyLimiter";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Overrides:graphql-ws-only:PermitLimit"] = "1";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Overrides:graphql-ws-only:QueueLimit"] = "0";
        builder.AddGraphQLTransport();
        builder.ConfigureGraphQLTransport(graphql => graphql.AddInMemorySubscriptions());
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();
        var policies = await client.GetFromJsonAsync<RateLimitingRuntimeDescriptor[]>("/engine/rate-limiting");
        Assert.NotNull(policies);
        var policy = Assert.Single(policies);
        var webSocketClient = app.GetTestServer().CreateWebSocketClient();
        webSocketClient.SubProtocols.Add("graphql-transport-ws");
        using var firstSocket = await webSocketClient.ConnectAsync(
            new Uri("ws://localhost/graph-socket"),
            CancellationToken.None);
        var secondAttempt = await Record.ExceptionAsync(async () =>
        {
            using var secondSocket = await webSocketClient.ConnectAsync(
                new Uri("ws://localhost/graph-socket"),
                CancellationToken.None);
        });

        Assert.Equal("graphql-ws", Assert.Single(policy.TransportIds));
        Assert.Equal("long-lived-transport-endpoints", policy.Scope);
        Assert.Equal("long-lived-connection", policy.Metadata["transportKind"]);
        Assert.Equal("active-connection-concurrency", policy.Metadata["transportSemantics"]);
        Assert.Equal("held-until-session-closes", policy.Metadata["enforcementMoment"]);
        Assert.NotNull(secondAttempt);
    }

    private static object CreateGreetingRequest(string name)
    {
        return new
        {
            query = "query ($name: String) { hello(name: $name) { message generatedAtUtc traits } }",
            variables = new
            {
                name
            }
        };
    }

    private static object CreateGreetingSubscriptionRequest(string name)
    {
        return new
        {
            query = "subscription ($name: String!) { greetingPublished(name: $name) { message generatedAtUtc traits } }",
            variables = new
            {
                name
            }
        };
    }

    private static object CreatePublishGreetingRequest(string name, int? delayMilliseconds = null)
    {
        return new
        {
            query = "mutation ($name: String!, $delayMilliseconds: Int) { publishGreeting(name: $name, delayMilliseconds: $delayMilliseconds) { message generatedAtUtc traits } }",
            variables = new
            {
                name,
                delayMilliseconds
            }
        };
    }

    private static async Task SendWebSocketJsonAsync(
        WebSocket socket,
        object payload,
        CancellationToken cancellationToken)
    {
        var buffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(payload));
        await socket.SendAsync(
            new ArraySegment<byte>(buffer),
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken: cancellationToken);
    }

    private static async Task<string> ReceiveWebSocketMessageMatchingAsync(
        WebSocket socket,
        Func<string, bool> match,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < 10; index++)
        {
            var message = await ReceiveWebSocketTextAsync(socket, cancellationToken);
            if (match(message))
            {
                return message;
            }
        }

        throw new Xunit.Sdk.XunitException("Expected a matching GraphQL WebSocket message but did not receive one.");
    }

    private static async Task<string> ReceiveWebSocketTextAsync(
        WebSocket socket,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        using var stream = new MemoryStream();

        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return string.Empty;
            }

            stream.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
                break;
            }
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
