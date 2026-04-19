using Cephalon.Abstractions.Technologies;

namespace Cephalon.Engine.Technologies;

/// <summary>
/// Provides the built-in technology descriptors used by Cephalon app profiles.
/// </summary>
public static class BuiltInTechnologies
{
    /// <summary>
    /// Gets the built-in agentic-workloads technology profile.
    /// </summary>
    public static TechnologyDescriptor AgenticWorkloads { get; } = new(
        id: "agentic-workloads",
        displayName: "Agentic Workloads",
        description: "Prepares the app model for assistants, tool-using agents, and long-running autonomous workflows.",
        kind: TechnologyKind.Intelligence,
        aliases: ["AgenticWorkloads", "Agentic"],
        tags: ["ai", "agents", "llm", "automation"],
        packageHints: ["Cephalon.Agentics"],
        guidance:
        [
            "Keep model providers and tool adapters behind module services or strategy slots.",
            "Prefer explicit capability contracts for tools, memory, and agent actions instead of ambient cross-module calls.",
            "Use worker-host or background-runtime paths for long-running agent loops, retries, and scheduled tasks."
        ]);

    /// <summary>
    /// Gets the built-in event-driven-integration technology profile.
    /// </summary>
    public static TechnologyDescriptor EventDrivenIntegration { get; } = new(
        id: "event-driven-integration",
        displayName: "Event-Driven Integration",
        description: "Prepares modules for broker-backed events, asynchronous workflows, and eventually consistent integration flows.",
        kind: TechnologyKind.Messaging,
        aliases: ["EventDrivenIntegration", "EventDriven"],
        tags: ["events", "messaging", "broker", "async"],
        packageHints: ["Cephalon.Eventing"],
        guidance:
        [
            "Keep messages and events as explicit contracts owned by modules.",
            "Design consumers to be idempotent and resilient to re-delivery or delayed processing.",
            "Isolate broker or queue SDKs behind adapters so infrastructure choices can evolve later."
        ]);

    /// <summary>
    /// Gets the built-in knowledge-retrieval technology profile.
    /// </summary>
    public static TechnologyDescriptor KnowledgeRetrieval { get; } = new(
        id: "knowledge-retrieval",
        displayName: "Knowledge Retrieval",
        description: "Prepares the app for semantic retrieval, indexing, search, and knowledge-backed experiences.",
        kind: TechnologyKind.Data,
        aliases: ["KnowledgeRetrieval", "Knowledge"],
        tags: ["knowledge", "retrieval", "search", "vector"],
        packageHints: ["Cephalon.Retrieval"],
        guidance:
        [
            "Keep indexing and retrieval contracts provider-neutral so vector, graph, and search backends remain swappable.",
            "Separate ingestion pipelines from query-time retrieval flows so each can scale independently.",
            "Treat retrieval components as supporting services under modules instead of leaking storage details into transports."
        ]);

    /// <summary>
    /// Gets the built-in realtime-experience technology profile.
    /// </summary>
    public static TechnologyDescriptor RealtimeExperience { get; } = new(
        id: "realtime-experience",
        displayName: "Realtime Experience",
        description: "Prepares the app for live collaboration, presence, streaming updates, and reactive client experiences.",
        kind: TechnologyKind.Experience,
        aliases: ["RealtimeExperience", "Realtime"],
        tags: ["realtime", "presence", "collaboration", "streaming"],
        guidance:
        [
            "Prefer streaming-capable transports such as WebSocket, Server-Sent Events, or gRPC when live interaction matters.",
            "Keep backpressure, fan-out, and session coordination concerns outside domain modules where possible.",
            "Model live updates as explicit contracts so clients can move between transports without rewriting domain behavior."
        ]);

    /// <summary>
    /// Gets the built-in edge-native-delivery technology profile.
    /// </summary>
    public static TechnologyDescriptor EdgeNativeDelivery { get; } = new(
        id: "edge-native-delivery",
        displayName: "Edge-Native Delivery",
        description: "Prepares the app for browser, device, edge, and intermittently connected deployment scenarios.",
        kind: TechnologyKind.Deployment,
        aliases: ["EdgeNativeDelivery", "EdgeNative", "Edge"],
        tags: ["edge", "offline", "hybrid", "device"],
        packageHints: ["Cephalon.Edge"],
        guidance:
        [
            "Keep state externalized and tolerate partial connectivity, local caching, or delayed synchronization.",
            "Isolate device, browser, or edge-host integrations behind adapters so the core runtime stays portable.",
            "Prefer graceful degradation paths instead of assuming permanent connectivity to central infrastructure."
        ]);

    /// <summary>
    /// Gets the built-in identity-access technology profile.
    /// </summary>
    public static TechnologyDescriptor IdentityAccess { get; } = new(
        id: "identity-access",
        displayName: "Identity Access",
        description: "Prepares the app for configurable authentication and authorization flows such as RBAC, ABAC, and policy evaluation.",
        kind: TechnologyKind.Security,
        aliases: ["IdentityAccess", "Identity"],
        tags: ["security", "identity", "authorization", "authn", "authz"],
        packageHints: ["Cephalon.Identity"],
        guidance:
        [
            "Keep identity and authorization decisions behind host-agnostic contracts so host adapters stay thin.",
            "Treat RBAC, ABAC, and policy-based evaluation as configuration-driven modes instead of hard-coded endpoint logic.",
            "Keep infrastructure-specific principal, token, and scheme details inside adapter packages."
        ]);

    /// <summary>
    /// Gets the built-in multi-tenancy technology profile.
    /// </summary>
    public static TechnologyDescriptor MultiTenancy { get; } = new(
        id: "multi-tenancy",
        displayName: "Multi-Tenancy",
        description: "Prepares the app for tenant-aware routing, isolation, membership, and runtime answers.",
        kind: TechnologyKind.Platform,
        aliases: ["MultiTenancy", "Multitenancy"],
        tags: ["platform", "tenancy", "tenant", "isolation"],
        packageHints: ["Cephalon.MultiTenancy"],
        guidance:
        [
            "Keep tenant resolution, membership, and domain mapping explicit instead of folding them into ad-hoc module state.",
            "Design audit, authorization, and runtime surfaces to remain tenant-aware when the technology is active.",
            "Prefer additive tenant policies and context resolution over hard-coded single-tenant assumptions."
        ]);

    /// <summary>
    /// Gets the built-in hybrid-cloud-runtime technology profile.
    /// </summary>
    public static TechnologyDescriptor HybridCloudRuntime { get; } = new(
        id: "hybrid-cloud-runtime",
        displayName: "Hybrid Cloud Runtime",
        description: "Prepares the app for mixed on-premises, edge, and cloud deployment handoffs without changing the engine core.",
        kind: TechnologyKind.Platform,
        aliases: ["HybridCloudRuntime", "HybridCloud"],
        tags: ["platform", "hybrid", "cloud", "runtime"],
        guidance:
        [
            "Keep platform-specific deployment and connectivity concerns inside companion packages or governance layers.",
            "Prefer explicit runtime surfaces that describe active topology assumptions instead of embedding cloud branches into modules.",
            "Treat hybrid deployment as an additive operational slice, not a new blueprint."
        ]);

    /// <summary>
    /// Gets the built-in service-mesh-integration technology profile.
    /// </summary>
    public static TechnologyDescriptor ServiceMeshIntegration { get; } = new(
        id: "service-mesh-integration",
        displayName: "Service Mesh Integration",
        description: "Prepares the app for additive service-mesh coordination, policy handoff, and traffic-governance guidance.",
        kind: TechnologyKind.Platform,
        aliases: ["ServiceMeshIntegration", "ServiceMesh"],
        tags: ["platform", "service-mesh", "traffic", "policy"],
        guidance:
        [
            "Keep mesh-specific traffic, policy, and identity handoff outside the engine core.",
            "Expose runtime metadata that helps operators understand mesh expectations when the technology is active.",
            "Prefer additive gateway or control-plane integration over engine-owned orchestration."
        ]);

    /// <summary>
    /// Gets the built-in serverless-hosting technology profile.
    /// </summary>
    public static TechnologyDescriptor ServerlessHosting { get; } = new(
        id: "serverless-hosting",
        displayName: "Serverless Hosting",
        description: "Prepares the app for event-triggered or function-style hosting without changing the host-agnostic core runtime model.",
        kind: TechnologyKind.Deployment,
        aliases: ["ServerlessHosting", "Serverless"],
        tags: ["deployment", "serverless", "functions", "hosting"],
        guidance:
        [
            "Keep host-trigger and cloud-function specifics inside adapters instead of `Cephalon.Abstractions` or `Cephalon.Engine`.",
            "Prefer explicit transport and execution surfaces that remain truthful when the hosting model is request-driven or event-triggered.",
            "Treat serverless delivery as an additive hosting slice, not as a replacement for the app profile or module model."
        ]);

    /// <summary>
    /// Gets the built-in cell-based-architecture technology profile.
    /// </summary>
    public static TechnologyDescriptor CellBasedArchitecture { get; } = new(
        id: "cell-based-architecture",
        displayName: "Cell-Based Architecture",
        description: "Prepares the app for explicit module-owned cell boundaries, governed cell routes, health-isolation posture, and configuration-driven traffic-automation follow-through.",
        kind: TechnologyKind.Platform,
        aliases: ["CellBasedArchitecture", "CellBased", "Cells"],
        tags: ["cells", "blast-radius", "isolation", "routing"],
        guidance:
        [
            "Keep cell boundaries module-owned and explicit instead of hiding them in host startup or deployment notes.",
            "Project blast-radius, routing, health-isolation posture, and effective traffic-automation answers through runtime catalogs so operators can inspect the active cell topology directly.",
            "Treat provider-specific traffic-management, failover, and health-isolation materialization as additive follow-through over the engine-owned boundary, route, and automation catalogs instead of bespoke host logic."
        ]);

    private static readonly TechnologyDescriptor[] Items =
    [
        AgenticWorkloads,
        EventDrivenIntegration,
        KnowledgeRetrieval,
        RealtimeExperience,
        EdgeNativeDelivery,
        IdentityAccess,
        MultiTenancy,
        HybridCloudRuntime,
        ServiceMeshIntegration,
        ServerlessHosting,
        CellBasedArchitecture
    ];

    private static readonly Dictionary<string, TechnologyDescriptor> Index = CreateIndex();

    /// <summary>
    /// Gets all built-in technology descriptors.
    /// </summary>
    public static IReadOnlyList<TechnologyDescriptor> All => Items;

    /// <summary>
    /// Attempts to resolve a technology identifier, display name, or alias.
    /// </summary>
    /// <param name="value">The technology identifier, display name, or alias to resolve.</param>
    /// <param name="technology">The resolved technology descriptor when the lookup succeeds.</param>
    /// <returns><see langword="true" /> when the technology was resolved; otherwise, <see langword="false" />.</returns>
    public static bool TryResolve(string value, out TechnologyDescriptor technology)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return Index.TryGetValue(NormalizeKey(value), out technology!);
    }

    /// <summary>
    /// Resolves a technology identifier, display name, or alias.
    /// </summary>
    /// <param name="value">The technology identifier, display name, or alias to resolve.</param>
    /// <returns>The resolved technology descriptor.</returns>
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

        foreach (var item in Items)
        {
            Add(index, item);
        }

        return index;
    }

    private static void Add(Dictionary<string, TechnologyDescriptor> index, TechnologyDescriptor technology)
    {
        index[NormalizeKey(technology.Id)] = technology;
        index[NormalizeKey(technology.DisplayName)] = technology;

        foreach (var alias in technology.Aliases)
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
