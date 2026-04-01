using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.HttpDependencies.Services;

internal sealed class HttpDependencyHealthStore
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
