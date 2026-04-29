using Cephalon.Abstractions.Agentics;

namespace Cephalon.Agentics.Services;

/// <summary>
/// Executes one registered agent tool through the Cephalon-managed agentics runtime.
/// </summary>
public interface IAgentToolExecutor
{
    /// <summary>
    /// Gets the stable tool identifier owned by this executor.
    /// </summary>
    string ToolId { get; }

    /// <summary>
    /// Executes the tool against the supplied context.
    /// </summary>
    /// <param name="context">The host-agnostic execution context for the current tool run.</param>
    /// <param name="cancellationToken">The cancellation token for the current execution attempt.</param>
    /// <returns>The result reported by the executor.</returns>
    ValueTask<AgentToolExecutionResult> ExecuteAsync(
        AgentToolExecutionContext context,
        CancellationToken cancellationToken = default);
}
