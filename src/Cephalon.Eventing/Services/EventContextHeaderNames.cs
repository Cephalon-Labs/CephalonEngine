namespace Cephalon.Eventing.Services;

/// <summary>
/// Defines the stable Cephalon event context header names used by provider-neutral eventing policies.
/// </summary>
public static class EventContextHeaderNames
{
    /// <summary>
    /// Identifies the tenant associated with an event publication.
    /// </summary>
    public const string TenantId = "cephalon-tenant-id";

    /// <summary>
    /// Identifies the correlation id associated with an event publication.
    /// </summary>
    public const string CorrelationId = "cephalon-correlation-id";

    /// <summary>
    /// Identifies the causation id associated with an event publication.
    /// </summary>
    public const string CausationId = "cephalon-causation-id";

    /// <summary>
    /// Carries provider-neutral baggage associated with an event publication.
    /// </summary>
    public const string Baggage = "cephalon-baggage";

    /// <summary>
    /// Identifies the source event message when a policy needs a header-level message identity.
    /// </summary>
    public const string MessageId = "cephalon-message-id";
}
