namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.Services;

/// <summary>
/// Sends Mailgun invitation delivery messages for the Mailgun companion pack.
/// </summary>
/// <remarks>
/// Hosts can replace this service to route Messages API requests through a custom Mailgun client, test double, gateway,
/// or platform-specific HTTP policy while retaining the same Cephalon invitation dispatcher and sender metadata contract.
/// </remarks>
public interface IMailgunInvitationDeliveryClient
{
    /// <summary>
    /// Sends one Mailgun invitation delivery message.
    /// </summary>
    /// <param name="message">The message prepared by the Mailgun invitation delivery sender.</param>
    /// <param name="cancellationToken">A token that cancels the send operation.</param>
    /// <returns>The client result normalized for Cephalon sender reporting.</returns>
    ValueTask<MailgunInvitationDeliveryClientResult> SendAsync(
        MailgunInvitationDeliveryMessage message,
        CancellationToken cancellationToken = default);
}
