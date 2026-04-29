namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the result of tenant invitation delivery status reconciliation.
/// </summary>
public sealed class TenantInvitationDeliveryStatusReconciliationResult
{
    /// <summary>
    /// Creates a tenant invitation delivery status reconciliation result.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that was evaluated.</param>
    /// <param name="invitationId">The invitation identifier that was evaluated.</param>
    /// <param name="status">The provider or receiver delivery status.</param>
    /// <param name="outcome">The stable delivery status reconciliation outcome.</param>
    /// <param name="reconciled">A value indicating whether the status observation was accepted for the invitation.</param>
    /// <param name="recorded">A value indicating whether delivery status metadata was recorded.</param>
    /// <param name="observedAtUtc">The UTC timestamp when the status was observed.</param>
    /// <param name="providerMessageId">The provider message identifier associated with the status observation.</param>
    /// <param name="senderId">The delivery sender identifier associated with the status observation.</param>
    /// <param name="channel">The delivery channel associated with the status observation.</param>
    /// <param name="invitation">The resulting invitation descriptor when one exists.</param>
    /// <param name="reason">The operator-facing delivery status reconciliation reason.</param>
    /// <param name="metadata">Optional result metadata.</param>
    public TenantInvitationDeliveryStatusReconciliationResult(
        string tenantId,
        string invitationId,
        string status,
        string outcome,
        bool reconciled,
        bool recorded,
        DateTimeOffset observedAtUtc,
        string? providerMessageId,
        string? senderId,
        string? channel,
        TenantInvitationDescriptor? invitation,
        string reason,
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

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        TenantId = tenantId.Trim();
        InvitationId = invitationId.Trim();
        Status = TenantInvitationDeliveryStatuses.Normalize(status);
        Outcome = NormalizeOutcome(outcome);
        Reconciled = reconciled;
        Recorded = recorded;
        ObservedAtUtc = observedAtUtc;
        ProviderMessageId = string.IsNullOrWhiteSpace(providerMessageId) ? null : providerMessageId.Trim();
        SenderId = string.IsNullOrWhiteSpace(senderId) ? null : senderId.Trim();
        Channel = string.IsNullOrWhiteSpace(channel) ? null : channel.Trim();
        Invitation = invitation;
        Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant identifier that was evaluated.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the invitation identifier that was evaluated.
    /// </summary>
    public string InvitationId { get; }

    /// <summary>
    /// Gets the provider or receiver delivery status.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the stable delivery status reconciliation outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether the status observation was accepted for the invitation.
    /// </summary>
    public bool Reconciled { get; }

    /// <summary>
    /// Gets a value indicating whether delivery status metadata was recorded.
    /// </summary>
    public bool Recorded { get; }

    /// <summary>
    /// Gets the UTC timestamp when the status was observed.
    /// </summary>
    public DateTimeOffset ObservedAtUtc { get; }

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
    /// Gets the resulting invitation descriptor when one exists.
    /// </summary>
    public TenantInvitationDescriptor? Invitation { get; }

    /// <summary>
    /// Gets the operator-facing delivery status reconciliation reason.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets optional result metadata.
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
