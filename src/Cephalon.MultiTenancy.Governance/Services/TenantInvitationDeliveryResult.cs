namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the result of tenant invitation delivery dispatch.
/// </summary>
public sealed class TenantInvitationDeliveryResult
{
    /// <summary>
    /// Creates a tenant invitation delivery dispatch result.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that was evaluated.</param>
    /// <param name="invitationId">The invitation identifier that was evaluated.</param>
    /// <param name="outcome">The stable delivery dispatch outcome.</param>
    /// <param name="dispatched">A value indicating whether a sender accepted the dispatch.</param>
    /// <param name="recorded">A value indicating whether delivery outcome metadata was recorded.</param>
    /// <param name="dispatchedAtUtc">The UTC timestamp used for dispatch.</param>
    /// <param name="channel">The delivery channel used by the dispatch attempt.</param>
    /// <param name="senderId">The delivery sender identifier used by the dispatch attempt.</param>
    /// <param name="providerMessageId">The provider message identifier returned by the sender.</param>
    /// <param name="invitation">The resulting invitation descriptor when one exists.</param>
    /// <param name="reason">The operator-facing delivery dispatch reason.</param>
    /// <param name="metadata">Optional result metadata.</param>
    public TenantInvitationDeliveryResult(
        string tenantId,
        string invitationId,
        string outcome,
        bool dispatched,
        bool recorded,
        DateTimeOffset dispatchedAtUtc,
        string? channel,
        string? senderId,
        string? providerMessageId,
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

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        TenantId = tenantId.Trim();
        InvitationId = invitationId.Trim();
        Outcome = NormalizeOutcome(outcome);
        Dispatched = dispatched;
        Recorded = recorded;
        DispatchedAtUtc = dispatchedAtUtc;
        Channel = string.IsNullOrWhiteSpace(channel) ? null : channel.Trim();
        SenderId = string.IsNullOrWhiteSpace(senderId) ? null : senderId.Trim();
        ProviderMessageId = string.IsNullOrWhiteSpace(providerMessageId) ? null : providerMessageId.Trim();
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
    /// Gets the stable delivery dispatch outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether a sender accepted the dispatch.
    /// </summary>
    public bool Dispatched { get; }

    /// <summary>
    /// Gets a value indicating whether delivery outcome metadata was recorded.
    /// </summary>
    public bool Recorded { get; }

    /// <summary>
    /// Gets the UTC timestamp used for dispatch.
    /// </summary>
    public DateTimeOffset DispatchedAtUtc { get; }

    /// <summary>
    /// Gets the delivery channel used by the dispatch attempt.
    /// </summary>
    public string? Channel { get; }

    /// <summary>
    /// Gets the delivery sender identifier used by the dispatch attempt.
    /// </summary>
    public string? SenderId { get; }

    /// <summary>
    /// Gets the provider message identifier returned by the sender.
    /// </summary>
    public string? ProviderMessageId { get; }

    /// <summary>
    /// Gets the resulting invitation descriptor when one exists.
    /// </summary>
    public TenantInvitationDescriptor? Invitation { get; }

    /// <summary>
    /// Gets the operator-facing delivery dispatch reason.
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
            TenantInvitationDeliveryOutcomes.Dispatched => TenantInvitationDeliveryOutcomes.Dispatched,
            TenantInvitationDeliveryOutcomes.Disabled => TenantInvitationDeliveryOutcomes.Disabled,
            TenantInvitationDeliveryOutcomes.InvitationNotFound => TenantInvitationDeliveryOutcomes.InvitationNotFound,
            TenantInvitationDeliveryOutcomes.InvitationNotPending => TenantInvitationDeliveryOutcomes.InvitationNotPending,
            TenantInvitationDeliveryOutcomes.InvitationExpired => TenantInvitationDeliveryOutcomes.InvitationExpired,
            TenantInvitationDeliveryOutcomes.SenderNotConfigured => TenantInvitationDeliveryOutcomes.SenderNotConfigured,
            TenantInvitationDeliveryOutcomes.SenderFailed => TenantInvitationDeliveryOutcomes.SenderFailed,
            TenantInvitationDeliveryOutcomes.Suppressed => TenantInvitationDeliveryOutcomes.Suppressed,
            TenantInvitationDeliveryOutcomes.StoreFailed => TenantInvitationDeliveryOutcomes.StoreFailed,
            _ => throw new ArgumentException($"Tenant invitation delivery outcome '{outcome}' is not supported.", nameof(outcome))
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
