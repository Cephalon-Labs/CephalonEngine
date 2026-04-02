namespace Cephalon.Abstractions.AppModel.Scaffolding;

/// <summary>
/// Defines the canonical project-role identifiers used by scaffold plans.
/// </summary>
public static class ProjectRoles
{
    /// <summary>
    /// Identifies the host project.
    /// </summary>
    public const string Host = "host";

    /// <summary>
    /// Identifies the shared foundation project.
    /// </summary>
    public const string Foundation = "foundation";

    /// <summary>
    /// Identifies the contracts project.
    /// </summary>
    public const string Contracts = "contracts";

    /// <summary>
    /// Identifies a module project.
    /// </summary>
    public const string Module = "module";

    /// <summary>
    /// Identifies a test project.
    /// </summary>
    public const string Tests = "tests";
}
