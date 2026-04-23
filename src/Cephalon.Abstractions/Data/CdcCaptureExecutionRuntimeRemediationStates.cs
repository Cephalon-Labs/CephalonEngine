namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable remediation-state identifiers used by CDC execution-runtime summaries.
/// </summary>
public static class CdcCaptureExecutionRuntimeRemediationStates
{
    /// <summary>
    /// The execution runtime cannot currently determine whether remediation is required.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// The execution runtime does not currently require operator remediation.
    /// </summary>
    public const string Ready = "ready";

    /// <summary>
    /// The execution runtime currently requires operator attention, but it is not blocked by a failed capture.
    /// </summary>
    public const string Attention = "attention";

    /// <summary>
    /// The execution runtime currently requires remediation for one or more failed CDC captures.
    /// </summary>
    public const string Blocked = "blocked";
}
