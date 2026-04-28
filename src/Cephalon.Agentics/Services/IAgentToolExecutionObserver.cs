namespace Cephalon.Agentics.Services;

/// <summary>
/// Observes agent-tool execution reports for audit, telemetry, or host-specific projection.
/// </summary>
public interface IAgentToolExecutionObserver
{
    /// <summary>
    /// Observes one execution report after it has been accepted by the runtime catalog.
    /// </summary>
    /// <param name="report">The execution report to observe.</param>
    /// <param name="cancellationToken">The cancellation token for the observation.</param>
    /// <returns>A task that completes when the report has been observed.</returns>
    ValueTask ObserveAsync(
        AgentToolExecutionReport report,
        CancellationToken cancellationToken = default);
}
