using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Engine.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace Cephalon.Tests.Hosting;

public sealed class BehaviorHttpTransportResilienceHostingTests
{
    private const string BehaviorId = "tests.rate-limited";

    [Fact]
    public async Task BehaviorHttpGraphQlReturnsProtocolRateLimitingEnvelopeWhenHostLimiterOverrideDisablesEndpointPolicy()
    {
        await using var app = await BuildRateLimitedBehaviorHttpAppAsync("http.graphql");
        var client = app.GetTestClient();

        var firstResponse = await client.PostAsJsonAsync("/graphql/v1/tests/rate-limited", CreateGraphqlRequest("alpha"));
        var firstPayload = await firstResponse.Content.ReadFromJsonAsync<JsonElement>();
        var rejectedResponse = await client.PostAsJsonAsync("/graphql/v1/tests/rate-limited", CreateGraphqlRequest("beta"));
        var rejectedPayload = await rejectedResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal("alpha", firstPayload.GetProperty("data").GetProperty("value").GetString());
        Assert.Equal(HttpStatusCode.OK, rejectedResponse.StatusCode);
        var error = Assert.Single(rejectedPayload.GetProperty("errors").EnumerateArray());
        Assert.Contains("rate limit", error.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("BEHAVIOR_EXECUTION_RATE_LIMITED", error.GetProperty("extensions").GetProperty("code").GetString());
        Assert.Equal("behavior_execution_rate_limited", error.GetProperty("extensions").GetProperty("cephalonCode").GetString());
        Assert.Equal(429, error.GetProperty("extensions").GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task BehaviorHttpJsonRpcReturnsProtocolRateLimitingEnvelopeWhenHostLimiterOverrideDisablesEndpointPolicy()
    {
        await using var app = await BuildRateLimitedBehaviorHttpAppAsync("http.jsonrpc");
        var client = app.GetTestClient();

        var firstResponse = await client.PostAsJsonAsync(
            "/json-rpc/v1/tests/rate-limited",
            new
            {
                jsonrpc = "2.0",
                method = "handle",
                @params = new { value = "alpha" },
                id = "req-1"
            });
        var firstPayload = await firstResponse.Content.ReadFromJsonAsync<JsonElement>();
        var rejectedResponse = await client.PostAsJsonAsync(
            "/json-rpc/v1/tests/rate-limited",
            new
            {
                jsonrpc = "2.0",
                method = "handle",
                @params = new { value = "beta" },
                id = "req-2"
            });
        var rejectedPayload = await rejectedResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal("alpha", firstPayload.GetProperty("result").GetProperty("Value").GetString());
        Assert.Equal(HttpStatusCode.OK, rejectedResponse.StatusCode);
        Assert.Equal(-32029, rejectedPayload.GetProperty("error").GetProperty("code").GetInt32());
        Assert.Equal("Too many requests", rejectedPayload.GetProperty("error").GetProperty("message").GetString());
        Assert.Contains(
            "behavior_execution_rate_limited",
            rejectedPayload.GetProperty("error").GetProperty("data").GetString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task BehaviorHttpGraphQlSseReturnsProtocolRateLimitingEnvelopeWhenHostLimiterOverrideDisablesEndpointPolicy()
    {
        await using var app = await BuildRateLimitedBehaviorHttpAppAsync("http.graphql-sse");
        var client = app.GetTestClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/graphql-sse/v1/tests/rate-limited");
        firstRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        firstRequest.Content = JsonContent.Create(CreateGraphqlRequest("alpha"));
        using var firstResponse = await client.SendAsync(firstRequest, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        var firstEvent = await ReadSseMessageAsync(firstResponse, cts.Token);
        using var firstPayload = JsonDocument.Parse(firstEvent.Data);

        using var rejectedRequest = new HttpRequestMessage(HttpMethod.Post, "/graphql-sse/v1/tests/rate-limited");
        rejectedRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        rejectedRequest.Content = JsonContent.Create(CreateGraphqlRequest("beta"));
        using var rejectedResponse = await client.SendAsync(
            rejectedRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);
        var rejectedEvent = await ReadSseMessageAsync(rejectedResponse, cts.Token);
        using var rejectedPayload = JsonDocument.Parse(rejectedEvent.Data);

        Assert.Equal("text/event-stream", firstResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal("next", firstEvent.EventName);
        Assert.Equal("alpha", firstPayload.RootElement.GetProperty("data").GetProperty("Value").GetString());
        Assert.Equal("next", rejectedEvent.EventName);
        var error = Assert.Single(rejectedPayload.RootElement.GetProperty("errors").EnumerateArray());
        Assert.Equal("behavior_execution_rate_limited", error.GetProperty("extensions").GetProperty("cephalonCode").GetString());
        Assert.Equal(429, error.GetProperty("extensions").GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task BehaviorHttpGraphQlWsReturnsProtocolRateLimitingEnvelopeWhenHostLimiterOverrideDisablesEndpointPolicy()
    {
        await using var app = await BuildRateLimitedBehaviorHttpAppAsync("http.graphql-ws");
        var webSocketClient = app.GetTestServer().CreateWebSocketClient();
        webSocketClient.SubProtocols.Add("graphql-transport-ws");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var socket = await webSocketClient.ConnectAsync(
            new Uri("ws://localhost/graphql-ws/v1/tests/rate-limited"),
            cts.Token);

        await SendWebSocketJsonAsync(socket, new { type = "connection_init" }, cts.Token);
        await ReceiveWebSocketMessageMatchingAsync(
            socket,
            message => message.Contains("\"type\":\"connection_ack\"", StringComparison.Ordinal),
            cts.Token);

        await SendWebSocketJsonAsync(
            socket,
            new
            {
                id = "req-1",
                type = "subscribe",
                payload = CreateGraphqlRequest("alpha")
            },
            cts.Token);
        var successMessage = await ReceiveWebSocketMessageMatchingAsync(
            socket,
            message => message.Contains("\"type\":\"next\"", StringComparison.Ordinal) &&
                message.Contains("\"id\":\"req-1\"", StringComparison.Ordinal),
            cts.Token);
        using var successPayload = JsonDocument.Parse(successMessage);

        await SendWebSocketJsonAsync(
            socket,
            new
            {
                id = "req-2",
                type = "subscribe",
                payload = CreateGraphqlRequest("beta")
            },
            cts.Token);
        var errorMessage = await ReceiveWebSocketMessageMatchingAsync(
            socket,
            message => message.Contains("\"type\":\"error\"", StringComparison.Ordinal) &&
                message.Contains("\"id\":\"req-2\"", StringComparison.Ordinal),
            cts.Token);
        using var errorPayload = JsonDocument.Parse(errorMessage);

        Assert.Equal("alpha", successPayload.RootElement
            .GetProperty("payload")
            .GetProperty("data")
            .GetProperty("Value")
            .GetString());
        var error = Assert.Single(errorPayload.RootElement.GetProperty("payload").EnumerateArray());
        Assert.Equal("behavior_execution_rate_limited", error.GetProperty("extensions").GetProperty("cephalonCode").GetString());
        Assert.Equal(429, error.GetProperty("extensions").GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task BehaviorHttpSseReturnsProtocolRateLimitingEnvelopeWhenHostLimiterOverrideDisablesEndpointPolicy()
    {
        await using var app = await BuildRateLimitedBehaviorHttpAppAsync("http.sse");
        var client = app.GetTestClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        using var firstRequest = new HttpRequestMessage(HttpMethod.Get, "/sse/v1/tests/rate-limited?value=alpha");
        using var firstResponse = await client.SendAsync(firstRequest, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        var firstEvent = await ReadSseMessageAsync(firstResponse, cts.Token);
        using var firstPayload = JsonDocument.Parse(firstEvent.Data);

        using var rejectedRequest = new HttpRequestMessage(HttpMethod.Get, "/sse/v1/tests/rate-limited?value=beta");
        using var rejectedResponse = await client.SendAsync(
            rejectedRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);
        var rejectedEvent = await ReadSseMessageAsync(rejectedResponse, cts.Token);
        using var rejectedPayload = JsonDocument.Parse(rejectedEvent.Data);

        Assert.Equal("text/event-stream", firstResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal("result", firstEvent.EventName);
        Assert.Equal("alpha", firstPayload.RootElement.GetProperty("Value").GetString());
        Assert.Equal("error", rejectedEvent.EventName);
        Assert.Equal("behavior_execution_rate_limited", rejectedPayload.RootElement.GetProperty("code").GetString());
        Assert.Equal(429, rejectedPayload.RootElement.GetProperty("statusCode").GetInt32());
        Assert.Contains("rate limit", rejectedPayload.RootElement.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BehaviorHttpWebSocketReturnsProtocolRateLimitingEnvelopeWhenHostLimiterOverrideDisablesEndpointPolicy()
    {
        await using var app = await BuildRateLimitedBehaviorHttpAppAsync("http.ws");
        var webSocketClient = app.GetTestServer().CreateWebSocketClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var socket = await webSocketClient.ConnectAsync(
            new Uri("ws://localhost/ws/v1/tests/rate-limited"),
            cts.Token);

        await SendWebSocketJsonAsync(socket, new { value = "alpha" }, cts.Token);
        var successMessage = await ReceiveWebSocketTextAsync(socket, cts.Token);
        using var successPayload = JsonDocument.Parse(successMessage);

        await SendWebSocketJsonAsync(socket, new { value = "beta" }, cts.Token);
        var errorMessage = await ReceiveWebSocketTextAsync(socket, cts.Token);
        using var errorPayload = JsonDocument.Parse(errorMessage);

        Assert.Equal("alpha", successPayload.RootElement.GetProperty("Value").GetString());
        Assert.Equal("behavior_execution_rate_limited", errorPayload.RootElement.GetProperty("code").GetString());
        Assert.Equal(429, errorPayload.RootElement.GetProperty("statusCode").GetInt32());
        Assert.Contains("rate limit", errorPayload.RootElement.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<WebApplication> BuildRateLimitedBehaviorHttpAppAsync(string transportId)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "BehaviorHttp";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Algorithm"] = "FixedWindow";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:PermitLimit"] = "1";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:QueueLimit"] = "0";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:WindowSeconds"] = "60";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Overrides:behavior-http-pass-through:Behaviors:0"] = BehaviorId;
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Overrides:behavior-http-pass-through:Transports:0"] = transportId;
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Overrides:behavior-http-pass-through:Enabled"] = "false";
        builder.AddCephalon(engine =>
        {
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
                behaviors.Register<RateLimitedBehavior>(topology =>
                {
                    topology.AsDirect();
                    ConfigureTransport(topology, transportId);
                });
            });
        });

        var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();
        return app;
    }

    private static void ConfigureTransport(IBehaviorTopologyBuilder topology, string transportId)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);

        switch (transportId)
        {
            case "http.graphql":
                topology.ViaHttpGraphQl();
                break;
            case "http.jsonrpc":
                topology.ViaHttpJsonRpc();
                break;
            case "http.graphql-sse":
                topology.ViaHttpGraphQlSse();
                break;
            case "http.graphql-ws":
                topology.ViaHttpGraphQlWs();
                break;
            case "http.sse":
                topology.ViaHttpSse();
                break;
            case "http.ws":
                topology.ViaWebSocket();
                break;
            default:
                throw new InvalidOperationException($"Unsupported behavior HTTP transport '{transportId}'.");
        }
    }

    private static object CreateGraphqlRequest(string value)
    {
        return new
        {
            query = "query ($value: String) { handle(value: $value) { value } }",
            variables = new
            {
                value
            }
        };
    }

    private static async Task<SseMessage> ReadSseMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: false);

        string? eventName = null;
        string? data = null;

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (line.Length == 0)
            {
                if (data is not null)
                {
                    break;
                }

                continue;
            }

            if (line.StartsWith("event: ", StringComparison.Ordinal))
            {
                eventName = line["event: ".Length..];
                continue;
            }

            if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                data = line["data: ".Length..];
            }
        }

        return new SseMessage(eventName, data ?? string.Empty);
    }

    private static async Task SendWebSocketJsonAsync(
        WebSocket socket,
        object payload,
        CancellationToken cancellationToken)
    {
        var buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
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

        throw new Xunit.Sdk.XunitException("Expected a matching WebSocket message but did not receive one.");
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

    [AppBehavior(BehaviorId)]
    private sealed class RateLimitedBehavior : IAppBehavior<RateLimitedInput, RateLimitedOutput>
    {
        public Task<RateLimitedOutput> HandleAsync(
            RateLimitedInput input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RateLimitedOutput(input.Value));
        }
    }

    private sealed record RateLimitedInput(string? Value);

    private sealed record RateLimitedOutput(string? Value);

    private sealed record SseMessage(string? EventName, string Data);
}
