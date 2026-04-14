namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Exposes grouped REST endpoint publication visibility for behavior-backed public REST candidates.
/// </summary>
/// <remarks>
/// This surface complements <see cref="IRestEndpointCandidateRuntimeCatalog" /> by grouping the
/// candidate-level truth per behavior so operators can inspect published, precedence-suppressed,
/// and governance-suppressed shorthand outcomes without reconstructing that story manually.
/// </remarks>
public interface IRestEndpointPublicationGroupRuntimeCatalog
{
    /// <summary>
    /// Gets the grouped REST endpoint publication answers visible to the current runtime.
    /// </summary>
    IReadOnlyList<RestEndpointPublicationGroupDescriptor> Groups { get; }

    /// <summary>
    /// Gets one grouped REST endpoint publication answer by behavior identifier.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier to resolve.</param>
    /// <returns>The matching grouped publication answer, or <see langword="null" /> when it is not present.</returns>
    RestEndpointPublicationGroupDescriptor? GetByBehaviorId(string behaviorId);
}
