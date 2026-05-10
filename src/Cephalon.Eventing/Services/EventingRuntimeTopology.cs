namespace Cephalon.Eventing.Services;

internal sealed record EventingRuntimeTopology(
    bool HasChannelContributors,
    bool HasDispatchStore,
    bool HasDispatchRuntimeContributors,
    bool HasExternalManagedSubscriptionExecutionBindings,
    bool HasInboxPath,
    bool HasInProcessSubscriptionDescriptorDiscovery,
    string InProcessSubscriptionDescriptorDiscoveryMode,
    bool HasInProcessSubscriptionExecutionPath,
    bool HasManagedSubscriptionExecutionBindings,
    bool HasOutboxPublishingPath,
    bool HasPublishingPath,
    bool HasSubscriptionContributors,
    bool HasSubscriptionExecutors,
    int SubscriptionExecutionMiddlewareCount)
{
    public string SubscriptionExecutionPipeline => SubscriptionExecutionMiddlewareCount > 0 ? "code-first" : "none";
}
