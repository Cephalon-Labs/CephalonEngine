namespace Cephalon.Agentics.Services;

/// <summary>
/// Defines stable decision identifiers returned by agent-tool execution policies.
/// </summary>
public static class AgentToolExecutionDecisionKinds
{
    /// <summary>
    /// Gets the decision identifier used when execution can continue.
    /// </summary>
    public const string Allow = "allow";

    /// <summary>
    /// Gets the decision identifier used when execution must wait for explicit approval.
    /// </summary>
    public const string ApprovalRequired = "approval-required";

    /// <summary>
    /// Gets the decision identifier used when execution is denied by policy.
    /// </summary>
    public const string Deny = "deny";
}
