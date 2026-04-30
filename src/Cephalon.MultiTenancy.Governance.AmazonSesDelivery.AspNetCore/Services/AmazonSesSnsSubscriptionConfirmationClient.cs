using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Configuration;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Hosting;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Services;

internal sealed class AmazonSesSnsSubscriptionConfirmationClient(
    IHttpClientFactory httpClientFactory,
    AmazonSesInvitationDeliveryAspNetCoreOptions options) : IAmazonSesSnsSubscriptionConfirmationClient
{
    public async ValueTask<AmazonSesSnsSubscriptionConfirmationResult> ConfirmAsync(
        AmazonSesSnsSubscriptionConfirmationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.GetSnsSubscriptionConfirmationTimeout());

        var client = httpClientFactory.CreateClient(AmazonSesInvitationDeliveryAspNetCoreServiceCollectionExtensions.HttpClientName);
        using var response = await client
            .GetAsync(request.SubscribeUrl, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
            .ConfigureAwait(false);
        var statusCode = (int)response.StatusCode;
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["amazonSesSnsSubscriptionConfirmationStatusCode"] = statusCode.ToString(CultureInfo.InvariantCulture),
            ["amazonSesSnsSubscriptionConfirmationHost"] = request.SubscribeUrl.IdnHost,
            ["amazonSesSnsSubscriptionConfirmationPath"] = request.SubscribeUrl.AbsolutePath,
            ["amazonSesSnsSubscriptionConfirmationHttpMethod"] = "GET"
        };

        if (!string.IsNullOrWhiteSpace(response.ReasonPhrase))
        {
            metadata["amazonSesSnsSubscriptionConfirmationReasonPhrase"] = response.ReasonPhrase.Trim();
        }

        return response.IsSuccessStatusCode
            ? AmazonSesSnsSubscriptionConfirmationResult.Confirmed(statusCode, metadata)
            : new AmazonSesSnsSubscriptionConfirmationResult(
                succeeded: false,
                outcome: "provider-rejected",
                reason: $"Amazon SNS subscription confirmation returned status code {statusCode}.",
                statusCode,
                metadata);
    }
}
