using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Configuration;

namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Services;

internal sealed class MicrosoftGraphInvitationDeliveryStaticAccessTokenProvider(
    MicrosoftGraphInvitationDeliveryOptions options) : IMicrosoftGraphInvitationDeliveryAccessTokenProvider
{
    public ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(string.IsNullOrWhiteSpace(options.AccessToken)
            ? null
            : options.AccessToken.Trim());
    }
}
