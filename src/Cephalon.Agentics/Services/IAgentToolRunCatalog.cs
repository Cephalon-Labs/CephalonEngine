namespace Cephalon.Agentics.Services;

/// <summary>
/// Exposes the latest reported runtime state for agent-tool runs.
/// </summary>
public interface IAgentToolRunCatalog
{
    /// <summary>
    /// Gets the currently known agent-tool run states ordered by tool identifier and run identifier.
    /// </summary>
    IReadOnlyList<AgentToolRunState> Runs { get; }

    /// <summary>
    /// Looks up one reported run-state entry by run identifier.
    /// </summary>
    /// <param name="runId">The stable run identifier to resolve.</param>
    /// <returns>The current run state when one has been reported; otherwise, <see langword="null" />.</returns>
    AgentToolRunState? GetByRunId(string runId);

    /// <summary>
    /// Gets all reported run-state entries for one tool.
    /// </summary>
    /// <param name="toolId">The stable tool identifier to resolve.</param>
    /// <returns>The run states reported for the tool.</returns>
    IReadOnlyList<AgentToolRunState> GetByToolId(string toolId);

    /// <summary>
    /// Attempts to look up one reported run-state entry by run identifier.
    /// </summary>
    /// <param name="runId">The stable run identifier to resolve.</param>
    /// <param name="state">The current run state when one has been reported.</param>
    /// <returns><see langword="true" /> when one run-state entry is available; otherwise, <see langword="false" />.</returns>
    bool TryGet(string runId, out AgentToolRunState? state);
}
