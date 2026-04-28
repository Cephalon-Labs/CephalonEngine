namespace Cephalon.Agentics.Services;

/// <summary>
/// Describes a policy decision for one agent-tool execution request.
/// </summary>
public sealed class AgentToolExecutionDecision
{
    /// <summary>
    /// Creates a new agent-tool execution decision.
    /// </summary>
    /// <param name="kind">The stable decision identifier.</param>
    /// <param name="reason">The operator-facing reason associated with the decision.</param>
    /// <param name="metadata">Optional metadata captured with the decision.</param>
    public AgentToolExecutionDecision(
        string kind,
        string? reason = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            throw new ArgumentException("Decision kind is required.", nameof(kind));
        }

        Kind = NormalizeDecisionKind(kind);
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the stable decision identifier.
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// Gets the operator-facing reason associated with the decision.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Gets optional metadata captured with the decision.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Creates an allow decision.
    /// </summary>
    /// <param name="reason">The optional operator-facing reason for the decision.</param>
    /// <param name="metadata">Optional metadata captured with the decision.</param>
    /// <returns>An allow decision.</returns>
    public static AgentToolExecutionDecision Allow(
        string? reason = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new AgentToolExecutionDecision(AgentToolExecutionDecisionKinds.Allow, reason, metadata);
    }

    /// <summary>
    /// Creates an approval-required decision.
    /// </summary>
    /// <param name="reason">The optional operator-facing reason for the decision.</param>
    /// <param name="metadata">Optional metadata captured with the decision.</param>
    /// <returns>An approval-required decision.</returns>
    public static AgentToolExecutionDecision RequireApproval(
        string? reason = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new AgentToolExecutionDecision(AgentToolExecutionDecisionKinds.ApprovalRequired, reason, metadata);
    }

    /// <summary>
    /// Creates a deny decision.
    /// </summary>
    /// <param name="reason">The optional operator-facing reason for the decision.</param>
    /// <param name="metadata">Optional metadata captured with the decision.</param>
    /// <returns>A deny decision.</returns>
    public static AgentToolExecutionDecision Deny(
        string? reason = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new AgentToolExecutionDecision(AgentToolExecutionDecisionKinds.Deny, reason, metadata);
    }

    private static string NormalizeDecisionKind(string kind)
    {
        var normalized = kind.Trim().ToLowerInvariant();
        return normalized switch
        {
            AgentToolExecutionDecisionKinds.Allow => AgentToolExecutionDecisionKinds.Allow,
            AgentToolExecutionDecisionKinds.ApprovalRequired => AgentToolExecutionDecisionKinds.ApprovalRequired,
            AgentToolExecutionDecisionKinds.Deny => AgentToolExecutionDecisionKinds.Deny,
            _ => throw new ArgumentException($"Agent-tool execution decision '{kind}' is not supported.", nameof(kind))
        };
    }

    private static Dictionary<string, string> CopyMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return metadata
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }
}
