namespace Cephalon.Eventing.Services;

/// <summary>
/// Declares the subscription descriptor owned by an in-process event subscription executor.
/// </summary>
/// <remarks>
/// Apply this attribute to a concrete <see cref="IEventSubscriptionExecutor" /> implementation
/// when the subscription metadata is static and can be discovered from code at startup. Richer
/// dynamic descriptor metadata can still be supplied by implementing
/// <see cref="IEventSubscriptionDescriptorProvider" /> directly.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class EventSubscriptionAttribute : Attribute
{
    private string[] tags = [];

    /// <summary>
    /// Creates a new event subscription descriptor attribute.
    /// </summary>
    /// <param name="id">The stable subscription identifier.</param>
    /// <param name="displayName">The operator-facing subscription name.</param>
    /// <param name="description">The human-readable description of the subscription.</param>
    /// <param name="channelId">The logical event channel that the subscription consumes.</param>
    /// <param name="handlerId">The logical handler or consumer identifier that receives the event.</param>
    /// <param name="deliveryMode">The declared delivery mode for the subscription.</param>
    public EventSubscriptionAttribute(
        string id,
        string displayName,
        string description,
        string channelId,
        string handlerId,
        string deliveryMode)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Subscription id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Subscription display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Subscription description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(channelId))
        {
            throw new ArgumentException("Subscription channel id is required.", nameof(channelId));
        }

        if (string.IsNullOrWhiteSpace(handlerId))
        {
            throw new ArgumentException("Subscription handler id is required.", nameof(handlerId));
        }

        if (string.IsNullOrWhiteSpace(deliveryMode))
        {
            throw new ArgumentException("Subscription delivery mode is required.", nameof(deliveryMode));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        ChannelId = channelId.Trim();
        HandlerId = handlerId.Trim();
        DeliveryMode = deliveryMode.Trim();
    }

    /// <summary>
    /// Gets the stable subscription identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing display name for the subscription.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the subscription.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the logical event channel that the subscription consumes.
    /// </summary>
    public string ChannelId { get; }

    /// <summary>
    /// Gets the logical handler or consumer identifier that receives the event.
    /// </summary>
    public string HandlerId { get; }

    /// <summary>
    /// Gets the declared delivery mode for the subscription.
    /// </summary>
    public string DeliveryMode { get; }

    /// <summary>
    /// Gets or sets optional tags that classify the subscription.
    /// </summary>
    public string[] Tags
    {
        get => tags;
        set => tags = value is null
            ? []
            : value
                .Where(static tag => !string.IsNullOrWhiteSpace(tag))
                .Select(static tag => tag.Trim())
                .ToArray();
    }
}
