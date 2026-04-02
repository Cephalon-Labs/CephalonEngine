namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Categorizes the role a pattern plays in an app shape.
/// </summary>
public enum PatternKind
{
    /// <summary>
    /// Identifies a composition pattern.
    /// </summary>
    Composition = 0,

    /// <summary>
    /// Identifies a deployment-topology pattern.
    /// </summary>
    Deployment = 1,

    /// <summary>
    /// Identifies an organization pattern.
    /// </summary>
    Organization = 2,

    /// <summary>
    /// Identifies a foundation pattern.
    /// </summary>
    Foundation = 3,

    /// <summary>
    /// Identifies a design pattern.
    /// </summary>
    Design = 4
}
