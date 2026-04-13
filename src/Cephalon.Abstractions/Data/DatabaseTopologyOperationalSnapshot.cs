namespace Cephalon.Abstractions.Data;

/// <summary>
/// Combines the engine-owned operator-facing database-topology posture into one reusable payload.
/// </summary>
public sealed class DatabaseTopologyOperationalSnapshot
{
    /// <summary>
    /// Creates a new database-topology operational snapshot.
    /// </summary>
    /// <param name="generatedAtUtc">The UTC timestamp when the snapshot was created.</param>
    /// <param name="summary">The aggregate operator-facing topology summary.</param>
    /// <param name="advisories">The reusable operator-facing advisories derived from the current topology state.</param>
    /// <param name="actionPlan">The ordered engine-owned operator action plan derived from the current topology state.</param>
    public DatabaseTopologyOperationalSnapshot(
        DateTimeOffset generatedAtUtc,
        DatabaseTopologyOperationalSummary summary,
        IReadOnlyList<DatabaseTopologyOperationalAdvisory>? advisories = null,
        DatabaseTopologyOperationalActionPlan? actionPlan = null)
    {
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        GeneratedAtUtc = generatedAtUtc;
        Advisories = advisories?.ToArray() ?? [];
        ActionPlan = actionPlan ?? new DatabaseTopologyOperationalActionPlan(generatedAtUtc);
    }

    /// <summary>
    /// Gets the UTC timestamp when the snapshot was created.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; }

    /// <summary>
    /// Gets the aggregate operator-facing topology summary.
    /// </summary>
    public DatabaseTopologyOperationalSummary Summary { get; }

    /// <summary>
    /// Gets the reusable operator-facing advisories derived from the current topology state.
    /// </summary>
    public IReadOnlyList<DatabaseTopologyOperationalAdvisory> Advisories { get; }

    /// <summary>
    /// Gets the ordered engine-owned operator action plan derived from the current topology state.
    /// </summary>
    public DatabaseTopologyOperationalActionPlan ActionPlan { get; }
}
