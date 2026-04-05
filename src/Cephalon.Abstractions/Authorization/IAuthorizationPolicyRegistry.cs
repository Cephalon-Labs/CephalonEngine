namespace Cephalon.Abstractions.Authorization;

/// <summary>
/// Receives authorization-policy descriptors contributed by active modules or packages.
/// </summary>
public interface IAuthorizationPolicyRegistry
{
    /// <summary>
    /// Adds an authorization policy to the current runtime composition.
    /// </summary>
    /// <param name="policy">The authorization-policy descriptor to register.</param>
    void Add(AuthorizationPolicyDescriptor policy);
}
