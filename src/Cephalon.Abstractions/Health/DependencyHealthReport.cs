namespace Cephalon.Abstractions.Health;

/// <summary>
/// Describes the health state of one dependency surfaced by the runtime.
/// </summary>
/// <param name="Id">The stable dependency identifier.</param>
/// <param name="DisplayName">The human-readable dependency name.</param>
/// <param name="State">The current health state.</param>
/// <param name="Description">The operator-facing health description.</param>
/// <param name="Required">Whether the dependency is required for readiness.</param>
/// <param name="Source">The contributor or subsystem that reported the dependency.</param>
public sealed record DependencyHealthReport(
    string Id,
    string DisplayName,
    HealthState State,
    string Description,
    bool Required,
    string Source)
{
    /// <summary>
    /// Gets the UTC timestamp at which the dependency observation completed, when the contributor provides it.
    /// </summary>
    public DateTimeOffset? CheckedAtUtc { get; init; }

    /// <summary>
    /// Gets the completed probe duration in milliseconds, or zero when the contributor does not provide it.
    /// </summary>
    public int ProbeDurationMilliseconds { get; init; }

    /// <summary>
    /// Gets the number of consecutive failed observations, or zero when the contributor does not track failure streaks.
    /// </summary>
    public int ConsecutiveFailureCount { get; init; }
}
