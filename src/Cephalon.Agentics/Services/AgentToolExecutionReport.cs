namespace Cephalon.Agentics.Services;

/// <summary>
/// Describes one runtime observation for an agent-tool run.
/// </summary>
public sealed class AgentToolExecutionReport
{
    /// <summary>
    /// Creates a new runtime observation for an agent-tool run.
    /// </summary>
    /// <param name="toolId">The stable tool identifier.</param>
    /// <param name="runId">The stable run identifier.</param>
    /// <param name="outcome">The stable outcome identifier.</param>
    /// <param name="observedAtUtc">The UTC timestamp when the observation occurred.</param>
    /// <param name="actorId">The optional actor identifier responsible for the run.</param>
    /// <param name="correlationId">The optional correlation identifier associated with the run.</param>
    /// <param name="attempt">The execution attempt number.</param>
    /// <param name="outputSummary">The optional operator-facing output summary.</param>
    /// <param name="error">The optional operator-facing error summary.</param>
    /// <param name="metadata">Optional operator-facing metadata captured alongside the observation.</param>
    public AgentToolExecutionReport(
        string toolId,
        string runId,
        string outcome,
        DateTimeOffset observedAtUtc,
        string? actorId = null,
        string? correlationId = null,
        int attempt = 1,
        string? outputSummary = null,
        string? error = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(toolId))
        {
            throw new ArgumentException("Tool id is required.", nameof(toolId));
        }

        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("Run id is required.", nameof(runId));
        }

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        if (attempt < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt), attempt, "Attempt must be greater than or equal to 1.");
        }

        ToolId = toolId.Trim();
        RunId = runId.Trim();
        Outcome = outcome.Trim();
        ObservedAtUtc = observedAtUtc;
        ActorId = string.IsNullOrWhiteSpace(actorId) ? null : actorId.Trim();
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        Attempt = attempt;
        OutputSummary = string.IsNullOrWhiteSpace(outputSummary) ? null : outputSummary.Trim();
        Error = string.IsNullOrWhiteSpace(error) ? null : error.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the stable tool identifier.
    /// </summary>
    public string ToolId { get; }

    /// <summary>
    /// Gets the stable run identifier.
    /// </summary>
    public string RunId { get; }

    /// <summary>
    /// Gets the stable outcome identifier.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets the UTC timestamp when the observation occurred.
    /// </summary>
    public DateTimeOffset ObservedAtUtc { get; }

    /// <summary>
    /// Gets the optional actor identifier responsible for the run.
    /// </summary>
    public string? ActorId { get; }

    /// <summary>
    /// Gets the optional correlation identifier associated with the run.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets the execution attempt number.
    /// </summary>
    public int Attempt { get; }

    /// <summary>
    /// Gets the optional operator-facing output summary.
    /// </summary>
    public string? OutputSummary { get; }

    /// <summary>
    /// Gets the optional operator-facing error summary.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets optional operator-facing metadata captured alongside the observation.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

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
