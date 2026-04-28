namespace Cephalon.Agentics.Services;

/// <summary>
/// Dispatches registered agent tools through Cephalon-managed execution, policy, and run-state services.
/// </summary>
public interface IAgentToolDispatcher
{
    /// <summary>
    /// Executes one registered agent tool.
    /// </summary>
    /// <param name="request">The execution request to dispatch.</param>
    /// <param name="cancellationToken">The cancellation token for the current execution attempt.</param>
    /// <returns>The result produced by the managed execution path.</returns>
    ValueTask<AgentToolExecutionResult> ExecuteAsync(
        AgentToolExecutionRequest request,
        CancellationToken cancellationToken = default);
}
