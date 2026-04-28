namespace Cephalon.Agentics.Services;

/// <summary>
/// Records runtime observations for agent-tool runs.
/// </summary>
public interface IAgentToolRunReporter
{
    /// <summary>
    /// Records one runtime observation for an agent-tool run.
    /// </summary>
    /// <param name="report">The runtime observation to record.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes when the observation has been recorded.</returns>
    ValueTask ReportAsync(
        AgentToolExecutionReport report,
        CancellationToken cancellationToken = default);
}
