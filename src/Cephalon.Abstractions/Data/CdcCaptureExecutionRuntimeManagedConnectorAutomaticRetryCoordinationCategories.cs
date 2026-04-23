namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector automatic background retry coordination answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationCategories
{
    /// <summary>
    /// The runtime can evaluate automatic retry on a single node without reporter-lease coordination.
    /// </summary>
    public const string SingleNodeRuntime = "single-node-runtime";

    /// <summary>
    /// The runtime depends on reporter-lease coordination before automatic retry should execute.
    /// </summary>
    public const string LeaseCoordinatedRuntime = "lease-coordinated-runtime";

    /// <summary>
    /// The current host declared a local coordination owner identifier for automatic retry.
    /// </summary>
    public const string CoordinationOwnerConfigured = "coordination-owner-configured";

    /// <summary>
    /// The current host did not declare a local coordination owner identifier for automatic retry.
    /// </summary>
    public const string CoordinationOwnerMissing = "coordination-owner-missing";

    /// <summary>
    /// The runtime currently exposes one active reporter owner.
    /// </summary>
    public const string ActiveReporterVisible = "active-reporter-visible";

    /// <summary>
    /// The current host coordination owner matches the active reporter lease.
    /// </summary>
    public const string OwnerMatch = "owner-match";

    /// <summary>
    /// The current host coordination owner does not match the active reporter lease.
    /// </summary>
    public const string OwnerMismatch = "owner-mismatch";

    /// <summary>
    /// The current host coordination owner holds the active reporter lease.
    /// </summary>
    public const string ActiveLeaseHeld = "active-lease-held";

    /// <summary>
    /// No active reporter lease is currently visible for automatic retry.
    /// </summary>
    public const string LeaseMissing = "lease-missing";

    /// <summary>
    /// Reporter coordination currently remains conflicted.
    /// </summary>
    public const string ReporterConflict = "reporter-conflict";

    /// <summary>
    /// Automatic retry still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.OperatorOnly;
}
