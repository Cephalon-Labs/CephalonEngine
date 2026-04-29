namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable metadata keys written by tenant invitation delivery dispatch.
/// </summary>
public static class TenantInvitationDeliveryMetadataKeys
{
    /// <summary>
    /// Metadata key containing the last tenant invitation delivery outcome.
    /// </summary>
    public const string LastDeliveryOutcome = "lastDeliveryOutcome";

    /// <summary>
    /// Metadata key containing the UTC timestamp when delivery dispatch was evaluated.
    /// </summary>
    public const string LastDeliveryDispatchedAtUtc = "lastDeliveryDispatchedAtUtc";

    /// <summary>
    /// Metadata key containing the delivery channel used by the last dispatch attempt.
    /// </summary>
    public const string LastDeliveryChannel = "lastDeliveryChannel";

    /// <summary>
    /// Metadata key containing the sender identifier used by the last dispatch attempt.
    /// </summary>
    public const string LastDeliverySenderId = "lastDeliverySenderId";

    /// <summary>
    /// Metadata key containing the provider message identifier returned by the sender.
    /// </summary>
    public const string LastDeliveryProviderMessageId = "lastDeliveryProviderMessageId";

    /// <summary>
    /// Metadata key containing the source that requested the last delivery dispatch.
    /// </summary>
    public const string LastDeliverySource = "lastDeliverySource";

    /// <summary>
    /// Metadata key containing the actor that requested the last delivery dispatch.
    /// </summary>
    public const string LastDeliveryActor = "lastDeliveryActor";

    /// <summary>
    /// Metadata key containing the correlation identifier for the last delivery dispatch.
    /// </summary>
    public const string LastDeliveryCorrelationId = "lastDeliveryCorrelationId";

    /// <summary>
    /// Metadata key describing Cephalon ownership of the host-agnostic dispatch pipeline.
    /// </summary>
    public const string DeliveryDispatchOwnership = "deliveryDispatchOwnership";

    /// <summary>
    /// Metadata key describing who owns provider-specific external delivery.
    /// </summary>
    public const string ExternalDeliveryOwnership = "externalDeliveryOwnership";
}
