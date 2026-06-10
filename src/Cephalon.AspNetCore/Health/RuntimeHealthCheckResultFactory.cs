using Cephalon.Abstractions.Health;
using Cephalon.Engine.Runtime;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cephalon.AspNetCore.Health;

internal static class RuntimeHealthCheckResultFactory
{
    public static HealthCheckResult Create(RuntimeHealthReport report)
    {
        var data = new Dictionary<string, object>
        {
            ["probe"] = report.Probe,
            ["runtimeStatus"] = report.RuntimeStatus.ToString(),
            ["restartCount"] = report.RestartCount
        };

        if (report.LastFailure is not null)
        {
            data["failurePhase"] = report.LastFailure.Phase;
            data["failureModuleId"] = report.LastFailure.ModuleId ?? "runtime";
            data["failureMessage"] = report.LastFailure.Message;
            data["canRestart"] = report.LastFailure.CanRestart;

            if (report.LastFailure.RestartAvailableAtUtc is DateTimeOffset restartAvailableAtUtc)
            {
                data["restartAvailableAtUtc"] = restartAvailableAtUtc;
            }
        }

        if (report.Dependencies.Count > 0)
        {
            data["dependencyCount"] = report.Dependencies.Count;
            data["dependencies"] = report.Dependencies.Select(static dependency => new
            {
                dependency.Id,
                dependency.DisplayName,
                state = dependency.State.ToString(),
                dependency.Description,
                dependency.Required,
                dependency.Source,
                dependency.CheckedAtUtc,
                dependency.ProbeDurationMilliseconds,
                dependency.ConsecutiveFailureCount
            }).ToArray();
        }

        if (!string.IsNullOrWhiteSpace(report.ActiveWindow))
        {
            data["activeWindow"] = report.ActiveWindow;
        }

        if (report.ActiveWindowEndsAtUtc is DateTimeOffset activeWindowEndsAtUtc)
        {
            data["activeWindowEndsAtUtc"] = activeWindowEndsAtUtc;
        }

        return new HealthCheckResult(
            status: MapStatus(report.State),
            description: report.Description,
            data: data);
    }

    private static HealthStatus MapStatus(RuntimeHealthState state)
    {
        return state switch
        {
            RuntimeHealthState.Healthy => HealthStatus.Healthy,
            RuntimeHealthState.Degraded => HealthStatus.Degraded,
            _ => HealthStatus.Unhealthy
        };
    }
}
