namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Services;

/// <summary>
/// Sends Amazon SES invitation delivery messages for the Amazon SES companion pack.
/// </summary>
/// <remarks>
/// Hosts can replace this service to route <c>SendEmail</c> requests through a custom AWS SDK client, test double,
/// gateway, or platform-specific retry policy while retaining the same Cephalon invitation dispatcher and sender
/// metadata contract.
/// </remarks>
public interface IAmazonSesInvitationDeliveryClient
{
    /// <summary>
    /// Sends one Amazon SES invitation delivery message.
    /// </summary>
    /// <param name="message">The message prepared by the Amazon SES invitation delivery sender.</param>
    /// <param name="cancellationToken">A token that cancels the send operation.</param>
    /// <returns>The client result normalized for Cephalon sender reporting.</returns>
    ValueTask<AmazonSesInvitationDeliveryClientResult> SendAsync(
        AmazonSesInvitationDeliveryMessage message,
        CancellationToken cancellationToken = default);
}
