namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable reporting-coverage state identifiers used by CDC execution-runtime summaries.
/// </summary>
public static class CdcCaptureExecutionRuntimeReportingCoverageStates
{
    /// <summary>
    /// The execution runtime cannot currently determine reporting coverage.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// The execution runtime does not currently own any declared CDC captures.
    /// </summary>
    public const string NotBound = "not-bound";

    /// <summary>
    /// The execution runtime owns declared CDC captures, but none of them have reported runtime state yet.
    /// </summary>
    public const string Unreported = "unreported";

    /// <summary>
    /// The execution runtime owns declared CDC captures and only part of that declared set has reported runtime state.
    /// </summary>
    public const string PartiallyReported = "partially-reported";

    /// <summary>
    /// Every declared CDC capture owned by the execution runtime has reported runtime state.
    /// </summary>
    public const string FullyReported = "fully-reported";
}
