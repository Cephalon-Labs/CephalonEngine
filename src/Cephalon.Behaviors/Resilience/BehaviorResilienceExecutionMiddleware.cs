using Cephalon.Behaviors.Services;
using Polly;
using Polly.Registry;

namespace Cephalon.Behaviors.Resilience;

internal sealed class BehaviorResilienceExecutionMiddleware : IBehaviorExecutionMiddleware
{
    private readonly bool _isActive;
    private readonly ResiliencePipeline _pipeline;

    public BehaviorResilienceExecutionMiddleware(
        ResolvedBehaviorResiliencePolicy policy,
        ResiliencePipelineProvider<string> pipelineProvider)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(pipelineProvider);

        _isActive = policy.HasEnforcedStrategies;
        _pipeline = pipelineProvider.GetPipeline(policy.Id);
    }

    public ValueTask<object?> InvokeAsync(
        BehaviorExecutionInvocation invocation,
        BehaviorExecutionDelegate next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        ArgumentNullException.ThrowIfNull(next);

        if (!_isActive)
        {
            return next(invocation, cancellationToken);
        }

        return _pipeline.ExecuteAsync(
            static (state, token) => state.Next(state.Invocation, token),
            (Invocation: invocation, Next: next),
            cancellationToken);
    }
}
