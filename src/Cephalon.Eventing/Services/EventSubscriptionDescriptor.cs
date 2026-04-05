namespace Cephalon.Eventing.Services;

/// <summary>
/// Describes one declared event subscription available to the active eventing runtime.
/// </summary>
public sealed class EventSubscriptionDescriptor
{
    /// <summary>
    /// Creates a new event subscription descriptor.
    /// </summary>
    /// <param name="id">The stable subscription identifier.</param>
    /// <param name="displayName">The operator-facing subscription name.</param>
    /// <param name="description">The human-readable description of the subscription.</param>
    /// <param name="channelId">The logical event channel that the subscription consumes.</param>
    /// <param name="handlerId">The logical handler or consumer identifier that receives the event.</param>
    /// <param name="deliveryMode">The declared delivery mode for the subscription.</param>
    /// <param name="tags">Optional tags that classify the subscription.</param>
    /// <param name="metadata">Optional subscription metadata.</param>
    public EventSubscriptionDescriptor(
        string id,
        string displayName,
        string description,
        string channelId,
        string handlerId,
        string deliveryMode,
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
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
        Tags = tags?
            .Where(static tag => !string.IsNullOrWhiteSpace(tag))
            .Select(static tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
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
    /// Gets the normalized tag set associated with the subscription.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets normalized metadata associated with the subscription.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
