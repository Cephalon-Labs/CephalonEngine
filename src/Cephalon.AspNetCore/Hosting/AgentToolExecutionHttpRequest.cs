namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Represents the operator HTTP request body used to execute an agent tool.
/// </summary>
public sealed class AgentToolExecutionHttpRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentToolExecutionHttpRequest"/> class.
    /// </summary>
    public AgentToolExecutionHttpRequest()
    {
    }

    /// <summary>
    /// Gets or initializes the caller-supplied run identifier.
    /// </summary>
    public string? RunId { get; init; }

    /// <summary>
    /// Gets or initializes the tool arguments.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Arguments { get; init; }

    /// <summary>
    /// Gets or initializes the actor identifier responsible for the run.
    /// </summary>
    public string? ActorId { get; init; }

    /// <summary>
    /// Gets or initializes the correlation identifier for the run.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets or initializes the execution attempt number.
    /// </summary>
    public int? Attempt { get; init; }

    /// <summary>
    /// Gets or initializes metadata to attach to the run.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
