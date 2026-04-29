namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Hosting;

/// <summary>
/// Describes how one Mailgun webhook event was translated and reconciled.
/// </summary>
public sealed class MailgunInvitationDeliveryStatusCallbackEventResult
{
    /// <summary>
    /// Creates a Mailgun callback event result.
    /// </summary>
    /// <param name="index">The zero-based event index inside the request payload.</param>
    /// <param name="mailgunEventId">The Mailgun event identifier when supplied.</param>
    /// <param name="mailgunMessageId">The Mailgun message identifier when supplied.</param>
    /// <param name="mailgunEventType">The Mailgun event type when supplied.</param>
    /// <param name="tenantId">The Cephalon tenant identifier when supplied.</param>
    /// <param name="invitationId">The Cephalon invitation identifier when supplied.</param>
    /// <param name="status">The normalized Cephalon delivery status when translated.</param>
    /// <param name="outcome">The translation or reconciliation outcome.</param>
    /// <param name="translated">A value indicating whether the event was translated into a reconciliation request.</param>
    /// <param name="reconciled">A value indicating whether the event reconciled a tenant invitation.</param>
    /// <param name="reason">The operator-facing reason for the event outcome.</param>
    public MailgunInvitationDeliveryStatusCallbackEventResult(
        int index,
        string? mailgunEventId,
        string? mailgunMessageId,
        string? mailgunEventType,
        string? tenantId,
        string? invitationId,
        string? status,
        string outcome,
        bool translated,
        bool reconciled,
        string reason)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Event index must not be negative.");
        }

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        Index = index;
        MailgunEventId = Normalize(mailgunEventId);
        MailgunMessageId = Normalize(mailgunMessageId);
        MailgunEventType = Normalize(mailgunEventType);
        TenantId = Normalize(tenantId);
        InvitationId = Normalize(invitationId);
        Status = Normalize(status);
        Outcome = outcome.Trim();
        Translated = translated;
        Reconciled = reconciled;
        Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
    }

    /// <summary>
    /// Gets the zero-based event index inside the request payload.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the Mailgun event identifier when supplied.
    /// </summary>
    public string? MailgunEventId { get; }

    /// <summary>
    /// Gets the Mailgun message identifier when supplied.
    /// </summary>
    public string? MailgunMessageId { get; }

    /// <summary>
    /// Gets the Mailgun event type when supplied.
    /// </summary>
    public string? MailgunEventType { get; }

    /// <summary>
    /// Gets the Cephalon tenant identifier when supplied.
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// Gets the Cephalon invitation identifier when supplied.
    /// </summary>
    public string? InvitationId { get; }

    /// <summary>
    /// Gets the normalized Cephalon delivery status when translated.
    /// </summary>
    public string? Status { get; }

    /// <summary>
    /// Gets the translation or reconciliation outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether the event was translated into a reconciliation request.
    /// </summary>
    public bool Translated { get; }

    /// <summary>
    /// Gets a value indicating whether the event reconciled a tenant invitation.
    /// </summary>
    public bool Reconciled { get; }

    /// <summary>
    /// Gets the operator-facing reason for the event outcome.
    /// </summary>
    public string Reason { get; }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
