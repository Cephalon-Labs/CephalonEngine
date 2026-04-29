namespace Cephalon.Abstractions.Agentics;

/// <summary>
/// Defines stable outcome identifiers for agent-tool execution observations.
/// </summary>
public static class AgentToolExecutionOutcomes
{
    /// <summary>
    /// Gets the outcome identifier used when a tool run begins.
    /// </summary>
    public const string Started = "started";

    /// <summary>
    /// Gets the outcome identifier used when a tool run completes successfully.
    /// </summary>
    public const string Succeeded = "succeeded";

    /// <summary>
    /// Gets the outcome identifier used when a tool run fails.
    /// </summary>
    public const string Failed = "failed";

    /// <summary>
    /// Gets the outcome identifier used when a tool run is intentionally skipped.
    /// </summary>
    public const string Skipped = "skipped";

    /// <summary>
    /// Gets the outcome identifier used when a tool run needs an approval step before execution.
    /// </summary>
    public const string ApprovalRequired = "approval-required";

    /// <summary>
    /// Gets the outcome identifier used when a policy denies a tool run.
    /// </summary>
    public const string Denied = "denied";
}
