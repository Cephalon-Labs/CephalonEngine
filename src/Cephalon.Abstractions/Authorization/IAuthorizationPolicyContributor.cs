namespace Cephalon.Abstractions.Authorization;

/// <summary>
/// Contributes one or more authorization-policy descriptors to the active runtime.
/// </summary>
public interface IAuthorizationPolicyContributor
{
    /// <summary>
    /// Registers one or more authorization-policy descriptors with the supplied registry.
    /// </summary>
    /// <param name="policies">The registry that collects contributed authorization-policy descriptors.</param>
    void RegisterPolicies(IAuthorizationPolicyRegistry policies);
}
