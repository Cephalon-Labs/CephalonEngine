namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Collects REST endpoint candidates while the active host resolves publication precedence.
/// </summary>
/// <remarks>
/// Host adapters and transport helpers use this registry to publish candidate visibility into
/// <see cref="IRestEndpointCandidateRuntimeCatalog" />. Consumer code should normally read the
/// catalog instead of mutating the registry directly.
/// </remarks>
public interface IRestEndpointCandidateRuntimeRegistry
{
    /// <summary>
    /// Clears any previously registered runtime candidates before a host rematerializes its REST surface.
    /// </summary>
    void Clear();

    /// <summary>
    /// Registers one REST endpoint candidate with the runtime catalog.
    /// </summary>
    /// <param name="candidate">The candidate descriptor to register.</param>
    void Register(RestEndpointCandidateRuntimeDescriptor candidate);
}
