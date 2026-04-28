namespace Cephalon.Agentics.Services;

/// <summary>
/// Describes the host-agnostic execution context delivered to one managed agent-tool executor.
/// </summary>
public sealed class AgentToolExecutionContext
{
    /// <summary>
    /// Creates a new agent-tool execution context.
    /// </summary>
    /// <param name="tool">The resolved tool descriptor being executed.</param>
    /// <param name="runId">The stable run identifier for this execution.</param>
    /// <param name="arguments">Optional string arguments supplied to the tool executor.</param>
    /// <param name="actorId">The optional actor identifier responsible for the request.</param>
    /// <param name="correlationId">The optional correlation identifier for the request.</param>
    /// <param name="attempt">The execution attempt number.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the request.</param>
    public AgentToolExecutionContext(
        AgentToolDescriptor tool,
        string runId,
        IReadOnlyDictionary<string, string>? arguments = null,
        string? actorId = null,
        string? correlationId = null,
        int attempt = 1,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Tool = tool ?? throw new ArgumentNullException(nameof(tool));
        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("Run id is required.", nameof(runId));
        }

        if (attempt < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt), attempt, "Attempt must be greater than or equal to 1.");
        }

        RunId = runId.Trim();
        Arguments = CopyValues(arguments);
        ActorId = string.IsNullOrWhiteSpace(actorId) ? null : actorId.Trim();
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        Attempt = attempt;
        Metadata = CopyValues(metadata);
    }

    /// <summary>
    /// Gets the resolved tool descriptor being executed.
    /// </summary>
    public AgentToolDescriptor Tool { get; }

    /// <summary>
    /// Gets the stable run identifier for this execution.
    /// </summary>
    public string RunId { get; }

    /// <summary>
    /// Gets optional string arguments supplied to the tool executor.
    /// </summary>
    public IReadOnlyDictionary<string, string> Arguments { get; }

    /// <summary>
    /// Gets the optional actor identifier responsible for the request.
    /// </summary>
    public string? ActorId { get; }

    /// <summary>
    /// Gets the optional correlation identifier for the request.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets the execution attempt number.
    /// </summary>
    public int Attempt { get; }

    /// <summary>
    /// Gets optional operator-facing metadata associated with the request.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static Dictionary<string, string> CopyValues(IReadOnlyDictionary<string, string>? values)
    {
        if (values is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return values
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }
}
