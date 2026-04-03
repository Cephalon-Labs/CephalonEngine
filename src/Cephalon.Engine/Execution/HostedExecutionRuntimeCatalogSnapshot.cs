using Cephalon.Abstractions.Execution;

namespace Cephalon.Engine.Execution;

internal sealed class HostedExecutionRuntimeCatalogSnapshot : IHostedExecutionRuntimeCatalog
{
    private readonly IReadOnlyList<HostedExecutionDescriptor> hostedExecutions;
    private readonly Dictionary<string, HostedExecutionDescriptor> hostedExecutionsById;
    private readonly Dictionary<string, IReadOnlyList<HostedExecutionDescriptor>> hostedExecutionsBySourceModule;
    private readonly Dictionary<string, IReadOnlyList<HostedExecutionDescriptor>> hostedExecutionsByExecutionGraph;

    public HostedExecutionRuntimeCatalogSnapshot(IEnumerable<HostedExecutionDescriptor> hostedExecutions)
    {
        ArgumentNullException.ThrowIfNull(hostedExecutions);

        this.hostedExecutions = hostedExecutions.ToArray();
        hostedExecutionsById = this.hostedExecutions.ToDictionary(static hostedExecution => hostedExecution.Id, StringComparer.OrdinalIgnoreCase);
        hostedExecutionsBySourceModule = this.hostedExecutions
            .GroupBy(static hostedExecution => hostedExecution.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<HostedExecutionDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        hostedExecutionsByExecutionGraph = this.hostedExecutions
            .Where(static hostedExecution => !string.IsNullOrWhiteSpace(hostedExecution.ExecutionGraphId))
            .GroupBy(static hostedExecution => hostedExecution.ExecutionGraphId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<HostedExecutionDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<HostedExecutionDescriptor> HostedExecutions => hostedExecutions;

    public HostedExecutionDescriptor? GetById(string hostedExecutionId)
    {
        if (string.IsNullOrWhiteSpace(hostedExecutionId))
        {
            return null;
        }

        return hostedExecutionsById.TryGetValue(hostedExecutionId.Trim(), out var hostedExecution)
            ? hostedExecution
            : null;
    }

    public IReadOnlyList<HostedExecutionDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return hostedExecutionsBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<HostedExecutionDescriptor> GetByExecutionGraph(string executionGraphId)
    {
        if (string.IsNullOrWhiteSpace(executionGraphId))
        {
            return [];
        }

        return hostedExecutionsByExecutionGraph.TryGetValue(executionGraphId.Trim(), out var matches)
            ? matches
            : [];
    }
}
