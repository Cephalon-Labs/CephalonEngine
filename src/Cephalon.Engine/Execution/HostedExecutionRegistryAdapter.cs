using Cephalon.Abstractions.Execution;

namespace Cephalon.Engine.Execution;

internal sealed class HostedExecutionRegistryAdapter(
    string moduleId,
    List<HostedExecutionDescriptor> hostedExecutions) : IHostedExecutionRegistry
{
    public void Add(HostedExecutionDescriptor hostedExecution)
    {
        ArgumentNullException.ThrowIfNull(hostedExecution);

        if (!string.Equals(hostedExecution.SourceModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Hosted execution '{hostedExecution.Id}' declared source module '{hostedExecution.SourceModuleId}', but it was contributed by module '{moduleId}'.");
        }

        hostedExecutions.Add(hostedExecution);
    }
}
