namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Services;

/// <summary>
/// Describes a verified Amazon SNS subscription-confirmation request received by the Amazon SES callback adapter.
/// </summary>
/// <remarks>
/// The request intentionally keeps only the values needed by a confirmation client. The signed <c>SubscribeURL</c>
/// may contain a provider token and should be treated as sensitive by custom client implementations.
/// </remarks>
public sealed class AmazonSesSnsSubscriptionConfirmationRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonSesSnsSubscriptionConfirmationRequest" /> class.
    /// </summary>
    /// <param name="topicArn">The SNS topic ARN from the verified envelope.</param>
    /// <param name="messageId">The SNS message id from the verified envelope.</param>
    /// <param name="token">The SNS confirmation token from the verified envelope.</param>
    /// <param name="subscribeUrl">The signed SNS subscription confirmation URL.</param>
    /// <param name="timestamp">The SNS timestamp from the verified envelope, when available.</param>
    public AmazonSesSnsSubscriptionConfirmationRequest(
        string topicArn,
        string messageId,
        string token,
        Uri subscribeUrl,
        string? timestamp)
    {
        if (string.IsNullOrWhiteSpace(topicArn))
        {
            throw new ArgumentException("Topic ARN is required.", nameof(topicArn));
        }

        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException("Message id is required.", nameof(messageId));
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Subscription token is required.", nameof(token));
        }

        ArgumentNullException.ThrowIfNull(subscribeUrl);

        TopicArn = topicArn.Trim();
        MessageId = messageId.Trim();
        Token = token.Trim();
        SubscribeUrl = subscribeUrl;
        Timestamp = string.IsNullOrWhiteSpace(timestamp) ? null : timestamp.Trim();
    }

    /// <summary>
    /// Gets the SNS topic ARN from the verified envelope.
    /// </summary>
    public string TopicArn { get; }

    /// <summary>
    /// Gets the SNS message id from the verified envelope.
    /// </summary>
    public string MessageId { get; }

    /// <summary>
    /// Gets the SNS confirmation token from the verified envelope.
    /// </summary>
    public string Token { get; }

    /// <summary>
    /// Gets the signed SNS subscription confirmation URL.
    /// </summary>
    public Uri SubscribeUrl { get; }

    /// <summary>
    /// Gets the SNS timestamp from the verified envelope, when available.
    /// </summary>
    public string? Timestamp { get; }
}
