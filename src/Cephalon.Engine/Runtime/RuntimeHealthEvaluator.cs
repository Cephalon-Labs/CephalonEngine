using Cephalon.Abstractions.Health;

namespace Cephalon.Engine.Runtime;

/// <summary>
/// Evaluates runtime liveness, readiness, and dependency health.
/// </summary>
public sealed class RuntimeHealthEvaluator
{
    private const string StartupWarmupWindow = "startup-warmup";
    private const string ShutdownDrainWindow = "shutdown-drain";
    private const string RestartBackoffWindow = "restart-backoff";
    private readonly IRuntime runtime;
    private readonly IDependencyHealthContributor[] dependencyHealthContributors;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeHealthEvaluator" /> class.
    /// </summary>
    /// <param name="runtime">The runtime to evaluate.</param>
    /// <param name="dependencyHealthContributors">The dependency contributors that provide health data.</param>
    public RuntimeHealthEvaluator(
        IRuntime runtime,
        IEnumerable<IDependencyHealthContributor> dependencyHealthContributors)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.dependencyHealthContributors = dependencyHealthContributors?.ToArray()
            ?? throw new ArgumentNullException(nameof(dependencyHealthContributors));
    }

    /// <summary>
    /// Evaluates whether the runtime process is live.
    /// </summary>
    /// <returns>The liveness report.</returns>
    public RuntimeHealthReport EvaluateLiveness()
    {
        var snapshot = runtime.StatusSnapshot;
        var dependencies = CollectDependencies();
        var now = DateTimeOffset.UtcNow;

        return snapshot.Status switch
        {
            RuntimeStatus.Failed => CreateFailureReport(
                probe: "liveness",
                state: RuntimeHealthState.Unhealthy,
                fallbackDescription: "Runtime entered the failed state.",
                snapshot: snapshot,
                dependencies: dependencies,
                now: now),
            RuntimeStatus.Stopped => CreateReport(
                probe: "liveness",
                state: RuntimeHealthState.Unhealthy,
                description: "Runtime is stopped.",
                snapshot: snapshot,
                dependencies: dependencies),
            RuntimeStatus.Stopping when GetShutdownDrainEndsAtUtc(snapshot) is DateTimeOffset drainEndsAt &&
                now <= drainEndsAt => CreateReport(
                    probe: "liveness",
                    state: RuntimeHealthState.Healthy,
                    description: $"Runtime is stopping and remains live during the configured shutdown drain window until '{drainEndsAt:O}'.",
                    snapshot: snapshot,
                    dependencies: dependencies,
                    activeWindow: ShutdownDrainWindow,
                    activeWindowEndsAtUtc: drainEndsAt),
            RuntimeStatus.Stopping when GetShutdownDrainEndsAtUtc(snapshot) is DateTimeOffset expiredDrainEndsAt => CreateReport(
                probe: "liveness",
                state: RuntimeHealthState.Unhealthy,
                description: $"Runtime is still stopping after the configured shutdown drain window ended at '{expiredDrainEndsAt:O}'.",
                snapshot: snapshot,
                dependencies: dependencies,
                activeWindow: ShutdownDrainWindow,
                activeWindowEndsAtUtc: expiredDrainEndsAt),
            RuntimeStatus.Stopping => CreateReport(
                probe: "liveness",
                state: RuntimeHealthState.Healthy,
                description: "Runtime is stopping but the host process is still live.",
                snapshot: snapshot,
                dependencies: dependencies),
            RuntimeStatus.Started when HasDependencyIssues(dependencies) => CreateReport(
                probe: "liveness",
                state: RuntimeHealthState.Degraded,
                description: "Runtime is live, but one or more dependencies need attention.",
                snapshot: snapshot,
                dependencies: dependencies),
            RuntimeStatus.Started => CreateReport(
                probe: "liveness",
                state: RuntimeHealthState.Healthy,
                description: "Runtime is live.",
                snapshot: snapshot,
                dependencies: dependencies),
            _ => CreateReport(
                probe: "liveness",
                state: RuntimeHealthState.Healthy,
                description: $"Runtime is live while status is '{snapshot.Status}'.",
                snapshot: snapshot,
                dependencies: dependencies)
        };
    }

    /// <summary>
    /// Evaluates whether the runtime is ready to serve traffic.
    /// </summary>
    /// <returns>The readiness report.</returns>
    public RuntimeHealthReport EvaluateReadiness()
    {
        var snapshot = runtime.StatusSnapshot;
        var dependencies = CollectDependencies();
        var now = DateTimeOffset.UtcNow;

        return snapshot.Status switch
        {
            RuntimeStatus.Started when GetStartupWarmupEndsAtUtc(snapshot) is DateTimeOffset warmupEndsAt &&
                now < warmupEndsAt => CreateReport(
                    probe: "readiness",
                    state: RuntimeHealthState.Unhealthy,
                    description: $"Runtime started but is still inside the configured readiness warmup window until '{warmupEndsAt:O}'.",
                    snapshot: snapshot,
                    dependencies: dependencies,
                    activeWindow: StartupWarmupWindow,
                    activeWindowEndsAtUtc: warmupEndsAt),
            RuntimeStatus.Started when HasRequiredDependencyFailures(dependencies) => CreateReport(
                probe: "readiness",
                state: RuntimeHealthState.Unhealthy,
                description: "Runtime started, but one or more required dependencies are unhealthy.",
                snapshot: snapshot,
                dependencies: dependencies),
            RuntimeStatus.Started when HasDependencyIssues(dependencies) => CreateReport(
                probe: "readiness",
                state: RuntimeHealthState.Degraded,
                description: "Runtime started, but one or more dependencies are degraded.",
                snapshot: snapshot,
                dependencies: dependencies),
            RuntimeStatus.Started => CreateReport(
                probe: "readiness",
                state: RuntimeHealthState.Healthy,
                description: "Runtime is ready to serve traffic.",
                snapshot: snapshot,
                dependencies: dependencies),
            RuntimeStatus.Failed => CreateFailureReport(
                probe: "readiness",
                state: RuntimeHealthState.Unhealthy,
                fallbackDescription: "Runtime is not ready because it failed.",
                snapshot: snapshot,
                dependencies: dependencies,
                now: now),
            RuntimeStatus.Stopping => CreateReport(
                probe: "readiness",
                state: RuntimeHealthState.Unhealthy,
                description: "Runtime is stopping and should not receive new traffic.",
                snapshot: snapshot,
                dependencies: dependencies),
            RuntimeStatus.Stopped => CreateReport(
                probe: "readiness",
                state: RuntimeHealthState.Unhealthy,
                description: "Runtime is stopped and not ready.",
                snapshot: snapshot,
                dependencies: dependencies),
            _ => CreateReport(
                probe: "readiness",
                state: RuntimeHealthState.Unhealthy,
                description: $"Runtime status '{snapshot.Status}' is not ready to serve traffic yet.",
                snapshot: snapshot,
                dependencies: dependencies)
        };
    }

    /// <summary>
    /// Evaluates dependency-level health reports without applying probe semantics.
    /// </summary>
    /// <returns>The dependency-health reports visible to the evaluator.</returns>
    public DependencyHealthReport[] EvaluateDependencies()
    {
        return CollectDependencies();
    }

    private static RuntimeHealthReport CreateFailureReport(
        string probe,
        RuntimeHealthState state,
        string fallbackDescription,
        RuntimeStatusSnapshot snapshot,
        DependencyHealthReport[] dependencies,
        DateTimeOffset now)
    {
        var failure = snapshot.LastFailure;
        var description = failure is null
            ? fallbackDescription
            : $"Runtime failed during phase '{failure.Phase}' on module '{failure.ModuleId ?? "runtime"}': {failure.Message}";
        var activeWindow = default(string);
        DateTimeOffset? activeWindowEndsAtUtc = null;

        if (failure?.RestartAvailableAtUtc is DateTimeOffset restartAvailableAtUtc &&
            now < restartAvailableAtUtc)
        {
            activeWindow = RestartBackoffWindow;
            activeWindowEndsAtUtc = restartAvailableAtUtc;
            description += $" Manual restart remains in backoff until '{restartAvailableAtUtc:O}'.";
        }

        return CreateReport(
            probe,
            state,
            description,
            snapshot,
            dependencies,
            activeWindow,
            activeWindowEndsAtUtc);
    }

    private static RuntimeHealthReport CreateReport(
        string probe,
        RuntimeHealthState state,
        string description,
        RuntimeStatusSnapshot snapshot,
        DependencyHealthReport[] dependencies,
        string? activeWindow = null,
        DateTimeOffset? activeWindowEndsAtUtc = null)
    {
        return new RuntimeHealthReport(
            Probe: probe,
            State: state,
            Description: description,
            RuntimeStatus: snapshot.Status,
            RestartCount: snapshot.RestartCount,
            LastFailure: snapshot.LastFailure,
            Dependencies: dependencies,
            ActiveWindow: activeWindow,
            ActiveWindowEndsAtUtc: activeWindowEndsAtUtc);
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

    private DateTimeOffset? GetStartupWarmupEndsAtUtc(RuntimeStatusSnapshot snapshot)
    {
        if (runtime.FailurePolicy.StartupReadinessDelay <= TimeSpan.Zero ||
            snapshot.StartedAtUtc is not DateTimeOffset startedAtUtc)
        {
            return null;
        }

        return startedAtUtc + runtime.FailurePolicy.StartupReadinessDelay;
    }

    private DateTimeOffset? GetShutdownDrainEndsAtUtc(RuntimeStatusSnapshot snapshot)
    {
        if (runtime.FailurePolicy.ShutdownLivenessGracePeriod <= TimeSpan.Zero ||
            snapshot.StoppingAtUtc is not DateTimeOffset stoppingAtUtc)
        {
            return null;
        }

        return stoppingAtUtc + runtime.FailurePolicy.ShutdownLivenessGracePeriod;
    }
}
