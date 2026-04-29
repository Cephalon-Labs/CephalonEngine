namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes a tenant invitation delivery dispatch request.
/// </summary>
public sealed class TenantInvitationDeliveryRequest
{
    /// <summary>
    /// Creates a tenant invitation delivery dispatch request.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that owns the invitation.</param>
    /// <param name="invitationId">The invitation identifier to deliver.</param>
    /// <param name="channel">The requested delivery channel.</param>
    /// <param name="senderId">The preferred delivery sender identifier.</param>
    /// <param name="source">The source that requested delivery dispatch.</param>
    /// <param name="actor">The actor that requested delivery dispatch when known.</param>
    /// <param name="atUtc">The UTC timestamp used for dispatch. The runtime clock is used when omitted.</param>
    /// <param name="correlationId">The optional correlation identifier for delivery dispatch.</param>
    /// <param name="recordDelivery">A value indicating whether delivery outcome metadata should be recorded on the invitation.</param>
    /// <param name="metadata">Optional delivery dispatch metadata.</param>
    public TenantInvitationDeliveryRequest(
        string tenantId,
        string invitationId,
        string? channel = null,
        string? senderId = null,
        string? source = null,
        string? actor = null,
        DateTimeOffset? atUtc = null,
        string? correlationId = null,
        bool recordDelivery = true,
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

        TenantId = tenantId.Trim();
        InvitationId = invitationId.Trim();
        Channel = string.IsNullOrWhiteSpace(channel) ? "default" : channel.Trim();
        SenderId = string.IsNullOrWhiteSpace(senderId) ? null : senderId.Trim();
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Actor = string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
        AtUtc = atUtc;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        RecordDelivery = recordDelivery;
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant identifier that owns the invitation.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the invitation identifier to deliver.
    /// </summary>
    public string InvitationId { get; }

    /// <summary>
    /// Gets the requested delivery channel.
    /// </summary>
    public string Channel { get; }

    /// <summary>
    /// Gets the preferred delivery sender identifier.
    /// </summary>
    public string? SenderId { get; }

    /// <summary>
    /// Gets the source that requested delivery dispatch.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the actor that requested delivery dispatch when known.
    /// </summary>
    public string? Actor { get; }

    /// <summary>
    /// Gets the UTC timestamp used for dispatch.
    /// </summary>
    public DateTimeOffset? AtUtc { get; }

    /// <summary>
    /// Gets the optional correlation identifier for delivery dispatch.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets a value indicating whether delivery outcome metadata should be recorded on the invitation.
    /// </summary>
    public bool RecordDelivery { get; }

    /// <summary>
    /// Gets optional delivery dispatch metadata.
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
