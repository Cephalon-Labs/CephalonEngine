using Cephalon.Abstractions.Health;

namespace Cephalon.Engine.Runtime;

public sealed class RuntimeHealthEvaluator
{
    private readonly IRuntime runtime;
    private readonly IDependencyHealthContributor[] dependencyHealthContributors;

    public RuntimeHealthEvaluator(
        IRuntime runtime,
        IEnumerable<IDependencyHealthContributor> dependencyHealthContributors)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.dependencyHealthContributors = dependencyHealthContributors?.ToArray()
            ?? throw new ArgumentNullException(nameof(dependencyHealthContributors));
    }

    public RuntimeHealthReport EvaluateLiveness()
    {
        var dependencies = CollectDependencies();

        return runtime.Status switch
        {
            RuntimeStatus.Failed => CreateFailureReport(
                probe: "liveness",
                state: RuntimeHealthState.Unhealthy,
                fallbackDescription: "Runtime entered the failed state.",
                dependencies: dependencies),
            RuntimeStatus.Stopped => CreateReport(
                probe: "liveness",
                state: RuntimeHealthState.Unhealthy,
                description: "Runtime is stopped.",
                dependencies: dependencies),
            RuntimeStatus.Stopping => CreateReport(
                probe: "liveness",
                state: RuntimeHealthState.Healthy,
                description: "Runtime is stopping but the host process is still live.",
                dependencies: dependencies),
            RuntimeStatus.Started when HasDependencyIssues(dependencies) => CreateReport(
                probe: "liveness",
                state: RuntimeHealthState.Degraded,
                description: "Runtime is live, but one or more dependencies need attention.",
                dependencies: dependencies),
            RuntimeStatus.Started => CreateReport(
                probe: "liveness",
                state: RuntimeHealthState.Healthy,
                description: "Runtime is live.",
                dependencies: dependencies),
            _ => CreateReport(
                probe: "liveness",
                state: RuntimeHealthState.Healthy,
                description: $"Runtime is live while status is '{runtime.Status}'.",
                dependencies: dependencies)
        };
    }

    public RuntimeHealthReport EvaluateReadiness()
    {
        var dependencies = CollectDependencies();

        return runtime.Status switch
        {
            RuntimeStatus.Started when HasRequiredDependencyFailures(dependencies) => CreateReport(
                probe: "readiness",
                state: RuntimeHealthState.Unhealthy,
                description: "Runtime started, but one or more required dependencies are unhealthy.",
                dependencies: dependencies),
            RuntimeStatus.Started when HasDependencyIssues(dependencies) => CreateReport(
                probe: "readiness",
                state: RuntimeHealthState.Degraded,
                description: "Runtime started, but one or more dependencies are degraded.",
                dependencies: dependencies),
            RuntimeStatus.Started => CreateReport(
                probe: "readiness",
                state: RuntimeHealthState.Healthy,
                description: "Runtime is ready to serve traffic.",
                dependencies: dependencies),
            RuntimeStatus.Failed => CreateFailureReport(
                probe: "readiness",
                state: RuntimeHealthState.Unhealthy,
                fallbackDescription: "Runtime is not ready because it failed.",
                dependencies: dependencies),
            RuntimeStatus.Stopping => CreateReport(
                probe: "readiness",
                state: RuntimeHealthState.Unhealthy,
                description: "Runtime is stopping and should not receive new traffic.",
                dependencies: dependencies),
            RuntimeStatus.Stopped => CreateReport(
                probe: "readiness",
                state: RuntimeHealthState.Unhealthy,
                description: "Runtime is stopped and not ready.",
                dependencies: dependencies),
            _ => CreateReport(
                probe: "readiness",
                state: RuntimeHealthState.Unhealthy,
                description: $"Runtime status '{runtime.Status}' is not ready to serve traffic yet.",
                dependencies: dependencies)
        };
    }

    public DependencyHealthReport[] EvaluateDependencies()
    {
        return CollectDependencies();
    }

    private RuntimeHealthReport CreateFailureReport(
        string probe,
        RuntimeHealthState state,
        string fallbackDescription,
        DependencyHealthReport[] dependencies)
    {
        var failure = runtime.LastFailure;
        var description = failure is null
            ? fallbackDescription
            : $"Runtime failed during phase '{failure.Phase}' on module '{failure.ModuleId ?? "runtime"}': {failure.Message}";

        return CreateReport(probe, state, description, dependencies);
    }

    private RuntimeHealthReport CreateReport(
        string probe,
        RuntimeHealthState state,
        string description,
        DependencyHealthReport[] dependencies)
    {
        return new RuntimeHealthReport(
            Probe: probe,
            State: state,
            Description: description,
            RuntimeStatus: runtime.Status,
            RestartCount: runtime.RestartCount,
            LastFailure: runtime.LastFailure,
            Dependencies: dependencies);
    }

    private DependencyHealthReport[] CollectDependencies()
    {
        if (dependencyHealthContributors.Length == 0)
        {
            return [];
        }

        var reports = new List<DependencyHealthReport>();

        foreach (var contributor in dependencyHealthContributors
            .OrderBy(static item => item.GetType().FullName, StringComparer.Ordinal))
        {
            try
            {
                var contributedReports = contributor.GetDependencyHealth();
                if (contributedReports is null)
                {
                    throw new InvalidOperationException("Contributor returned null dependency health data.");
                }

                reports.AddRange(contributedReports.Select(report => Normalize(report, contributor)));
            }
            catch (Exception exception)
            {
                reports.Add(CreateContributorFailureReport(contributor, exception));
            }
        }

        return reports
            .OrderBy(static report => report.Required ? 0 : 1)
            .ThenBy(static report => report.Source, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static report => report.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static DependencyHealthReport Normalize(
        DependencyHealthReport report,
        IDependencyHealthContributor contributor)
    {
        ArgumentNullException.ThrowIfNull(report);

        var fallbackName = contributor.GetType().Name;
        var id = string.IsNullOrWhiteSpace(report.Id)
            ? fallbackName
            : report.Id.Trim();
        var displayName = string.IsNullOrWhiteSpace(report.DisplayName)
            ? id
            : report.DisplayName.Trim();
        var description = string.IsNullOrWhiteSpace(report.Description)
            ? $"{displayName} reported state '{report.State}'."
            : report.Description.Trim();
        var source = string.IsNullOrWhiteSpace(report.Source)
            ? fallbackName
            : report.Source.Trim();

        return report with
        {
            Id = id,
            DisplayName = displayName,
            Description = description,
            Source = source
        };
    }

    private static DependencyHealthReport CreateContributorFailureReport(
        IDependencyHealthContributor contributor,
        Exception exception)
    {
        var contributorName = contributor.GetType().Name;

        return new DependencyHealthReport(
            Id: contributorName,
            DisplayName: contributorName,
            State: HealthState.Unhealthy,
            Description: $"Dependency health contributor '{contributorName}' failed: {exception.Message}",
            Required: true,
            Source: contributor.GetType().FullName ?? contributorName);
    }

    private static bool HasDependencyIssues(DependencyHealthReport[] dependencies)
    {
        return dependencies.Any(static dependency => dependency.State != HealthState.Healthy);
    }

    private static bool HasRequiredDependencyFailures(DependencyHealthReport[] dependencies)
    {
        return dependencies.Any(static dependency => dependency.Required && dependency.State == HealthState.Unhealthy);
    }
}
