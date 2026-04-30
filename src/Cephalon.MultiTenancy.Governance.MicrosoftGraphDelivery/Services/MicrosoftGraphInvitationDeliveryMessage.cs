namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Services;

/// <summary>
/// Describes a prepared Microsoft Graph invitation delivery message.
/// </summary>
public sealed class MicrosoftGraphInvitationDeliveryMessage
{
    /// <summary>
    /// Creates a prepared Microsoft Graph invitation delivery message.
    /// </summary>
    /// <param name="messageId">The deterministic Cephalon message identifier carried in request metadata.</param>
    /// <param name="senderUserId">The Microsoft Graph sender mailbox scope or <c>me</c>.</param>
    /// <param name="toEmail">The recipient email address.</param>
    /// <param name="toName">The optional recipient display name.</param>
    /// <param name="subject">The message subject.</param>
    /// <param name="textBody">The plain-text message body.</param>
    /// <param name="htmlBody">The optional HTML message body.</param>
    /// <param name="categories">Microsoft Graph categories attached to the message.</param>
    /// <param name="headers">Safe Microsoft Graph internet message headers.</param>
    /// <param name="saveToSentItems">A value indicating whether Graph should save the message to Sent Items.</param>
    public MicrosoftGraphInvitationDeliveryMessage(
        string messageId,
        string senderUserId,
        string toEmail,
        string? toName,
        string subject,
        string textBody,
        string? htmlBody = null,
        IReadOnlyList<string>? categories = null,
        IReadOnlyDictionary<string, string>? headers = null,
        bool saveToSentItems = false)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException("Message id is required.", nameof(messageId));
        }

        if (string.IsNullOrWhiteSpace(senderUserId))
        {
            throw new ArgumentException("Sender user id is required.", nameof(senderUserId));
        }

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new ArgumentException("To email is required.", nameof(toEmail));
        }

        MessageId = messageId.Trim();
        SenderUserId = senderUserId.Trim();
        ToEmail = toEmail.Trim();
        ToName = string.IsNullOrWhiteSpace(toName) ? null : toName.Trim();
        Subject = string.IsNullOrWhiteSpace(subject) ? "(no subject)" : subject.Trim();
        TextBody = string.IsNullOrWhiteSpace(textBody) ? string.Empty : textBody;
        HtmlBody = string.IsNullOrWhiteSpace(htmlBody) ? null : htmlBody;
        Categories = CopyList(categories, limit: 25);
        Headers = CopyDictionary(headers);
        SaveToSentItems = saveToSentItems;
    }

    /// <summary>
    /// Gets the deterministic Cephalon message identifier carried in request metadata.
    /// </summary>
    public string MessageId { get; }

    /// <summary>
    /// Gets the Microsoft Graph sender mailbox scope or <c>me</c>.
    /// </summary>
    public string SenderUserId { get; }

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
    /// Gets Microsoft Graph categories attached to the message.
    /// </summary>
    public IReadOnlyList<string> Categories { get; }

    /// <summary>
    /// Gets safe Microsoft Graph internet message headers.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets a value indicating whether Graph should save the message to Sent Items.
    /// </summary>
    public bool SaveToSentItems { get; }

    /// <summary>
    /// Gets a value indicating whether the message uses HTML content.
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
