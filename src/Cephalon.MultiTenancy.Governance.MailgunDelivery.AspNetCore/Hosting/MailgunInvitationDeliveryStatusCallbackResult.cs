namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Hosting;

/// <summary>
/// Describes a Mailgun webhook callback translation response.
/// </summary>
public sealed class MailgunInvitationDeliveryStatusCallbackResult
{
    /// <summary>
    /// Creates a Mailgun callback translation response.
    /// </summary>
    /// <param name="routePattern">The endpoint route pattern that accepted the callback.</param>
    /// <param name="totalEvents">The number of events supplied in the callback payload.</param>
    /// <param name="translatedEvents">The number of events translated into Cephalon reconciliation requests.</param>
    /// <param name="reconciledEvents">The number of events reconciled by Cephalon governance.</param>
    /// <param name="skippedEvents">The number of events skipped before reconciliation.</param>
    /// <param name="deniedEvents">The number of translated events denied by the reconciler.</param>
    /// <param name="signedWebhookVerificationRequired">A value indicating whether Mailgun webhook signature verification was required.</param>
    /// <param name="signedWebhookVerified">A value indicating whether the Mailgun webhook signature verified.</param>
    /// <param name="signedWebhookVerificationOutcome">The Mailgun webhook signature verification outcome.</param>
    /// <param name="signedWebhookSignatureField">The Mailgun signature field that verified the callback, when configured.</param>
    /// <param name="events">Per-event translation and reconciliation results.</param>
    /// <param name="signedWebhookReplayProtectionEnabled">A value indicating whether process-local replay protection was enabled for this verified signed callback.</param>
    /// <param name="signedWebhookReplayProtectionOutcome">The replay-protection outcome for this callback.</param>
    /// <param name="duplicateEvents">The number of translated Mailgun events skipped because their event id was already observed.</param>
    public MailgunInvitationDeliveryStatusCallbackResult(
        string routePattern,
        int totalEvents,
        int translatedEvents,
        int reconciledEvents,
        int skippedEvents,
        int deniedEvents,
        bool signedWebhookVerificationRequired,
        bool signedWebhookVerified,
        string signedWebhookVerificationOutcome,
        string? signedWebhookSignatureField,
        IReadOnlyList<MailgunInvitationDeliveryStatusCallbackEventResult> events,
        bool signedWebhookReplayProtectionEnabled = false,
        string signedWebhookReplayProtectionOutcome = "not-configured",
        int duplicateEvents = 0)
    {
        if (string.IsNullOrWhiteSpace(routePattern))
        {
            throw new ArgumentException("Route pattern is required.", nameof(routePattern));
        }

        if (string.IsNullOrWhiteSpace(signedWebhookVerificationOutcome))
        {
            throw new ArgumentException("Signed webhook verification outcome is required.", nameof(signedWebhookVerificationOutcome));
        }

        RoutePattern = routePattern.Trim();
        TotalEvents = totalEvents;
        TranslatedEvents = translatedEvents;
        ReconciledEvents = reconciledEvents;
        SkippedEvents = skippedEvents;
        DeniedEvents = deniedEvents;
        SignedWebhookVerificationRequired = signedWebhookVerificationRequired;
        SignedWebhookVerified = signedWebhookVerified;
        SignedWebhookVerificationOutcome = signedWebhookVerificationOutcome.Trim();
        SignedWebhookSignatureField = string.IsNullOrWhiteSpace(signedWebhookSignatureField)
            ? null
            : signedWebhookSignatureField.Trim();
        Events = events ?? throw new ArgumentNullException(nameof(events));
        SignedWebhookReplayProtectionEnabled = signedWebhookReplayProtectionEnabled;
        SignedWebhookReplayProtectionOutcome = string.IsNullOrWhiteSpace(signedWebhookReplayProtectionOutcome)
            ? "unknown"
            : signedWebhookReplayProtectionOutcome.Trim();
        DuplicateEvents = duplicateEvents;
    }

    /// <summary>
    /// Gets the endpoint route pattern that accepted the callback.
    /// </summary>
    public string RoutePattern { get; }

    /// <summary>
    /// Gets the number of events supplied in the callback payload.
    /// </summary>
    public int TotalEvents { get; }

    /// <summary>
    /// Gets the number of events translated into Cephalon delivery-status events.
    /// </summary>
    public int TranslatedEvents { get; }

    /// <summary>
    /// Gets the number of events reconciled by Cephalon governance.
    /// </summary>
    public int ReconciledEvents { get; }

    /// <summary>
    /// Gets the number of events skipped before reconciliation.
    /// </summary>
    public int SkippedEvents { get; }

    /// <summary>
    /// Gets the number of translated events denied by the reconciler.
    /// </summary>
    public int DeniedEvents { get; }

    /// <summary>
    /// Gets a value indicating whether Mailgun webhook signature verification was required.
    /// </summary>
    public bool SignedWebhookVerificationRequired { get; }

    /// <summary>
    /// Gets a value indicating whether the Mailgun webhook signature verified.
    /// </summary>
    public bool SignedWebhookVerified { get; }

    /// <summary>
    /// Gets the Mailgun webhook signature verification outcome.
    /// </summary>
    public string SignedWebhookVerificationOutcome { get; }

    /// <summary>
    /// Gets the Mailgun signature field that verified the callback, when configured.
    /// </summary>
    public string? SignedWebhookSignatureField { get; }

    /// <summary>
    /// Gets a value indicating whether process-local replay protection was enabled for this verified signed callback.
    /// </summary>
    public bool SignedWebhookReplayProtectionEnabled { get; }

    /// <summary>
    /// Gets the replay-protection outcome for this callback.
    /// </summary>
    public string SignedWebhookReplayProtectionOutcome { get; }

    /// <summary>
    /// Gets the number of translated Mailgun events skipped because their event id was already observed.
    /// </summary>
    public int DuplicateEvents { get; }

    /// <summary>
    /// Gets per-event translation and reconciliation results.
    /// </summary>
    public IReadOnlyList<MailgunInvitationDeliveryStatusCallbackEventResult> Events { get; }
}
