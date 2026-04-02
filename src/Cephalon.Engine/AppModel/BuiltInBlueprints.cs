using Cephalon.Abstractions.AppModel;
using Cephalon.Engine.Patterns;
using Cephalon.Engine.AppModel.Scaffolding;

namespace Cephalon.Engine.AppModel;

/// <summary>
/// Provides the built-in Cephalon app blueprints.
/// </summary>
public static class BuiltInBlueprints
{
    /// <summary>
    /// Gets the built-in modular-monolith blueprint.
    /// </summary>
    public static AppBlueprint ModularMonolith { get; } = new(
        id: "modular-monolith",
        displayName: "Modular Monolith",
        description: "A single host application with modular composition and module-first organization.",
        patterns:
        [
            BuiltInPatterns.ModularArchitecture,
            BuiltInPatterns.SingleHostTopology,
            BuiltInPatterns.ModuleFirstOrganization,
            BuiltInPatterns.SharedFoundationPattern
        ],
        scaffold: BuiltInScaffolds.ModularMonolith);

    /// <summary>
    /// Gets the built-in modular-vertical-slice blueprint.
    /// </summary>
    public static AppBlueprint ModularVerticalSlice { get; } = new(
        id: "modular-vertical-slice",
        displayName: "Modular Vertical Slice",
        description: "A single host application with modules as boundaries and vertical slices inside each module.",
        patterns:
        [
            BuiltInPatterns.ModularArchitecture,
            BuiltInPatterns.SingleHostTopology,
            BuiltInPatterns.VerticalSliceOrganization,
            BuiltInPatterns.SharedFoundationPattern
        ],
        scaffold: BuiltInScaffolds.ModularVerticalSlice);

    /// <summary>
    /// Gets the built-in microservice blueprint.
    /// </summary>
    public static AppBlueprint Microservice { get; } = new(
        id: "microservice",
        displayName: "Microservice",
        description: "An independently deployable service that still uses Cephalon modules and shared foundation.",
        patterns:
        [
            BuiltInPatterns.ModularArchitecture,
            BuiltInPatterns.MicroserviceTopology,
            BuiltInPatterns.VerticalSliceOrganization,
            BuiltInPatterns.SharedFoundationPattern
        ],
        scaffold: BuiltInScaffolds.Microservice);

    private static readonly AppBlueprint[] Items =
    [
        ModularMonolith,
        ModularVerticalSlice,
        Microservice
    ];

    private static readonly Dictionary<string, AppBlueprint> Index = CreateIndex();

    /// <summary>
    /// Gets all built-in blueprints.
    /// </summary>
    public static IReadOnlyList<AppBlueprint> All => Items;

    /// <summary>
    /// Attempts to resolve a blueprint identifier, display name, or alias.
    /// </summary>
    /// <param name="value">The blueprint identifier, display name, or alias to resolve.</param>
    /// <param name="blueprint">The resolved blueprint when the lookup succeeds.</param>
    /// <returns><see langword="true" /> when the blueprint was resolved; otherwise, <see langword="false" />.</returns>
    public static bool TryResolve(string value, out AppBlueprint blueprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return Index.TryGetValue(NormalizeKey(value), out blueprint!);
    }

    /// <summary>
    /// Resolves a blueprint identifier, display name, or alias.
    /// </summary>
    /// <param name="value">The blueprint identifier, display name, or alias to resolve.</param>
    /// <returns>The resolved blueprint.</returns>
    public static AppBlueprint Resolve(string value)
    {
        if (TryResolve(value, out var blueprint))
        {
            return blueprint;
        }

        throw new InvalidOperationException(
            $"Blueprint '{value}' is not supported. Supported blueprints: {string.Join(", ", Items.Select(item => item.DisplayName))}.");
    }

    private static Dictionary<string, AppBlueprint> CreateIndex()
    {
        var index = new Dictionary<string, AppBlueprint>(StringComparer.Ordinal);

        Add(index, ModularMonolith, "ModularMonolith");
        Add(index, ModularVerticalSlice, "ModularVerticalSlice");
        Add(index, Microservice, "Microservice");

        return index;
    }

    private static void Add(Dictionary<string, AppBlueprint> index, AppBlueprint blueprint, params string[] aliases)
    {
        index[NormalizeKey(blueprint.Id)] = blueprint;
        index[NormalizeKey(blueprint.DisplayName)] = blueprint;

        foreach (var alias in aliases)
        {
            index[NormalizeKey(alias)] = blueprint;
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
