using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Configuration;
using System.Globalization;
using System.Net;

namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Services;

internal sealed class AmazonSesInvitationDeliveryClient(
    IAmazonSimpleEmailServiceV2 amazonSesClient,
    AmazonSesInvitationDeliveryOptions options) : IAmazonSesInvitationDeliveryClient
{
    public async ValueTask<AmazonSesInvitationDeliveryClientResult> SendAsync(
        AmazonSesInvitationDeliveryMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.GetTimeout());

        try
        {
            var request = CreateRequest(message);
            var response = await amazonSesClient.SendEmailAsync(request, timeout.Token).ConfigureAwait(false);
            var statusCode = (int)response.HttpStatusCode;
            var accepted = IsAccepted(statusCode) && !string.IsNullOrWhiteSpace(response.MessageId);
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["amazonSesStatusCode"] = statusCode.ToString(CultureInfo.InvariantCulture),
                ["amazonSesSdkClient"] = amazonSesClient.GetType().Name
            };

            if (!string.IsNullOrWhiteSpace(response.MessageId))
            {
                metadata["amazonSesMessageId"] = response.MessageId.Trim();
            }

            if (!string.IsNullOrWhiteSpace(response.ResponseMetadata?.RequestId))
            {
                metadata["amazonSesRequestId"] = response.ResponseMetadata.RequestId;
            }

            return new AmazonSesInvitationDeliveryClientResult(
                accepted,
                statusCode,
                response.MessageId,
                accepted
                    ? $"Amazon SES accepted the message with status code {statusCode}."
                    : $"Amazon SES returned status code {statusCode}.",
                metadata);
        }
        catch (AmazonServiceException exception)
        {
            var statusCode = exception.StatusCode == 0 ? null : (int?)exception.StatusCode;
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["amazonSesSdkClient"] = amazonSesClient.GetType().Name,
                ["amazonSesExceptionType"] = exception.GetType().Name
            };

            if (statusCode is not null)
            {
                metadata["amazonSesStatusCode"] = statusCode.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(exception.ErrorCode))
            {
                metadata["amazonSesErrorCode"] = exception.ErrorCode.Trim();
            }

            if (!string.IsNullOrWhiteSpace(exception.RequestId))
            {
                metadata["amazonSesRequestId"] = exception.RequestId.Trim();
            }

            return new AmazonSesInvitationDeliveryClientResult(
                accepted: false,
                statusCode: statusCode,
                providerMessageId: null,
                reason: string.IsNullOrWhiteSpace(exception.Message)
                    ? "Amazon SES rejected the invitation delivery request."
                    : exception.Message,
                metadata: metadata);
        }
    }

    private static SendEmailRequest CreateRequest(AmazonSesInvitationDeliveryMessage message)
    {
        var request = new SendEmailRequest
        {
            FromEmailAddress = message.From,
            Destination = new Destination
            {
                ToAddresses = [message.ToEmail]
            },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content
                    {
                        Charset = "UTF-8",
                        Data = message.Subject
                    },
                    Body = new Body
                    {
                        Text = new Content
                        {
                            Charset = "UTF-8",
                            Data = message.TextBody
                        }
                    }
                }
            },
            EmailTags = message.Tags
                .Select(static tag => new MessageTag { Name = tag.Key, Value = tag.Value })
                .ToList()
        };

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            request.Content.Simple.Body.Html = new Content
            {
                Charset = "UTF-8",
                Data = message.HtmlBody
            };
        }

        if (message.ReplyToAddresses.Count > 0)
        {
            request.ReplyToAddresses = message.ReplyToAddresses.ToList();
        }

        if (!string.IsNullOrWhiteSpace(message.ConfigurationSetName))
        {
            request.ConfigurationSetName = message.ConfigurationSetName;
        }

        return request;
    }

    private bool IsAccepted(int statusCode) => options.GetAcceptedStatusCodes().Contains(statusCode);
}
