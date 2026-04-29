namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.Services;

/// <summary>
/// Describes a prepared Mailgun invitation delivery message.
/// </summary>
public sealed class MailgunInvitationDeliveryMessage
{
    /// <summary>
    /// Creates a prepared Mailgun invitation delivery message.
    /// </summary>
    /// <param name="messageId">The deterministic Cephalon message identifier carried in Mailgun user variables.</param>
    /// <param name="from">The formatted sender address.</param>
    /// <param name="to">The formatted recipient address.</param>
    /// <param name="toEmail">The recipient email address.</param>
    /// <param name="subject">The message subject.</param>
    /// <param name="textBody">The plain-text message body.</param>
    /// <param name="htmlBody">The optional HTML message body.</param>
    /// <param name="tags">Mailgun tags attached to the message.</param>
    /// <param name="variables">Mailgun user variables attached to the message.</param>
    /// <param name="headers">Mailgun custom headers attached to the message.</param>
    /// <param name="testMode">A value indicating whether Mailgun test mode should be enabled.</param>
    public MailgunInvitationDeliveryMessage(
        string messageId,
        string from,
        string to,
        string toEmail,
        string subject,
        string textBody,
        string? htmlBody = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? variables = null,
        IReadOnlyDictionary<string, string>? headers = null,
        bool testMode = false)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException("Message id is required.", nameof(messageId));
        }

        if (string.IsNullOrWhiteSpace(from))
        {
            throw new ArgumentException("From address is required.", nameof(from));
        }

        if (string.IsNullOrWhiteSpace(to))
        {
            throw new ArgumentException("To address is required.", nameof(to));
        }

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new ArgumentException("Recipient email is required.", nameof(toEmail));
        }

        MessageId = messageId.Trim();
        From = from.Trim();
        To = to.Trim();
        ToEmail = toEmail.Trim();
        Subject = string.IsNullOrWhiteSpace(subject) ? "(no subject)" : subject.Trim();
        TextBody = string.IsNullOrWhiteSpace(textBody) ? string.Empty : textBody;
        HtmlBody = string.IsNullOrWhiteSpace(htmlBody) ? null : htmlBody;
        Tags = CopyList(tags, limit: 10);
        Variables = CopyDictionary(variables);
        Headers = CopyDictionary(headers);
        TestMode = testMode;
    }

    /// <summary>
    /// Gets the deterministic Cephalon message identifier carried in Mailgun user variables.
    /// </summary>
    public string MessageId { get; }

    /// <summary>
    /// Gets the formatted sender address.
    /// </summary>
    public string From { get; }

    /// <summary>
    /// Gets the formatted recipient address.
    /// </summary>
    public string To { get; }

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
    /// Gets Mailgun tags attached to the message.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets Mailgun user variables attached to the message.
    /// </summary>
    public IReadOnlyDictionary<string, string> Variables { get; }

    /// <summary>
    /// Gets Mailgun custom headers attached to the message.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets a value indicating whether Mailgun test mode should be enabled.
    /// </summary>
    public bool TestMode { get; }

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

    private static Dictionary<string, string> CopyDictionary(IReadOnlyDictionary<string, string>? values)
    {
        if (values is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return values
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }
}
