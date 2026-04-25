namespace Cephalon.Eventing.Services;

internal sealed class EventSubscriptionExecutionBindingCatalog
{
    private readonly Dictionary<string, EventSubscriptionExecutionBindingDescriptor> index;

    public EventSubscriptionExecutionBindingCatalog(
        IEventSubscriptionCatalog subscriptions,
        IEnumerable<IEventSubscriptionExecutionBindingContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);
        ArgumentNullException.ThrowIfNull(contributors);

        var bindings = contributors
            .SelectMany(static contributor => contributor.GetExecutionBindings() ?? [])
            .OrderBy(static binding => binding.SubscriptionId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        index = new Dictionary<string, EventSubscriptionExecutionBindingDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var binding in bindings)
        {
            if (!subscriptions.TryGet(binding.SubscriptionId, out _))
            {
                throw new InvalidOperationException(
                    $"Managed execution binding references unknown event subscription '{binding.SubscriptionId}'. Register the subscription before contributing managed execution ownership.");
            }

            if (!index.TryAdd(binding.SubscriptionId, binding))
            {
                throw new InvalidOperationException(
                    $"Managed execution binding for event subscription '{binding.SubscriptionId}' was contributed more than once. Only one managed execution owner can be active for a declared subscription.");
            }
        }
    }

    public IReadOnlyList<EventSubscriptionExecutionBindingDescriptor> Bindings => index.Values
        .OrderBy(static binding => binding.SubscriptionId, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public EventSubscriptionExecutionBindingDescriptor? GetBySubscriptionId(string subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return null;
        }

        return index.TryGetValue(subscriptionId.Trim(), out var binding)
            ? binding
            : null;
    }
}
