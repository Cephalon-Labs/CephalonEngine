using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cephalon.Abstractions.Behaviors;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Engine.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace Cephalon.Tests.Hosting;

public sealed partial class BehaviorHttpTransportResilienceHostingTests
{
    private const string BulkheadBehaviorId = "tests.bulkhead";
    private const string CircuitBreakerBehaviorId = "tests.circuit-breaker";
    private const string RateLimitedBehaviorId = "tests.rate-limited";
    private const int JsonRpcServiceUnavailableCode = -32053;
    private const int JsonRpcTooManyRequestsCode = -32029;
    private const string TimeoutBehaviorId = "tests.timeout";

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
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Overrides:behavior-http-pass-through:Behaviors:0"] = RateLimitedBehaviorId;
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Overrides:behavior-http-pass-through:Transports:0"] = transportId;
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Overrides:behavior-http-pass-through:Enabled"] = "false";
        builder.AddCephalon(engine =>
        {
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
                behaviors.Register<RateLimitedBehavior, RateLimitedInput, RateLimitedOutput>(
                    BehaviorHttpTransportJsonSerializerContext.Default.RateLimitedInput,
                    topology =>
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

    [Theory]
    [InlineData("http.graphql")]
    [InlineData("http.jsonrpc")]
    [InlineData("http.graphql-sse")]
    [InlineData("http.graphql-ws")]
    [InlineData("http.sse")]
    [InlineData("http.ws")]
    public async Task BehaviorHttpTransportsReturnProtocolTimeoutEnvelopeWhenBehaviorExecutionTimeoutApplies(string transportId)
    {
        await AssertTimeoutTransportEnvelopeAsync(transportId);
    }

    [Theory]
    [InlineData("http.graphql")]
    [InlineData("http.jsonrpc")]
    [InlineData("http.graphql-sse")]
    [InlineData("http.graphql-ws")]
    [InlineData("http.sse")]
    [InlineData("http.ws")]
    public async Task BehaviorHttpTransportsReturnProtocolCircuitBreakerEnvelopeWhenBehaviorExecutionCircuitBreakerOpens(string transportId)
    {
        await AssertCircuitBreakerTransportEnvelopeAsync(transportId);
    }

    [Theory]
    [InlineData("http.graphql")]
    [InlineData("http.jsonrpc")]
    [InlineData("http.graphql-sse")]
    [InlineData("http.graphql-ws")]
    [InlineData("http.sse")]
    [InlineData("http.ws")]
    public async Task BehaviorHttpTransportsReturnProtocolBulkheadEnvelopeWhenBehaviorExecutionBulkheadSaturates(string transportId)
    {
        await AssertBulkheadTransportEnvelopeAsync(transportId);
    }

    private static async Task<WebApplication> BuildBulkheadBehaviorHttpAppAsync(string transportId)
    {
        BulkheadProbe.Reset();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "BehaviorHttp";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:MaxConcurrentExecutions"] = "1";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:MaxQueuedActions"] = "0";
        builder.AddCephalon(engine =>
        {
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
                behaviors.Register<BulkheadBehavior, BulkheadInput, BulkheadOutput>(
                    BehaviorHttpTransportJsonSerializerContext.Default.BulkheadInput,
                    topology =>
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

    private static async Task<WebApplication> BuildTimeoutBehaviorHttpAppAsync(string transportId)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "BehaviorHttp";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:TotalTimeoutSeconds"] = "1";
        builder.AddCephalon(engine =>
        {
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
                behaviors.Register<TimeoutBehavior, SlowInput, SlowOutput>(
                    BehaviorHttpTransportJsonSerializerContext.Default.SlowInput,
                    topology =>
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

    private static async Task<WebApplication> BuildCircuitBreakerBehaviorHttpAppAsync(string transportId)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "BehaviorHttp";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:TotalTimeoutSeconds"] = "1";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:FailureRatio"] = "0.5";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:MinimumThroughput"] = "2";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:SamplingDurationSeconds"] = "30";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:BreakDurationSeconds"] = "20";
        builder.AddCephalon(engine =>
        {
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
                behaviors.Register<CircuitBreakerBehavior, SlowInput, SlowOutput>(
                    BehaviorHttpTransportJsonSerializerContext.Default.SlowInput,
                    topology =>
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

    private static async Task AssertBulkheadTransportEnvelopeAsync(string transportId)
    {
        switch (transportId)
        {
            case "http.graphql":
            {
                await using var app = await BuildBulkheadBehaviorHttpAppAsync(transportId);
                var client = app.GetTestClient();
                var firstRequest = client.PostAsJsonAsync("/graphql/v1/tests/bulkhead", CreateGraphqlRequest("alpha"));
                await BulkheadProbe.WaitUntilStartedAsync();

                try
                {
                    var rejectedResponse = await client.PostAsJsonAsync(
                        "/graphql/v1/tests/bulkhead",
                        CreateGraphqlRequest("beta"));
                    var rejectedPayload = await rejectedResponse.Content.ReadFromJsonAsync<JsonElement>();

                    Assert.Equal(HttpStatusCode.OK, rejectedResponse.StatusCode);
                    AssertGraphqlError(
                        Assert.Single(rejectedPayload.GetProperty("errors").EnumerateArray()),
                        "behavior_execution_rejected",
                        429,
                        "concurrency");
                }
                finally
                {
                    BulkheadProbe.Release();
                }

                var firstResponse = await firstRequest;
                var firstPayload = await firstResponse.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
                Assert.Equal("alpha", firstPayload.GetProperty("data").GetProperty("value").GetString());
                break;
            }
            case "http.jsonrpc":
            {
                await using var app = await BuildBulkheadBehaviorHttpAppAsync(transportId);
                var client = app.GetTestClient();
                var firstRequest = client.PostAsJsonAsync(
                    "/json-rpc/v1/tests/bulkhead",
                    CreateJsonRpcRequest("req-1", "alpha"));
                await BulkheadProbe.WaitUntilStartedAsync();

                try
                {
                    var rejectedResponse = await client.PostAsJsonAsync(
                        "/json-rpc/v1/tests/bulkhead",
                        CreateJsonRpcRequest("req-2", "beta"));
                    var rejectedPayload = await rejectedResponse.Content.ReadFromJsonAsync<JsonElement>();

                    Assert.Equal(HttpStatusCode.OK, rejectedResponse.StatusCode);
                    AssertJsonRpcTooManyRequests(
                        rejectedPayload,
                        "behavior_execution_rejected",
                        "concurrency",
                        expectRetryAfter: false);
                }
                finally
                {
                    BulkheadProbe.Release();
                }

                var firstResponse = await firstRequest;
                var firstPayload = await firstResponse.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
                Assert.Equal("alpha", firstPayload.GetProperty("result").GetProperty("Value").GetString());
                break;
            }
            case "http.graphql-sse":
            {
                await using var app = await BuildBulkheadBehaviorHttpAppAsync(transportId);
                var client = app.GetTestClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/graphql-sse/v1/tests/bulkhead");
                firstRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                firstRequest.Content = JsonContent.Create(CreateGraphqlRequest("alpha"));
                var firstResponseTask = client.SendAsync(firstRequest, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                await BulkheadProbe.WaitUntilStartedAsync();

                try
                {
                    using var rejectedRequest = new HttpRequestMessage(
                        HttpMethod.Post,
                        "/graphql-sse/v1/tests/bulkhead");
                    rejectedRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                    rejectedRequest.Content = JsonContent.Create(CreateGraphqlRequest("beta"));
                    using var rejectedResponse = await client.SendAsync(
                        rejectedRequest,
                        HttpCompletionOption.ResponseHeadersRead,
                        cts.Token);
                    var rejectedEvent = await ReadSseMessageAsync(rejectedResponse, cts.Token);
                    using var rejectedPayload = JsonDocument.Parse(rejectedEvent.Data);

                    Assert.Equal("text/event-stream", rejectedResponse.Content.Headers.ContentType?.MediaType);
                    Assert.Equal("next", rejectedEvent.EventName);
                    AssertGraphqlError(
                        Assert.Single(rejectedPayload.RootElement.GetProperty("errors").EnumerateArray()),
                        "behavior_execution_rejected",
                        429,
                        "concurrency");
                }
                finally
                {
                    BulkheadProbe.Release();
                }

                using var firstResponse = await firstResponseTask;
                var firstEvent = await ReadSseMessageAsync(firstResponse, cts.Token);
                using var firstPayload = JsonDocument.Parse(firstEvent.Data);
                Assert.Equal("text/event-stream", firstResponse.Content.Headers.ContentType?.MediaType);
                Assert.Equal("next", firstEvent.EventName);
                Assert.Equal("alpha", firstPayload.RootElement.GetProperty("data").GetProperty("Value").GetString());
                break;
            }
            case "http.graphql-ws":
            {
                await using var app = await BuildBulkheadBehaviorHttpAppAsync(transportId);
                var webSocketClient = app.GetTestServer().CreateWebSocketClient();
                webSocketClient.SubProtocols.Add("graphql-transport-ws");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var firstSocket = await webSocketClient.ConnectAsync(
                    new Uri("ws://localhost/graphql-ws/v1/tests/bulkhead"),
                    cts.Token);
                using var rejectedSocket = await webSocketClient.ConnectAsync(
                    new Uri("ws://localhost/graphql-ws/v1/tests/bulkhead"),
                    cts.Token);

                await InitializeGraphqlWebSocketAsync(firstSocket, cts.Token);
                await InitializeGraphqlWebSocketAsync(rejectedSocket, cts.Token);
                await SendWebSocketJsonAsync(
                    firstSocket,
                    new
                    {
                        id = "req-1",
                        type = "subscribe",
                        payload = CreateGraphqlRequest("alpha")
                    },
                    cts.Token);
                await BulkheadProbe.WaitUntilStartedAsync();

                try
                {
                    using var rejectedPayload = await SubscribeGraphqlWsErrorAsync(
                        rejectedSocket,
                        "req-2",
                        "beta",
                        cts.Token);
                    AssertGraphqlError(
                        Assert.Single(rejectedPayload.RootElement.GetProperty("payload").EnumerateArray()),
                        "behavior_execution_rejected",
                        429,
                        "concurrency");
                }
                finally
                {
                    BulkheadProbe.Release();
                }

                var successMessage = await ReceiveWebSocketMessageMatchingAsync(
                    firstSocket,
                    message => message.Contains("\"type\":\"next\"", StringComparison.Ordinal) &&
                        message.Contains("\"id\":\"req-1\"", StringComparison.Ordinal),
                    cts.Token);
                using var successPayload = JsonDocument.Parse(successMessage);
                Assert.Equal("alpha", successPayload.RootElement
                    .GetProperty("payload")
                    .GetProperty("data")
                    .GetProperty("Value")
                    .GetString());
                break;
            }
            case "http.sse":
            {
                await using var app = await BuildBulkheadBehaviorHttpAppAsync(transportId);
                var client = app.GetTestClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var firstRequest = new HttpRequestMessage(
                    HttpMethod.Get,
                    "/sse/v1/tests/bulkhead?value=alpha");
                var firstResponseTask = client.SendAsync(firstRequest, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                await BulkheadProbe.WaitUntilStartedAsync();

                try
                {
                    using var rejectedRequest = new HttpRequestMessage(
                        HttpMethod.Get,
                        "/sse/v1/tests/bulkhead?value=beta");
                    using var rejectedResponse = await client.SendAsync(
                        rejectedRequest,
                        HttpCompletionOption.ResponseHeadersRead,
                        cts.Token);
                    var rejectedEvent = await ReadSseMessageAsync(rejectedResponse, cts.Token);
                    using var rejectedPayload = JsonDocument.Parse(rejectedEvent.Data);

                    Assert.Equal("text/event-stream", rejectedResponse.Content.Headers.ContentType?.MediaType);
                    Assert.Equal("error", rejectedEvent.EventName);
                    AssertStreamingError(
                        rejectedPayload.RootElement,
                        "behavior_execution_rejected",
                        429,
                        "concurrency");
                }
                finally
                {
                    BulkheadProbe.Release();
                }

                using var firstResponse = await firstResponseTask;
                var firstEvent = await ReadSseMessageAsync(firstResponse, cts.Token);
                using var firstPayload = JsonDocument.Parse(firstEvent.Data);
                Assert.Equal("text/event-stream", firstResponse.Content.Headers.ContentType?.MediaType);
                Assert.Equal("result", firstEvent.EventName);
                Assert.Equal("alpha", firstPayload.RootElement.GetProperty("Value").GetString());
                break;
            }
            case "http.ws":
            {
                await using var app = await BuildBulkheadBehaviorHttpAppAsync(transportId);
                var webSocketClient = app.GetTestServer().CreateWebSocketClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var firstSocket = await webSocketClient.ConnectAsync(
                    new Uri("ws://localhost/ws/v1/tests/bulkhead"),
                    cts.Token);
                using var rejectedSocket = await webSocketClient.ConnectAsync(
                    new Uri("ws://localhost/ws/v1/tests/bulkhead"),
                    cts.Token);
                await SendWebSocketJsonAsync(firstSocket, new { value = "alpha" }, cts.Token);
                await BulkheadProbe.WaitUntilStartedAsync();

                try
                {
                    using var rejectedPayload = await SendWebSocketRequestAndReadJsonAsync(
                        rejectedSocket,
                        "beta",
                        cts.Token);
                    AssertStreamingError(
                        rejectedPayload.RootElement,
                        "behavior_execution_rejected",
                        429,
                        "concurrency");
                }
                finally
                {
                    BulkheadProbe.Release();
                }

                var successMessage = await ReceiveWebSocketTextAsync(firstSocket, cts.Token);
                using var successPayload = JsonDocument.Parse(successMessage);
                Assert.Equal("alpha", successPayload.RootElement.GetProperty("Value").GetString());
                break;
            }
            default:
                throw new InvalidOperationException($"Unsupported behavior HTTP transport '{transportId}'.");
        }
    }

    private static async Task AssertTimeoutTransportEnvelopeAsync(string transportId)
    {
        switch (transportId)
        {
            case "http.graphql":
            {
                await using var app = await BuildTimeoutBehaviorHttpAppAsync(transportId);
                var client = app.GetTestClient();

                var response = await client.PostAsJsonAsync("/graphql/v1/tests/timeout", CreateGraphqlRequest("alpha"));
                var payload = await response.Content.ReadFromJsonAsync<JsonElement>();

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                AssertGraphqlError(
                    Assert.Single(payload.GetProperty("errors").EnumerateArray()),
                    "behavior_execution_timeout",
                    503,
                    "timeout");
                break;
            }
            case "http.jsonrpc":
            {
                await using var app = await BuildTimeoutBehaviorHttpAppAsync(transportId);
                var client = app.GetTestClient();

                var response = await client.PostAsJsonAsync(
                    "/json-rpc/v1/tests/timeout",
                    CreateJsonRpcRequest("req-1", "alpha"));
                var payload = await response.Content.ReadFromJsonAsync<JsonElement>();

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                AssertJsonRpcServiceUnavailable(
                    payload,
                    "behavior_execution_timeout",
                    "timeout",
                    expectRetryAfter: false);
                break;
            }
            case "http.graphql-sse":
            {
                await using var app = await BuildTimeoutBehaviorHttpAppAsync(transportId);
                var client = app.GetTestClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

                using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql-sse/v1/tests/timeout");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                request.Content = JsonContent.Create(CreateGraphqlRequest("alpha"));
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                var message = await ReadSseMessageAsync(response, cts.Token);
                using var payload = JsonDocument.Parse(message.Data);

                Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
                Assert.Equal("next", message.EventName);
                AssertGraphqlError(
                    Assert.Single(payload.RootElement.GetProperty("errors").EnumerateArray()),
                    "behavior_execution_timeout",
                    503,
                    "timeout");
                break;
            }
            case "http.graphql-ws":
            {
                await using var app = await BuildTimeoutBehaviorHttpAppAsync(transportId);
                var webSocketClient = app.GetTestServer().CreateWebSocketClient();
                webSocketClient.SubProtocols.Add("graphql-transport-ws");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var socket = await webSocketClient.ConnectAsync(
                    new Uri("ws://localhost/graphql-ws/v1/tests/timeout"),
                    cts.Token);

                await InitializeGraphqlWebSocketAsync(socket, cts.Token);
                await SendWebSocketJsonAsync(
                    socket,
                    new
                    {
                        id = "req-1",
                        type = "subscribe",
                        payload = CreateGraphqlRequest("alpha")
                    },
                    cts.Token);
                var message = await ReceiveWebSocketMessageMatchingAsync(
                    socket,
                    text => text.Contains("\"type\":\"error\"", StringComparison.Ordinal) &&
                        text.Contains("\"id\":\"req-1\"", StringComparison.Ordinal),
                    cts.Token);
                using var payload = JsonDocument.Parse(message);

                AssertGraphqlError(
                    Assert.Single(payload.RootElement.GetProperty("payload").EnumerateArray()),
                    "behavior_execution_timeout",
                    503,
                    "timeout");
                break;
            }
            case "http.sse":
            {
                await using var app = await BuildTimeoutBehaviorHttpAppAsync(transportId);
                var client = app.GetTestClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

                using var request = new HttpRequestMessage(HttpMethod.Get, "/sse/v1/tests/timeout?value=alpha");
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                var message = await ReadSseMessageAsync(response, cts.Token);
                using var payload = JsonDocument.Parse(message.Data);

                Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
                Assert.Equal("error", message.EventName);
                AssertStreamingError(payload.RootElement, "behavior_execution_timeout", 503, "timeout");
                break;
            }
            case "http.ws":
            {
                await using var app = await BuildTimeoutBehaviorHttpAppAsync(transportId);
                var webSocketClient = app.GetTestServer().CreateWebSocketClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var socket = await webSocketClient.ConnectAsync(
                    new Uri("ws://localhost/ws/v1/tests/timeout"),
                    cts.Token);

                await SendWebSocketJsonAsync(socket, new { value = "alpha" }, cts.Token);
                var message = await ReceiveWebSocketTextAsync(socket, cts.Token);
                using var payload = JsonDocument.Parse(message);

                AssertStreamingError(payload.RootElement, "behavior_execution_timeout", 503, "timeout");
                break;
            }
            default:
                throw new InvalidOperationException($"Unsupported behavior HTTP transport '{transportId}'.");
        }
    }

    private static async Task AssertCircuitBreakerTransportEnvelopeAsync(string transportId)
    {
        switch (transportId)
        {
            case "http.graphql":
            {
                await using var app = await BuildCircuitBreakerBehaviorHttpAppAsync(transportId);
                var client = app.GetTestClient();

                var firstPayload = await PostGraphqlAsync(client, "/graphql/v1/tests/circuit-breaker", "alpha");
                var secondPayload = await PostGraphqlAsync(client, "/graphql/v1/tests/circuit-breaker", "beta");
                var openCircuitPayload = await PostGraphqlAsync(client, "/graphql/v1/tests/circuit-breaker", "gamma");

                AssertGraphqlError(
                    Assert.Single(firstPayload.GetProperty("errors").EnumerateArray()),
                    "behavior_execution_timeout",
                    503,
                    "timeout");
                AssertGraphqlError(
                    Assert.Single(secondPayload.GetProperty("errors").EnumerateArray()),
                    "behavior_execution_timeout",
                    503,
                    "timeout");
                AssertGraphqlError(
                    Assert.Single(openCircuitPayload.GetProperty("errors").EnumerateArray()),
                    "behavior_execution_circuit_breaker_open",
                    503,
                    "circuit breaker",
                    expectRetryAfter: true);
                break;
            }
            case "http.jsonrpc":
            {
                await using var app = await BuildCircuitBreakerBehaviorHttpAppAsync(transportId);
                var client = app.GetTestClient();

                var firstPayload = await PostJsonRpcAsync(client, "/json-rpc/v1/tests/circuit-breaker", "req-1", "alpha");
                var secondPayload = await PostJsonRpcAsync(client, "/json-rpc/v1/tests/circuit-breaker", "req-2", "beta");
                var openCircuitPayload = await PostJsonRpcAsync(client, "/json-rpc/v1/tests/circuit-breaker", "req-3", "gamma");

                AssertJsonRpcServiceUnavailable(firstPayload, "behavior_execution_timeout", "timeout", expectRetryAfter: false);
                AssertJsonRpcServiceUnavailable(secondPayload, "behavior_execution_timeout", "timeout", expectRetryAfter: false);
                AssertJsonRpcServiceUnavailable(
                    openCircuitPayload,
                    "behavior_execution_circuit_breaker_open",
                    "circuit breaker",
                    expectRetryAfter: true);
                break;
            }
            case "http.graphql-sse":
            {
                await using var app = await BuildCircuitBreakerBehaviorHttpAppAsync(transportId);
                var client = app.GetTestClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

                using var firstPayload = await PostGraphqlSseAsync(client, "/graphql-sse/v1/tests/circuit-breaker", "alpha", cts.Token);
                using var secondPayload = await PostGraphqlSseAsync(client, "/graphql-sse/v1/tests/circuit-breaker", "beta", cts.Token);
                using var openCircuitPayload = await PostGraphqlSseAsync(client, "/graphql-sse/v1/tests/circuit-breaker", "gamma", cts.Token);

                AssertGraphqlError(
                    Assert.Single(firstPayload.RootElement.GetProperty("errors").EnumerateArray()),
                    "behavior_execution_timeout",
                    503,
                    "timeout");
                AssertGraphqlError(
                    Assert.Single(secondPayload.RootElement.GetProperty("errors").EnumerateArray()),
                    "behavior_execution_timeout",
                    503,
                    "timeout");
                AssertGraphqlError(
                    Assert.Single(openCircuitPayload.RootElement.GetProperty("errors").EnumerateArray()),
                    "behavior_execution_circuit_breaker_open",
                    503,
                    "circuit breaker",
                    expectRetryAfter: true);
                break;
            }
            case "http.graphql-ws":
            {
                await using var app = await BuildCircuitBreakerBehaviorHttpAppAsync(transportId);
                var webSocketClient = app.GetTestServer().CreateWebSocketClient();
                webSocketClient.SubProtocols.Add("graphql-transport-ws");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var socket = await webSocketClient.ConnectAsync(
                    new Uri("ws://localhost/graphql-ws/v1/tests/circuit-breaker"),
                    cts.Token);

                await InitializeGraphqlWebSocketAsync(socket, cts.Token);
                using var firstPayload = await SubscribeGraphqlWsErrorAsync(socket, "req-1", "alpha", cts.Token);
                using var secondPayload = await SubscribeGraphqlWsErrorAsync(socket, "req-2", "beta", cts.Token);
                using var openCircuitPayload = await SubscribeGraphqlWsErrorAsync(socket, "req-3", "gamma", cts.Token);

                AssertGraphqlError(
                    Assert.Single(firstPayload.RootElement.GetProperty("payload").EnumerateArray()),
                    "behavior_execution_timeout",
                    503,
                    "timeout");
                AssertGraphqlError(
                    Assert.Single(secondPayload.RootElement.GetProperty("payload").EnumerateArray()),
                    "behavior_execution_timeout",
                    503,
                    "timeout");
                AssertGraphqlError(
                    Assert.Single(openCircuitPayload.RootElement.GetProperty("payload").EnumerateArray()),
                    "behavior_execution_circuit_breaker_open",
                    503,
                    "circuit breaker",
                    expectRetryAfter: true);
                break;
            }
            case "http.sse":
            {
                await using var app = await BuildCircuitBreakerBehaviorHttpAppAsync(transportId);
                var client = app.GetTestClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

                using var firstPayload = await GetStreamingErrorAsync(client, "/sse/v1/tests/circuit-breaker?value=alpha", cts.Token);
                using var secondPayload = await GetStreamingErrorAsync(client, "/sse/v1/tests/circuit-breaker?value=beta", cts.Token);
                using var openCircuitPayload = await GetStreamingErrorAsync(client, "/sse/v1/tests/circuit-breaker?value=gamma", cts.Token);

                AssertStreamingError(firstPayload.RootElement, "behavior_execution_timeout", 503, "timeout");
                AssertStreamingError(secondPayload.RootElement, "behavior_execution_timeout", 503, "timeout");
                AssertStreamingError(
                    openCircuitPayload.RootElement,
                    "behavior_execution_circuit_breaker_open",
                    503,
                    "circuit breaker",
                    expectRetryAfter: true);
                break;
            }
            case "http.ws":
            {
                await using var app = await BuildCircuitBreakerBehaviorHttpAppAsync(transportId);
                var webSocketClient = app.GetTestServer().CreateWebSocketClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var socket = await webSocketClient.ConnectAsync(
                    new Uri("ws://localhost/ws/v1/tests/circuit-breaker"),
                    cts.Token);

                using var firstPayload = await SendWebSocketRequestAndReadJsonAsync(socket, "alpha", cts.Token);
                using var secondPayload = await SendWebSocketRequestAndReadJsonAsync(socket, "beta", cts.Token);
                using var openCircuitPayload = await SendWebSocketRequestAndReadJsonAsync(socket, "gamma", cts.Token);

                AssertStreamingError(firstPayload.RootElement, "behavior_execution_timeout", 503, "timeout");
                AssertStreamingError(secondPayload.RootElement, "behavior_execution_timeout", 503, "timeout");
                AssertStreamingError(
                    openCircuitPayload.RootElement,
                    "behavior_execution_circuit_breaker_open",
                    503,
                    "circuit breaker",
                    expectRetryAfter: true);
                break;
            }
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

    private static object CreateJsonRpcRequest(string requestId, string value)
    {
        return new
        {
            jsonrpc = "2.0",
            method = "handle",
            @params = new { value },
            id = requestId
        };
    }

    private static async Task<JsonElement> PostGraphqlAsync(HttpClient client, string route, string value)
    {
        var response = await client.PostAsJsonAsync(route, CreateGraphqlRequest(value));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> PostJsonRpcAsync(HttpClient client, string route, string requestId, string value)
    {
        var response = await client.PostAsJsonAsync(route, CreateJsonRpcRequest(requestId, value));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonDocument> PostGraphqlSseAsync(
        HttpClient client,
        string route,
        string value,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, route);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Content = JsonContent.Create(CreateGraphqlRequest(value));
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        var message = await ReadSseMessageAsync(response, cancellationToken);
        Assert.Equal("next", message.EventName);
        return JsonDocument.Parse(message.Data);
    }

    private static async Task<JsonDocument> GetStreamingErrorAsync(
        HttpClient client,
        string route,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        var message = await ReadSseMessageAsync(response, cancellationToken);
        Assert.Equal("error", message.EventName);
        return JsonDocument.Parse(message.Data);
    }

    private static async Task InitializeGraphqlWebSocketAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        await SendWebSocketJsonAsync(socket, new { type = "connection_init" }, cancellationToken);
        await ReceiveWebSocketMessageMatchingAsync(
            socket,
            message => message.Contains("\"type\":\"connection_ack\"", StringComparison.Ordinal),
            cancellationToken);
    }

    private static async Task<JsonDocument> SubscribeGraphqlWsErrorAsync(
        WebSocket socket,
        string requestId,
        string value,
        CancellationToken cancellationToken)
    {
        await SendWebSocketJsonAsync(
            socket,
            new
            {
                id = requestId,
                type = "subscribe",
                payload = CreateGraphqlRequest(value)
            },
            cancellationToken);
        var message = await ReceiveWebSocketMessageMatchingAsync(
            socket,
            text => text.Contains("\"type\":\"error\"", StringComparison.Ordinal) &&
                text.Contains($"\"id\":\"{requestId}\"", StringComparison.Ordinal),
            cancellationToken);
        return JsonDocument.Parse(message);
    }

    private static async Task<JsonDocument> SendWebSocketRequestAndReadJsonAsync(
        WebSocket socket,
        string value,
        CancellationToken cancellationToken)
    {
        await SendWebSocketJsonAsync(socket, new { value }, cancellationToken);
        var message = await ReceiveWebSocketTextAsync(socket, cancellationToken);
        return JsonDocument.Parse(message);
    }

    private static void AssertGraphqlError(
        JsonElement error,
        string expectedCephalonCode,
        int expectedStatusCode,
        string expectedMessageFragment,
        bool expectRetryAfter = false)
    {
        Assert.Equal(expectedCephalonCode, error.GetProperty("extensions").GetProperty("cephalonCode").GetString());
        Assert.Equal(expectedCephalonCode.ToUpperInvariant(), error.GetProperty("extensions").GetProperty("code").GetString());
        Assert.Equal(expectedStatusCode, error.GetProperty("extensions").GetProperty("statusCode").GetInt32());
        Assert.Contains(expectedMessageFragment, error.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
        if (expectRetryAfter)
        {
            Assert.True(error.GetProperty("extensions").GetProperty("retryAfterSeconds").GetInt32() > 0);
        }
        else
        {
            Assert.False(error.GetProperty("extensions").TryGetProperty("retryAfterSeconds", out _));
        }
    }

    private static void AssertStreamingError(
        JsonElement payload,
        string expectedCode,
        int expectedStatusCode,
        string expectedMessageFragment,
        bool expectRetryAfter = false)
    {
        Assert.Equal(expectedCode, payload.GetProperty("code").GetString());
        Assert.Equal(expectedStatusCode, payload.GetProperty("statusCode").GetInt32());
        Assert.Contains(expectedMessageFragment, payload.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
        if (expectRetryAfter)
        {
            Assert.True(payload.GetProperty("retryAfterSeconds").GetInt32() > 0);
        }
        else
        {
            Assert.False(payload.TryGetProperty("retryAfterSeconds", out _));
        }
    }

    private static void AssertJsonRpcServiceUnavailable(
        JsonElement payload,
        string expectedCephalonCode,
        string expectedMessageFragment,
        bool expectRetryAfter)
    {
        Assert.Equal(JsonRpcServiceUnavailableCode, payload.GetProperty("error").GetProperty("code").GetInt32());
        Assert.Equal("Service unavailable", payload.GetProperty("error").GetProperty("message").GetString());
        var data = payload.GetProperty("error").GetProperty("data").GetString();
        Assert.Contains(expectedCephalonCode, data, StringComparison.Ordinal);
        Assert.Contains(expectedMessageFragment, data, StringComparison.OrdinalIgnoreCase);
        if (expectRetryAfter)
        {
            Assert.Contains("Retry after", data, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.DoesNotContain("Retry after", data, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void AssertJsonRpcTooManyRequests(
        JsonElement payload,
        string expectedCephalonCode,
        string expectedMessageFragment,
        bool expectRetryAfter)
    {
        Assert.Equal(JsonRpcTooManyRequestsCode, payload.GetProperty("error").GetProperty("code").GetInt32());
        Assert.Equal("Too many requests", payload.GetProperty("error").GetProperty("message").GetString());
        var data = payload.GetProperty("error").GetProperty("data").GetString();
        Assert.Contains(expectedCephalonCode, data, StringComparison.Ordinal);
        Assert.Contains(expectedMessageFragment, data, StringComparison.OrdinalIgnoreCase);
        if (expectRetryAfter)
        {
            Assert.Contains("Retry after", data, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.DoesNotContain("Retry after", data, StringComparison.OrdinalIgnoreCase);
        }
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

    [AppBehavior(RateLimitedBehaviorId)]
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

    [AppBehavior(BulkheadBehaviorId)]
    private sealed class BulkheadBehavior : IAppBehavior<BulkheadInput, BulkheadOutput>
    {
        public async Task<BulkheadOutput> HandleAsync(
            BulkheadInput input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            BulkheadProbe.MarkStarted();
            await BulkheadProbe.WaitForReleaseAsync(cancellationToken);
            return new BulkheadOutput(input.Value);
        }
    }

    private sealed record BulkheadInput(string? Value);

    private sealed record BulkheadOutput(string? Value);

    [AppBehavior(TimeoutBehaviorId)]
    private sealed class TimeoutBehavior : IAppBehavior<SlowInput, SlowOutput>
    {
        public async Task<SlowOutput> HandleAsync(
            SlowInput input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            return new SlowOutput(input.Value);
        }
    }

    [AppBehavior(CircuitBreakerBehaviorId)]
    private sealed class CircuitBreakerBehavior : IAppBehavior<SlowInput, SlowOutput>
    {
        public async Task<SlowOutput> HandleAsync(
            SlowInput input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            return new SlowOutput(input.Value);
        }
    }

    private sealed record SlowInput(string? Value);

    private sealed record SlowOutput(string? Value);

    private sealed record SseMessage(string? EventName, string Data);

    private static class BulkheadProbe
    {
        private static TaskCompletionSource<bool> releaseSignal = CreateCompletionSource();
        private static TaskCompletionSource<bool> startedSignal = CreateCompletionSource();

        public static void Reset()
        {
            releaseSignal = CreateCompletionSource();
            startedSignal = CreateCompletionSource();
        }

        public static void MarkStarted()
        {
            startedSignal.TrySetResult(true);
        }

        public static Task<bool> WaitUntilStartedAsync()
        {
            return startedSignal.Task;
        }

        public static Task<bool> WaitForReleaseAsync(CancellationToken cancellationToken)
        {
            return releaseSignal.Task.WaitAsync(cancellationToken);
        }

        public static void Release()
        {
            releaseSignal.TrySetResult(true);
        }

        private static TaskCompletionSource<bool> CreateCompletionSource()
        {
            return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    [JsonSerializable(typeof(BulkheadInput))]
    [JsonSerializable(typeof(RateLimitedInput))]
    [JsonSerializable(typeof(SlowInput))]
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    private sealed partial class BehaviorHttpTransportJsonSerializerContext : JsonSerializerContext;
}
