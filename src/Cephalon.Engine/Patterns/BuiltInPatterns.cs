using Cephalon.Abstractions.Patterns;

namespace Cephalon.Engine.Patterns;

/// <summary>
/// Provides the built-in pattern descriptors used by Cephalon app profiles.
/// </summary>
public static class BuiltInPatterns
{
    /// <summary>
    /// Gets the modular-architecture composition pattern.
    /// </summary>
    public static PatternDescriptor ModularArchitecture { get; } = new(
        id: "modular-architecture",
        displayName: "Modular Architecture",
        description: "Composes the application from explicit modules with bounded responsibilities.",
        kind: PatternKind.Composition,
        aliases: ["ModularArchitecture"],
        tags: ["architecture", "modular"]);

    /// <summary>
    /// Gets the single-host deployment topology pattern.
    /// </summary>
    public static PatternDescriptor SingleHostTopology { get; } = new(
        id: "single-host-topology",
        displayName: "Single Host Topology",
        description: "Runs the application as one deployable host while preserving internal boundaries.",
        kind: PatternKind.Deployment,
        aliases: ["SingleHostTopology", "SingleHost"],
        tags: ["topology", "monolith"],
        conflictsWith: ["microservice-topology"]);

    /// <summary>
    /// Gets the microservice deployment topology pattern.
    /// </summary>
    public static PatternDescriptor MicroserviceTopology { get; } = new(
        id: "microservice-topology",
        displayName: "Microservice Topology",
        description: "Deploys the application as an independently deployable service boundary.",
        kind: PatternKind.Deployment,
        aliases: ["MicroserviceTopology", "Microservice"],
        tags: ["topology", "microservice"],
        conflictsWith: ["single-host-topology"]);

    /// <summary>
    /// Gets the vertical-slice organization pattern.
    /// </summary>
    public static PatternDescriptor VerticalSliceOrganization { get; } = new(
        id: "vertical-slice-organization",
        displayName: "Vertical Slice Organization",
        description: "Organizes code around features so endpoints, handlers, and rules stay together.",
        kind: PatternKind.Organization,
        aliases: ["VerticalSliceOrganization", "VerticalSlice"],
        tags: ["organization", "vertical-slice"],
        conflictsWith: ["module-first-organization"]);

    /// <summary>
    /// Gets the module-first organization pattern.
    /// </summary>
    public static PatternDescriptor ModuleFirstOrganization { get; } = new(
        id: "module-first-organization",
        displayName: "Module-First Organization",
        description: "Organizes code around modules first, then features inside each module.",
        kind: PatternKind.Organization,
        aliases: ["ModuleFirstOrganization", "ModuleFirst"],
        tags: ["organization", "module-first"],
        conflictsWith: ["vertical-slice-organization"]);

    /// <summary>
    /// Gets the shared-foundation pattern.
    /// </summary>
    public static PatternDescriptor SharedFoundationPattern { get; } = new(
        id: "shared-foundation-pattern",
        displayName: "Shared Foundation Pattern",
        description: "Uses the Cephalon engine foundation for contracts, runtime conventions, and diagnostics.",
        kind: PatternKind.Foundation,
        aliases: ["SharedFoundationPattern", "SharedFoundation"],
        tags: ["foundation", "platform"]);

    /// <summary>
    /// Gets the strategy design pattern.
    /// </summary>
    public static PatternDescriptor StrategyPattern { get; } = new(
        id: "strategy-pattern",
        displayName: "Strategy Pattern",
        description: "Enables pluggable behaviors that can be swapped without changing the calling feature.",
        kind: PatternKind.Design,
        aliases: ["StrategyPattern", "Strategy"],
        tags: ["design-pattern", "behavior"]);

    /// <summary>
    /// Gets the pipeline design pattern.
    /// </summary>
    public static PatternDescriptor PipelinePattern { get; } = new(
        id: "pipeline-pattern",
        displayName: "Pipeline Pattern",
        description: "Applies behavior through ordered stages such as validation, enrichment, and execution.",
        kind: PatternKind.Design,
        aliases: ["PipelinePattern", "Pipeline"],
        tags: ["design-pattern", "pipeline"]);

    /// <summary>
    /// Gets the mediator design pattern.
    /// </summary>
    public static PatternDescriptor MediatorPattern { get; } = new(
        id: "mediator-pattern",
        displayName: "Mediator Pattern",
        description: "Routes requests through handlers to keep senders and receivers decoupled.",
        kind: PatternKind.Design,
        aliases: ["MediatorPattern", "Mediator"],
        tags: ["design-pattern", "mediator"]);

    /// <summary>
    /// Gets the specification design pattern.
    /// </summary>
    public static PatternDescriptor SpecificationPattern { get; } = new(
        id: "specification-pattern",
        displayName: "Specification Pattern",
        description: "Encapsulates reusable business rules and query predicates behind explicit specifications.",
        kind: PatternKind.Design,
        aliases: ["SpecificationPattern", "Specification"],
        tags: ["design-pattern", "specification"]);

    /// <summary>
    /// Gets the hexagonal-architecture pattern.
    /// </summary>
    public static PatternDescriptor HexagonalArchitecture { get; } = new(
        id: "hexagonal-architecture",
        displayName: "Hexagonal Architecture",
        description: "Keeps domain logic at the center and isolates infrastructure behind explicit ports and adapters.",
        kind: PatternKind.Architecture,
        aliases: ["HexagonalArchitecture", "Hexagonal", "PortsAndAdapters"],
        tags: ["architecture", "hexagonal", "ports-and-adapters"]);

    /// <summary>
    /// Gets the layered-architecture pattern.
    /// </summary>
    public static PatternDescriptor LayeredArchitecture { get; } = new(
        id: "layered-architecture",
        displayName: "Layered Architecture",
        description: "Organizes responsibilities into explicit layers with clear direction of dependency flow.",
        kind: PatternKind.Architecture,
        aliases: ["LayeredArchitecture", "Layered"],
        tags: ["architecture", "layered"]);

    /// <summary>
    /// Gets the clean-architecture pattern.
    /// </summary>
    public static PatternDescriptor CleanArchitecture { get; } = new(
        id: "clean-architecture",
        displayName: "Clean Architecture",
        description: "Keeps domain and application rules independent from infrastructure and delivery details.",
        kind: PatternKind.Architecture,
        aliases: ["CleanArchitecture", "Clean"],
        tags: ["architecture", "clean", "boundaries"]);

    /// <summary>
    /// Gets the onion-architecture pattern.
    /// </summary>
    public static PatternDescriptor OnionArchitecture { get; } = new(
        id: "onion-architecture",
        displayName: "Onion Architecture",
        description: "Organizes the app in concentric rings so dependencies point inward toward the core domain model.",
        kind: PatternKind.Architecture,
        aliases: ["OnionArchitecture", "Onion"],
        tags: ["architecture", "onion", "concentric-layers"]);

    /// <summary>
    /// Gets the strangler-fig migration pattern.
    /// </summary>
    public static PatternDescriptor StranglerFigPattern { get; } = new(
        id: "strangler-fig",
        displayName: "Strangler Fig",
        description: "Routes traffic incrementally between legacy and new Cephalon boundaries so modernization can happen without one cutover.",
        kind: PatternKind.Architecture,
        aliases: ["StranglerFig", "Strangler"],
        tags: ["architecture", "migration", "incremental-modernization"]);

    /// <summary>
    /// Gets the backend-for-frontend pattern.
    /// </summary>
    public static PatternDescriptor BackendForFrontendPattern { get; } = new(
        id: "backend-for-frontend",
        displayName: "Backend for Frontend",
        description: "Shapes one backend surface per client experience so transport, payload, and policy choices can stay explicit per frontend.",
        kind: PatternKind.Architecture,
        aliases: ["BackendForFrontend", "BFF"],
        tags: ["architecture", "bff", "client-specific"]);

    /// <summary>
    /// Gets the domain-driven-design pattern.
    /// </summary>
    public static PatternDescriptor DomainDrivenDesign { get; } = new(
        id: "domain-driven-design",
        displayName: "Domain-Driven Design",
        description: "Centers the model on domain language, aggregates, invariants, and explicit bounded contexts.",
        kind: PatternKind.Domain,
        aliases: ["DomainDrivenDesign", "DDD"],
        tags: ["domain", "ddd", "modeling"]);

    /// <summary>
    /// Gets the anti-corruption-layer pattern.
    /// </summary>
    public static PatternDescriptor AntiCorruptionLayer { get; } = new(
        id: "anti-corruption-layer",
        displayName: "Anti-Corruption Layer",
        description: "Protects the core domain model by translating across external, legacy, or upstream integration boundaries.",
        kind: PatternKind.Domain,
        aliases: ["AntiCorruptionLayer", "ACL"],
        tags: ["domain", "integration-boundary", "translation"]);

    /// <summary>
    /// Gets the CQRS pattern.
    /// </summary>
    public static PatternDescriptor CqrsPattern { get; } = new(
        id: "cqrs",
        displayName: "CQRS",
        description: "Separates command-side and query-side responsibilities so write and read concerns can evolve independently.",
        kind: PatternKind.Data,
        aliases: ["Cqrs", "CQRS", "CqrsPattern"],
        tags: ["data", "cqrs", "read-write-split"]);

    /// <summary>
    /// Gets the outbox pattern.
    /// </summary>
    public static PatternDescriptor OutboxPattern { get; } = new(
        id: "outbox",
        displayName: "Outbox",
        description: "Coordinates persistence and message publication through a durable handoff that can be replayed safely.",
        kind: PatternKind.Data,
        aliases: ["Outbox", "OutboxPattern"],
        tags: ["data", "messaging", "outbox"]);

    /// <summary>
    /// Gets the event-sourcing pattern.
    /// </summary>
    public static PatternDescriptor EventSourcingPattern { get; } = new(
        id: "event-sourcing",
        displayName: "Event Sourcing",
        description: "Represents state changes as an ordered stream of domain events instead of only storing current state.",
        kind: PatternKind.Data,
        aliases: ["EventSourcing", "EventSourcingPattern"],
        tags: ["data", "events", "event-sourcing"]);

    private static readonly PatternDescriptor[] Items =
    [
        ModularArchitecture,
        SingleHostTopology,
        MicroserviceTopology,
        VerticalSliceOrganization,
        ModuleFirstOrganization,
        SharedFoundationPattern,
        StrategyPattern,
        PipelinePattern,
        MediatorPattern,
        SpecificationPattern,
        HexagonalArchitecture,
        LayeredArchitecture,
        CleanArchitecture,
        OnionArchitecture,
        StranglerFigPattern,
        BackendForFrontendPattern,
        DomainDrivenDesign,
        AntiCorruptionLayer,
        CqrsPattern,
        OutboxPattern,
        EventSourcingPattern
    ];

    private static readonly Dictionary<string, PatternDescriptor> Index = CreateIndex();

    /// <summary>
    /// Gets all built-in pattern descriptors.
    /// </summary>
    public static IReadOnlyList<PatternDescriptor> All => Items;

    /// <summary>
    /// Attempts to resolve a pattern identifier, display name, or alias.
    /// </summary>
    /// <param name="value">The pattern identifier, display name, or alias to resolve.</param>
    /// <param name="pattern">The resolved pattern descriptor when the lookup succeeds.</param>
    /// <returns><see langword="true" /> when the pattern was resolved; otherwise, <see langword="false" />.</returns>
    public static bool TryResolve(string value, out PatternDescriptor pattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return Index.TryGetValue(NormalizeKey(value), out pattern!);
    }

    /// <summary>
    /// Resolves a pattern identifier, display name, or alias.
    /// </summary>
    /// <param name="value">The pattern identifier, display name, or alias to resolve.</param>
    /// <returns>The resolved pattern descriptor.</returns>
    public static PatternDescriptor Resolve(string value)
    {
        if (TryResolve(value, out var pattern))
        {
            return pattern;
        }

        throw new InvalidOperationException(
            $"Pattern '{value}' is not supported. Supported patterns: {string.Join(", ", Items.Select(item => item.DisplayName))}.");
    }

    private static Dictionary<string, PatternDescriptor> CreateIndex()
    {
        var index = new Dictionary<string, PatternDescriptor>(StringComparer.Ordinal);

        foreach (var item in Items)
        {
            Add(index, item);
        }

        return index;
    }

    private static void Add(Dictionary<string, PatternDescriptor> index, PatternDescriptor pattern)
    {
        index[NormalizeKey(pattern.Id)] = pattern;
        index[NormalizeKey(pattern.DisplayName)] = pattern;

        foreach (var alias in pattern.Aliases)
        {
            index[NormalizeKey(alias)] = pattern;
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
