namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes one queued tenant invitation delivery retry entry.
/// </summary>
public sealed class TenantInvitationDeliveryRetryDescriptor
{
    /// <summary>
    /// Creates a tenant invitation delivery retry descriptor.
    /// </summary>
    /// <param name="retryId">The stable retry entry identifier.</param>
    /// <param name="tenantId">The tenant identifier that owns the invitation.</param>
    /// <param name="invitationId">The invitation identifier to retry.</param>
    /// <param name="channel">The delivery channel to retry.</param>
    /// <param name="senderId">The sender identifier to retry when specified.</param>
    /// <param name="source">The source recorded on the next retry attempt.</param>
    /// <param name="actor">The actor recorded on the next retry attempt.</param>
    /// <param name="correlationId">The correlation identifier retained for retry attempts.</param>
    /// <param name="recordDelivery">A value indicating whether retry attempts should record delivery metadata.</param>
    /// <param name="status">The retry entry status.</param>
    /// <param name="attemptCount">The number of dispatch attempts represented by this entry, including the original failed attempt.</param>
    /// <param name="maxAttempts">The maximum number of dispatch attempts allowed for this entry.</param>
    /// <param name="createdAtUtc">The UTC timestamp when the retry entry was created.</param>
    /// <param name="nextAttemptAtUtc">The UTC timestamp when the next attempt is due.</param>
    /// <param name="lastAttemptAtUtc">The UTC timestamp of the latest dispatch attempt.</param>
    /// <param name="lastOutcome">The latest delivery dispatch outcome.</param>
    /// <param name="lastReason">The latest delivery dispatch reason.</param>
    /// <param name="metadata">Optional retry metadata.</param>
    public TenantInvitationDeliveryRetryDescriptor(
        string retryId,
        string tenantId,
        string invitationId,
        string? channel,
        string? senderId,
        string? source,
        string? actor,
        string? correlationId,
        bool recordDelivery,
        string status,
        int attemptCount,
        int maxAttempts,
        DateTimeOffset createdAtUtc,
        DateTimeOffset nextAttemptAtUtc,
        DateTimeOffset? lastAttemptAtUtc,
        string? lastOutcome,
        string? lastReason,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(retryId))
        {
            throw new ArgumentException("Retry id is required.", nameof(retryId));
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(invitationId))
        {
            throw new ArgumentException("Invitation id is required.", nameof(invitationId));
        }

        RetryId = retryId.Trim();
        TenantId = tenantId.Trim();
        InvitationId = invitationId.Trim();
        Channel = string.IsNullOrWhiteSpace(channel) ? "default" : channel.Trim();
        SenderId = string.IsNullOrWhiteSpace(senderId) ? null : senderId.Trim();
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Actor = string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        RecordDelivery = recordDelivery;
        Status = NormalizeStatus(status);
        AttemptCount = Math.Max(0, attemptCount);
        MaxAttempts = Math.Max(1, maxAttempts);
        CreatedAtUtc = createdAtUtc;
        NextAttemptAtUtc = nextAttemptAtUtc;
        LastAttemptAtUtc = lastAttemptAtUtc;
        LastOutcome = string.IsNullOrWhiteSpace(lastOutcome) ? null : lastOutcome.Trim().ToLowerInvariant();
        LastReason = string.IsNullOrWhiteSpace(lastReason) ? null : lastReason.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the stable retry entry identifier.
    /// </summary>
    public string RetryId { get; }

    /// <summary>
    /// Gets the tenant identifier that owns the invitation.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the invitation identifier to retry.
    /// </summary>
    public string InvitationId { get; }

    /// <summary>
    /// Gets the delivery channel to retry.
    /// </summary>
    public string Channel { get; }

    /// <summary>
    /// Gets the sender identifier to retry when specified.
    /// </summary>
    public string? SenderId { get; }

    /// <summary>
    /// Gets the source recorded on retry attempts.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the actor recorded on retry attempts.
    /// </summary>
    public string? Actor { get; }

    /// <summary>
    /// Gets the correlation identifier retained for retry attempts.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets a value indicating whether retry attempts should record delivery metadata.
    /// </summary>
    public bool RecordDelivery { get; }

    /// <summary>
    /// Gets the retry entry status.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the number of dispatch attempts represented by this entry.
    /// </summary>
    public int AttemptCount { get; }

    /// <summary>
    /// Gets the maximum number of dispatch attempts allowed for this entry.
    /// </summary>
    public int MaxAttempts { get; }

    /// <summary>
    /// Gets the UTC timestamp when the retry entry was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp when the next attempt is due.
    /// </summary>
    public DateTimeOffset NextAttemptAtUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp of the latest dispatch attempt.
    /// </summary>
    public DateTimeOffset? LastAttemptAtUtc { get; }

    /// <summary>
    /// Gets the latest delivery dispatch outcome.
    /// </summary>
    public string? LastOutcome { get; }

    /// <summary>
    /// Gets the latest delivery dispatch reason.
    /// </summary>
    public string? LastReason { get; }

    /// <summary>
    /// Gets optional retry metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Creates a copy of this retry entry with updated retry state.
    /// </summary>
    /// <param name="status">The updated status.</param>
    /// <param name="attemptCount">The updated attempt count.</param>
    /// <param name="nextAttemptAtUtc">The updated next-attempt timestamp.</param>
    /// <param name="lastAttemptAtUtc">The updated last-attempt timestamp.</param>
    /// <param name="lastOutcome">The updated latest outcome.</param>
    /// <param name="lastReason">The updated latest reason.</param>
    /// <param name="metadata">The updated metadata.</param>
    /// <returns>The updated retry descriptor.</returns>
    public TenantInvitationDeliveryRetryDescriptor WithRetryState(
        string status,
        int attemptCount,
        DateTimeOffset nextAttemptAtUtc,
        DateTimeOffset? lastAttemptAtUtc,
        string? lastOutcome,
        string? lastReason,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new TenantInvitationDeliveryRetryDescriptor(
            RetryId,
            TenantId,
            InvitationId,
            Channel,
            SenderId,
            Source,
            Actor,
            CorrelationId,
            RecordDelivery,
            status,
            attemptCount,
            MaxAttempts,
            CreatedAtUtc,
            nextAttemptAtUtc,
            lastAttemptAtUtc,
            lastOutcome,
            lastReason,
            metadata ?? Metadata);
    }

    private static string NormalizeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return TenantInvitationDeliveryRetryStatuses.Pending;
        }

        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            TenantInvitationDeliveryRetryStatuses.Pending => TenantInvitationDeliveryRetryStatuses.Pending,
            TenantInvitationDeliveryRetryStatuses.Dispatched => TenantInvitationDeliveryRetryStatuses.Dispatched,
            TenantInvitationDeliveryRetryStatuses.Exhausted => TenantInvitationDeliveryRetryStatuses.Exhausted,
            TenantInvitationDeliveryRetryStatuses.Terminal => TenantInvitationDeliveryRetryStatuses.Terminal,
            _ => throw new ArgumentException($"Tenant invitation delivery retry status '{status}' is not supported.", nameof(status))
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
