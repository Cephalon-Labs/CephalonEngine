using Cephalon.Abstractions.Technologies;

namespace Cephalon.Engine.Technologies;

internal sealed class CellHealthIsolationRegistryAdapter(
    string moduleId,
    List<CellHealthIsolationDescriptor> healthIsolations) : ICellHealthIsolationRegistry
{
    public void Add(CellHealthIsolationDescriptor healthIsolation)
    {
        ArgumentNullException.ThrowIfNull(healthIsolation);

        if (!string.Equals(healthIsolation.SourceModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Cell health isolation '{healthIsolation.Id}' declared source module '{healthIsolation.SourceModuleId}', but it was contributed by module '{moduleId}'.");
        }

        healthIsolations.Add(healthIsolation);
    }
}
