namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Provides canonical wire-name helpers for <see cref="RestEndpointCandidateStatus"/>.
/// </summary>
public static class RestEndpointCandidateStatusExtensions
{
    /// <summary>
    /// Gets the stable wire name used by JSON serialization for the candidate status.
    /// </summary>
    /// <param name="status">The candidate status.</param>
    /// <returns>The stable wire name.</returns>
    public static string GetWireName(this RestEndpointCandidateStatus status)
    {
        return status switch
        {
            RestEndpointCandidateStatus.Unspecified => "unspecified",
            RestEndpointCandidateStatus.Published => "published",
            RestEndpointCandidateStatus.Suppressed => "suppressed",
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "A supported REST endpoint candidate status is required.")
        };
    }

    /// <summary>
    /// Tries to parse the stable wire name used by JSON serialization into a candidate status.
    /// </summary>
    /// <param name="value">The wire name to parse.</param>
    /// <param name="status">The parsed candidate status when the wire name is recognized.</param>
    /// <returns><see langword="true"/> when the wire name maps to a supported candidate status; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseWireName(string? value, out RestEndpointCandidateStatus status)
    {
        switch (value?.Trim())
        {
            case "unspecified":
                status = RestEndpointCandidateStatus.Unspecified;
                return true;
            case "published":
                status = RestEndpointCandidateStatus.Published;
                return true;
            case "suppressed":
                status = RestEndpointCandidateStatus.Suppressed;
                return true;
            default:
                status = default;
                return false;
        }
    }
}
