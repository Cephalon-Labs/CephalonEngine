namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

/// <summary>
/// Defines stable operator remediation actions for tenant-invitation delivery status observation reads.
/// </summary>
/// <remarks>
/// Remediation actions are deterministic guidance derived from normalized observation history. They do not execute
/// provider calls, mutate callback inboxes, run polling loops, or claim distributed remediation ownership.
/// </remarks>
public static class TenantInvitationDeliveryStatusObservationRemediationActions
{
    /// <summary>
    /// Review the recipient, sender configuration, or provider status before retrying or replacing the invitation.
    /// </summary>
    public const string ReviewRecipientOrSender = "review-recipient-or-sender";

    /// <summary>
    /// Monitor a deferred delivery status or retry through an owned delivery-dispatch path when appropriate.
    /// </summary>
    public const string MonitorDeferredDelivery = "monitor-deferred-delivery";

    /// <summary>
    /// Review suppression or unsubscribe policy before sending more invitations to the recipient.
    /// </summary>
    public const string ReviewSuppressionPolicy = "review-suppression-policy";

    /// <summary>
    /// Review provider callback translation or payload mapping because the normalized status was unknown.
    /// </summary>
    public const string ReviewStatusTranslation = "review-status-translation";

    /// <summary>
    /// Review reconciliation inputs such as tenant id, invitation id, provider message id, or status ownership.
    /// </summary>
    public const string ReviewReconciliationInput = "review-reconciliation-input";

    /// <summary>
    /// Review observation-store configuration or metadata recording failure before relying on the audit trail.
    /// </summary>
    public const string ReviewObservationRecording = "review-observation-recording";

    internal static string KnownValues =>
        string.Join(
            ", ",
            [
                ReviewRecipientOrSender,
                MonitorDeferredDelivery,
                ReviewSuppressionPolicy,
                ReviewStatusTranslation,
                ReviewReconciliationInput,
                ReviewObservationRecording
            ]);
}
