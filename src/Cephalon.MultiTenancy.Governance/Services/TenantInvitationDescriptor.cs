namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes one invitation to join or access a tenant.
/// </summary>
public sealed class TenantInvitationDescriptor
{
    /// <summary>
    /// Creates a new tenant-invitation descriptor.
    /// </summary>
    /// <param name="invitationId">The stable invitation identifier within the tenant.</param>
    /// <param name="tenantId">The stable tenant identifier.</param>
    /// <param name="inviteeId">The stable invitee identifier.</param>
    /// <param name="inviteeKind">The invitee kind, such as user, group, service, or organization.</param>
    /// <param name="displayName">The optional operator-facing invitation name.</param>
    /// <param name="roles">The tenant-local roles proposed by the invitation.</param>
    /// <param name="status">The invitation status.</param>
    /// <param name="createdAtUtc">The UTC timestamp when the invitation was created.</param>
    /// <param name="expiresAtUtc">The UTC timestamp when the invitation expires.</param>
    /// <param name="sourceModuleId">The module that contributed the invitation when one is known.</param>
    /// <param name="metadata">Optional operator-facing metadata attached to the invitation.</param>
    public TenantInvitationDescriptor(
        string invitationId,
        string tenantId,
        string inviteeId,
        string? inviteeKind = null,
        string? displayName = null,
        IReadOnlyList<string>? roles = null,
        string status = TenantInvitationStatuses.Pending,
        DateTimeOffset? createdAtUtc = null,
        DateTimeOffset? expiresAtUtc = null,
        string? sourceModuleId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(invitationId))
        {
            throw new ArgumentException("Invitation id is required.", nameof(invitationId));
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(inviteeId))
        {
            throw new ArgumentException("Invitee id is required.", nameof(inviteeId));
        }

        InvitationId = invitationId.Trim();
        TenantId = tenantId.Trim();
        InviteeId = inviteeId.Trim();
        InviteeKind = string.IsNullOrWhiteSpace(inviteeKind) ? "user" : inviteeKind.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        Roles = NormalizeValues(roles);
        Status = NormalizeStatus(status);
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        SourceModuleId = string.IsNullOrWhiteSpace(sourceModuleId) ? null : sourceModuleId.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the stable invitation identifier within the tenant.
    /// </summary>
    public string InvitationId { get; }

    /// <summary>
    /// Gets the stable tenant identifier.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the stable invitee identifier.
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
    /// Gets the invitation status.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the UTC timestamp when the invitation was created.
    /// </summary>
    public DateTimeOffset? CreatedAtUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp when the invitation expires.
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; }

    /// <summary>
    /// Gets the module that contributed the invitation when one is known.
    /// </summary>
    public string? SourceModuleId { get; }

    /// <summary>
    /// Gets optional operator-facing metadata attached to the invitation.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string[] NormalizeValues(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static string NormalizeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return TenantInvitationStatuses.Pending;
        }

        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            TenantInvitationStatuses.Pending => TenantInvitationStatuses.Pending,
            TenantInvitationStatuses.Accepted => TenantInvitationStatuses.Accepted,
            TenantInvitationStatuses.Revoked => TenantInvitationStatuses.Revoked,
            TenantInvitationStatuses.Expired => TenantInvitationStatuses.Expired,
            _ => throw new ArgumentException($"Tenant-invitation status '{status}' is not supported.", nameof(status))
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
