using Cephalon.Abstractions.Technologies;

namespace Cephalon.Engine.Technologies;

public static class BuiltInTechnologies
{
    public static TechnologyDescriptor AgenticWorkloads { get; } = new(
        id: "agentic-workloads",
        displayName: "Agentic Workloads",
        description: "Prepares the app model for assistants, tool-using agents, and long-running autonomous workflows.",
        kind: TechnologyKind.Intelligence,
        tags: ["ai", "agents", "llm", "automation"],
        packageHints: ["Cephalon.Agentics"],
        guidance:
        [
            "Keep model providers and tool adapters behind module services or strategy slots.",
            "Prefer explicit capability contracts for tools, memory, and agent actions instead of ambient cross-module calls.",
            "Use worker-host or background-runtime paths for long-running agent loops, retries, and scheduled tasks."
        ]);

    public static TechnologyDescriptor EventDrivenIntegration { get; } = new(
        id: "event-driven-integration",
        displayName: "Event-Driven Integration",
        description: "Prepares modules for broker-backed events, asynchronous workflows, and eventually consistent integration flows.",
        kind: TechnologyKind.Messaging,
        tags: ["events", "messaging", "broker", "async"],
        packageHints: ["Cephalon.Eventing"],
        guidance:
        [
            "Keep messages and events as explicit contracts owned by modules.",
            "Design consumers to be idempotent and resilient to re-delivery or delayed processing.",
            "Isolate broker or queue SDKs behind adapters so infrastructure choices can evolve later."
        ]);

    public static TechnologyDescriptor KnowledgeRetrieval { get; } = new(
        id: "knowledge-retrieval",
        displayName: "Knowledge Retrieval",
        description: "Prepares the app for semantic retrieval, indexing, search, and knowledge-backed experiences.",
        kind: TechnologyKind.Data,
        tags: ["knowledge", "retrieval", "search", "vector"],
        packageHints: ["Cephalon.Retrieval"],
        guidance:
        [
            "Keep indexing and retrieval contracts provider-neutral so vector, graph, and search backends remain swappable.",
            "Separate ingestion pipelines from query-time retrieval flows so each can scale independently.",
            "Treat retrieval components as supporting services under modules instead of leaking storage details into transports."
        ]);

    public static TechnologyDescriptor RealtimeExperience { get; } = new(
        id: "realtime-experience",
        displayName: "Realtime Experience",
        description: "Prepares the app for live collaboration, presence, streaming updates, and reactive client experiences.",
        kind: TechnologyKind.Experience,
        tags: ["realtime", "presence", "collaboration", "streaming"],
        guidance:
        [
            "Prefer streaming-capable transports such as WebSocket, Server-Sent Events, or gRPC when live interaction matters.",
            "Keep backpressure, fan-out, and session coordination concerns outside domain modules where possible.",
            "Model live updates as explicit contracts so clients can move between transports without rewriting domain behavior."
        ]);

    public static TechnologyDescriptor EdgeNativeDelivery { get; } = new(
        id: "edge-native-delivery",
        displayName: "Edge-Native Delivery",
        description: "Prepares the app for browser, device, edge, and intermittently connected deployment scenarios.",
        kind: TechnologyKind.Deployment,
        tags: ["edge", "offline", "hybrid", "device"],
        packageHints: ["Cephalon.Edge"],
        guidance:
        [
            "Keep state externalized and tolerate partial connectivity, local caching, or delayed synchronization.",
            "Isolate device, browser, or edge-host integrations behind adapters so the core runtime stays portable.",
            "Prefer graceful degradation paths instead of assuming permanent connectivity to central infrastructure."
        ]);

    private static readonly TechnologyDescriptor[] Items =
    [
        AgenticWorkloads,
        EventDrivenIntegration,
        KnowledgeRetrieval,
        RealtimeExperience,
        EdgeNativeDelivery
    ];

    private static readonly Dictionary<string, TechnologyDescriptor> Index = CreateIndex();

    public static IReadOnlyList<TechnologyDescriptor> All => Items;

    public static bool TryResolve(string value, out TechnologyDescriptor technology)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return Index.TryGetValue(NormalizeKey(value), out technology!);
    }

    public static TechnologyDescriptor Resolve(string value)
    {
        if (TryResolve(value, out var technology))
        {
            return technology;
        }

        throw new InvalidOperationException(
            $"Technology '{value}' is not supported. Supported technologies: {string.Join(", ", Items.Select(item => item.DisplayName))}.");
    }

    private static Dictionary<string, TechnologyDescriptor> CreateIndex()
    {
        var index = new Dictionary<string, TechnologyDescriptor>(StringComparer.Ordinal);

        Add(index, AgenticWorkloads, "AgenticWorkloads", "Agentic");
        Add(index, EventDrivenIntegration, "EventDrivenIntegration", "EventDriven");
        Add(index, KnowledgeRetrieval, "KnowledgeRetrieval", "Knowledge");
        Add(index, RealtimeExperience, "RealtimeExperience", "Realtime");
        Add(index, EdgeNativeDelivery, "EdgeNativeDelivery", "EdgeNative", "Edge");

        return index;
    }

    private static void Add(
        Dictionary<string, TechnologyDescriptor> index,
        TechnologyDescriptor technology,
        params string[] aliases)
    {
        index[NormalizeKey(technology.Id)] = technology;
        index[NormalizeKey(technology.DisplayName)] = technology;

        foreach (var alias in aliases)
        {
            index[NormalizeKey(alias)] = technology;
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
