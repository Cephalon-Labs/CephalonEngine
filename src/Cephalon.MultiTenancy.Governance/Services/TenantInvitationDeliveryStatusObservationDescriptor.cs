namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes one recorded tenant invitation delivery status observation.
/// </summary>
public sealed class TenantInvitationDeliveryStatusObservationDescriptor
{
    /// <summary>
    /// Creates a tenant invitation delivery status observation descriptor.
    /// </summary>
    /// <param name="observationId">The stable observation identifier.</param>
    /// <param name="tenantId">The tenant identifier that owns the invitation.</param>
    /// <param name="invitationId">The invitation identifier associated with the observation.</param>
    /// <param name="status">The normalized provider or receiver delivery status.</param>
    /// <param name="outcome">The reconciliation outcome produced for the observation.</param>
    /// <param name="reconciled">A value indicating whether the observation was accepted for the invitation.</param>
    /// <param name="recorded">A value indicating whether invitation delivery status metadata was recorded.</param>
    /// <param name="observedAtUtc">The UTC timestamp when the status was observed.</param>
    /// <param name="recordedAtUtc">The UTC timestamp when Cephalon recorded the observation.</param>
    /// <param name="providerMessageId">The provider message identifier associated with the observation.</param>
    /// <param name="senderId">The delivery sender identifier associated with the observation.</param>
    /// <param name="channel">The delivery channel associated with the observation.</param>
    /// <param name="source">The source that reported the observation.</param>
    /// <param name="actor">The actor that reported the observation when known.</param>
    /// <param name="correlationId">The optional correlation identifier for the observation.</param>
    /// <param name="reason">The provider or receiver status reason.</param>
    /// <param name="metadata">Optional safe observation metadata.</param>
    public TenantInvitationDeliveryStatusObservationDescriptor(
        string observationId,
        string tenantId,
        string invitationId,
        string status,
        string outcome,
        bool reconciled,
        bool recorded,
        DateTimeOffset observedAtUtc,
        DateTimeOffset recordedAtUtc,
        string? providerMessageId = null,
        string? senderId = null,
        string? channel = null,
        string? source = null,
        string? actor = null,
        string? correlationId = null,
        string? reason = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(observationId))
        {
            throw new ArgumentException("Observation id is required.", nameof(observationId));
        }

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

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        ObservationId = observationId.Trim();
        TenantId = tenantId.Trim();
        InvitationId = invitationId.Trim();
        Status = TenantInvitationDeliveryStatuses.Normalize(status);
        Outcome = NormalizeOutcome(outcome);
        Reconciled = reconciled;
        Recorded = recorded;
        ObservedAtUtc = observedAtUtc;
        RecordedAtUtc = recordedAtUtc;
        ProviderMessageId = string.IsNullOrWhiteSpace(providerMessageId) ? null : providerMessageId.Trim();
        SenderId = string.IsNullOrWhiteSpace(senderId) ? null : senderId.Trim();
        Channel = string.IsNullOrWhiteSpace(channel) ? null : channel.Trim();
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Actor = string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the stable observation identifier.
    /// </summary>
    public string ObservationId { get; }

    /// <summary>
    /// Gets the tenant identifier that owns the invitation.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the invitation identifier associated with the observation.
    /// </summary>
    public string InvitationId { get; }

    /// <summary>
    /// Gets the normalized provider or receiver delivery status.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the reconciliation outcome produced for the observation.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether the observation was accepted for the invitation.
    /// </summary>
    public bool Reconciled { get; }

    /// <summary>
    /// Gets a value indicating whether invitation delivery status metadata was recorded.
    /// </summary>
    public bool Recorded { get; }

    /// <summary>
    /// Gets the UTC timestamp when the status was observed.
    /// </summary>
    public DateTimeOffset ObservedAtUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp when Cephalon recorded the observation.
    /// </summary>
    public DateTimeOffset RecordedAtUtc { get; }

    /// <summary>
    /// Gets the provider message identifier associated with the observation.
    /// </summary>
    public string? ProviderMessageId { get; }

    /// <summary>
    /// Gets the delivery sender identifier associated with the observation.
    /// </summary>
    public string? SenderId { get; }

    /// <summary>
    /// Gets the delivery channel associated with the observation.
    /// </summary>
    public string? Channel { get; }

    /// <summary>
    /// Gets the source that reported the observation.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the actor that reported the observation when known.
    /// </summary>
    public string? Actor { get; }

    /// <summary>
    /// Gets the optional correlation identifier for the observation.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets the provider or receiver status reason.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Gets optional safe observation metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string NormalizeOutcome(string outcome)
    {
        var normalized = outcome.Trim().ToLowerInvariant();
        return normalized switch
        {
            TenantInvitationDeliveryStatusReconciliationOutcomes.Reconciled => TenantInvitationDeliveryStatusReconciliationOutcomes.Reconciled,
            TenantInvitationDeliveryStatusReconciliationOutcomes.Disabled => TenantInvitationDeliveryStatusReconciliationOutcomes.Disabled,
            TenantInvitationDeliveryStatusReconciliationOutcomes.InvitationNotFound => TenantInvitationDeliveryStatusReconciliationOutcomes.InvitationNotFound,
            TenantInvitationDeliveryStatusReconciliationOutcomes.ProviderMessageMissing => TenantInvitationDeliveryStatusReconciliationOutcomes.ProviderMessageMissing,
            TenantInvitationDeliveryStatusReconciliationOutcomes.ProviderMessageMismatch => TenantInvitationDeliveryStatusReconciliationOutcomes.ProviderMessageMismatch,
            TenantInvitationDeliveryStatusReconciliationOutcomes.StoreFailed => TenantInvitationDeliveryStatusReconciliationOutcomes.StoreFailed,
            _ => throw new ArgumentException($"Tenant invitation delivery status reconciliation outcome '{outcome}' is not supported.", nameof(outcome))
        };
    }

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
