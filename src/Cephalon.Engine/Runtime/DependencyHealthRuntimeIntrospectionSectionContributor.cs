using System.Globalization;
using Cephalon.Abstractions.Health;

namespace Cephalon.Engine.Runtime;

internal sealed class DependencyHealthRuntimeIntrospectionSectionContributor(
    RuntimeHealthEvaluator healthEvaluator) : IRuntimeIntrospectionSectionContributor
{
    public RuntimeIntrospectionSection DescribeSection()
    {
        var entries = healthEvaluator.EvaluateDependencies()
            .Select(CreateEntry)
            .ToArray();

        return new RuntimeIntrospectionSection(
            id: "dependency-health",
            schemaVersion: "1.0.0",
            source: "Cephalon.Engine",
            displayName: "Dependency health",
            description: "Desired and observed health state for dependencies contributed to the Cephalon runtime.",
            entries: entries);
    }

    private static RuntimeIntrospectionSectionEntry CreateEntry(DependencyHealthReport report)
    {
        var observedState = report.State.ToString().ToLowerInvariant();
        var conditionStatus = report.State == HealthState.Healthy ? "true" : "false";
        var severity = report.State switch
        {
            HealthState.Healthy => "info",
            HealthState.Degraded => "warning",
            HealthState.Unhealthy when report.Required => "error",
            _ => "warning"
        };
        var reason = report.State switch
        {
            HealthState.Healthy => "dependency-healthy",
            HealthState.Degraded => "dependency-degraded",
            HealthState.Unhealthy when report.Required => "required-dependency-unhealthy",
            _ => "optional-dependency-unhealthy"
        };
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["consecutiveFailureCount"] = report.ConsecutiveFailureCount.ToString(CultureInfo.InvariantCulture),
            ["probeDurationMilliseconds"] = report.ProbeDurationMilliseconds.ToString(CultureInfo.InvariantCulture),
            ["required"] = report.Required.ToString().ToLowerInvariant(),
            ["source"] = report.Source
        };

        if (report.CheckedAtUtc is DateTimeOffset checkedAtUtc)
        {
            metadata["checkedAtUtc"] = checkedAtUtc.ToString("O", CultureInfo.InvariantCulture);
        }

        return new RuntimeIntrospectionSectionEntry(
            id: report.Id,
            displayName: report.DisplayName,
            description: report.Description,
            desiredState: "healthy",
            observedState: observedState,
            conditions:
            [
                new RuntimeOperatorCondition(
                    type: "healthy",
                    status: conditionStatus,
                    severity: severity,
                    reason: reason,
                    message: report.Description,
                    observedAtUtc: report.CheckedAtUtc)
            ],
            metadata: metadata);
    }
}
