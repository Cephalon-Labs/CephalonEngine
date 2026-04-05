namespace Cephalon.Eventing.Services;

/// <summary>
/// Records application-managed runtime observations for declared event subscriptions.
/// </summary>
public interface IEventSubscriptionRuntimeReporter
{
    /// <summary>
    /// Records one application-managed execution observation for a declared event subscription.
    /// </summary>
    /// <param name="report">The runtime observation to record.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes when the observation has been recorded.</returns>
    ValueTask ReportAsync(EventSubscriptionExecutionReport report, CancellationToken cancellationToken = default);
}
