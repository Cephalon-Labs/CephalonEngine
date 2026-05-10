using System.Reflection;

namespace Cephalon.Eventing.Services;

internal sealed class InProcessEventSubscriptionDescriptorContributor(
    IEnumerable<IEventSubscriptionExecutor> executors) : IEventSubscriptionContributor
{
    public void RegisterSubscriptions(IEventSubscriptionRegistry subscriptions)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);

        foreach (var executor in executors)
        {
            if (TryGetDescriptor(executor) is not { } descriptor)
            {
                continue;
            }

            subscriptions.Add(descriptor);
        }
    }

    private static EventSubscriptionDescriptor? TryGetDescriptor(IEventSubscriptionExecutor executor)
    {
        var executorType = executor.GetType();
        if (executor is IEventSubscriptionDescriptorProvider provider)
        {
            if (provider.SubscriptionDescriptor is not { } descriptor)
            {
                throw new InvalidOperationException(
                    $"In-process event-subscription executor '{executorType.FullName ?? executorType.Name}' returned a null subscription descriptor.");
            }

            ValidateDescriptorId(executor, descriptor, source: "provides descriptor");

            return WithDiscoveryMetadata(
                descriptor,
                executorType,
                source: "executor-provider",
                discovery: "code-first-executor",
                attributeType: null);
        }

        var attribute = executorType.GetCustomAttribute<EventSubscriptionAttribute>(inherit: false);
        if (attribute is null)
        {
            return null;
        }

        var attributeDescriptor = new EventSubscriptionDescriptor(
            attribute.Id,
            attribute.DisplayName,
            attribute.Description,
            attribute.ChannelId,
            attribute.HandlerId,
            attribute.DeliveryMode,
            attribute.Tags);

        ValidateDescriptorId(executor, attributeDescriptor, source: "declares attribute descriptor");

        return WithDiscoveryMetadata(
            attributeDescriptor,
            executorType,
            source: "executor-attribute",
            discovery: "code-first-attribute",
            attributeType: typeof(EventSubscriptionAttribute));
    }

    private static void ValidateDescriptorId(
        IEventSubscriptionExecutor executor,
        EventSubscriptionDescriptor descriptor,
        string source)
    {
        if (!string.Equals(executor.SubscriptionId, descriptor.Id, StringComparison.OrdinalIgnoreCase))
        {
            var executorType = executor.GetType();
            throw new InvalidOperationException(
                $"In-process event-subscription executor '{executorType.FullName ?? executorType.Name}' {source} '{descriptor.Id}' but its SubscriptionId is '{executor.SubscriptionId}'. The descriptor id must match the executor subscription id.");
        }
    }

    private static EventSubscriptionDescriptor WithDiscoveryMetadata(
        EventSubscriptionDescriptor descriptor,
        Type executorType,
        string source,
        string discovery,
        Type? attributeType)
    {
        var metadata = new Dictionary<string, string>(descriptor.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["descriptorSource"] = source,
            ["descriptorDiscovery"] = discovery,
            ["descriptorProvider"] = executorType.FullName ?? executorType.Name
        };

        if (attributeType is not null)
        {
            metadata["descriptorAttribute"] = attributeType.FullName ?? attributeType.Name;
        }

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
