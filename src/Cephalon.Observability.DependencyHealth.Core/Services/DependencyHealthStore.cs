using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.DependencyHealth.Core.Services;

/// <summary>Thread-safe cache for the most recent dependency-health reports.</summary>
internal sealed class DependencyHealthStore
{
    private readonly object gate = new();
    private DependencyHealthReport[] reports = [];

    /// <summary>Returns the most recently cached reports.</summary>
    public IReadOnlyList<DependencyHealthReport> GetReports()
    {
        lock (gate) { return reports; }
    }

    /// <summary>Atomically replaces the cached report set.</summary>
    public void SetReports(IEnumerable<DependencyHealthReport> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);
        lock (gate) { this.reports = reports.ToArray(); }
    }
}
