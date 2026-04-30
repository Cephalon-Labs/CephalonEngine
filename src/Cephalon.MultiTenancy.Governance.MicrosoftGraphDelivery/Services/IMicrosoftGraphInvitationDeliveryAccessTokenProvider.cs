namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Services;

/// <summary>
/// Provides bearer tokens for Microsoft Graph invitation delivery requests.
/// </summary>
/// <remarks>
/// Hosts can replace this service to integrate Azure.Identity, managed identity, workload identity, a token cache, or
/// another organization-specific OAuth flow without changing the Cephalon invitation dispatcher.
/// </remarks>
public interface IMicrosoftGraphInvitationDeliveryAccessTokenProvider
{
    /// <summary>
    /// Gets a bearer token that authorizes the Microsoft Graph <c>sendMail</c> request.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels token acquisition.</param>
    /// <returns>The bearer token without the <c>Bearer</c> scheme prefix.</returns>
    ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
