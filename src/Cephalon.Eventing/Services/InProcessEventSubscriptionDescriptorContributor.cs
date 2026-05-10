namespace Cephalon.Eventing.Services;

internal sealed class InProcessEventSubscriptionDescriptorContributor(
    IEnumerable<IEventSubscriptionExecutor> executors) : IEventSubscriptionContributor
{
    public void RegisterSubscriptions(IEventSubscriptionRegistry subscriptions)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);

        foreach (var executor in executors)
        {
            if (executor is not IEventSubscriptionDescriptorProvider provider)
            {
                continue;
            }

            if (provider.SubscriptionDescriptor is not { } descriptor)
            {
                throw new InvalidOperationException(
                    $"In-process event-subscription executor '{executor.GetType().FullName ?? executor.GetType().Name}' returned a null subscription descriptor.");
            }

            if (!string.Equals(executor.SubscriptionId, descriptor.Id, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"In-process event-subscription executor '{executor.GetType().FullName ?? executor.GetType().Name}' provides descriptor '{descriptor.Id}' but its SubscriptionId is '{executor.SubscriptionId}'. The descriptor id must match the executor subscription id.");
            }

            subscriptions.Add(WithDiscoveryMetadata(descriptor, executor.GetType()));
        }
    }

    private static EventSubscriptionDescriptor WithDiscoveryMetadata(
        EventSubscriptionDescriptor descriptor,
        Type executorType)
    {
        var metadata = new Dictionary<string, string>(descriptor.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["descriptorSource"] = "executor-provider",
            ["descriptorDiscovery"] = "code-first-executor",
            ["descriptorProvider"] = executorType.FullName ?? executorType.Name
        };

        return new EventSubscriptionDescriptor(
            descriptor.Id,
            descriptor.DisplayName,
            descriptor.Description,
            descriptor.ChannelId,
            descriptor.HandlerId,
            descriptor.DeliveryMode,
            descriptor.Tags,
            metadata);
    }
}
