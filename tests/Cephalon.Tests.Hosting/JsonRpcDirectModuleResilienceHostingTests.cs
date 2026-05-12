using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.JsonRpc.Hosting;
using Cephalon.AspNetCore.JsonRpc.Modules;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

public sealed class JsonRpcDirectModuleResilienceHostingTests
{
    private const string EndpointPath = "/json-rpc/resilience";

    [Fact]
    public async Task JsonRpcDirectModule_ReturnsServiceUnavailableEnvelope_WhenTimeoutApplies()
    {
        await using var app = await BuildHostAsync(enableTimeout: true);
        var client = app.GetTestClient();

        using var response = await PostJsonRpcAsync(client, "slow", "req-timeout");
        var payload = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        AssertJsonRpcResilienceError(
            payload,
            "req-timeout",
            -32053,
            "jsonrpc_execution_timeout",
            503,
            "timeout");
    }

    [Fact]
    public async Task JsonRpcDirectModule_ReturnsServiceUnavailableEnvelope_WhenCircuitBreakerOpens()
    {
        await using var app = await BuildHostAsync(
            enableTimeout: true,
            enableCircuitBreaker: true);
        var client = app.GetTestClient();

        using var firstResponse = await PostJsonRpcAsync(client, "slow", "req-circuit-1");
        using var openCircuitResponse = await PostJsonRpcAsync(client, "echo", "req-circuit-2");
        var openCircuitPayload = await ReadJsonAsync(openCircuitResponse);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, openCircuitResponse.StatusCode);
        Assert.True(openCircuitResponse.Headers.RetryAfter?.Delta?.TotalSeconds > 0);
        AssertJsonRpcResilienceError(
            openCircuitPayload,
            "req-circuit-2",
            -32053,
            "jsonrpc_circuit_breaker_open",
            503,
            "circuit breaker",
            expectRetryAfter: true);
    }

    [Fact]
    public async Task JsonRpcDirectModule_ReturnsTooManyRequestsEnvelope_WhenBulkheadSaturates()
    {
        await using var app = await BuildHostAsync(enableBulkhead: true);
        var client = app.GetTestClient();
        var gate = app.Services.GetRequiredService<JsonRpcBulkheadTestGate>();

        var heldResponseTask = PostJsonRpcAsync(client, "bulkhead", "req-held");
        await gate.WaitUntilStartedAsync();

        using var rejectedResponse = await PostJsonRpcAsync(client, "bulkhead", "req-rejected");
        var rejectedPayload = await ReadJsonAsync(rejectedResponse);

        gate.Release();
        using var heldResponse = await heldResponseTask;
        var heldPayload = await ReadJsonAsync(heldResponse);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);
        AssertJsonRpcResilienceError(
            rejectedPayload,
            "req-rejected",
            -32029,
            "jsonrpc_bulkhead_rejected",
            429,
            "bulkhead");
        Assert.Equal(HttpStatusCode.OK, heldResponse.StatusCode);
        Assert.Equal("held", heldPayload.GetProperty("result").GetProperty("value").GetString());
    }

    [Fact]
    public async Task JsonRpcTransport_ReportsDirectModuleResilienceRuntimeSurface()
    {
        await using var app = await BuildHostAsync(
            enableTimeout: true,
            enableCircuitBreaker: true,
            enableBulkhead: true);
        var client = app.GetTestClient();

        var surfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/json-rpc");
        var surface = Assert.Single(surfaces ?? []);
        var entry = Assert.Single(surface.Entries);

        Assert.Equal("json-rpc-direct-module-resilience", surface.SurfaceId);
        Assert.Equal("json-rpc-direct-module-resilience", entry.Id);
        Assert.Equal("aspnetcore-jsonrpc-endpoint-filter", entry.Metadata["executionMode"]);
        Assert.Equal("Engine:Resilience", entry.Metadata["policySource"]);
        Assert.Equal("direct-json-rpc-module-endpoints", entry.Metadata["scope"]);
        Assert.Equal("false", entry.Metadata["wolverineRequired"]);
        Assert.Equal("false", entry.Metadata["consumerCodeRequired"]);
        Assert.Equal("True", entry.Metadata["timeoutEnabled"]);
        Assert.Equal("1", entry.Metadata["timeoutSeconds"]);
        Assert.Equal("503", entry.Metadata["timeoutStatusCode"]);
        Assert.Equal("-32053", entry.Metadata["timeoutJsonRpcErrorCode"]);
        Assert.Equal("True", entry.Metadata["circuitBreakerEnabled"]);
        Assert.Equal("503", entry.Metadata["circuitBreakerOpenStatusCode"]);
        Assert.Equal("-32053", entry.Metadata["circuitBreakerOpenJsonRpcErrorCode"]);
        Assert.Equal("True", entry.Metadata["bulkheadEnabled"]);
        Assert.Equal("1", entry.Metadata["bulkheadMaxConcurrentExecutions"]);
        Assert.Equal("429", entry.Metadata["bulkheadRejectedStatusCode"]);
        Assert.Equal("-32029", entry.Metadata["bulkheadRejectedJsonRpcErrorCode"]);
    }

    private static async Task<WebApplication> BuildHostAsync(
        bool enableTimeout = false,
        bool enableCircuitBreaker = false,
        bool enableBulkhead = false)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "JsonRpc";

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

        builder.Services.AddSingleton<JsonRpcBulkheadTestGate>();
        builder.AddJsonRpcTransport();
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new JsonRpcDirectModuleResilienceTestModule());
        });

        var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();
        return app;
    }

    private static Task<HttpResponseMessage> PostJsonRpcAsync(HttpClient client, string method, string id)
        => client.PostAsJsonAsync(EndpointPath, new
        {
            jsonrpc = "2.0",
            method,
            @params = new Dictionary<string, string?> { ["value"] = id },
            id
        });

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.Clone();
    }

    private static void AssertJsonRpcResilienceError(
        JsonElement payload,
        string expectedId,
        int expectedCode,
        string expectedCephalonCode,
        int expectedStatusCode,
        string expectedMessageFragment,
        bool expectRetryAfter = false)
    {
        Assert.Equal("2.0", payload.GetProperty("jsonRpc").GetString());
        Assert.Equal(expectedId, payload.GetProperty("id").GetString());
        Assert.True(payload.GetProperty("result").ValueKind is JsonValueKind.Null);

        var error = payload.GetProperty("error");
        Assert.Equal(expectedCode, error.GetProperty("code").GetInt32());
        Assert.Contains(expectedMessageFragment, error.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);

        var data = error.GetProperty("data");
        Assert.Equal(expectedCephalonCode, data.GetProperty("cephalonCode").GetString());
        Assert.Equal("resilience", data.GetProperty("fault").GetString());
        Assert.Equal(expectedStatusCode, data.GetProperty("statusCode").GetInt32());

        if (expectRetryAfter)
        {
            Assert.True(data.GetProperty("retryAfterSeconds").GetInt32() > 0);
        }
        else
        {
            Assert.False(data.TryGetProperty("retryAfterSeconds", out _));
        }
    }
}

internal sealed class JsonRpcDirectModuleResilienceTestModule : ModuleBase, IJsonRpcModule
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "json-rpc-direct-module-resilience",
        displayName: "JSON-RPC Direct Module Resilience",
        description: "Test module that exercises host-enforced JSON-RPC direct-module resilience.",
        tags: ["test-only"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "transport-test",
            ["surface"] = "json-rpc-resilience"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public void MapJsonRpcEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/resilience");
        group.MapPost("/", HandleAsync);
    }

    private static async Task HandleAsync(HttpContext context)
    {
        using var document = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);
        var root = document.RootElement;
        var id = root.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.String
            ? idElement.GetString()
            : null;
        var method = root.TryGetProperty("method", out var methodElement) && methodElement.ValueKind == JsonValueKind.String
            ? methodElement.GetString()
            : string.Empty;

        switch (method)
        {
            case "slow":
                await Task.Delay(TimeSpan.FromSeconds(10), context.RequestAborted);
                await WriteResultAsync(context, new { value = "slow" }, id);
                return;

            case "bulkhead":
                var gate = context.RequestServices.GetRequiredService<JsonRpcBulkheadTestGate>();
                gate.MarkStarted();
                await gate.WaitForReleaseAsync(context.RequestAborted);
                await WriteResultAsync(context, new { value = "held" }, id);
                return;

            default:
                await WriteResultAsync(context, new { value = "echo" }, id);
                return;
        }
    }

    private static Task WriteResultAsync(HttpContext context, object result, string? id)
    {
        var envelope = new
        {
            jsonRpc = "2.0",
            result,
            error = (object?)null,
            id
        };

        return context.Response.WriteAsJsonAsync(envelope, cancellationToken: context.RequestAborted);
    }
}

internal sealed class JsonRpcBulkheadTestGate
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
