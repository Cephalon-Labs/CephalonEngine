using Cephalon.Abstractions.Patterns;

namespace Cephalon.Engine.Patterns;

public static class BuiltInPatterns
{
    public static PatternDescriptor ModularArchitecture { get; } = new(
        id: "modular-architecture",
        displayName: "Modular Architecture",
        description: "Composes the application from explicit modules with bounded responsibilities.",
        kind: PatternKind.Composition,
        tags: ["architecture", "modular"]);

    public static PatternDescriptor SingleHostTopology { get; } = new(
        id: "single-host-topology",
        displayName: "Single Host Topology",
        description: "Runs the application as one deployable host while preserving internal boundaries.",
        kind: PatternKind.Deployment,
        tags: ["topology", "monolith"],
        conflictsWith: ["microservice-topology"]);

    public static PatternDescriptor MicroserviceTopology { get; } = new(
        id: "microservice-topology",
        displayName: "Microservice Topology",
        description: "Deploys the application as an independently deployable service boundary.",
        kind: PatternKind.Deployment,
        tags: ["topology", "microservice"],
        conflictsWith: ["single-host-topology"]);

    public static PatternDescriptor VerticalSliceOrganization { get; } = new(
        id: "vertical-slice-organization",
        displayName: "Vertical Slice Organization",
        description: "Organizes code around features so endpoints, handlers, and rules stay together.",
        kind: PatternKind.Organization,
        tags: ["organization", "vertical-slice"],
        conflictsWith: ["module-first-organization"]);

    public static PatternDescriptor ModuleFirstOrganization { get; } = new(
        id: "module-first-organization",
        displayName: "Module-First Organization",
        description: "Organizes code around modules first, then features inside each module.",
        kind: PatternKind.Organization,
        tags: ["organization", "module-first"],
        conflictsWith: ["vertical-slice-organization"]);

    public static PatternDescriptor SharedFoundationPattern { get; } = new(
        id: "shared-foundation-pattern",
        displayName: "Shared Foundation Pattern",
        description: "Uses the Cephalon engine foundation for contracts, runtime conventions, and diagnostics.",
        kind: PatternKind.Foundation,
        tags: ["foundation", "platform"]);

    public static PatternDescriptor StrategyPattern { get; } = new(
        id: "strategy-pattern",
        displayName: "Strategy Pattern",
        description: "Enables pluggable behaviors that can be swapped without changing the calling feature.",
        kind: PatternKind.Design,
        tags: ["design-pattern", "behavior"]);

    public static PatternDescriptor PipelinePattern { get; } = new(
        id: "pipeline-pattern",
        displayName: "Pipeline Pattern",
        description: "Applies behavior through ordered stages such as validation, enrichment, and execution.",
        kind: PatternKind.Design,
        tags: ["design-pattern", "pipeline"]);

    public static PatternDescriptor MediatorPattern { get; } = new(
        id: "mediator-pattern",
        displayName: "Mediator Pattern",
        description: "Routes requests through handlers to keep senders and receivers decoupled.",
        kind: PatternKind.Design,
        tags: ["design-pattern", "mediator"]);

    public static PatternDescriptor SpecificationPattern { get; } = new(
        id: "specification-pattern",
        displayName: "Specification Pattern",
        description: "Encapsulates reusable business rules and query predicates behind explicit specifications.",
        kind: PatternKind.Design,
        tags: ["design-pattern", "specification"]);

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
        SpecificationPattern
    ];

    private static readonly Dictionary<string, PatternDescriptor> Index = CreateIndex();

    public static IReadOnlyList<PatternDescriptor> All => Items;

    public static bool TryResolve(string value, out PatternDescriptor pattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return Index.TryGetValue(NormalizeKey(value), out pattern!);
    }

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

        Add(index, ModularArchitecture, "ModularArchitecture");
        Add(index, SingleHostTopology, "SingleHostTopology");
        Add(index, MicroserviceTopology, "MicroserviceTopology");
        Add(index, VerticalSliceOrganization, "VerticalSliceOrganization");
        Add(index, ModuleFirstOrganization, "ModuleFirstOrganization");
        Add(index, SharedFoundationPattern, "SharedFoundationPattern");
        Add(index, StrategyPattern, "StrategyPattern", "Strategy");
        Add(index, PipelinePattern, "PipelinePattern", "Pipeline");
        Add(index, MediatorPattern, "MediatorPattern", "Mediator");
        Add(index, SpecificationPattern, "SpecificationPattern", "Specification");

        return index;
    }

    private static void Add(Dictionary<string, PatternDescriptor> index, PatternDescriptor pattern, params string[] aliases)
    {
        index[NormalizeKey(pattern.Id)] = pattern;
        index[NormalizeKey(pattern.DisplayName)] = pattern;

        foreach (var alias in aliases)
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
