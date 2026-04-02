using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.PostgresDependencies.Services;

internal sealed class PostgresDependencyHealthStore
{
    private readonly object gate = new();
    private DependencyHealthReport[] reports = [];

    public IReadOnlyList<DependencyHealthReport> GetReports()
    {
        lock (gate)
        {
            return reports;
        }
    }

    public void SetReports(IEnumerable<DependencyHealthReport> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);

        lock (gate)
        {
            this.reports = reports.ToArray();
        }
    }
}
