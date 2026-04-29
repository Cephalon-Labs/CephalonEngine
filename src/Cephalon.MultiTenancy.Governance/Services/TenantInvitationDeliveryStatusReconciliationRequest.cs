namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes a tenant invitation delivery status reconciliation request.
/// </summary>
public sealed class TenantInvitationDeliveryStatusReconciliationRequest
{
    /// <summary>
    /// Creates a tenant invitation delivery status reconciliation request.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that owns the invitation.</param>
    /// <param name="invitationId">The invitation identifier to reconcile.</param>
    /// <param name="status">The provider or receiver delivery status.</param>
    /// <param name="providerMessageId">The provider message identifier associated with the status observation.</param>
    /// <param name="senderId">The delivery sender identifier associated with the status observation.</param>
    /// <param name="channel">The delivery channel associated with the status observation.</param>
    /// <param name="reason">The provider or receiver status reason.</param>
    /// <param name="observedAtUtc">The UTC timestamp when the status was observed. The runtime clock is used when omitted.</param>
    /// <param name="source">The source that reported the status observation.</param>
    /// <param name="actor">The actor that reported the status observation when known.</param>
    /// <param name="correlationId">The optional correlation identifier for the status observation.</param>
    /// <param name="recordStatus">A value indicating whether reconciled status metadata should be recorded on the invitation.</param>
    /// <param name="requireProviderMessageMatch">A value indicating whether an existing dispatch provider message identifier must match the request.</param>
    /// <param name="metadata">Optional delivery status metadata.</param>
    public TenantInvitationDeliveryStatusReconciliationRequest(
        string tenantId,
        string invitationId,
        string status,
        string? providerMessageId = null,
        string? senderId = null,
        string? channel = null,
        string? reason = null,
        DateTimeOffset? observedAtUtc = null,
        string? source = null,
        string? actor = null,
        string? correlationId = null,
        bool recordStatus = true,
        bool requireProviderMessageMatch = true,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(invitationId))
        {
            throw new ArgumentException("Invitation id is required.", nameof(invitationId));
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Delivery status is required.", nameof(status));
        }

        TenantId = tenantId.Trim();
        InvitationId = invitationId.Trim();
        Status = TenantInvitationDeliveryStatuses.Normalize(status);
        ProviderMessageId = string.IsNullOrWhiteSpace(providerMessageId) ? null : providerMessageId.Trim();
        SenderId = string.IsNullOrWhiteSpace(senderId) ? null : senderId.Trim();
        Channel = string.IsNullOrWhiteSpace(channel) ? null : channel.Trim();
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        ObservedAtUtc = observedAtUtc;
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Actor = string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        RecordStatus = recordStatus;
        RequireProviderMessageMatch = requireProviderMessageMatch;
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant identifier that owns the invitation.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the invitation identifier to reconcile.
    /// </summary>
    public string InvitationId { get; }

    /// <summary>
    /// Gets the provider or receiver delivery status.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the provider message identifier associated with the status observation.
    /// </summary>
    public string? ProviderMessageId { get; }

    /// <summary>
    /// Gets the delivery sender identifier associated with the status observation.
    /// </summary>
    public string? SenderId { get; }

    /// <summary>
    /// Gets the delivery channel associated with the status observation.
    /// </summary>
    public string? Channel { get; }

    /// <summary>
    /// Gets the provider or receiver status reason.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Gets the UTC timestamp when the status was observed.
    /// </summary>
    public DateTimeOffset? ObservedAtUtc { get; }

    /// <summary>
    /// Gets the source that reported the status observation.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the actor that reported the status observation when known.
    /// </summary>
    public string? Actor { get; }

    /// <summary>
    /// Gets the optional correlation identifier for the status observation.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets a value indicating whether reconciled status metadata should be recorded on the invitation.
    /// </summary>
    public bool RecordStatus { get; }

    /// <summary>
    /// Gets a value indicating whether an existing dispatch provider message identifier must match the request.
    /// </summary>
    public bool RequireProviderMessageMatch { get; }

    /// <summary>
    /// Gets optional delivery status metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static Dictionary<string, string> CopyMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return metadata
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }
}
