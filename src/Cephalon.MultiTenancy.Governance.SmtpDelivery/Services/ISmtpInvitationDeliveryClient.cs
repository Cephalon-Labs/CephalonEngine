namespace Cephalon.MultiTenancy.Governance.SmtpDelivery.Services;

/// <summary>
/// Sends SMTP invitation delivery messages for the SMTP companion pack.
/// </summary>
/// <remarks>
/// Hosts can replace this service to route SMTP messages through a custom relay client, test double, or platform-specific
/// mail transport while retaining the same Cephalon invitation dispatcher and sender metadata contract.
/// </remarks>
public interface ISmtpInvitationDeliveryClient
{
    /// <summary>
    /// Sends one SMTP invitation delivery message.
    /// </summary>
    /// <param name="message">The message prepared by the SMTP invitation delivery sender.</param>
    /// <param name="cancellationToken">A token that cancels the send operation.</param>
    /// <returns>The client result normalized for Cephalon sender reporting.</returns>
    ValueTask<SmtpInvitationDeliveryClientResult> SendAsync(
        SmtpInvitationDeliveryMessage message,
        CancellationToken cancellationToken = default);
}
