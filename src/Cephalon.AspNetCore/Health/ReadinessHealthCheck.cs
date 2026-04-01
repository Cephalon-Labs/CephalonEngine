using Cephalon.Engine.Runtime;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cephalon.AspNetCore.Health;

internal sealed class ReadinessHealthCheck : IHealthCheck
{
    private readonly RuntimeHealthEvaluator evaluator;

    public ReadinessHealthCheck(RuntimeHealthEvaluator evaluator)
    {
        this.evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(RuntimeHealthCheckResultFactory.Create(evaluator.EvaluateReadiness()));
    }
}
