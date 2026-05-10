namespace Cephalon.Eventing.Services;

internal sealed record EventSubscriptionExecutionPipelineDescriptor(int MiddlewareCount)
{
    public string PipelineId => MiddlewareCount > 0 ? "code-first" : "none";
}
