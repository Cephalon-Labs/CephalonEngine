using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Localization;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Agentics.Services;
using Cephalon.AspNetCore.Grpc.Modules;
using Cephalon.AspNetCore.Modules;
using Cephalon.AspNetCore.JsonRpc.Modules;
using Cephalon.Edge.Services;
using Microsoft.AspNetCore.Builder;
using Cephalon.AspNetCore.Transports.ServerSentEvents;
using Cephalon.AspNetCore.Transports.WebSockets;
using Cephalon.Eventing.Services;
using Cephalon.Playground.Services;
using Cephalon.Retrieval.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Cephalon.Playground.Modules;

public sealed class DiscoveryModule : ModuleBase, IEndpointModule, IJsonRpcModule, IGrpcModule, IServerSentEventsModule, IWebSocketModule, ILocalizedResourceContributor, ITechnologyContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "discovery",
        displayName: "Discovery",
        description: "Human-friendly discovery endpoints that reveal the framework posture.",
        dependsOn: [typeof(PlatformModule)],
        tags: ["experience", "api"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "experience",
            ["surface"] = "multi-transport"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<GreetingComposer>();
        services.AddTransient<DiscoveryGrpcService>();
        services.AddSingleton<IAgentToolContributor, DiscoveryAgentToolContributor>();
        services.AddSingleton<IKnowledgeCollectionContributor, DiscoveryKnowledgeCollectionContributor>();
        services.AddSingleton<IEventChannelContributor, DiscoveryEventChannelContributor>();
        services.AddSingleton<IEdgeNodeContributor, DiscoveryEdgeNodeContributor>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "discovery.greetings",
            displayName: "Greeting Composer",
            description: "Builds starter responses that reflect the current runtime state.",
            metadata: new Dictionary<string, string>
            {
                ["category"] = "experience",
                ["dependsOn"] = "platform.clock"
            }));
    }

    public void RegisterResources(ILocalizedResourceRegistry resources)
    {
        resources.Add("es", new Dictionary<string, string>
        {
            ["engine.docs.rest.title"] = "API REST de Cephalon Playground",
            ["engine.docs.rest.description"] = "Superficie REST expuesta por el host de Cephalon Playground.",
            ["engine.docs.scalar.title"] = "Referencia REST de Cephalon Playground"
        });
    }

    public void RegisterTechnologies(ITechnologyRegistry technologies)
    {
        technologies.Add(new TechnologyDescriptor(
            id: "digital-twin-orchestration",
            displayName: "Digital Twin Orchestration",
            description: "Prepares the playground for digital-twin coordination, telemetry loops, and state synchronization across live transports.",
            kind: TechnologyKind.Experience,
            requiresTransports: ["websocket"],
            packageHints: ["Cephalon.DigitalTwin"],
            guidance:
            [
                "Keep simulation and twin-state orchestration behind module services so transports stay replaceable.",
                "Prefer explicit live-update contracts when twin state needs to flow over WebSocket, SSE, or gRPC streams."
            ]));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/discovery");
        group.MapGet("/hello/{name?}", (string? name, GreetingComposer composer) =>
            TypedResults.Ok(composer.Compose(name)));
        group.MapGet("/principles", () => TypedResults.Ok(DiscoveryDefaults.Principles));
    }

    public void MapJsonRpcEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/discovery");
        group.MapPost("/", async context =>
        {
            var composer = context.RequestServices.GetRequiredService<GreetingComposer>();
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
            var composer = context.RequestServices.GetRequiredService<GreetingComposer>();

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
                "Cephalon discovery stream completed.",
                context.RequestAborted);
        });
    }
}

/// <summary>
/// JSON-RPC request envelope for the discovery transport.
/// </summary>
public sealed record JsonRpcRequest(
    string? JsonRpc,
    string? Method,
    Dictionary<string, string?>? Params,
    string? Id)
{
    public const string Version = "2.0";
}

public sealed record JsonRpcResponse(
    string JsonRpc,
    object? Result,
    JsonRpcError? Error,
    string? Id);

public sealed record JsonRpcError(int Code, string Message);

/// <summary>
/// Discovery principle event payload exposed by the SSE transport.
/// </summary>
public sealed record PrincipleEnvelope(string Principle);

internal sealed class DiscoveryAgentToolContributor : IAgentToolContributor
{
    public void RegisterTools(IAgentToolRegistry tools)
    {
        tools.Add(new AgentToolDescriptor(
            id: "discovery.principles",
            displayName: "Discovery Principles",
            description: "Returns future-facing framework principles from the discovery module.",
            tags: ["discovery", "principles"]));
    }
}

internal sealed class DiscoveryKnowledgeCollectionContributor : IKnowledgeCollectionContributor
{
    public void RegisterCollections(IKnowledgeCollectionRegistry collections)
    {
        collections.Add(new KnowledgeCollectionDescriptor(
            id: "discovery-streams",
            displayName: "Discovery Streams",
            description: "Knowledge collection describing discovery streams and interactive endpoints.",
            tags: ["discovery", "streams"]));
    }
}

internal sealed class DiscoveryEventChannelContributor : IEventChannelContributor
{
    public void RegisterChannels(IEventChannelRegistry channels)
    {
        channels.Add(new EventChannelDescriptor(
            id: "discovery-updates",
            displayName: "Discovery Updates",
            description: "Event stream carrying discovery-oriented notifications and lifecycle updates.",
            tags: ["discovery", "updates"]));
    }
}

internal sealed class DiscoveryEdgeNodeContributor : IEdgeNodeContributor
{
    public void RegisterNodes(IEdgeNodeRegistry nodes)
    {
        nodes.Add(new EdgeNodeDescriptor(
            id: "discovery-kiosk",
            displayName: "Discovery Kiosk",
            description: "Edge-facing kiosk node for intermittently connected showcase deployments.",
            tags: ["discovery", "kiosk"]));
    }
}
