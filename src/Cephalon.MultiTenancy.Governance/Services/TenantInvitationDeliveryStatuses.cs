namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable status values reported by tenant invitation delivery providers or receivers.
/// </summary>
public static class TenantInvitationDeliveryStatuses
{
    /// <summary>
    /// The delivery provider accepted the message for processing.
    /// </summary>
    public const string Accepted = "accepted";

    /// <summary>
    /// The invitation was delivered to the provider-recognized recipient endpoint.
    /// </summary>
    public const string Delivered = "delivered";

    /// <summary>
    /// The invitation delivery failed.
    /// </summary>
    public const string Failed = "failed";

    /// <summary>
    /// The invitation bounced at the provider or receiver boundary.
    /// </summary>
    public const string Bounced = "bounced";

    /// <summary>
    /// The invitation delivery was deferred by the provider or receiver boundary.
    /// </summary>
    public const string Deferred = "deferred";

    /// <summary>
    /// The invitation delivery was suppressed by provider or policy rules.
    /// </summary>
    public const string Suppressed = "suppressed";

    /// <summary>
    /// The delivery provider reported a status that could not be classified.
    /// </summary>
    public const string Unknown = "unknown";

    internal static string Normalize(string status)
    {
        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            Accepted => Accepted,
            Delivered => Delivered,
            Failed => Failed,
            Bounced => Bounced,
            Deferred => Deferred,
            Suppressed => Suppressed,
            Unknown => Unknown,
            _ => normalized
        };
    }
}
