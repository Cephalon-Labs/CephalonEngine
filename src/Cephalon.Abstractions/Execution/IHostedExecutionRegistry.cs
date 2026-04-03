namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Receives hosted-execution descriptors contributed by active modules.
/// </summary>
public interface IHostedExecutionRegistry
{
    /// <summary>
    /// Adds a hosted execution to the current runtime composition.
    /// </summary>
    /// <param name="hostedExecution">The hosted execution to register.</param>
    void Add(HostedExecutionDescriptor hostedExecution);
}
