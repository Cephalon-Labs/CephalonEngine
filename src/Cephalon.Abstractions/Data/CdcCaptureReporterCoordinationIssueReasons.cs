namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable degraded-reason identifiers used by CDC reporter-coordination answers.
/// </summary>
public static class CdcCaptureReporterCoordinationIssueReasons
{
    /// <summary>
    /// Reporter coordination is not currently degraded.
    /// </summary>
    public const string None = "none";

    /// <summary>
    /// The latest known reporter lease expired and the runtime is still awaiting takeover by a replacement reporter.
    /// </summary>
    public const string AwaitingTakeover = "awaiting-takeover";

    /// <summary>
    /// At least one conflicting reporter remains visible while another reporter still holds the active lease.
    /// </summary>
    public const string RejectedReporterConflict = "rejected-reporter-conflict";

    /// <summary>
    /// Multiple reporters currently appear to hold active leases for the same execution runtime.
    /// </summary>
    public const string MultipleActiveReporters = "multiple-active-reporters";
}
