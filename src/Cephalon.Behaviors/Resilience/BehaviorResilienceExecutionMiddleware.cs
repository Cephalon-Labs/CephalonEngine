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

    public async ValueTask<object?> InvokeAsync(
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
            return await next(invocation, cancellationToken).ConfigureAwait(false);
        }

        var pipeline = _pipelineProvider.GetPipeline(resolution.Policy.Id);
        var resilienceContext = ResilienceContextPool.Shared.Get(invocation.BehaviorId, cancellationToken);
        resilienceContext.Properties.Set(BehaviorResilienceExecutionContextKeys.BehaviorId, invocation.BehaviorId);
        if (!string.IsNullOrWhiteSpace(transportId))
        {
            resilienceContext.Properties.Set(BehaviorResilienceExecutionContextKeys.TransportId, transportId);
        }

        try
        {
            return await pipeline.ExecuteAsync(
                    static (context, state) => state.Next(state.Invocation, context.CancellationToken),
                    resilienceContext,
                    (Invocation: invocation, Next: next))
                .ConfigureAwait(false);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(resilienceContext);
        }
    }
}
