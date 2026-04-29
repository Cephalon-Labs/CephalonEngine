namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.Services;

/// <summary>
/// Describes a prepared SendGrid invitation delivery message.
/// </summary>
public sealed class SendGridInvitationDeliveryMessage
{
    /// <summary>
    /// Creates a prepared SendGrid invitation delivery message.
    /// </summary>
    /// <param name="messageId">The deterministic Cephalon message identifier carried in SendGrid custom arguments.</param>
    /// <param name="fromEmail">The sender email address.</param>
    /// <param name="fromName">The optional sender display name.</param>
    /// <param name="toEmail">The recipient email address.</param>
    /// <param name="toName">The optional recipient display name.</param>
    /// <param name="subject">The message subject.</param>
    /// <param name="textBody">The plain-text message body.</param>
    /// <param name="htmlBody">The optional HTML message body.</param>
    /// <param name="categories">SendGrid categories attached to the message.</param>
    /// <param name="customArgs">SendGrid custom arguments attached to the personalization.</param>
    /// <param name="headers">Safe SendGrid message headers.</param>
    /// <param name="sandboxMode">A value indicating whether SendGrid sandbox mode should be enabled.</param>
    public SendGridInvitationDeliveryMessage(
        string messageId,
        string fromEmail,
        string? fromName,
        string toEmail,
        string? toName,
        string subject,
        string textBody,
        string? htmlBody = null,
        IReadOnlyList<string>? categories = null,
        IReadOnlyDictionary<string, string>? customArgs = null,
        IReadOnlyDictionary<string, string>? headers = null,
        bool sandboxMode = false)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException("Message id is required.", nameof(messageId));
        }

        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new ArgumentException("From email is required.", nameof(fromEmail));
        }

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new ArgumentException("To email is required.", nameof(toEmail));
        }

        MessageId = messageId.Trim();
        FromEmail = fromEmail.Trim();
        FromName = string.IsNullOrWhiteSpace(fromName) ? null : fromName.Trim();
        ToEmail = toEmail.Trim();
        ToName = string.IsNullOrWhiteSpace(toName) ? null : toName.Trim();
        Subject = string.IsNullOrWhiteSpace(subject) ? "(no subject)" : subject.Trim();
        TextBody = string.IsNullOrWhiteSpace(textBody) ? string.Empty : textBody;
        HtmlBody = string.IsNullOrWhiteSpace(htmlBody) ? null : htmlBody;
        Categories = CopyList(categories, limit: 10);
        CustomArgs = CopyDictionary(customArgs);
        Headers = CopyDictionary(headers);
        SandboxMode = sandboxMode;
    }

    /// <summary>
    /// Gets the deterministic Cephalon message identifier carried in SendGrid custom arguments.
    /// </summary>
    public string MessageId { get; }

    /// <summary>
    /// Gets the sender email address.
    /// </summary>
    public string FromEmail { get; }

    /// <summary>
    /// Gets the optional sender display name.
    /// </summary>
    public string? FromName { get; }

    /// <summary>
    /// Gets the recipient email address.
    /// </summary>
    public string ToEmail { get; }

    /// <summary>
    /// Gets the optional recipient display name.
    /// </summary>
    public string? ToName { get; }

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
    /// Gets SendGrid categories attached to the message.
    /// </summary>
    public IReadOnlyList<string> Categories { get; }

    /// <summary>
    /// Gets SendGrid custom arguments attached to the personalization.
    /// </summary>
    public IReadOnlyDictionary<string, string> CustomArgs { get; }

    /// <summary>
    /// Gets safe SendGrid message headers.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets a value indicating whether SendGrid sandbox mode should be enabled.
    /// </summary>
    public bool SandboxMode { get; }

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
