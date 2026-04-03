using Cephalon.Abstractions.Execution;
using Cephalon.Engine.Manifest;

namespace Cephalon.Engine.Execution;

internal static class HostedExecutionValidation
{
    public static HostedExecutionDescriptor[] Validate(
        IEnumerable<HostedExecutionDescriptor> hostedExecutions,
        IReadOnlyList<ModuleManifest> modules,
        IReadOnlyList<ExecutionGraphDescriptor> executionGraphs)
    {
        ArgumentNullException.ThrowIfNull(hostedExecutions);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(executionGraphs);

        var hostedExecutionArray = hostedExecutions.ToArray();
        var moduleIds = modules
            .Select(static module => module.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var graphsById = executionGraphs.ToDictionary(static graph => graph.Id, StringComparer.OrdinalIgnoreCase);
        var seenHostedExecutionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var hostedExecution in hostedExecutionArray)
        {
            if (!seenHostedExecutionIds.Add(hostedExecution.Id))
            {
                throw new InvalidOperationException(
                    $"Hosted execution '{hostedExecution.Id}' was contributed more than once.");
            }

            if (!moduleIds.Contains(hostedExecution.SourceModuleId))
            {
                throw new InvalidOperationException(
                    $"Hosted execution '{hostedExecution.Id}' references unknown source module '{hostedExecution.SourceModuleId}'.");
            }

            if (string.IsNullOrWhiteSpace(hostedExecution.ExecutionGraphId))
            {
                continue;
            }

            if (!graphsById.TryGetValue(hostedExecution.ExecutionGraphId, out var graph))
            {
                throw new InvalidOperationException(
                    $"Hosted execution '{hostedExecution.Id}' references unknown execution graph '{hostedExecution.ExecutionGraphId}'.");
            }

            if (!string.Equals(graph.SourceModuleId, hostedExecution.SourceModuleId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Hosted execution '{hostedExecution.Id}' references execution graph '{graph.Id}' from module '{graph.SourceModuleId}', but the hosted execution is owned by module '{hostedExecution.SourceModuleId}'.");
            }
        }

        return hostedExecutionArray;
    }
}
