namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable reporter-coordination state identifiers used by CDC runtime-state and execution-runtime summaries.
/// </summary>
public static class CdcCaptureReporterCoordinationStates
{
    /// <summary>
    /// The runtime cannot currently determine the reporter-coordination posture.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// The execution runtime does not currently declare reporter-lease coordination semantics.
    /// </summary>
    public const string NotConfigured = "not-configured";

    /// <summary>
    /// The execution runtime has not reported any capture observations yet.
    /// </summary>
    public const string Unreported = "unreported";

    /// <summary>
    /// Exactly one reporter currently holds the active lease for the execution runtime.
    /// </summary>
    public const string Active = "active";

    /// <summary>
    /// The latest known reporter lease expired before a replacement reporter took over, so the runtime is awaiting takeover.
    /// </summary>
    public const string LeaseExpired = "lease-expired";

    /// <summary>
    /// Reporter coordination is currently degraded because conflicting or ambiguous reporters are visible.
    /// </summary>
    public const string Conflicted = "conflicted";
}
