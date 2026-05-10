namespace Cephalon.Eventing.Services;

/// <summary>
/// Describes a configured handler type that should execute a declared event subscription.
/// </summary>
public sealed class EventSubscriptionHandlerDescriptor
{
    /// <summary>
    /// Creates a new configured subscription handler descriptor.
    /// </summary>
    /// <param name="subscriptionId">The declared subscription identifier executed by the handler.</param>
    /// <param name="handlerTypeName">The fully qualified or assembly-qualified handler type name.</param>
    /// <param name="source">The descriptor source, such as <c>configuration</c> or <c>code</c>.</param>
    /// <param name="configurationPath">The configuration path that declared the handler binding, when known.</param>
    public EventSubscriptionHandlerDescriptor(
        string subscriptionId,
        string handlerTypeName,
        string? source = "code",
        string? configurationPath = null)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            throw new ArgumentException("Subscription id is required.", nameof(subscriptionId));
        }

        if (string.IsNullOrWhiteSpace(handlerTypeName))
        {
            throw new ArgumentException("Handler type name is required.", nameof(handlerTypeName));
        }

        SubscriptionId = subscriptionId.Trim();
        HandlerTypeName = handlerTypeName.Trim();
        Source = string.IsNullOrWhiteSpace(source) ? "code" : source.Trim();
        ConfigurationPath = configurationPath?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Gets the declared subscription identifier executed by the handler.
    /// </summary>
    public string SubscriptionId { get; }

    /// <summary>
    /// Gets the fully qualified or assembly-qualified handler type name.
    /// </summary>
    public string HandlerTypeName { get; }

    /// <summary>
    /// Gets the descriptor source, such as <c>configuration</c> or <c>code</c>.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets the configuration path that declared the handler binding, when known.
    /// </summary>
    public string ConfigurationPath { get; }
}
