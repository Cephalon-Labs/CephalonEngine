namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.Services;

/// <summary>
/// Sends SendGrid invitation delivery messages for the SendGrid companion pack.
/// </summary>
/// <remarks>
/// Hosts can replace this service to route Mail Send requests through a custom SendGrid client, test double, gateway,
/// or platform-specific HTTP policy while retaining the same Cephalon invitation dispatcher and sender metadata contract.
/// </remarks>
public interface ISendGridInvitationDeliveryClient
{
    /// <summary>
    /// Sends one SendGrid invitation delivery message.
    /// </summary>
    /// <param name="message">The message prepared by the SendGrid invitation delivery sender.</param>
    /// <param name="cancellationToken">A token that cancels the send operation.</param>
    /// <returns>The client result normalized for Cephalon sender reporting.</returns>
    ValueTask<SendGridInvitationDeliveryClientResult> SendAsync(
        SendGridInvitationDeliveryMessage message,
        CancellationToken cancellationToken = default);
}
