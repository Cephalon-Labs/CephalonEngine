namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Services;

/// <summary>
/// Sends Microsoft Graph invitation delivery messages for the Microsoft Graph companion pack.
/// </summary>
/// <remarks>
/// Hosts can replace this service to route <c>sendMail</c> requests through a custom Graph SDK client, test double,
/// gateway, or platform-specific HTTP policy while retaining the same Cephalon invitation dispatcher and sender
/// metadata contract.
/// </remarks>
public interface IMicrosoftGraphInvitationDeliveryClient
{
    /// <summary>
    /// Sends one Microsoft Graph invitation delivery message.
    /// </summary>
    /// <param name="message">The message prepared by the Microsoft Graph invitation delivery sender.</param>
    /// <param name="cancellationToken">A token that cancels the send operation.</param>
    /// <returns>The client result normalized for Cephalon sender reporting.</returns>
    ValueTask<MicrosoftGraphInvitationDeliveryClientResult> SendAsync(
        MicrosoftGraphInvitationDeliveryMessage message,
        CancellationToken cancellationToken = default);
}
