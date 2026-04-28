namespace Cephalon.Agentics.Services;

/// <summary>
/// Evaluates whether an agent-tool execution request can continue.
/// </summary>
public interface IAgentToolExecutionPolicy
{
    /// <summary>
    /// Evaluates one resolved agent-tool execution context.
    /// </summary>
    /// <param name="context">The execution context to evaluate.</param>
    /// <param name="cancellationToken">The cancellation token for the evaluation.</param>
    /// <returns>The policy decision for this execution request.</returns>
    ValueTask<AgentToolExecutionDecision> EvaluateAsync(
        AgentToolExecutionContext context,
        CancellationToken cancellationToken = default);
}
