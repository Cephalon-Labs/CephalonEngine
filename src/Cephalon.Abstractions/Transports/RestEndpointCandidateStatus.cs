namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes whether a REST endpoint candidate is published or suppressed in the active runtime.
/// </summary>
public enum RestEndpointCandidateStatus
{
    /// <summary>
    /// The candidate has not been classified.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// The candidate is published into the active public REST surface.
    /// </summary>
    Published = 1,

    /// <summary>
    /// The candidate was considered but suppressed by a higher-precedence candidate.
    /// </summary>
    Suppressed = 2
}
