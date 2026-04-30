namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Hosting;

/// <summary>
/// Describes an Amazon SES over SNS callback translation response.
/// </summary>
public sealed class AmazonSesInvitationDeliveryStatusCallbackResult
{
    /// <summary>
    /// Creates an Amazon SES callback translation response.
    /// </summary>
    /// <param name="routePattern">The endpoint route pattern that accepted the callback.</param>
    /// <param name="totalEvents">The number of events supplied in the callback payload.</param>
    /// <param name="translatedEvents">The number of events translated into Cephalon reconciliation requests.</param>
    /// <param name="reconciledEvents">The number of events reconciled by Cephalon governance.</param>
    /// <param name="skippedEvents">The number of events skipped before reconciliation.</param>
    /// <param name="deniedEvents">The number of translated events denied by the reconciler.</param>
    /// <param name="snsSignatureVerificationRequired">A value indicating whether SNS signature verification was required.</param>
    /// <param name="snsSignatureVerified">A value indicating whether the SNS signature verified.</param>
    /// <param name="snsSignatureVerificationOutcome">The SNS signature verification outcome.</param>
    /// <param name="events">Per-event translation and reconciliation results.</param>
    public AmazonSesInvitationDeliveryStatusCallbackResult(
        string routePattern,
        int totalEvents,
        int translatedEvents,
        int reconciledEvents,
        int skippedEvents,
        int deniedEvents,
        bool snsSignatureVerificationRequired,
        bool snsSignatureVerified,
        string snsSignatureVerificationOutcome,
        IReadOnlyList<AmazonSesInvitationDeliveryStatusCallbackEventResult> events)
    {
        if (string.IsNullOrWhiteSpace(routePattern))
        {
            throw new ArgumentException("Route pattern is required.", nameof(routePattern));
        }

        if (string.IsNullOrWhiteSpace(snsSignatureVerificationOutcome))
        {
            throw new ArgumentException("SNS signature verification outcome is required.", nameof(snsSignatureVerificationOutcome));
        }

        RoutePattern = routePattern.Trim();
        TotalEvents = totalEvents;
        TranslatedEvents = translatedEvents;
        ReconciledEvents = reconciledEvents;
        SkippedEvents = skippedEvents;
        DeniedEvents = deniedEvents;
        SnsSignatureVerificationRequired = snsSignatureVerificationRequired;
        SnsSignatureVerified = snsSignatureVerified;
        SnsSignatureVerificationOutcome = snsSignatureVerificationOutcome.Trim();
        Events = events ?? throw new ArgumentNullException(nameof(events));
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
    /// Gets a value indicating whether SNS signature verification was required.
    /// </summary>
    public bool SnsSignatureVerificationRequired { get; }

    /// <summary>
    /// Gets a value indicating whether the SNS signature verified.
    /// </summary>
    public bool SnsSignatureVerified { get; }

    /// <summary>
    /// Gets the SNS signature verification outcome.
    /// </summary>
    public string SnsSignatureVerificationOutcome { get; }

    /// <summary>
    /// Gets per-event translation and reconciliation results.
    /// </summary>
    public IReadOnlyList<AmazonSesInvitationDeliveryStatusCallbackEventResult> Events { get; }
}
