using Cephalon.Behaviors.Services;
using Polly;
using Polly.Registry;

namespace Cephalon.Behaviors.Resilience;

internal sealed class BehaviorResilienceExecutionMiddleware : IBehaviorExecutionMiddleware
{
    private readonly BehaviorResiliencePolicyCatalog _catalog;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;

    public BehaviorResilienceExecutionMiddleware(
        BehaviorResiliencePolicyCatalog catalog,
        ResiliencePipelineProvider<string> pipelineProvider)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(pipelineProvider);

        _catalog = catalog;
        _pipelineProvider = pipelineProvider;
    }

    public ValueTask<object?> InvokeAsync(
        BehaviorExecutionInvocation invocation,
        BehaviorExecutionDelegate next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        ArgumentNullException.ThrowIfNull(next);

        var transportId = invocation.Context.Metadata.TryGetValue("TransportId", out var resolvedTransportId)
            ? resolvedTransportId
            : null;
        var resolution = _catalog.Resolve(invocation.BehaviorId, transportId);
        if (resolution.Mode != BehaviorResiliencePolicyMode.Active ||
            resolution.Policy is null ||
            !resolution.Policy.HasEnforcedStrategies)
        {
            return next(invocation, cancellationToken);
        }

        var pipeline = _pipelineProvider.GetPipeline(resolution.Policy.Id);
        return pipeline.ExecuteAsync(
            static (state, token) => state.Next(state.Invocation, token),
            (Invocation: invocation, Next: next),
            cancellationToken);
    }
}
