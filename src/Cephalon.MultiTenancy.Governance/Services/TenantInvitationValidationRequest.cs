namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes one request to validate a tenant invitation.
/// </summary>
public sealed class TenantInvitationValidationRequest
{
    /// <summary>
    /// Creates a tenant-invitation validation request.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to validate.</param>
    /// <param name="invitationId">The invitation identifier to validate.</param>
    /// <param name="inviteeId">The optional invitee identifier expected by the caller.</param>
    /// <param name="inviteeKind">The optional invitee kind expected by the caller.</param>
    /// <param name="requiredRoles">The optional tenant-local roles required for validation.</param>
    /// <param name="atUtc">The UTC timestamp used for expiration evaluation. The runtime clock is used when omitted.</param>
    /// <param name="correlationId">The optional correlation identifier for the validation.</param>
    /// <param name="metadata">Optional request metadata.</param>
    public TenantInvitationValidationRequest(
        string tenantId,
        string invitationId,
        string? inviteeId = null,
        string? inviteeKind = null,
        IReadOnlyList<string>? requiredRoles = null,
        DateTimeOffset? atUtc = null,
        string? correlationId = null,
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
        InviteeId = string.IsNullOrWhiteSpace(inviteeId) ? null : inviteeId.Trim();
        InviteeKind = string.IsNullOrWhiteSpace(inviteeKind) ? "user" : inviteeKind.Trim();
        RequiredRoles = NormalizeValues(requiredRoles);
        AtUtc = atUtc;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant identifier to validate.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the invitation identifier to validate.
    /// </summary>
    public string InvitationId { get; }

    /// <summary>
    /// Gets the optional invitee identifier expected by the caller.
    /// </summary>
    public string? InviteeId { get; }

    /// <summary>
    /// Gets the invitee kind expected by the caller.
    /// </summary>
    public string InviteeKind { get; }

    /// <summary>
    /// Gets the tenant-local roles required for validation.
    /// </summary>
    public IReadOnlyList<string> RequiredRoles { get; }

    /// <summary>
    /// Gets the UTC timestamp used for expiration evaluation.
    /// </summary>
    public DateTimeOffset? AtUtc { get; }

    /// <summary>
    /// Gets the optional correlation identifier for the validation.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets optional request metadata.
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
