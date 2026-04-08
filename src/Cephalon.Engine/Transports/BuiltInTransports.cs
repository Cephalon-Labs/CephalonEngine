using Cephalon.Abstractions.Transports;

namespace Cephalon.Engine.Transports;

/// <summary>
/// Provides the built-in transport descriptors used by Cephalon app profiles.
/// </summary>
public static class BuiltInTransports
{
    /// <summary>
    /// Gets the built-in REST transport descriptor.
    /// </summary>
    public static TransportDescriptor RestApi { get; } = new(
        id: "rest-api",
        displayName: "REST API",
        description: "Resource-oriented HTTP endpoints for request/response application integration.",
        features: TransportFeatures.RequestResponse,
        tags: ["http", "rest", "api"],
        metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["aspnet.adapterPackage"] = "Cephalon.AspNetCore",
            ["aspnet.registration"] = "Built into Cephalon.AspNetCore with OpenAPI and Scalar docs."
        });

    /// <summary>
    /// Gets the built-in JSON-RPC transport descriptor.
    /// </summary>
    public static TransportDescriptor JsonRpc { get; } = new(
        id: "json-rpc",
        displayName: "JSON-RPC",
        description: "Procedure-style JSON messaging over a transport chosen by the host.",
        features: TransportFeatures.RequestResponse,
        tags: ["json-rpc", "rpc", "json"],
        metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["aspnet.adapterPackage"] = "Cephalon.AspNetCore.JsonRpc",
            ["aspnet.registration"] = "Call AddJsonRpcTransport()."
        });

    /// <summary>
    /// Gets the built-in gRPC transport descriptor.
    /// </summary>
    public static TransportDescriptor Grpc { get; } = new(
        id: "grpc",
        displayName: "gRPC",
        description: "High-performance contract-first RPC with unary and streaming support.",
        features: TransportFeatures.RequestResponse |
                  TransportFeatures.ServerStreaming |
                  TransportFeatures.ClientStreaming |
                  TransportFeatures.DuplexStreaming,
        tags: ["grpc", "rpc", "protobuf", "streaming"],
        metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["aspnet.adapterPackage"] = "Cephalon.AspNetCore.Grpc",
            ["aspnet.registration"] = "Call AddGrpcTransport()."
        });

    /// <summary>
    /// Gets the built-in GraphQL transport descriptor.
    /// </summary>
    public static TransportDescriptor GraphQL { get; } = new(
        id: "graphql",
        displayName: "GraphQL",
        description: "Schema-based queries, mutations, and subscriptions over HTTP and WebSocket.",
        features: TransportFeatures.RequestResponse | TransportFeatures.DuplexStreaming,
        tags: ["graphql", "http", "schema", "api"],
        metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["aspnet.adapterPackage"] = "Cephalon.AspNetCore.GraphQL",
            ["aspnet.registration"] = "Call AddGraphQLTransport()."
        });

    /// <summary>
    /// Gets the built-in server-sent-events transport descriptor.
    /// </summary>
    public static TransportDescriptor ServerSentEvents { get; } = new(
        id: "server-sent-events",
        displayName: "Server-Sent Events",
        description: "HTTP-based server push for one-way event streaming from server to client.",
        features: TransportFeatures.ServerStreaming,
        tags: ["http", "sse", "events", "streaming"],
        metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["aspnet.adapterPackage"] = "Cephalon.AspNetCore",
            ["aspnet.registration"] = "Built into Cephalon.AspNetCore."
        });

    /// <summary>
    /// Gets the built-in WebSocket transport descriptor.
    /// </summary>
    public static TransportDescriptor WebSocket { get; } = new(
        id: "websocket",
        displayName: "WebSocket",
        description: "Persistent bidirectional messaging between client and server.",
        features: TransportFeatures.DuplexStreaming,
        tags: ["websocket", "socket", "realtime", "streaming"],
        metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["aspnet.adapterPackage"] = "Cephalon.AspNetCore",
            ["aspnet.registration"] = "Built into Cephalon.AspNetCore."
        });

    /// <summary>
    /// Gets the built-in behavior HTTP transport descriptor that bridges behavior topology
    /// bindings to ASP.NET Core endpoints, including canonical versioned routes for route-shaped
    /// transports without the older <c>/behaviors/{id}</c> compatibility aliases.
    /// </summary>
    public static TransportDescriptor BehaviorHttp { get; } = new(
        id: "behavior-http",
        displayName: "Behavior HTTP",
        description: "Aggregate transport that maps registered behavior topologies to per-behavior HTTP endpoints using shared canonical routes for REST, GraphQL, JSON-RPC, GraphQL-SSE, GraphQL-WS, SSE, and WebSocket.",
        features: TransportFeatures.RequestResponse |
                  TransportFeatures.ServerStreaming |
                  TransportFeatures.DuplexStreaming,
        tags: ["http", "behaviors", "rest", "sse", "websocket", "graphql", "jsonrpc"],
        metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["aspnet.adapterPackage"] = "Cephalon.Behaviors.Http",
            ["aspnet.registration"] = "Call AddHttpBehaviorBindings() inside AddBehaviors()."
        });

    private static readonly TransportDescriptor[] Items =
    [
        RestApi,
        JsonRpc,
        Grpc,
        GraphQL,
        ServerSentEvents,
        WebSocket,
        BehaviorHttp
    ];

    private static readonly Dictionary<string, TransportDescriptor> Index = CreateIndex();

    /// <summary>
    /// Gets all built-in transport descriptors.
    /// </summary>
    public static IReadOnlyList<TransportDescriptor> All => Items;

    /// <summary>
    /// Attempts to resolve a transport identifier, display name, or alias.
    /// </summary>
    /// <param name="value">The transport identifier, display name, or alias to resolve.</param>
    /// <param name="transport">The resolved transport descriptor when the lookup succeeds.</param>
    /// <returns><see langword="true" /> when the transport was resolved; otherwise, <see langword="false" />.</returns>
    public static bool TryResolve(string value, out TransportDescriptor transport)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return Index.TryGetValue(NormalizeKey(value), out transport!);
    }

    /// <summary>
    /// Resolves a transport identifier, display name, or alias.
    /// </summary>
    /// <param name="value">The transport identifier, display name, or alias to resolve.</param>
    /// <returns>The resolved transport descriptor.</returns>
    public static TransportDescriptor Resolve(string value)
    {
        if (TryResolve(value, out var transport))
        {
            return transport;
        }

        throw new InvalidOperationException(
            $"Transport '{value}' is not supported. Supported transports: {string.Join(", ", Items.Select(item => item.DisplayName))}.");
    }

    private static Dictionary<string, TransportDescriptor> CreateIndex()
    {
        var index = new Dictionary<string, TransportDescriptor>(StringComparer.Ordinal);

        Add(index, RestApi, "RestApi", "Rest", "HttpApi");
        Add(index, JsonRpc, "JsonRpc");
        Add(index, Grpc, "Grpc");
        Add(index, GraphQL, "GraphQL");
        Add(index, ServerSentEvents, "ServerSentEvents", "Sse");
        Add(index, WebSocket, "WebSocket", "WebSockets");
        Add(index, BehaviorHttp, "BehaviorHttp", "BehaviorTransport");

        return index;
    }

    private static void Add(
        Dictionary<string, TransportDescriptor> index,
        TransportDescriptor transport,
        params string[] aliases)
    {
        index[NormalizeKey(transport.Id)] = transport;
        index[NormalizeKey(transport.DisplayName)] = transport;

        foreach (var alias in aliases)
        {
            index[NormalizeKey(alias)] = transport;
        }
    }

    private static string NormalizeKey(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
    }
}
