using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Transports.ServerSentEvents;
using Cephalon.AspNetCore.Transports.WebSockets;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

public sealed class DirectStreamingModuleResilienceHostingTests
{
    [Fact]
    public async Task SseDirectModule_ReturnsErrorEvent_WhenTimeoutApplies()
    {
        await using var app = await BuildHostAsync("ServerSentEvents", enableTimeout: true);
        var client = app.GetTestClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        using var response = await SendSseAsync(client, "/sse/resilience/slow", cts.Token);
        var message = await ReadSseMessageAsync(response, cts.Token);
        using var payload = JsonDocument.Parse(message.Data);

        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("error", message.EventName);
        AssertStreamingError(payload.RootElement, "sse_execution_timeout", 503, "timeout");
    }

    [Fact]
    public async Task SseDirectModule_ReturnsErrorEvent_WhenCircuitBreakerOpens()
    {
        await using var app = await BuildHostAsync(
            "ServerSentEvents",
            enableTimeout: true,
            enableCircuitBreaker: true);
        var client = app.GetTestClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        using var firstResponse = await SendSseAsync(client, "/sse/resilience/slow", cts.Token);
        _ = await ReadSseMessageAsync(firstResponse, cts.Token);
        using var openCircuitResponse = await SendSseAsync(client, "/sse/resilience/echo", cts.Token);
        var openCircuitMessage = await ReadSseMessageAsync(openCircuitResponse, cts.Token);
        using var openCircuitPayload = JsonDocument.Parse(openCircuitMessage.Data);

        Assert.True(openCircuitResponse.Headers.RetryAfter?.Delta?.TotalSeconds > 0);
        Assert.Equal("error", openCircuitMessage.EventName);
        AssertStreamingError(
            openCircuitPayload.RootElement,
            "sse_circuit_breaker_open",
            503,
            "circuit breaker",
            expectRetryAfter: true);
    }

    [Fact]
    public async Task SseDirectModule_ReturnsErrorEvent_WhenBulkheadSaturates()
    {
        await using var app = await BuildHostAsync("ServerSentEvents", enableBulkhead: true);
        var client = app.GetTestClient();
        var gate = app.Services.GetRequiredService<DirectStreamingBulkheadTestGate>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var heldResponseTask = SendSseAsync(client, "/sse/resilience/bulkhead", cts.Token);
        await gate.WaitUntilStartedAsync();

        using var rejectedResponse = await SendSseAsync(client, "/sse/resilience/bulkhead", cts.Token);
        var rejectedMessage = await ReadSseMessageAsync(rejectedResponse, cts.Token);
        using var rejectedPayload = JsonDocument.Parse(rejectedMessage.Data);

        gate.Release();
        using var heldResponse = await heldResponseTask;
        var heldMessage = await ReadSseMessageAsync(heldResponse, cts.Token);
        using var heldPayload = JsonDocument.Parse(heldMessage.Data);

        Assert.Equal("error", rejectedMessage.EventName);
        AssertStreamingError(rejectedPayload.RootElement, "sse_bulkhead_rejected", 429, "concurrency");
        Assert.Equal("result", heldMessage.EventName);
        Assert.Equal("held", heldPayload.RootElement.GetProperty("value").GetString());
    }

    [Fact]
    public async Task WebSocketDirectModule_ReturnsErrorFrame_WhenTimeoutApplies()
    {
        await using var app = await BuildHostAsync("WebSocket", enableTimeout: true);
        var webSocketClient = app.GetTestServer().CreateWebSocketClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var socket = await webSocketClient.ConnectAsync(new Uri("ws://localhost/ws/resilience/slow"), cts.Token);

        using var payload = await ReceiveWebSocketJsonAsync(socket, cts.Token);

        AssertStreamingError(payload.RootElement, "websocket_execution_timeout", 503, "timeout");
    }

    [Fact]
    public async Task WebSocketDirectModule_ReturnsErrorFrame_WhenCircuitBreakerOpens()
    {
        await using var app = await BuildHostAsync(
            "WebSocket",
            enableTimeout: true,
            enableCircuitBreaker: true);
        var webSocketClient = app.GetTestServer().CreateWebSocketClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        using var firstSocket = await webSocketClient.ConnectAsync(new Uri("ws://localhost/ws/resilience/slow"), cts.Token);
        using var firstPayload = await ReceiveWebSocketJsonAsync(firstSocket, cts.Token);
        using var openCircuitSocket = await webSocketClient.ConnectAsync(new Uri("ws://localhost/ws/resilience/echo"), cts.Token);
        using var openCircuitPayload = await ReceiveWebSocketJsonAsync(openCircuitSocket, cts.Token);

        AssertStreamingError(firstPayload.RootElement, "websocket_execution_timeout", 503, "timeout");
        AssertStreamingError(
            openCircuitPayload.RootElement,
            "websocket_circuit_breaker_open",
            503,
            "circuit breaker",
            expectRetryAfter: true);
    }

    [Fact]
    public async Task WebSocketDirectModule_ReturnsErrorFrame_WhenBulkheadSaturates()
    {
        await using var app = await BuildHostAsync("WebSocket", enableBulkhead: true);
        var webSocketClient = app.GetTestServer().CreateWebSocketClient();
        var gate = app.Services.GetRequiredService<DirectStreamingBulkheadTestGate>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        using var heldSocket = await webSocketClient.ConnectAsync(new Uri("ws://localhost/ws/resilience/bulkhead"), cts.Token);
        await gate.WaitUntilStartedAsync();
        using var rejectedSocket = await webSocketClient.ConnectAsync(new Uri("ws://localhost/ws/resilience/bulkhead"), cts.Token);
        using var rejectedPayload = await ReceiveWebSocketJsonAsync(rejectedSocket, cts.Token);

        gate.Release();
        using var heldPayload = await ReceiveWebSocketJsonAsync(heldSocket, cts.Token);

        AssertStreamingError(rejectedPayload.RootElement, "websocket_bulkhead_rejected", 429, "concurrency");
        Assert.Equal("held", heldPayload.RootElement.GetProperty("value").GetString());
    }

    [Fact]
    public async Task StreamingTransports_ReportDirectModuleResilienceRuntimeSurfaces()
    {
        await using var app = await BuildHostAsync(
            ["ServerSentEvents", "WebSocket"],
            enableTimeout: true,
            enableCircuitBreaker: true,
            enableBulkhead: true);
        var client = app.GetTestClient();

        var sseSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/server-sent-events");
        var webSocketSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/websocket");
        var sseEntry = Assert.Single(Assert.Single(sseSurfaces ?? []).Entries);
        var webSocketEntry = Assert.Single(Assert.Single(webSocketSurfaces ?? []).Entries);

        Assert.Equal("sse-direct-module-resilience", sseEntry.Id);
        Assert.Equal("aspnetcore-streaming-endpoint-filter", sseEntry.Metadata["executionMode"]);
        Assert.Equal("Engine:Resilience", sseEntry.Metadata["policySource"]);
        Assert.Equal("direct-sse-module-endpoints", sseEntry.Metadata["scope"]);
        Assert.Equal("sse-error-event", sseEntry.Metadata["protocolEnvelope"]);
        Assert.Equal("false", sseEntry.Metadata["wolverineRequired"]);
        Assert.Equal("false", sseEntry.Metadata["consumerCodeRequired"]);
        Assert.Equal("True", sseEntry.Metadata["timeoutEnabled"]);
        Assert.Equal("1", sseEntry.Metadata["timeoutSeconds"]);
        Assert.Equal("sse_execution_timeout", sseEntry.Metadata["timeoutCephalonCode"]);
        Assert.Equal("True", sseEntry.Metadata["circuitBreakerEnabled"]);
        Assert.Equal("sse_circuit_breaker_open", sseEntry.Metadata["circuitBreakerOpenCephalonCode"]);
        Assert.Equal("True", sseEntry.Metadata["bulkheadEnabled"]);
        Assert.Equal("1", sseEntry.Metadata["bulkheadMaxConcurrentExecutions"]);
        Assert.Equal("sse_bulkhead_rejected", sseEntry.Metadata["bulkheadRejectedCephalonCode"]);

        Assert.Equal("websocket-direct-module-resilience", webSocketEntry.Id);
        Assert.Equal("aspnetcore-streaming-endpoint-filter", webSocketEntry.Metadata["executionMode"]);
        Assert.Equal("direct-websocket-module-endpoints", webSocketEntry.Metadata["scope"]);
        Assert.Equal("websocket-error-frame", webSocketEntry.Metadata["protocolEnvelope"]);
        Assert.Equal("false", webSocketEntry.Metadata["wolverineRequired"]);
        Assert.Equal("false", webSocketEntry.Metadata["consumerCodeRequired"]);
        Assert.Equal("websocket_execution_timeout", webSocketEntry.Metadata["timeoutCephalonCode"]);
        Assert.Equal("websocket_circuit_breaker_open", webSocketEntry.Metadata["circuitBreakerOpenCephalonCode"]);
        Assert.Equal("websocket_bulkhead_rejected", webSocketEntry.Metadata["bulkheadRejectedCephalonCode"]);
    }

    private static async Task<WebApplication> BuildHostAsync(
        params string[] transports)
        => await BuildHostAsync(
            transports,
            enableTimeout: false,
            enableCircuitBreaker: false,
            enableBulkhead: false);

    private static async Task<WebApplication> BuildHostAsync(
        string transport,
        bool enableTimeout = false,
        bool enableCircuitBreaker = false,
        bool enableBulkhead = false)
        => await BuildHostAsync(
            [transport],
            enableTimeout,
            enableCircuitBreaker,
            enableBulkhead);

    private static async Task<WebApplication> BuildHostAsync(
        string[] transports,
        bool enableTimeout = false,
        bool enableCircuitBreaker = false,
        bool enableBulkhead = false)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        for (var index = 0; index < transports.Length; index++)
        {
            builder.Configuration[$"{EngineSettings.SectionName}:Transports:{index}"] = transports[index];
        }

        if (enableTimeout)
        {
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:Enabled"] = "true";
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:TotalTimeoutSeconds"] = "1";
        }

        if (enableCircuitBreaker)
        {
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:Enabled"] = "true";
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:FailureRatio"] = "0.5";
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:MinimumThroughput"] = "1";
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:SamplingDurationSeconds"] = "60";
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:BreakDurationSeconds"] = "60";
        }

        if (enableBulkhead)
        {
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:Enabled"] = "true";
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:MaxConcurrentExecutions"] = "1";
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:MaxQueuedActions"] = "0";
        }

        builder.Services.AddSingleton<DirectStreamingBulkheadTestGate>();
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DirectStreamingModuleResilienceTestModule());
        });

        var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();
        return app;
    }

    private static async Task<HttpResponseMessage> SendSseAsync(
        HttpClient client,
        string path,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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

    private static async Task<JsonDocument> ReceiveWebSocketJsonAsync(
        WebSocket socket,
        CancellationToken cancellationToken)
    {
        var message = await ReceiveWebSocketTextAsync(socket, cancellationToken);
        return JsonDocument.Parse(message);
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

    private static void AssertStreamingError(
        JsonElement payload,
        string expectedCephalonCode,
        int expectedStatusCode,
        string expectedMessageFragment,
        bool expectRetryAfter = false)
    {
        Assert.Equal(expectedCephalonCode, payload.GetProperty("code").GetString());
        Assert.Equal("resilience", payload.GetProperty("fault").GetString());
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

    private sealed record SseMessage(string? EventName, string Data);
}

internal sealed class DirectStreamingModuleResilienceTestModule : ModuleBase, IServerSentEventsModule, IWebSocketModule
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "direct-streaming-module-resilience",
        displayName: "Direct Streaming Module Resilience",
        description: "Test module that exercises host-enforced direct streaming resilience.",
        tags: ["test-only"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "transport-test",
            ["surface"] = "streaming-resilience"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public void MapServerSentEvents(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/resilience");
        group.MapGet("/slow", SlowSseAsync);
        group.MapGet("/echo", EchoSseAsync);
        group.MapGet("/bulkhead", BulkheadSseAsync);
    }

    public void MapWebSocketEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/resilience");
        group.MapGet("/slow", SlowWebSocketAsync);
        group.MapGet("/echo", EchoWebSocketAsync);
        group.MapGet("/bulkhead", BulkheadWebSocketAsync);
    }

    private static async Task SlowSseAsync(HttpContext context)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), context.RequestAborted);
        await WriteSseResultAsync(context, "slow");
    }

    private static Task EchoSseAsync(HttpContext context)
        => WriteSseResultAsync(context, "echo");

    private static async Task BulkheadSseAsync(HttpContext context)
    {
        var gate = context.RequestServices.GetRequiredService<DirectStreamingBulkheadTestGate>();
        gate.MarkStarted();
        await gate.WaitForReleaseAsync(context.RequestAborted);
        await WriteSseResultAsync(context, "held");
    }

    private static async Task SlowWebSocketAsync(HttpContext context)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), context.RequestAborted);
        await WriteWebSocketResultAsync(context, "slow");
    }

    private static Task EchoWebSocketAsync(HttpContext context)
        => WriteWebSocketResultAsync(context, "echo");

    private static async Task BulkheadWebSocketAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var gate = context.RequestServices.GetRequiredService<DirectStreamingBulkheadTestGate>();
        gate.MarkStarted();
        await gate.WaitForReleaseAsync(context.RequestAborted);
        await SendWebSocketPayloadAsync(socket, new { value = "held" });
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
    }

    private static async Task WriteSseResultAsync(HttpContext context, string value)
    {
        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";
        context.Response.Headers["X-Accel-Buffering"] = "no";

        var json = JsonSerializer.Serialize(new { value });
        await context.Response.WriteAsync($"event: result\ndata: {json}\n\n", Encoding.UTF8, context.RequestAborted);
        await context.Response.Body.FlushAsync(context.RequestAborted);
    }

    private static async Task WriteWebSocketResultAsync(HttpContext context, string value)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        await SendWebSocketPayloadAsync(socket, new { value });
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
    }

    private static Task SendWebSocketPayloadAsync(WebSocket socket, object payload)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        return socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
    }
}

internal sealed class DirectStreamingBulkheadTestGate
{
    private readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void MarkStarted()
    {
        started.TrySetResult();
    }

    public Task WaitUntilStartedAsync()
        => started.Task.WaitAsync(TimeSpan.FromSeconds(5));

    public Task WaitForReleaseAsync(CancellationToken cancellationToken)
        => release.Task.WaitAsync(cancellationToken);

    public void Release()
    {
        release.TrySetResult();
    }
}
