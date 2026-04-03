using Cephalon.Abstractions.AppModel;
using Cephalon.Engine.AppModel.Scaffolding;

namespace Cephalon.Engine.AppModel;

/// <summary>
/// Provides the built-in Cephalon suite blueprints.
/// </summary>
public static class BuiltInSuiteBlueprints
{
    /// <summary>
    /// Gets the built-in microservice-suite blueprint.
    /// </summary>
    public static SuiteBlueprint MicroserviceSuite { get; } = new(
        id: "microservice-suite",
        displayName: "Microservice Suite",
        description: "A coordinated suite of Cephalon microservices composed from the shipped microservice app blueprint.",
        scaffold: BuiltInSuiteScaffolds.MicroserviceSuite,
        metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["serviceBlueprintId"] = BuiltInBlueprints.Microservice.Id,
            ["derivedFromScaffoldId"] = BuiltInScaffolds.Microservice.Id
        });

    private static readonly SuiteBlueprint[] Items =
    [
        MicroserviceSuite
    ];

    private static readonly Dictionary<string, SuiteBlueprint> Index = CreateIndex();

    /// <summary>
    /// Gets all built-in suite blueprints.
    /// </summary>
    public static IReadOnlyList<SuiteBlueprint> All => Items;

    /// <summary>
    /// Attempts to resolve a suite blueprint identifier, display name, or alias.
    /// </summary>
    /// <param name="value">The suite blueprint identifier, display name, or alias to resolve.</param>
    /// <param name="blueprint">The resolved suite blueprint when the lookup succeeds.</param>
    /// <returns><see langword="true" /> when the suite blueprint was resolved; otherwise, <see langword="false" />.</returns>
    public static bool TryResolve(string value, out SuiteBlueprint blueprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return Index.TryGetValue(NormalizeKey(value), out blueprint!);
    }

    /// <summary>
    /// Resolves a suite blueprint identifier, display name, or alias.
    /// </summary>
    /// <param name="value">The suite blueprint identifier, display name, or alias to resolve.</param>
    /// <returns>The resolved suite blueprint.</returns>
    public static SuiteBlueprint Resolve(string value)
    {
        if (TryResolve(value, out var blueprint))
        {
            return blueprint;
        }

        throw new InvalidOperationException(
            $"Suite blueprint '{value}' is not supported. Supported suite blueprints: {string.Join(", ", Items.Select(item => item.DisplayName))}.");
    }

    private static Dictionary<string, SuiteBlueprint> CreateIndex()
    {
        var index = new Dictionary<string, SuiteBlueprint>(StringComparer.Ordinal);

        Add(index, MicroserviceSuite, "MicroserviceSuite");

        return index;
    }

    private static void Add(Dictionary<string, SuiteBlueprint> index, SuiteBlueprint blueprint, params string[] aliases)
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
