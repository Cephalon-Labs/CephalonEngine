namespace Cephalon.Abstractions.AppModel.Scaffolding;

/// <summary>
/// Defines the canonical scaffold-scope identifiers used by scaffold plans.
/// </summary>
public static class ScaffoldScopes
{
    /// <summary>
    /// Identifies a suite-level scaffold scope.
    /// </summary>
    public const string Suite = "suite";

    /// <summary>
    /// Identifies a solution-level scaffold scope.
    /// </summary>
    public const string Solution = "solution";

    /// <summary>
    /// Identifies a module-level scaffold scope.
    /// </summary>
    public const string Module = "module";

    /// <summary>
    /// Identifies a feature-level scaffold scope.
    /// </summary>
    public const string Feature = "feature";
}
