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
    /// <param name="attempt">The managed execution attempt represented by this request.</param>
    public WolverineManagedEventSubscriptionExecutionRequest(
        string subscriptionId,
        global::Cephalon.Eventing.Services.EventPublication publication,
        int attempt = 1)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            throw new ArgumentException("Subscription id is required.", nameof(subscriptionId));
        }

        if (attempt < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt), attempt, "Attempt must be greater than or equal to 1.");
        }

        SubscriptionId = subscriptionId.Trim();
        Publication = publication ?? throw new ArgumentNullException(nameof(publication));
        Attempt = attempt;
    }

    /// <summary>
    /// Gets the declared subscription identifier that should be executed.
    /// </summary>
    public string SubscriptionId { get; }

    /// <summary>
    /// Gets the staged publication that should be delivered to the managed subscription.
    /// </summary>
    public global::Cephalon.Eventing.Services.EventPublication Publication { get; }

    /// <summary>
    /// Gets the managed execution attempt represented by this request.
    /// </summary>
    public int Attempt { get; }
}
