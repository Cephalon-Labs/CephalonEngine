namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Services;

/// <summary>
/// Describes a prepared Amazon SES invitation delivery message.
/// </summary>
public sealed class AmazonSesInvitationDeliveryMessage
{
    /// <summary>
    /// Creates a prepared Amazon SES invitation delivery message.
    /// </summary>
    /// <param name="messageId">The deterministic Cephalon message identifier carried in Amazon SES message tags.</param>
    /// <param name="from">The formatted sender address.</param>
    /// <param name="toEmail">The recipient email address.</param>
    /// <param name="subject">The message subject.</param>
    /// <param name="textBody">The plain-text message body.</param>
    /// <param name="htmlBody">The optional HTML message body.</param>
    /// <param name="replyToAddresses">Reply-to addresses attached to the message.</param>
    /// <param name="tags">Amazon SES message tags attached to the message.</param>
    /// <param name="configurationSetName">The optional SES configuration set name attached to the request.</param>
    public AmazonSesInvitationDeliveryMessage(
        string messageId,
        string from,
        string toEmail,
        string subject,
        string textBody,
        string? htmlBody = null,
        IReadOnlyList<string>? replyToAddresses = null,
        IReadOnlyDictionary<string, string>? tags = null,
        string? configurationSetName = null)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException("Message id is required.", nameof(messageId));
        }

        if (string.IsNullOrWhiteSpace(from))
        {
            throw new ArgumentException("From address is required.", nameof(from));
        }

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new ArgumentException("Recipient email is required.", nameof(toEmail));
        }

        MessageId = messageId.Trim();
        From = from.Trim();
        ToEmail = toEmail.Trim();
        Subject = string.IsNullOrWhiteSpace(subject) ? "(no subject)" : subject.Trim();
        TextBody = string.IsNullOrWhiteSpace(textBody) ? string.Empty : textBody;
        HtmlBody = string.IsNullOrWhiteSpace(htmlBody) ? null : htmlBody;
        ReplyToAddresses = CopyList(replyToAddresses, limit: 10);
        Tags = CopyDictionary(tags, limit: 50);
        ConfigurationSetName = string.IsNullOrWhiteSpace(configurationSetName) ? null : configurationSetName.Trim();
    }

    /// <summary>
    /// Gets the deterministic Cephalon message identifier carried in Amazon SES message tags.
    /// </summary>
    public string MessageId { get; }

    /// <summary>
    /// Gets the formatted sender address.
    /// </summary>
    public string From { get; }

    /// <summary>
    /// Gets the recipient email address.
    /// </summary>
    public string ToEmail { get; }

    /// <summary>
    /// Gets the message subject.
    /// </summary>
    public string Subject { get; }

    /// <summary>
    /// Gets the plain-text message body.
    /// </summary>
    public string TextBody { get; }

    /// <summary>
    /// Gets the optional HTML message body.
    /// </summary>
    public string? HtmlBody { get; }

    /// <summary>
    /// Gets reply-to addresses attached to the message.
    /// </summary>
    public IReadOnlyList<string> ReplyToAddresses { get; }

    /// <summary>
    /// Gets Amazon SES message tags attached to the message.
    /// </summary>
    public IReadOnlyDictionary<string, string> Tags { get; }

    /// <summary>
    /// Gets the optional SES configuration set name attached to the request.
    /// </summary>
    public string? ConfigurationSetName { get; }

    /// <summary>
    /// Gets a value indicating whether the message has an HTML body.
    /// </summary>
    public bool HasHtmlBody => !string.IsNullOrWhiteSpace(HtmlBody);

    private static string[] CopyList(IReadOnlyList<string>? values, int limit)
    {
        if (values is null)
        {
            return [];
        }

        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToArray();
    }

    private static Dictionary<string, string> CopyDictionary(IReadOnlyDictionary<string, string>? values, int limit)
    {
        if (values is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return values
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .Take(limit)
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }
}
