namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Hosting;

/// <summary>
/// Describes a SendGrid Event Webhook callback translation response.
/// </summary>
public sealed class SendGridInvitationDeliveryStatusCallbackResult
{
    /// <summary>
    /// Creates a SendGrid callback translation response.
    /// </summary>
    /// <param name="routePattern">The endpoint route pattern that accepted the callback.</param>
    /// <param name="totalEvents">The number of events supplied in the callback payload.</param>
    /// <param name="translatedEvents">The number of events translated into Cephalon reconciliation requests.</param>
    /// <param name="reconciledEvents">The number of events reconciled by Cephalon governance.</param>
    /// <param name="skippedEvents">The number of events skipped before reconciliation.</param>
    /// <param name="deniedEvents">The number of translated events denied by the reconciler.</param>
    /// <param name="events">Per-event translation and reconciliation results.</param>
    /// <param name="signedEventWebhookVerificationRequired">A value indicating whether SendGrid signed Event Webhook verification was required for this callback.</param>
    /// <param name="signedEventWebhookVerified">A value indicating whether the required SendGrid signed Event Webhook signature verified.</param>
    /// <param name="signedEventWebhookVerificationOutcome">The signed Event Webhook verification outcome for this callback.</param>
    public SendGridInvitationDeliveryStatusCallbackResult(
        string routePattern,
        int totalEvents,
        int translatedEvents,
        int reconciledEvents,
        int skippedEvents,
        int deniedEvents,
        IReadOnlyList<SendGridInvitationDeliveryStatusCallbackEventResult> events,
        bool signedEventWebhookVerificationRequired = false,
        bool signedEventWebhookVerified = false,
        string signedEventWebhookVerificationOutcome = "not-configured")
    {
        if (string.IsNullOrWhiteSpace(routePattern))
        {
            throw new ArgumentException("Route pattern is required.", nameof(routePattern));
        }

        RoutePattern = routePattern.Trim();
        TotalEvents = totalEvents;
        TranslatedEvents = translatedEvents;
        ReconciledEvents = reconciledEvents;
        SkippedEvents = skippedEvents;
        DeniedEvents = deniedEvents;
        Events = events ?? throw new ArgumentNullException(nameof(events));
        SignedEventWebhookVerificationRequired = signedEventWebhookVerificationRequired;
        SignedEventWebhookVerified = signedEventWebhookVerified;
        SignedEventWebhookVerificationOutcome = string.IsNullOrWhiteSpace(signedEventWebhookVerificationOutcome)
            ? "unknown"
            : signedEventWebhookVerificationOutcome.Trim();
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
    /// Gets the number of events translated into Cephalon reconciliation requests.
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
    /// Gets per-event translation and reconciliation results.
    /// </summary>
    public IReadOnlyList<SendGridInvitationDeliveryStatusCallbackEventResult> Events { get; }

    /// <summary>
    /// Gets a value indicating whether SendGrid signed Event Webhook verification was required for this callback.
    /// </summary>
    public bool SignedEventWebhookVerificationRequired { get; }

    /// <summary>
    /// Gets a value indicating whether the required SendGrid signed Event Webhook signature verified.
    /// </summary>
    public bool SignedEventWebhookVerified { get; }

    /// <summary>
    /// Gets the signed Event Webhook verification outcome for this callback.
    /// </summary>
    public string SignedEventWebhookVerificationOutcome { get; }
}
