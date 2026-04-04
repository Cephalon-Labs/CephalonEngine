namespace Cephalon.Abstractions.Authorization;

/// <summary>
/// Exposes the authorization policies visible to the current runtime.
/// </summary>
public interface IAuthorizationPolicyCatalog
{
    /// <summary>
    /// Gets all authorization policies visible to the current runtime.
    /// </summary>
    IReadOnlyList<AuthorizationPolicyDescriptor> Policies { get; }

    /// <summary>
    /// Gets one authorization policy by its stable identifier.
    /// </summary>
    /// <param name="policyId">The authorization-policy identifier to resolve.</param>
    /// <returns>The matching policy, or <see langword="null" /> when it is not active.</returns>
    AuthorizationPolicyDescriptor? GetById(string policyId);

    /// <summary>
    /// Gets all authorization policies that support the requested mode.
    /// </summary>
    /// <param name="mode">The authorization mode to filter by.</param>
    /// <returns>The matching policies, or an empty list when none support the requested mode.</returns>
    IReadOnlyList<AuthorizationPolicyDescriptor> GetByMode(AuthorizationMode mode);
}
