namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable outcomes for tenant invitation delivery retry runner passes.
/// </summary>
public static class TenantInvitationDeliveryRetryOutcomes
{
    /// <summary>
    /// Invitation delivery retry queue processing is disabled.
    /// </summary>
    public const string Disabled = "disabled";

    /// <summary>
    /// No pending retry entries matched the request.
    /// </summary>
    public const string NoPendingRetries = "no-pending-retries";

    /// <summary>
    /// Another coordinated retry runner pass is already running in this host process.
    /// </summary>
    public const string AlreadyRunning = "already-running";

    /// <summary>
    /// Every attempted retry entry dispatched successfully.
    /// </summary>
    public const string Retried = "retried";

    /// <summary>
    /// Some attempted retry entries succeeded and some remained pending or terminal.
    /// </summary>
    public const string Partial = "partial";

    /// <summary>
    /// No attempted retry entries dispatched successfully.
    /// </summary>
    public const string Failed = "failed";
}
