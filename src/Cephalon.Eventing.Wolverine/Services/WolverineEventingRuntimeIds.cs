namespace Cephalon.Eventing.Wolverine.Services;

internal static class WolverineEventingRuntimeIds
{
    public const string DispatchRuntimeId = "wolverine-dispatch-loop";
    public const string ExecutionGraphId = "wolverine-event-dispatch-flow";
    public const string HostedExecutionId = "wolverine-event-dispatch-pump";
    public const string PublisherId = DispatchRuntimeId;
    public const string SubscriptionExecutionRuntimeId = "wolverine-subscription-execution";
}
