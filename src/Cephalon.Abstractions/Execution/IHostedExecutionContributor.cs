namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Contributes one or more hosted or background execution descriptors to the active runtime.
/// </summary>
public interface IHostedExecutionContributor
{
    /// <summary>
    /// Registers one or more hosted execution descriptors owned by the contributor.
    /// </summary>
    /// <param name="hostedExecutions">The hosted-execution registry receiving hosted-execution descriptors.</param>
    void RegisterHostedExecutions(IHostedExecutionRegistry hostedExecutions);
}
