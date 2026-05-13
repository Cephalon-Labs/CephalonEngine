using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.AspNetCore.GraphQL.Hosting;
using Cephalon.AspNetCore.GraphQL.Modules;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

public sealed class GraphQLExecutionResilienceHostingTests
{
    [Fact]
    public async Task GraphQlQuery_ReturnsError_WhenResolverTimeoutApplies()
    {
        await using var app = BuildApp(configuration =>
        {
            configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:Enabled"] = "true";
            configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:TotalTimeoutSeconds"] = "1";
        });
        await app.StartAsync();
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/graph-http",
            new
            {
                query = "query { slow(delayMilliseconds: 3000) }"
            });
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("graphql_execution_timeout", payload, StringComparison.Ordinal);
        Assert.Contains("\"fault\":\"resilience\"", payload, StringComparison.Ordinal);
        Assert.Contains("\"statusCode\":503", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GraphQlMutation_ReturnsError_WhenResolverTimeoutApplies()
    {
        await using var app = BuildApp(configuration =>
        {
            configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:Enabled"] = "true";
            configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:TotalTimeoutSeconds"] = "1";
        });
        await app.StartAsync();
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/graph-http",
            new
            {
                query = "mutation { slowMutation(delayMilliseconds: 3000) }"
            });
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("graphql_execution_timeout", payload, StringComparison.Ordinal);
        Assert.Contains("\"fault\":\"resilience\"", payload, StringComparison.Ordinal);
        Assert.Contains("\"statusCode\":503", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GraphQlQuery_ReturnsError_WhenCircuitBreakerOpens()
    {
        await using var app = BuildApp(configuration =>
        {
            configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:Enabled"] = "true";
            configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:FailureRatio"] = "1";
            configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:MinimumThroughput"] = "1";
            configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:SamplingDurationSeconds"] = "30";
            configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:BreakDurationSeconds"] = "10";
        });
        await app.StartAsync();
        using var client = app.GetTestClient();

        var firstResponse = await client.PostAsJsonAsync(
            "/graph-http",
            new
            {
                query = "query { unstable }"
            });
        var firstPayload = await firstResponse.Content.ReadAsStringAsync();
        var secondResponse = await client.PostAsJsonAsync(
            "/graph-http",
            new
            {
                query = "query { unstable }"
            });
        var secondPayload = await secondResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Contains("errors", firstPayload, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Contains("graphql_circuit_breaker_open", secondPayload, StringComparison.Ordinal);
        Assert.Contains("\"fault\":\"resilience\"", secondPayload, StringComparison.Ordinal);
        Assert.Contains("\"statusCode\":503", secondPayload, StringComparison.Ordinal);
        Assert.Contains("\"retryAfterSeconds\":", secondPayload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GraphQlQuery_ReturnsError_WhenBulkheadSaturates()
    {
        await using var app = BuildApp(configuration =>
        {
            configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:Enabled"] = "true";
            configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:MaxConcurrentExecutions"] = "1";
            configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:MaxQueuedActions"] = "0";
        });
        await app.StartAsync();
        using var client = app.GetTestClient();
        var gate = app.Services.GetRequiredService<GraphQLResilienceTestGate>();

        var firstRequest = client.PostAsJsonAsync(
            "/graph-http",
            new
            {
                query = "query { hold }"
            });
        await gate.WaitUntilEnteredAsync();
        var secondResponse = await client.PostAsJsonAsync(
            "/graph-http",
            new
            {
                query = "query { hold }"
            });
        var secondPayload = await secondResponse.Content.ReadAsStringAsync();
        gate.Release();
        var firstResponse = await firstRequest;
        var firstPayload = await firstResponse.Content.ReadAsStringAsync();

        Assert.True(firstResponse.IsSuccessStatusCode);
        Assert.Contains("released", firstPayload, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Contains("graphql_bulkhead_rejected", secondPayload, StringComparison.Ordinal);
        Assert.Contains("\"fault\":\"resilience\"", secondPayload, StringComparison.Ordinal);
        Assert.Contains("\"statusCode\":429", secondPayload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GraphQlSubscription_ReturnsErrorFrame_WhenResolverTimeoutApplies()
    {
        await using var app = BuildApp(
            configuration =>
            {
                configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:Enabled"] = "true";
                configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:TotalTimeoutSeconds"] = "1";
            },
            includeSubscriptions: true);
        await app.StartAsync();
        var webSocketClient = app.GetTestServer().CreateWebSocketClient();
        webSocketClient.SubProtocols.Add("graphql-transport-ws");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        using var socket = await webSocketClient.ConnectAsync(
            new Uri("ws://localhost/graph-socket"),
            cts.Token);

        await SendWebSocketJsonAsync(socket, new { type = "connection_init" }, cts.Token);
        _ = await ReceiveWebSocketMessageMatchingAsync(
            socket,
            message => message.Contains("\"type\":\"connection_ack\"", StringComparison.Ordinal),
            cts.Token);
        await SendWebSocketJsonAsync(
            socket,
            new
            {
                id = "sub-timeout",
                type = "subscribe",
                payload = new
                {
                    query = "subscription { slowPublished(delayMilliseconds: 3000) }"
                }
            },
            cts.Token);

        var nextMessage = await ReceiveWebSocketMessageMatchingAsync(
            socket,
            message => message.Contains("graphql_execution_timeout", StringComparison.Ordinal),
            cts.Token);

        Assert.Contains("\"type\":\"next\"", nextMessage, StringComparison.Ordinal);
        Assert.Contains("\"id\":\"sub-timeout\"", nextMessage, StringComparison.Ordinal);
        Assert.Contains("\"fault\":\"resilience\"", nextMessage, StringComparison.Ordinal);
        Assert.Contains("\"statusCode\":503", nextMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GraphQlReportsExecutionResilienceRuntimeSurface()
    {
        await using var app = BuildApp(configuration =>
        {
            configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:Enabled"] = "true";
            configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:TotalTimeoutSeconds"] = "2";
            configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:Enabled"] = "true";
            configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:MinimumThroughput"] = "1";
            configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:Enabled"] = "true";
            configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:MaxConcurrentExecutions"] = "3";
        });
        await app.StartAsync();
        using var client = app.GetTestClient();

        var surfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/graphql");

        Assert.NotNull(surfaces);
        var surface = Assert.Single(surfaces, candidate => candidate.SurfaceId == "graphql-execution-resilience");
        var entry = Assert.Single(surface.Entries);
        Assert.Equal("graphql-execution-resilience", entry.Id);
        Assert.Equal("hotchocolate-field-middleware", entry.Metadata["executionMode"]);
        Assert.Equal("Engine:Resilience", entry.Metadata["policySource"]);
        Assert.Equal("built-in-graphql-root-operation-fields", entry.Metadata["scope"]);
        Assert.Equal("query,mutation,subscription", entry.Metadata["operationTypes"]);
        Assert.Equal("graphql-errors-extensions", entry.Metadata["protocolEnvelope"]);
        Assert.Equal("false", entry.Metadata["wolverineRequired"]);
        Assert.Equal("false", entry.Metadata["consumerCodeRequired"]);
        Assert.Equal("True", entry.Metadata["timeoutEnabled"]);
        Assert.Equal("2", entry.Metadata["timeoutSeconds"]);
        Assert.Equal("True", entry.Metadata["circuitBreakerEnabled"]);
        Assert.Equal("True", entry.Metadata["bulkheadEnabled"]);
        Assert.Equal("3", entry.Metadata["bulkheadMaxConcurrentExecutions"]);
    }

    private static WebApplication BuildApp(
        Action<IConfigurationManager> configure,
        bool includeSubscriptions = false)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "GraphQL";
        builder.Configuration["ApiRoutes:Prefixes:GraphQL"] = "/graph-http";
        builder.Configuration["ApiRoutes:Prefixes:GraphQLSse"] = "/graph-stream";
        builder.Configuration["ApiRoutes:Prefixes:GraphQLWs"] = "/graph-socket";
        configure(builder.Configuration);
        builder.AddGraphQLTransport();
        builder.AddCephalon(engine => engine.AddModule(new GraphQLResilienceTestModule(includeSubscriptions)));

        var app = builder.Build();
        app.MapCephalon();
        return app;
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
        for (var index = 0; index < 12; index++)
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

    private sealed class GraphQLResilienceTestModule(bool includeSubscriptions) : ModuleBase, IGraphQLModule
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "graphql-resilience-test",
            displayName: "GraphQL Resilience Test",
            description: "Exercises built-in GraphQL execution resilience.",
            tags: ["graphql", "resilience"],
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<GraphQLResilienceTestGate>();
            services.ConfigureGraphQLQuery(GraphQLResilienceQueries.Configure);
            services.ConfigureGraphQLMutation(GraphQLResilienceMutations.Configure);
            if (includeSubscriptions)
            {
                services.ConfigureGraphQLSubscription(GraphQLResilienceSubscriptions.Configure);
            }
        }
    }

    private sealed class GraphQLResilienceQueries
    {
        public static void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Field("slow")
                .Argument("delayMilliseconds", argument => argument.Type<NonNullType<IntType>>())
                .Type<StringType>()
                .Resolve(async context =>
                {
                    await Task.Delay(context.ArgumentValue<int>("delayMilliseconds"), CancellationToken.None)
                        .ConfigureAwait(false);
                    return "slow-complete";
                });

            descriptor.Field("unstable")
                .Type<StringType>()
                .Resolve(static _ => throw new HttpRequestException("Simulated transient GraphQL dependency failure."));

            descriptor.Field("hold")
                .Type<StringType>()
                .Resolve(async context =>
                {
                    var gate = context.Service<GraphQLResilienceTestGate>();
                    await gate.HoldAsync(context.RequestAborted).ConfigureAwait(false);
                    return "released";
                });
        }
    }

    private sealed class GraphQLResilienceMutations
    {
        public static void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Field("slowMutation")
                .Argument("delayMilliseconds", argument => argument.Type<NonNullType<IntType>>())
                .Type<StringType>()
                .Resolve(async context =>
                {
                    await Task.Delay(context.ArgumentValue<int>("delayMilliseconds"), CancellationToken.None)
                        .ConfigureAwait(false);
                    return "slow-mutation-complete";
                });
        }
    }

    private sealed class GraphQLResilienceSubscriptions
    {
        public static void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Field("slowPublished")
                .Argument("delayMilliseconds", argument => argument.Type<NonNullType<IntType>>())
                .Type<StringType>()
                .Subscribe<string>(static _ => CreateSlowMessages())
                .Resolve(async context =>
                {
                    await Task.Delay(context.ArgumentValue<int>("delayMilliseconds"), CancellationToken.None)
                        .ConfigureAwait(false);
                    return context.GetEventMessage<string>();
                });
        }

        private static async IAsyncEnumerable<string> CreateSlowMessages()
        {
            await Task.Yield();
            yield return "subscription-timeout";
        }
    }

    private sealed class GraphQLResilienceTestGate
    {
        private readonly TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task HoldAsync(CancellationToken cancellationToken)
        {
            entered.TrySetResult();
            await released.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public Task WaitUntilEnteredAsync()
            => entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        public void Release()
        {
            released.TrySetResult();
        }
    }
}
