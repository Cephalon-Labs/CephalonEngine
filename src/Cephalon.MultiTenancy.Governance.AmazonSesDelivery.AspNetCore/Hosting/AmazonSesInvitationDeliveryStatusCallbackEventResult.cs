namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Hosting;

/// <summary>
/// Describes how one SNS-wrapped Amazon SES event was translated and reconciled.
/// </summary>
public sealed class AmazonSesInvitationDeliveryStatusCallbackEventResult
{
    /// <summary>
    /// Creates an Amazon SES callback event result.
    /// </summary>
    /// <param name="index">The zero-based event index inside the request payload.</param>
    /// <param name="snsMessageId">The SNS message identifier when supplied.</param>
    /// <param name="snsMessageType">The SNS message type when supplied.</param>
    /// <param name="amazonSesMessageId">The Amazon SES message identifier when supplied.</param>
    /// <param name="amazonSesEventType">The Amazon SES event type when supplied.</param>
    /// <param name="tenantId">The Cephalon tenant identifier when supplied.</param>
    /// <param name="invitationId">The Cephalon invitation identifier when supplied.</param>
    /// <param name="status">The normalized Cephalon delivery status when translated.</param>
    /// <param name="outcome">The translation or reconciliation outcome.</param>
    /// <param name="translated">A value indicating whether the event was translated into a reconciliation request.</param>
    /// <param name="reconciled">A value indicating whether the event reconciled a tenant invitation.</param>
    /// <param name="reason">The operator-facing reason for the event outcome.</param>
    public AmazonSesInvitationDeliveryStatusCallbackEventResult(
        int index,
        string? snsMessageId,
        string? snsMessageType,
        string? amazonSesMessageId,
        string? amazonSesEventType,
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
        SnsMessageId = Normalize(snsMessageId);
        SnsMessageType = Normalize(snsMessageType);
        AmazonSesMessageId = Normalize(amazonSesMessageId);
        AmazonSesEventType = Normalize(amazonSesEventType);
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
    /// Gets the SNS message identifier when supplied.
    /// </summary>
    public string? SnsMessageId { get; }

    /// <summary>
    /// Gets the SNS message type when supplied.
    /// </summary>
    public string? SnsMessageType { get; }

    /// <summary>
    /// Gets the Amazon SES message identifier when supplied.
    /// </summary>
    public string? AmazonSesMessageId { get; }

    /// <summary>
    /// Gets the Amazon SES event type when supplied.
    /// </summary>
    public string? AmazonSesEventType { get; }

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
