namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

/// <summary>
/// Describes a normalized ASP.NET Core tenant-invitation delivery status callback request.
/// </summary>
/// <remarks>
/// Provider-specific webhook payloads should be translated into this provider-neutral shape by the host or a future
/// provider companion before the request is reconciled by Cephalon governance.
/// </remarks>
public sealed class TenantInvitationDeliveryStatusCallbackRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantInvitationDeliveryStatusCallbackRequest" /> class.
    /// </summary>
    public TenantInvitationDeliveryStatusCallbackRequest()
    {
    }

    /// <summary>
    /// Gets or sets the tenant identifier that owns the invitation.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the invitation identifier to reconcile.
    /// </summary>
    public string? InvitationId { get; set; }

    /// <summary>
    /// Gets or sets the provider or receiver delivery status.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the provider message identifier associated with the status observation.
    /// </summary>
    public string? ProviderMessageId { get; set; }

    /// <summary>
    /// Gets or sets the delivery sender identifier associated with the status observation.
    /// </summary>
    public string? SenderId { get; set; }

    /// <summary>
    /// Gets or sets the delivery channel associated with the status observation.
    /// </summary>
    public string? Channel { get; set; }

    /// <summary>
    /// Gets or sets the provider or receiver status reason.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the status was observed. The runtime clock is used when omitted.
    /// </summary>
    public DateTimeOffset? ObservedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the source that reported the status observation.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the actor that reported the status observation when known.
    /// </summary>
    public string? Actor { get; set; }

    /// <summary>
    /// Gets or sets the optional correlation identifier for the status observation.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether reconciled status metadata should be recorded on the invitation.
    /// </summary>
    public bool RecordStatus { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether an existing dispatch provider message identifier must match the request.
    /// </summary>
    /// <remarks>
    /// Hosts can also enforce matching through
    /// <see cref="Cephalon.MultiTenancy.Governance.AspNetCore.Configuration.MultiTenancyGovernanceAspNetCoreOptions.RequireTenantInvitationDeliveryStatusCallbackProviderMessageMatch" />.
    /// </remarks>
    public bool RequireProviderMessageMatch { get; set; } = true;

    /// <summary>
    /// Gets or sets optional delivery status metadata.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; set; }
}
