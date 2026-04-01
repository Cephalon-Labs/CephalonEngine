using Cephalon.Abstractions.Transports;

namespace Cephalon.Engine.Transports;

public static class BuiltInTransports
{
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

    private static readonly TransportDescriptor[] Items =
    [
        RestApi,
        JsonRpc,
        Grpc,
        ServerSentEvents,
        WebSocket
    ];

    private static readonly Dictionary<string, TransportDescriptor> Index = CreateIndex();

    public static IReadOnlyList<TransportDescriptor> All => Items;

    public static bool TryResolve(string value, out TransportDescriptor transport)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return Index.TryGetValue(NormalizeKey(value), out transport!);
    }

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
        Add(index, ServerSentEvents, "ServerSentEvents", "Sse");
        Add(index, WebSocket, "WebSocket", "WebSockets");

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
