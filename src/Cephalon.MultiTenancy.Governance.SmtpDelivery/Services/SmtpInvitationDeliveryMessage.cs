namespace Cephalon.MultiTenancy.Governance.SmtpDelivery.Services;

/// <summary>
/// Describes a prepared SMTP invitation delivery message.
/// </summary>
public sealed class SmtpInvitationDeliveryMessage
{
    /// <summary>
    /// Creates a prepared SMTP invitation delivery message.
    /// </summary>
    /// <param name="messageId">The deterministic SMTP message identifier.</param>
    /// <param name="fromAddress">The sender email address.</param>
    /// <param name="fromDisplayName">The optional sender display name.</param>
    /// <param name="toAddress">The recipient email address.</param>
    /// <param name="toDisplayName">The optional recipient display name.</param>
    /// <param name="subject">The message subject.</param>
    /// <param name="textBody">The plain-text message body.</param>
    /// <param name="htmlBody">The optional HTML message body.</param>
    /// <param name="headers">Safe additional SMTP headers.</param>
    public SmtpInvitationDeliveryMessage(
        string messageId,
        string fromAddress,
        string? fromDisplayName,
        string toAddress,
        string? toDisplayName,
        string subject,
        string textBody,
        string? htmlBody = null,
        IReadOnlyDictionary<string, string>? headers = null)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException("Message id is required.", nameof(messageId));
        }

        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            throw new ArgumentException("From address is required.", nameof(fromAddress));
        }

        if (string.IsNullOrWhiteSpace(toAddress))
        {
            throw new ArgumentException("To address is required.", nameof(toAddress));
        }

        MessageId = messageId.Trim();
        FromAddress = fromAddress.Trim();
        FromDisplayName = string.IsNullOrWhiteSpace(fromDisplayName) ? null : fromDisplayName.Trim();
        ToAddress = toAddress.Trim();
        ToDisplayName = string.IsNullOrWhiteSpace(toDisplayName) ? null : toDisplayName.Trim();
        Subject = string.IsNullOrWhiteSpace(subject) ? "(no subject)" : subject.Trim();
        TextBody = string.IsNullOrWhiteSpace(textBody) ? string.Empty : textBody;
        HtmlBody = string.IsNullOrWhiteSpace(htmlBody) ? null : htmlBody;
        Headers = CopyHeaders(headers);
    }

    /// <summary>
    /// Gets the deterministic SMTP message identifier.
    /// </summary>
    public string MessageId { get; }

    /// <summary>
    /// Gets the sender email address.
    /// </summary>
    public string FromAddress { get; }

    /// <summary>
    /// Gets the optional sender display name.
    /// </summary>
    public string? FromDisplayName { get; }

    /// <summary>
    /// Gets the recipient email address.
    /// </summary>
    public string ToAddress { get; }

    /// <summary>
    /// Gets the optional recipient display name.
    /// </summary>
    public string? ToDisplayName { get; }

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
    /// Gets safe additional SMTP headers.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    private static Dictionary<string, string> CopyHeaders(IReadOnlyDictionary<string, string>? headers)
    {
        if (headers is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return headers
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }
}
