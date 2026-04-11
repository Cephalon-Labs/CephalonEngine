using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using Cephalon.Abstractions.Resilience;
using Cephalon.AspNetCore.GraphQL.Hosting;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

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
        var graphQlSseResponse = await client.PostAsJsonAsync("/graph-stream", CreateGreetingRequest("Stream"));
        var graphQlSsePayload = await graphQlSseResponse.Content.ReadAsStringAsync();
        var legacyGraphQlResponse = await client.PostAsJsonAsync("/graphql", CreateGreetingRequest("Legacy"));
        var policies = await client.GetFromJsonAsync<RateLimitingRuntimeDescriptor[]>("/engine/rate-limiting");
        var webSocketClient = app.GetTestServer().CreateWebSocketClient();
        webSocketClient.SubProtocols.Add("graphql-transport-ws");
        using var graphQlSocket = await webSocketClient.ConnectAsync(
            new Uri("ws://localhost/graph-socket"),
            CancellationToken.None);

        Assert.True(graphQlResponse.IsSuccessStatusCode);
        Assert.Contains("Hello, Http", graphQlPayload, StringComparison.Ordinal);
        Assert.True(graphQlSseResponse.IsSuccessStatusCode);
        Assert.Contains("Hello, Stream", graphQlSsePayload, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, legacyGraphQlResponse.StatusCode);
        Assert.Equal(WebSocketState.Open, graphQlSocket.State);
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
}
