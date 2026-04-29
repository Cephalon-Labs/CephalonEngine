namespace Cephalon.MultiTenancy.Governance.HttpDelivery.Services;

/// <summary>
/// Describes the JSON payload sent to an HTTP invitation delivery webhook.
/// </summary>
public sealed class HttpInvitationDeliveryPayload
{
    /// <summary>
    /// Creates an empty HTTP invitation delivery payload for JSON serialization.
    /// </summary>
    public HttpInvitationDeliveryPayload()
    {
    }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the invitation identifier.
    /// </summary>
    public string InvitationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the invitee identifier.
    /// </summary>
    public string InviteeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the invitee kind.
    /// </summary>
    public string InviteeKind { get; set; } = "user";

    /// <summary>
    /// Gets or sets the optional invitation display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the requested delivery channel.
    /// </summary>
    public string Channel { get; set; } = "default";

    /// <summary>
    /// Gets or sets the requested sender identifier.
    /// </summary>
    public string? RequestedSenderId { get; set; }

    /// <summary>
    /// Gets or sets the dispatch source.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the actor that requested dispatch.
    /// </summary>
    public string? Actor { get; set; }

    /// <summary>
    /// Gets or sets the optional correlation identifier.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp used for dispatch.
    /// </summary>
    public DateTimeOffset DispatchedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the tenant-local roles proposed by the invitation.
    /// </summary>
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets optional request metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets optional metadata attached to the invitation.
    /// </summary>
    public IReadOnlyDictionary<string, string> InvitationMetadata { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
