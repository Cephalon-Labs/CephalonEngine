namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable outcomes returned by tenant-invitation validation.
/// </summary>
public static class TenantInvitationValidationOutcomes
{
    /// <summary>
    /// The invitation is pending and satisfies the validation request.
    /// </summary>
    public const string Valid = "valid";

    /// <summary>
    /// No invitation matched the supplied tenant and invitation identifiers.
    /// </summary>
    public const string NotFound = "not-found";

    /// <summary>
    /// The invitation has already been accepted.
    /// </summary>
    public const string Accepted = "accepted";

    /// <summary>
    /// The invitation has been revoked.
    /// </summary>
    public const string Revoked = "revoked";

    /// <summary>
    /// The invitation is expired or outside its valid time window.
    /// </summary>
    public const string Expired = "expired";

    /// <summary>
    /// The invitation exists but does not match the requested invitee boundary.
    /// </summary>
    public const string InviteeMismatch = "invitee-mismatch";

    /// <summary>
    /// The invitation does not include every required tenant-local role.
    /// </summary>
    public const string MissingRole = "missing-role";

    /// <summary>
    /// Invitation validation is disabled by host configuration.
    /// </summary>
    public const string Disabled = "disabled";
}
