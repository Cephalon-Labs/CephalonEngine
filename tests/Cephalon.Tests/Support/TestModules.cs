using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.GraphQL.Hosting;
using Cephalon.AspNetCore.GraphQL.Modules;
using Cephalon.AspNetCore.Grpc.Modules;
using Cephalon.AspNetCore.Modules;
using Cephalon.AspNetCore.JsonRpc.Modules;
using Cephalon.AspNetCore.Transports.ServerSentEvents;
using Cephalon.AspNetCore.Transports.WebSockets;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Cephalon.Tests.Support;

internal sealed class PlatformTestModule : ModuleBase, IEndpointModule
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "platform",
        displayName: "Platform",
        description: "Foundation runtime services.",
        tags: ["foundation"],
        version: "1.2.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "foundation",
            ["surface"] = "time"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ITestClock, FixedClock>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "platform.clock",
            displayName: "Platform Clock",
            description: "Provides deterministic time for tests.",
            metadata: new Dictionary<string, string>
            {
                ["kind"] = "clock",
                ["mode"] = "deterministic"
            }));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/platform");
        group.MapGet("/time", (ITestClock clock) =>
            TypedResults.Ok(new PlatformTimeEnvelope(clock.GetUtcNow(), "utc")));
    }
}

internal sealed class DiscoveryTestModule : ModuleBase, IEndpointModule, IGraphQLModule, IJsonRpcModule, IGrpcModule, IServerSentEventsModule, IWebSocketModule
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "discovery",
        displayName: "Discovery",
        description: "Discovery endpoints for test coverage.",
        dependsOn: [typeof(PlatformTestModule)],
        tags: ["experience"],
        version: "2.4.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "experience",
            ["surface"] = "api"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<TestGreetingComposer>();
        services.AddTransient<DiscoveryGrpcService>();
        services.ConfigureGraphQLQuery(DiscoveryGraphQLQueries.Configure);
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "discovery.greetings",
            displayName: "Greeting Composer",
            description: "Builds discovery greetings.",
            metadata: new Dictionary<string, string>
            {
                ["kind"] = "greeting",
                ["dependsOn"] = "platform.clock"
            }));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/discovery");
        group.MapGet("/hello/{name?}", (string? name, TestGreetingComposer composer) =>
            TypedResults.Ok(composer.Compose(name)));
    }

    public void MapJsonRpcEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/discovery");
        group.MapPost("/", async context =>
        {
            var composer = context.RequestServices.GetRequiredService<TestGreetingComposer>();
            var request = await context.Request.ReadFromJsonAsync<JsonRpcRequest>(cancellationToken: context.RequestAborted);
            JsonRpcResponse response;

            if (request is null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response = new JsonRpcResponse(
                    JsonRpcRequest.Version,
                    Result: null,
                    Error: new JsonRpcError(-32600, "Invalid Request"),
                    Id: null);
                await context.Response.WriteAsJsonAsync(response, cancellationToken: context.RequestAborted);
                return;
            }

            if (!string.Equals(request.Method, "discovery.hello", StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response = new JsonRpcResponse(
                    request.JsonRpc ?? JsonRpcRequest.Version,
                    Result: null,
                    Error: new JsonRpcError(-32601, $"Method '{request.Method}' was not found."),
                    Id: request.Id);
                await context.Response.WriteAsJsonAsync(response, cancellationToken: context.RequestAborted);
                return;
            }

            var name = request.Params?.GetValueOrDefault("name");
            response = new JsonRpcResponse(
                request.JsonRpc ?? JsonRpcRequest.Version,
                Result: composer.Compose(name),
                Error: null,
                Id: request.Id);

            await context.Response.WriteAsJsonAsync(response, cancellationToken: context.RequestAborted);
        });
    }

    public void MapGrpcEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<DiscoveryGrpcService>();
    }

    public void MapServerSentEvents(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/discovery");
        group.MapGet("/principles", async context =>
        {
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.ContentType = "text/event-stream";

            foreach (var principle in DiscoveryDefaults.Principles)
            {
                var payload = JsonSerializer.Serialize(new PrincipleEnvelope(principle));
                await context.Response.WriteAsync("event: principle\n", context.RequestAborted);
                await context.Response.WriteAsync($"data: {payload}\n\n", context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
            }
        });
    }

    public void MapWebSocketEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/discovery");
        group.MapGet("/", async context =>
        {
            var composer = context.RequestServices.GetRequiredService<TestGreetingComposer>();

            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Expected a WebSocket upgrade request.", context.RequestAborted);
                return;
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            var payload = JsonSerializer.Serialize(composer.Compose("socket"));
            var buffer = Encoding.UTF8.GetBytes(payload);

            await socket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: context.RequestAborted);

            await socket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Discovery test stream completed.",
                context.RequestAborted);
        });
    }
}

public interface ITestClock
{
    DateTimeOffset GetUtcNow();
}

internal sealed class FixedClock : ITestClock
{
    private static readonly DateTimeOffset UtcNowValue = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public DateTimeOffset GetUtcNow()
    {
        return UtcNowValue;
    }
}

public sealed class TestGreetingComposer
{
    private readonly ITestClock clock;

    public TestGreetingComposer(ITestClock clock)
    {
        this.clock = clock;
    }

    public GreetingEnvelope Compose(string? name)
    {
        var visitor = string.IsNullOrWhiteSpace(name) ? "builder" : name.Trim();

        return new GreetingEnvelope(
            Message: $"Hello, {visitor} from the Cephalon future stack.",
            GeneratedAtUtc: clock.GetUtcNow(),
            Traits: DiscoveryDefaults.Principles);
    }
}

public sealed class DiscoveryGraphQLQueries
{
    public static void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Field("hello")
            .Argument("name", argument => argument.Type<StringType>())
            .Resolve(context =>
            {
                var composer = context.Service<TestGreetingComposer>();
                var name = context.ArgumentValue<string?>("name");
                return composer.Compose(name);
            });
        descriptor.Field("principles")
            .Resolve(static _ => DiscoveryDefaults.Principles);
    }
}

/// <summary>
/// Discovery greeting payload returned by the REST surface.
/// </summary>
public sealed record GreetingEnvelope(
    string Message,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<string> Traits);

/// <summary>
/// Deterministic platform time payload returned by the platform module.
/// </summary>
internal sealed record PlatformTimeEnvelope(DateTimeOffset UtcNow, string Kind);

internal sealed record JsonRpcRequest(
    string? JsonRpc,
    string? Method,
    Dictionary<string, string?>? Params,
    string? Id)
{
    public const string Version = "2.0";
}

internal sealed record JsonRpcResponse(
    string JsonRpc,
    object? Result,
    JsonRpcError? Error,
    string? Id);

internal sealed record JsonRpcError(int Code, string Message);

/// <summary>
/// Server-sent event payload for discovery principles.
/// </summary>
internal sealed record PrincipleEnvelope(string Principle);
