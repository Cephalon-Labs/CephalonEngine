namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines built-in tenant-domain ownership verification workflow commands.
/// </summary>
public static class TenantDomainOwnershipVerificationWorkflowCommands
{
    /// <summary>
    /// Creates a pending tenant-domain ownership declaration.
    /// </summary>
    public const string Request = "request";

    /// <summary>
    /// Marks a pending tenant-domain ownership declaration as verified.
    /// </summary>
    public const string Verify = "verify";

    /// <summary>
    /// Rejects a pending tenant-domain ownership declaration.
    /// </summary>
    public const string Reject = "reject";

    /// <summary>
    /// Suspends a verified tenant-domain ownership declaration.
    /// </summary>
    public const string Suspend = "suspend";

    /// <summary>
    /// Expires a non-expired tenant-domain ownership declaration.
    /// </summary>
    public const string Expire = "expire";
}
