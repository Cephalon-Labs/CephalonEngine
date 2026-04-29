namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Hosting;

/// <summary>
/// Describes how one SendGrid Event Webhook item was translated and reconciled.
/// </summary>
public sealed class SendGridInvitationDeliveryStatusCallbackEventResult
{
    /// <summary>
    /// Creates a SendGrid callback event result.
    /// </summary>
    /// <param name="index">The zero-based event index inside the request payload.</param>
    /// <param name="sendGridEventId">The SendGrid event identifier when supplied.</param>
    /// <param name="sendGridMessageId">The SendGrid message identifier when supplied.</param>
    /// <param name="sendGridEventType">The SendGrid event type when supplied.</param>
    /// <param name="tenantId">The Cephalon tenant identifier when supplied.</param>
    /// <param name="invitationId">The Cephalon invitation identifier when supplied.</param>
    /// <param name="status">The normalized Cephalon delivery status when translated.</param>
    /// <param name="outcome">The translation or reconciliation outcome.</param>
    /// <param name="translated">A value indicating whether the event was translated into a reconciliation request.</param>
    /// <param name="reconciled">A value indicating whether the event reconciled a tenant invitation.</param>
    /// <param name="reason">The operator-facing reason for the event outcome.</param>
    public SendGridInvitationDeliveryStatusCallbackEventResult(
        int index,
        string? sendGridEventId,
        string? sendGridMessageId,
        string? sendGridEventType,
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
        SendGridEventId = Normalize(sendGridEventId);
        SendGridMessageId = Normalize(sendGridMessageId);
        SendGridEventType = Normalize(sendGridEventType);
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
    /// Gets the SendGrid event identifier when supplied.
    /// </summary>
    public string? SendGridEventId { get; }

    /// <summary>
    /// Gets the SendGrid message identifier when supplied.
    /// </summary>
    public string? SendGridMessageId { get; }

    /// <summary>
    /// Gets the SendGrid event type when supplied.
    /// </summary>
    public string? SendGridEventType { get; }

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
