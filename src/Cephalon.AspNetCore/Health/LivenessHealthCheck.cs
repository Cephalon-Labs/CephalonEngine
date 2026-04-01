using Cephalon.Engine.Runtime;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cephalon.AspNetCore.Health;

internal sealed class LivenessHealthCheck : IHealthCheck
{
    private readonly RuntimeHealthEvaluator evaluator;

    public LivenessHealthCheck(RuntimeHealthEvaluator evaluator)
    {
        this.evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(RuntimeHealthCheckResultFactory.Create(evaluator.EvaluateLiveness()));
    }
}
