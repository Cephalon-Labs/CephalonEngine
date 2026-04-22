namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable takeover-state identifiers used by CDC reporter-coordination answers.
/// </summary>
public static class CdcCaptureReporterTakeoverStates
{
    /// <summary>
    /// Reporter takeover does not currently apply to the coordination answer.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Reporter takeover is not currently required because the execution runtime still has a single active reporter owner.
    /// </summary>
    public const string NotRequired = "not-required";

    /// <summary>
    /// The latest known reporter lease expired and the execution runtime is awaiting takeover by a replacement reporter.
    /// </summary>
    public const string AwaitingTakeover = "awaiting-takeover";

    /// <summary>
    /// A replacement reporter already took over after the previous lease expired.
    /// </summary>
    public const string Completed = "completed";
}
