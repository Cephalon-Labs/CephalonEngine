namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Services;

/// <summary>
/// Confirms Amazon SNS subscriptions for the Amazon SES callback adapter.
/// </summary>
/// <remarks>
/// Hosts can replace this service to route subscription confirmation through a platform HTTP policy, an AWS SDK seam,
/// a queue-backed approval workflow, or a test double while keeping the same Cephalon callback result and runtime
/// metadata contract.
/// </remarks>
public interface IAmazonSesSnsSubscriptionConfirmationClient
{
    /// <summary>
    /// Confirms one verified SNS subscription-confirmation envelope.
    /// </summary>
    /// <param name="request">The verified SNS subscription-confirmation request.</param>
    /// <param name="cancellationToken">A token that cancels the confirmation operation.</param>
    /// <returns>The normalized confirmation result.</returns>
    ValueTask<AmazonSesSnsSubscriptionConfirmationResult> ConfirmAsync(
        AmazonSesSnsSubscriptionConfirmationRequest request,
        CancellationToken cancellationToken = default);
}
