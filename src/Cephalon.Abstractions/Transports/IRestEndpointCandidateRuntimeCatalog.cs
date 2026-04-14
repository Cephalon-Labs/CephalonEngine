namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Exposes the module-owned REST endpoint candidates visible to the current runtime.
/// </summary>
/// <remarks>
/// This surface complements <see cref="IRestEndpointRuntimeCatalog" /> by showing candidate
/// projections that were published or suppressed after precedence resolution rather than only the
/// final active public REST endpoints.
/// </remarks>
public interface IRestEndpointCandidateRuntimeCatalog
{
    /// <summary>
    /// Gets all REST endpoint candidates visible to the current runtime.
    /// </summary>
    IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> Candidates { get; }

    /// <summary>
    /// Gets one REST endpoint candidate by its stable identifier.
    /// </summary>
    /// <param name="candidateId">The candidate identifier to resolve.</param>
    /// <returns>The matching candidate descriptor, or <see langword="null" /> when it is not present.</returns>
    RestEndpointCandidateRuntimeDescriptor? GetById(string candidateId);

    /// <summary>
    /// Gets all REST endpoint candidates owned by the requested source module.
    /// </summary>
    /// <param name="sourceModuleId">The stable source-module identifier to filter by.</param>
    /// <returns>The matching candidate descriptors, or an empty list when no candidates exist.</returns>
    IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all REST endpoint candidates that target the requested behavior identifier.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier to filter by.</param>
    /// <returns>The matching candidate descriptors, or an empty list when no candidates exist.</returns>
    IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> GetByBehaviorId(string behaviorId);
}
