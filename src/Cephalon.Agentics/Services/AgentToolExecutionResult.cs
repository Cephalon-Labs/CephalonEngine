using Cephalon.Abstractions.Agentics;

namespace Cephalon.Agentics.Services;

/// <summary>
/// Describes the result returned by one managed agent-tool executor.
/// </summary>
public sealed class AgentToolExecutionResult
{
    /// <summary>
    /// Creates a new agent-tool execution result.
    /// </summary>
    /// <param name="outcome">The stable execution outcome identifier.</param>
    /// <param name="outputSummary">The optional operator-facing output summary.</param>
    /// <param name="error">The optional operator-facing error summary.</param>
    /// <param name="metadata">Optional metadata captured by the executor.</param>
    public AgentToolExecutionResult(
        string outcome,
        string? outputSummary = null,
        string? error = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        Outcome = NormalizeOutcome(outcome);
        OutputSummary = string.IsNullOrWhiteSpace(outputSummary) ? null : outputSummary.Trim();
        Error = string.IsNullOrWhiteSpace(error) ? null : error.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the stable execution outcome identifier.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets the optional operator-facing output summary.
    /// </summary>
    public string? OutputSummary { get; }

    /// <summary>
    /// Gets the optional operator-facing error summary.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets optional metadata captured by the executor.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Creates a successful execution result.
    /// </summary>
    /// <param name="outputSummary">The optional operator-facing output summary.</param>
    /// <param name="metadata">Optional metadata captured by the executor.</param>
    /// <returns>A successful execution result.</returns>
    public static AgentToolExecutionResult Succeeded(
        string? outputSummary = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new AgentToolExecutionResult(AgentToolExecutionOutcomes.Succeeded, outputSummary, metadata: metadata);
    }

    /// <summary>
    /// Creates a failed execution result.
    /// </summary>
    /// <param name="error">The operator-facing error summary.</param>
    /// <param name="metadata">Optional metadata captured by the executor.</param>
    /// <returns>A failed execution result.</returns>
    public static AgentToolExecutionResult Failed(
        string error,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new AgentToolExecutionResult(AgentToolExecutionOutcomes.Failed, error: error, metadata: metadata);
    }

    /// <summary>
    /// Creates a skipped execution result.
    /// </summary>
    /// <param name="outputSummary">The optional operator-facing output summary.</param>
    /// <param name="metadata">Optional metadata captured by the executor.</param>
    /// <returns>A skipped execution result.</returns>
    public static AgentToolExecutionResult Skipped(
        string? outputSummary = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new AgentToolExecutionResult(AgentToolExecutionOutcomes.Skipped, outputSummary, metadata: metadata);
    }

    /// <summary>
    /// Creates an approval-required execution result.
    /// </summary>
    /// <param name="outputSummary">The optional operator-facing output summary.</param>
    /// <param name="metadata">Optional metadata captured by the policy layer.</param>
    /// <returns>An approval-required execution result.</returns>
    public static AgentToolExecutionResult ApprovalRequired(
        string? outputSummary = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new AgentToolExecutionResult(AgentToolExecutionOutcomes.ApprovalRequired, outputSummary, metadata: metadata);
    }

    /// <summary>
    /// Creates a denied execution result.
    /// </summary>
    /// <param name="error">The operator-facing denial reason.</param>
    /// <param name="metadata">Optional metadata captured by the policy layer.</param>
    /// <returns>A denied execution result.</returns>
    public static AgentToolExecutionResult Denied(
        string? error = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new AgentToolExecutionResult(AgentToolExecutionOutcomes.Denied, error: error, metadata: metadata);
    }

    private static string NormalizeOutcome(string outcome)
    {
        var normalized = outcome.Trim().ToLowerInvariant();
        return normalized switch
        {
            AgentToolExecutionOutcomes.Started => AgentToolExecutionOutcomes.Started,
            AgentToolExecutionOutcomes.Succeeded => AgentToolExecutionOutcomes.Succeeded,
            AgentToolExecutionOutcomes.Failed => AgentToolExecutionOutcomes.Failed,
            AgentToolExecutionOutcomes.Skipped => AgentToolExecutionOutcomes.Skipped,
            AgentToolExecutionOutcomes.ApprovalRequired => AgentToolExecutionOutcomes.ApprovalRequired,
            AgentToolExecutionOutcomes.Denied => AgentToolExecutionOutcomes.Denied,
            _ => throw new ArgumentException($"Agent-tool execution outcome '{outcome}' is not supported.", nameof(outcome))
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
