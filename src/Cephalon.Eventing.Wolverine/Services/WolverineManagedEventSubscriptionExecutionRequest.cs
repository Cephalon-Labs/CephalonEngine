using System.ComponentModel;

namespace Cephalon.Eventing.Wolverine.Services;

/// <summary>
/// Infrastructure message used by the Wolverine eventing pack to requeue managed subscription executions.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class WolverineManagedEventSubscriptionExecutionRequest
{
    /// <summary>
    /// Creates a new infrastructure retry message for one managed event-subscription execution.
    /// </summary>
    /// <param name="subscriptionId">The declared subscription identifier that should be retried.</param>
    /// <param name="publication">The staged publication that should be delivered to the subscription.</param>
    public WolverineManagedEventSubscriptionExecutionRequest(
        string subscriptionId,
        global::Cephalon.Eventing.Services.EventPublication publication)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            throw new ArgumentException("Subscription id is required.", nameof(subscriptionId));
        }

        SubscriptionId = subscriptionId.Trim();
        Publication = publication ?? throw new ArgumentNullException(nameof(publication));
    }

    /// <summary>
    /// Gets the declared subscription identifier that should be executed.
    /// </summary>
    public string SubscriptionId { get; }

    /// <summary>
    /// Gets the staged publication that should be delivered to the managed subscription.
    /// </summary>
    public global::Cephalon.Eventing.Services.EventPublication Publication { get; }
}
