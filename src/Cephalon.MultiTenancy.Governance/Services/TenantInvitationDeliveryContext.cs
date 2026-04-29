namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the tenant invitation payload passed to an invitation delivery sender.
/// </summary>
public sealed class TenantInvitationDeliveryContext
{
    /// <summary>
    /// Creates a tenant invitation delivery context.
    /// </summary>
    /// <param name="invitation">The invitation being delivered.</param>
    /// <param name="channel">The requested delivery channel.</param>
    /// <param name="requestedSenderId">The requested sender identifier when one was specified.</param>
    /// <param name="source">The source that requested delivery dispatch.</param>
    /// <param name="actor">The actor that requested delivery dispatch when known.</param>
    /// <param name="dispatchedAtUtc">The UTC timestamp used for dispatch.</param>
    /// <param name="correlationId">The optional correlation identifier for delivery dispatch.</param>
    /// <param name="metadata">Optional request metadata for the sender.</param>
    public TenantInvitationDeliveryContext(
        TenantInvitationDescriptor invitation,
        string channel,
        string? requestedSenderId,
        string? source,
        string? actor,
        DateTimeOffset dispatchedAtUtc,
        string? correlationId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(invitation);

        Invitation = invitation;
        TenantId = invitation.TenantId;
        InvitationId = invitation.InvitationId;
        InviteeId = invitation.InviteeId;
        InviteeKind = invitation.InviteeKind;
        DisplayName = invitation.DisplayName;
        Roles = invitation.Roles;
        Channel = string.IsNullOrWhiteSpace(channel) ? "default" : channel.Trim();
        RequestedSenderId = string.IsNullOrWhiteSpace(requestedSenderId) ? null : requestedSenderId.Trim();
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Actor = string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
        DispatchedAtUtc = dispatchedAtUtc;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the invitation being delivered.
    /// </summary>
    public TenantInvitationDescriptor Invitation { get; }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the invitation identifier.
    /// </summary>
    public string InvitationId { get; }

    /// <summary>
    /// Gets the invitee identifier.
    /// </summary>
    public string InviteeId { get; }

    /// <summary>
    /// Gets the invitee kind, such as user, group, service, or organization.
    /// </summary>
    public string InviteeKind { get; }

    /// <summary>
    /// Gets the optional operator-facing invitation name.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets the tenant-local roles proposed by the invitation.
    /// </summary>
    public IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Gets the requested delivery channel.
    /// </summary>
    public string Channel { get; }

    /// <summary>
    /// Gets the requested sender identifier when one was specified.
    /// </summary>
    public string? RequestedSenderId { get; }

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
    public DateTimeOffset DispatchedAtUtc { get; }

    /// <summary>
    /// Gets the optional correlation identifier for delivery dispatch.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets optional request metadata for the sender.
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
