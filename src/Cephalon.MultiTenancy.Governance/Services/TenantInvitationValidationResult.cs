namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the result of one tenant-invitation validation.
/// </summary>
public sealed class TenantInvitationValidationResult
{
    /// <summary>
    /// Creates a tenant-invitation validation result.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that was validated.</param>
    /// <param name="invitationId">The invitation identifier that was validated.</param>
    /// <param name="outcome">The stable validation outcome.</param>
    /// <param name="valid">A value indicating whether validation granted invitation use.</param>
    /// <param name="validatedAtUtc">The UTC timestamp when validation executed.</param>
    /// <param name="requiredRoles">The tenant-local roles required by the request.</param>
    /// <param name="matchedRoles">The tenant-local roles found on the invitation.</param>
    /// <param name="missingRoles">The required roles that were not found.</param>
    /// <param name="matchedInvitation">The matching invitation considered by validation.</param>
    /// <param name="reason">The optional operator-facing validation reason.</param>
    /// <param name="metadata">Optional result metadata.</param>
    /// <param name="inviteeId">The optional invitee identifier expected by the request.</param>
    /// <param name="inviteeKind">The invitee kind expected by the request.</param>
    public TenantInvitationValidationResult(
        string tenantId,
        string invitationId,
        string outcome,
        bool valid,
        DateTimeOffset validatedAtUtc,
        IReadOnlyList<string>? requiredRoles = null,
        IReadOnlyList<string>? matchedRoles = null,
        IReadOnlyList<string>? missingRoles = null,
        TenantInvitationDescriptor? matchedInvitation = null,
        string? reason = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        string? inviteeId = null,
        string? inviteeKind = null)
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
        Valid = valid;
        ValidatedAtUtc = validatedAtUtc;
        RequiredRoles = NormalizeValues(requiredRoles);
        MatchedRoles = NormalizeValues(matchedRoles);
        MissingRoles = NormalizeValues(missingRoles);
        MatchedInvitation = matchedInvitation;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Metadata = CopyMetadata(metadata);
        InviteeId = string.IsNullOrWhiteSpace(inviteeId) ? null : inviteeId.Trim();
        InviteeKind = string.IsNullOrWhiteSpace(inviteeKind) ? "user" : inviteeKind.Trim();
    }

    /// <summary>
    /// Gets the tenant identifier that was validated.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the invitation identifier that was validated.
    /// </summary>
    public string InvitationId { get; }

    /// <summary>
    /// Gets the optional invitee identifier expected by the request.
    /// </summary>
    public string? InviteeId { get; }

    /// <summary>
    /// Gets the invitee kind expected by the request.
    /// </summary>
    public string InviteeKind { get; }

    /// <summary>
    /// Gets the stable validation outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether validation granted invitation use.
    /// </summary>
    public bool Valid { get; }

    /// <summary>
    /// Gets the UTC timestamp when validation executed.
    /// </summary>
    public DateTimeOffset ValidatedAtUtc { get; }

    /// <summary>
    /// Gets the tenant-local roles required by the request.
    /// </summary>
    public IReadOnlyList<string> RequiredRoles { get; }

    /// <summary>
    /// Gets the tenant-local roles found on the invitation.
    /// </summary>
    public IReadOnlyList<string> MatchedRoles { get; }

    /// <summary>
    /// Gets the required roles that were not found.
    /// </summary>
    public IReadOnlyList<string> MissingRoles { get; }

    /// <summary>
    /// Gets the matching invitation considered by validation.
    /// </summary>
    public TenantInvitationDescriptor? MatchedInvitation { get; }

    /// <summary>
    /// Gets the optional operator-facing validation reason.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Gets optional result metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string NormalizeOutcome(string outcome)
    {
        var normalized = outcome.Trim().ToLowerInvariant();
        return normalized switch
        {
            TenantInvitationValidationOutcomes.Valid => TenantInvitationValidationOutcomes.Valid,
            TenantInvitationValidationOutcomes.NotFound => TenantInvitationValidationOutcomes.NotFound,
            TenantInvitationValidationOutcomes.Accepted => TenantInvitationValidationOutcomes.Accepted,
            TenantInvitationValidationOutcomes.Revoked => TenantInvitationValidationOutcomes.Revoked,
            TenantInvitationValidationOutcomes.Expired => TenantInvitationValidationOutcomes.Expired,
            TenantInvitationValidationOutcomes.InviteeMismatch => TenantInvitationValidationOutcomes.InviteeMismatch,
            TenantInvitationValidationOutcomes.MissingRole => TenantInvitationValidationOutcomes.MissingRole,
            TenantInvitationValidationOutcomes.Disabled => TenantInvitationValidationOutcomes.Disabled,
            _ => throw new ArgumentException($"Tenant-invitation validation outcome '{outcome}' is not supported.", nameof(outcome))
        };
    }

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
