using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Resilience;
using Cephalon.Abstractions.Technologies;
using Cephalon.AspNetCore.Grpc.Contracts.Discovery;
using Cephalon.AspNetCore.Grpc.Hosting;
using Cephalon.AspNetCore.Grpc.Modules;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

public sealed class GrpcTransportErrorAndStreamingHostingTests
{
    private const string ScenarioHeader = "test-scenario";

    [Theory]
    [InlineData("not-found", StatusCode.NotFound, "principle not found")]
    [InlineData("invalid-argument", StatusCode.InvalidArgument, "name is invalid")]
    [InlineData("unauthenticated", StatusCode.Unauthenticated, "missing credentials")]
    [InlineData("permission-denied", StatusCode.PermissionDenied, "permission denied")]
    [InlineData("unavailable", StatusCode.Unavailable, "upstream unavailable")]
    [InlineData("resource-exhausted", StatusCode.ResourceExhausted, "rate limit exceeded")]
    [InlineData("failed-precondition", StatusCode.FailedPrecondition, "precondition failed")]
    [InlineData("aborted", StatusCode.Aborted, "aborted by server")]
    public async Task SayHello_MapsCanonicalRpcExceptionsThrownByServer(
        string scenarioName,
        StatusCode expectedStatus,
        string expectedDetail)
    {
        await using var host = await BuildGrpcHostAsync();
        var client = CreateGrpcClient(host);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await client.SayHelloAsync(new HelloRequest { Name = scenarioName });
        });

        Assert.Equal(expectedStatus, exception.StatusCode);
        Assert.Contains(expectedDetail, exception.Status.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SayHello_MapsUnhandledExceptionToUnknownStatus()
    {
        await using var host = await BuildGrpcHostAsync();
        var client = CreateGrpcClient(host);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await client.SayHelloAsync(new HelloRequest { Name = "throw" });
        });

        // ASP.NET Core's gRPC stack maps any non-RpcException to Status.Unknown unless
        // an interceptor enriches it. The contract under test is that the canonical
        // unmapped failure is reported as UNKNOWN, never silently surfaced as OK.
        Assert.Equal(StatusCode.Unknown, exception.StatusCode);
    }

    [Theory]
    [InlineData("timeout-exception")]
    [InlineData("timeout-rejected")]
    public async Task SayHello_MapsTimeoutFaultsToDeadlineExceededStatus(string scenarioName)
    {
        await using var host = await BuildGrpcHostAsync();
        var client = CreateGrpcClient(host);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await client.SayHelloAsync(new HelloRequest { Name = scenarioName });
        });

        Assert.Equal(StatusCode.DeadlineExceeded, exception.StatusCode);
        Assert.Contains("timeout", exception.Status.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(exception.Trailers, entry =>
            string.Equals(entry.Key, "cephalon-code", StringComparison.Ordinal) &&
            string.Equals(entry.Value, "grpc_execution_timeout", StringComparison.Ordinal));
        Assert.Contains(exception.Trailers, entry =>
            string.Equals(entry.Key, "cephalon-fault", StringComparison.Ordinal) &&
            string.Equals(entry.Value, "resilience", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SayHello_MapsCircuitBreakerFaultToUnavailableStatus()
    {
        await using var host = await BuildGrpcHostAsync();
        var client = CreateGrpcClient(host);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await client.SayHelloAsync(new HelloRequest { Name = "circuit-open" });
        });

        Assert.Equal(StatusCode.Unavailable, exception.StatusCode);
        Assert.Contains("circuit breaker", exception.Status.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(exception.Trailers, entry =>
            string.Equals(entry.Key, "cephalon-code", StringComparison.Ordinal) &&
            string.Equals(entry.Value, "grpc_circuit_breaker_open", StringComparison.Ordinal));
        Assert.Contains(exception.Trailers, entry =>
            string.Equals(entry.Key, "cephalon-fault", StringComparison.Ordinal) &&
            string.Equals(entry.Value, "resilience", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SayHello_EnforcesConfiguredGrpcDirectModuleTimeout()
    {
        await using var host = await BuildGrpcHostAsync(enableDirectGrpcTimeout: true);
        var httpClient = host.GetTestClient();
        var client = CreateGrpcClient(host);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await client.SayHelloAsync(new HelloRequest { Name = "host-timeout" });
        });

        Assert.Equal(StatusCode.DeadlineExceeded, exception.StatusCode);
        Assert.Contains("timeout", exception.Status.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(exception.Trailers, entry =>
            string.Equals(entry.Key, "cephalon-code", StringComparison.Ordinal) &&
            string.Equals(entry.Value, "grpc_execution_timeout", StringComparison.Ordinal));

        var entry = await WaitForGrpcResilienceEntryAsync(httpClient, static candidate =>
            string.Equals(candidate.Metadata.GetValueOrDefault("timeoutOccurredCount"), "1", StringComparison.Ordinal));
        Assert.True(entry.Metadata.ContainsKey("timeoutLastOccurredAtUtc"));
    }

    [Fact]
    public async Task SayHello_EnforcesConfiguredGrpcDirectModuleCircuitBreaker()
    {
        await using var host = await BuildGrpcHostAsync(enableDirectGrpcCircuitBreaker: true);
        var httpClient = host.GetTestClient();
        var client = CreateGrpcClient(host);

        var firstFailure = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await client.SayHelloAsync(new HelloRequest { Name = "timeout-exception" });
        });
        var openCircuit = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await client.SayHelloAsync(new HelloRequest { Name = "ok" });
        });

        Assert.Equal(StatusCode.DeadlineExceeded, firstFailure.StatusCode);
        Assert.Equal(StatusCode.Unavailable, openCircuit.StatusCode);
        Assert.Contains(openCircuit.Trailers, entry =>
            string.Equals(entry.Key, "cephalon-code", StringComparison.Ordinal) &&
            string.Equals(entry.Value, "grpc_circuit_breaker_open", StringComparison.Ordinal));

        var surfaces = await httpClient.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/grpc");
        var entry = Assert.Single(Assert.Single(surfaces ?? []).Entries);
        Assert.Equal("open", entry.Metadata["circuitState"]);
        Assert.Equal("1", entry.Metadata["circuitSampleCount"]);
        Assert.Equal("1", entry.Metadata["circuitFailedSampleCount"]);
        Assert.NotEqual("0", entry.Metadata["circuitRetryAfterSeconds"]);
        Assert.Equal("1", entry.Metadata["circuitOpenedCount"]);
        Assert.Equal("1", entry.Metadata["circuitRejectedWhileOpenCount"]);
        Assert.True(entry.Metadata.ContainsKey("circuitLastRejectedWhileOpenAtUtc"));
    }

    [Fact]
    public async Task SayHello_EnforcesConfiguredGrpcDirectModuleBulkhead()
    {
        await using var host = await BuildGrpcHostAsync(enableDirectGrpcBulkhead: true);
        var httpClient = host.GetTestClient();
        var client = CreateGrpcClient(host);
        var gate = host.Services.GetRequiredService<GrpcBulkheadTestGate>();

        using var heldCall = client.SayHelloAsync(new HelloRequest { Name = "bulkhead-hold" });
        await gate.WaitUntilStartedAsync();

        var rejected = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await client.SayHelloAsync(new HelloRequest { Name = "ok" });
        });

        gate.Release();
        var heldReply = await heldCall.ResponseAsync;

        Assert.Equal("held", heldReply.Message);
        Assert.Equal(StatusCode.ResourceExhausted, rejected.StatusCode);
        Assert.Contains("bulkhead", rejected.Status.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(rejected.Trailers, entry =>
            string.Equals(entry.Key, "cephalon-code", StringComparison.Ordinal) &&
            string.Equals(entry.Value, "grpc_bulkhead_rejected", StringComparison.Ordinal));

        var surfaces = await httpClient.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/grpc");
        var entry = Assert.Single(Assert.Single(surfaces ?? []).Entries);
        Assert.Equal("True", entry.Metadata["bulkheadEnabled"]);
        Assert.Equal("1", entry.Metadata["bulkheadMaxConcurrentExecutions"]);
        Assert.Equal("0", entry.Metadata["bulkheadMaxQueuedActions"]);
        Assert.Equal("disabled-reject-on-entry", entry.Metadata["bulkheadQueueingMode"]);
        Assert.Equal("ResourceExhausted", entry.Metadata["bulkheadRejectedStatusCode"]);
        Assert.Equal("1", entry.Metadata["bulkheadRejectedCount"]);
        Assert.Equal("1", entry.Metadata["bulkheadAcceptedCount"]);
        Assert.Equal("1", entry.Metadata["bulkheadMaxObservedConcurrency"]);
    }

    [Fact]
    public async Task SayHello_HonorsConfiguredGrpcDirectModuleBulkheadQueue()
    {
        await using var host = await BuildGrpcHostAsync(
            enableDirectGrpcBulkhead: true,
            directGrpcBulkheadMaxQueuedActions: 1);
        var httpClient = host.GetTestClient();
        var client = CreateGrpcClient(host);
        var gate = host.Services.GetRequiredService<GrpcBulkheadTestGate>();

        using var heldCall = client.SayHelloAsync(new HelloRequest { Name = "bulkhead-hold" });
        await gate.WaitUntilStartedAsync();

        using var queuedCall = client.SayHelloAsync(new HelloRequest { Name = "ok" });
        await WaitForGrpcResilienceEntryAsync(httpClient, static entry =>
            string.Equals(entry.Metadata.GetValueOrDefault("bulkheadQueuedCount"), "1", StringComparison.Ordinal));

        var rejected = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await client.SayHelloAsync(new HelloRequest { Name = "ok" });
        });

        gate.Release();
        var heldReply = await heldCall.ResponseAsync;
        var queuedReply = await queuedCall.ResponseAsync;

        Assert.Equal("held", heldReply.Message);
        Assert.Equal("ok", queuedReply.Message);
        Assert.Equal(StatusCode.ResourceExhausted, rejected.StatusCode);

        var entry = await WaitForGrpcResilienceEntryAsync(httpClient, static candidate =>
            string.Equals(candidate.Metadata.GetValueOrDefault("bulkheadRejectedCount"), "1", StringComparison.Ordinal) &&
            string.Equals(candidate.Metadata.GetValueOrDefault("bulkheadAcceptedCount"), "2", StringComparison.Ordinal));

        Assert.Equal("bounded-queue", entry.Metadata["bulkheadQueueingMode"]);
        Assert.Equal("1", entry.Metadata["bulkheadMaxQueuedActions"]);
        Assert.Equal("1", entry.Metadata["bulkheadMaxObservedQueueLength"]);
    }

    [Fact]
    public async Task GrpcTransport_ReportsDirectModuleResilienceRuntimeSurface()
    {
        await using var host = await BuildGrpcHostAsync(
            enableDirectGrpcTimeout: true,
            enableDirectGrpcCircuitBreaker: true,
            enableDirectGrpcBulkhead: true);
        var httpClient = host.GetTestClient();

        var surfaces = await httpClient.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/grpc");
        var surface = Assert.Single(surfaces ?? []);
        var entry = Assert.Single(surface.Entries);

        Assert.Equal("grpc-direct-module-resilience", surface.SurfaceId);
        Assert.Equal("grpc-direct-module-resilience", entry.Id);
        Assert.Equal("aspnetcore-grpc-interceptor", entry.Metadata["executionMode"]);
        Assert.Equal("Engine:Resilience", entry.Metadata["policySource"]);
        Assert.Equal("false", entry.Metadata["wolverineRequired"]);
        Assert.Equal("false", entry.Metadata["consumerCodeRequired"]);
        Assert.Equal("True", entry.Metadata["timeoutEnabled"]);
        Assert.Equal("1", entry.Metadata["timeoutSeconds"]);
        Assert.Equal("True", entry.Metadata["circuitBreakerEnabled"]);
        Assert.Equal("Unavailable", entry.Metadata["circuitBreakerOpenStatusCode"]);
        Assert.Equal("True", entry.Metadata["bulkheadEnabled"]);
        Assert.Equal("1", entry.Metadata["bulkheadMaxConcurrentExecutions"]);
        Assert.Equal("ResourceExhausted", entry.Metadata["bulkheadRejectedStatusCode"]);
        Assert.Equal("0", entry.Metadata["circuitOpenedCount"]);
        Assert.Equal("0", entry.Metadata["circuitRejectedWhileOpenCount"]);
        Assert.Equal("0", entry.Metadata["timeoutOccurredCount"]);
    }

    [Fact]
    public async Task SayHello_ReturnsHappyPath_WhenScenarioNameIsOk()
    {
        await using var host = await BuildGrpcHostAsync();
        var client = CreateGrpcClient(host);

        var reply = await client.SayHelloAsync(new HelloRequest { Name = "ok" });

        Assert.Equal("ok", reply.Message);
    }

    [Fact]
    public async Task GrpcTransport_AppliesCephalonRateLimitingAndReportsRuntimeCatalog()
    {
        await using var host = await BuildGrpcHostAsync(enableTightRateLimiting: true);
        var httpClient = host.GetTestClient();
        var client = CreateGrpcClient(host);

        var policies = await httpClient.GetFromJsonAsync<RateLimitingRuntimeDescriptor[]>("/engine/rate-limiting");
        var policy = Assert.Single(policies ?? []);

        Assert.Contains("grpc", policy.TransportIds);
        Assert.Equal("aspnetcore-endpoint-policy", policy.ExecutionMode);
        Assert.Equal("request-response", policy.Metadata["transportKind"]);
        Assert.Equal("request-entry-rate", policy.Metadata["transportSemantics"]);
        Assert.Equal("checked-on-request-entry", policy.Metadata["enforcementMoment"]);

        var firstReply = await client.SayHelloAsync(new HelloRequest { Name = "ok" });
        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await client.SayHelloAsync(new HelloRequest { Name = "ok" });
        });

        Assert.Equal("ok", firstReply.Message);
        Assert.Equal(StatusCode.ResourceExhausted, exception.StatusCode);
        Assert.Contains("rate limit", exception.Status.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StreamPrinciples_ProducesFullSequence_OnHappyPath()
    {
        await using var host = await BuildGrpcHostAsync();
        var client = CreateGrpcClient(host);

        var collected = new List<string>();
        using var call = client.StreamPrinciples(
            new PrinciplesRequest(),
            new CallOptions(headers: ScenarioMetadata("ok")));

        while (await call.ResponseStream.MoveNext(CancellationToken.None))
        {
            collected.Add(call.ResponseStream.Current.Principle);
        }

        Assert.Equal(["first", "second", "third"], collected);
    }

    [Fact]
    public async Task StreamPrinciples_PropagatesInvalidArgument_WhenServerThrowsMidStream()
    {
        await using var host = await BuildGrpcHostAsync();
        var client = CreateGrpcClient(host);

        using var call = client.StreamPrinciples(
            new PrinciplesRequest(),
            new CallOptions(headers: ScenarioMetadata("stream-error-mid")));

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            while (await call.ResponseStream.MoveNext(CancellationToken.None))
            {
                // Drain until the server-side throw propagates as an RpcException to the client.
            }
        });

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        Assert.Contains("mid-stream failure", exception.Status.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StreamPrinciples_ReturnsCancelled_WhenClientCancelsMidStream()
    {
        await using var host = await BuildGrpcHostAsync();
        var client = CreateGrpcClient(host);

        using var cts = new CancellationTokenSource();
        using var call = client.StreamPrinciples(
            new PrinciplesRequest(),
            new CallOptions(
                headers: ScenarioMetadata("stream-cancel-mid"),
                cancellationToken: cts.Token));

        // The first item arrives before the server blocks waiting for cancellation; once we have it
        // we trigger the client cancellation and expect MoveNext to surface RpcException(Cancelled).
        var moved = await call.ResponseStream.MoveNext(CancellationToken.None);
        Assert.True(moved);
        Assert.Equal("first", call.ResponseStream.Current.Principle);

        cts.Cancel();

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            while (await call.ResponseStream.MoveNext(CancellationToken.None))
            {
                // No additional items are expected before the cancellation fires.
            }
        });

        Assert.Equal(StatusCode.Cancelled, exception.StatusCode);
    }

    [Fact]
    public async Task ExchangeGreetings_EchoesEachRequest_OnHappyPath()
    {
        await using var host = await BuildGrpcHostAsync();
        var client = CreateGrpcClient(host);

        using var call = client.ExchangeGreetings(
            new CallOptions(headers: ScenarioMetadata("ok")));

        await call.RequestStream.WriteAsync(new HelloRequest { Name = "Alpha" });
        await call.RequestStream.WriteAsync(new HelloRequest { Name = "Beta" });
        await call.RequestStream.CompleteAsync();

        var replies = new List<string>();
        while (await call.ResponseStream.MoveNext(CancellationToken.None))
        {
            replies.Add(call.ResponseStream.Current.Message);
        }

        Assert.Equal(["echo:Alpha", "echo:Beta"], replies);
    }

    [Fact]
    public async Task ExchangeGreetings_PropagatesInternalStatus_WhenServerThrowsAfterFirstReply()
    {
        await using var host = await BuildGrpcHostAsync();
        var client = CreateGrpcClient(host);

        using var call = client.ExchangeGreetings(
            new CallOptions(headers: ScenarioMetadata("bidi-error-after-first")));

        await call.RequestStream.WriteAsync(new HelloRequest { Name = "Alpha" });

        // The server replies once, then throws RpcException(Internal). Drain until propagation.
        var moved = await call.ResponseStream.MoveNext(CancellationToken.None);
        Assert.True(moved);
        Assert.Equal("echo:Alpha", call.ResponseStream.Current.Message);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            while (await call.ResponseStream.MoveNext(CancellationToken.None))
            {
                // No additional replies are expected after the server throws.
            }
        });

        Assert.Equal(StatusCode.Internal, exception.StatusCode);
        Assert.Contains("bidi failure after first reply", exception.Status.Detail, StringComparison.Ordinal);
    }

    private static Metadata ScenarioMetadata(string scenario)
    {
        return new Metadata { { ScenarioHeader, scenario } };
    }

    private static async Task<WebApplication> BuildGrpcHostAsync(
        bool enableTightRateLimiting = false,
        bool enableDirectGrpcTimeout = false,
        bool enableDirectGrpcCircuitBreaker = false,
        bool enableDirectGrpcBulkhead = false,
        int directGrpcBulkheadMaxQueuedActions = 0)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "Grpc";
        if (enableTightRateLimiting)
        {
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Enabled"] = "true";
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Algorithm"] = "FixedWindow";
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:PermitLimit"] = "1";
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:QueueLimit"] = "0";
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:WindowSeconds"] = "60";
        }

        if (enableDirectGrpcTimeout)
        {
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:Enabled"] = "true";
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:TotalTimeoutSeconds"] = "1";
        }

        if (enableDirectGrpcCircuitBreaker)
        {
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:Enabled"] = "true";
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:FailureRatio"] = "0.5";
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:MinimumThroughput"] = "1";
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:SamplingDurationSeconds"] = "60";
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:BreakDurationSeconds"] = "60";
        }

        if (enableDirectGrpcBulkhead)
        {
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:Enabled"] = "true";
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:MaxConcurrentExecutions"] = "1";
            builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:MaxQueuedActions"] = directGrpcBulkheadMaxQueuedActions.ToString(CultureInfo.InvariantCulture);
        }

        builder.AddGrpcTransport();
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new GrpcStreamingAndErrorModesTestModule());
        });

        var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();
        return app;
    }

    private static DiscoveryService.DiscoveryServiceClient CreateGrpcClient(WebApplication host)
    {
        var handler = new GrpcSubdirectoryHandler(host.GetTestServer().CreateHandler(), "/grpc");
        var grpcHttpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost"),
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
        };

        var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpClient = grpcHttpClient,
            DisposeHttpClient = true
        });

        return new DiscoveryService.DiscoveryServiceClient(channel);
    }

    private static async Task<TechnologyRuntimeEntry> WaitForGrpcResilienceEntryAsync(
        HttpClient httpClient,
        Func<TechnologyRuntimeEntry, bool> predicate)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var surfaces = await httpClient.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/grpc");
            var entry = surfaces?.SingleOrDefault()?.Entries.SingleOrDefault();
            if (entry is not null && predicate(entry))
            {
                return entry;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        throw new TimeoutException("Timed out waiting for the gRPC resilience runtime entry to reach the expected state.");
    }

    private sealed class GrpcSubdirectoryHandler : DelegatingHandler
    {
        private readonly string subdirectory;

        public GrpcSubdirectoryHandler(HttpMessageHandler innerHandler, string subdirectory)
            : base(innerHandler)
        {
            ArgumentNullException.ThrowIfNull(innerHandler);
            ArgumentException.ThrowIfNullOrWhiteSpace(subdirectory);

            this.subdirectory = subdirectory.StartsWith('/')
                ? subdirectory.TrimEnd('/')
                : $"/{subdirectory.TrimEnd('/')}";
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.RequestUri);

            var requestUri = request.RequestUri;
            var rewritten = new UriBuilder(requestUri)
            {
                Path = $"{subdirectory}{requestUri.AbsolutePath}"
            };
            request.RequestUri = rewritten.Uri;

            return base.SendAsync(request, cancellationToken);
        }
    }
}

internal sealed class GrpcStreamingAndErrorModesTestModule : ModuleBase, IGrpcModule
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "grpc-streaming-and-error-modes",
        displayName: "gRPC Streaming and Error Modes",
        description: "Test module that exercises canonical gRPC Status mapping and streaming error/cancellation paths.",
        tags: ["test-only"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "transport-test",
            ["surface"] = "grpc"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<GrpcBulkheadTestGate>();
        services.AddTransient<GrpcStreamingAndErrorModesService>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "grpc-streaming-and-error-modes.coverage",
            displayName: "gRPC streaming and error-mode coverage",
            description: "Marker capability for the gRPC streaming/error-mode test scenarios."));
    }

    public void MapGrpcEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<GrpcStreamingAndErrorModesService>();
    }
}

internal sealed class GrpcBulkheadTestGate
{
    private readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void MarkStarted()
    {
        started.TrySetResult();
    }

    public Task WaitUntilStartedAsync()
    {
        return started.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    public Task WaitUntilReleasedAsync(CancellationToken cancellationToken)
    {
        return released.Task.WaitAsync(cancellationToken);
    }

    public void Release()
    {
        released.TrySetResult();
    }
}

internal sealed class GrpcStreamingAndErrorModesService(GrpcBulkheadTestGate bulkheadGate) : DiscoveryService.DiscoveryServiceBase
{
    private const string ScenarioHeader = "test-scenario";

    public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var scenario = (request.Name ?? string.Empty).Trim().ToLowerInvariant();
        switch (scenario)
        {
            case "":
            case "ok":
                return new HelloReply { Message = "ok" };
            case "not-found":
                throw new RpcException(new Status(StatusCode.NotFound, "principle not found"));
            case "invalid-argument":
                throw new RpcException(new Status(StatusCode.InvalidArgument, "name is invalid"));
            case "unauthenticated":
                throw new RpcException(new Status(StatusCode.Unauthenticated, "missing credentials"));
            case "permission-denied":
                throw new RpcException(new Status(StatusCode.PermissionDenied, "permission denied"));
            case "unavailable":
                throw new RpcException(new Status(StatusCode.Unavailable, "upstream unavailable"));
            case "resource-exhausted":
                throw new RpcException(new Status(StatusCode.ResourceExhausted, "rate limit exceeded"));
            case "failed-precondition":
                throw new RpcException(new Status(StatusCode.FailedPrecondition, "precondition failed"));
            case "aborted":
                throw new RpcException(new Status(StatusCode.Aborted, "aborted by server"));
            case "throw":
                throw new InvalidOperationException("simulated unhandled handler failure");
            case "timeout-exception":
                throw new TimeoutException("simulated handler timeout");
            case "timeout-rejected":
                throw CreatePollyException("Polly.Timeout.TimeoutRejectedException", "simulated Polly timeout");
            case "circuit-open":
                throw CreatePollyException("Polly.CircuitBreaker.BrokenCircuitException", "simulated open circuit");
            case "host-timeout":
                await Task.Delay(TimeSpan.FromMilliseconds(1500), context.CancellationToken);
                return new HelloReply { Message = "late" };
            case "bulkhead-hold":
                bulkheadGate.MarkStarted();
                await bulkheadGate.WaitUntilReleasedAsync(context.CancellationToken);
                return new HelloReply { Message = "held" };
            case "delay":
                // Reserved for future deadline / cancellation coverage; under
                // Microsoft.AspNetCore.TestHost the in-memory pipe does not propagate the
                // client-side cancellation token to the server-side ServerCallContext reliably,
                // so unary client-cancellation and unary deadline-expiry coverage is currently
                // exercised through the streaming variants instead. Leave the case in place so
                // the test module remains a self-documenting reference for canonical scenarios.
                throw new RpcException(new Status(
                    StatusCode.Unimplemented,
                    "scenario 'delay' is reserved for future unary deadline / cancellation coverage"));
            default:
                throw new RpcException(new Status(
                    StatusCode.Unimplemented,
                    $"scenario '{scenario}' is not implemented"));
        }
    }

    public override async Task StreamPrinciples(
        PrinciplesRequest request,
        IServerStreamWriter<PrincipleReply> responseStream,
        ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(responseStream);
        ArgumentNullException.ThrowIfNull(context);

        var scenario = ResolveScenario(context);

        await responseStream.WriteAsync(
            new PrincipleReply { Principle = "first" },
            context.CancellationToken);

        if (string.Equals(scenario, "stream-error-mid", StringComparison.Ordinal))
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "mid-stream failure for streaming-error coverage"));
        }

        if (string.Equals(scenario, "stream-cancel-mid", StringComparison.Ordinal))
        {
            // Block until the client cancellation surfaces through ServerCallContext.
            await Task.Delay(Timeout.InfiniteTimeSpan, context.CancellationToken);
            return;
        }

        await responseStream.WriteAsync(
            new PrincipleReply { Principle = "second" },
            context.CancellationToken);

        await responseStream.WriteAsync(
            new PrincipleReply { Principle = "third" },
            context.CancellationToken);
    }

    public override async Task ExchangeGreetings(
        IAsyncStreamReader<HelloRequest> requestStream,
        IServerStreamWriter<HelloReply> responseStream,
        ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(requestStream);
        ArgumentNullException.ThrowIfNull(responseStream);
        ArgumentNullException.ThrowIfNull(context);

        var scenario = ResolveScenario(context);
        var replied = 0;

        await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
        {
            await responseStream.WriteAsync(
                new HelloReply { Message = $"echo:{request.Name}" },
                context.CancellationToken);
            replied++;

            if (string.Equals(scenario, "bidi-error-after-first", StringComparison.Ordinal) && replied == 1)
            {
                throw new RpcException(new Status(
                    StatusCode.Internal,
                    "bidi failure after first reply for streaming-error coverage"));
            }
        }
    }

    private static string ResolveScenario(ServerCallContext context)
    {
        foreach (var entry in context.RequestHeaders)
        {
            if (string.Equals(entry.Key, ScenarioHeader, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Value ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static Exception CreatePollyException(string typeName, string message)
    {
        var type = Type.GetType($"{typeName}, Polly.Core", throwOnError: true)!;
        return (Exception)Activator.CreateInstance(type, message)!;
    }
}
