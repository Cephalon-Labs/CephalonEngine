namespace Cephalon.Abstractions.Authorization;

/// <summary>
/// Identifies one authorization approach active inside a policy evaluation.
/// </summary>
public enum AuthorizationMode
{
    /// <summary>
    /// Indicates a role-based access-control evaluation.
    /// </summary>
    Rbac = 0,

    /// <summary>
    /// Indicates an attribute-based access-control evaluation.
    /// </summary>
    Abac = 1,

    /// <summary>
    /// Indicates a policy-driven authorization evaluation.
    /// </summary>
    Policy = 2
}
