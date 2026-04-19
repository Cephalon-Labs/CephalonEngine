using Cephalon.Abstractions.Data;

namespace Cephalon.Engine.Data;

internal sealed class DataProductRegistryAdapter(
    string moduleId,
    List<DataProductDescriptor> dataProducts) : IDataProductRegistry
{
    public void Add(DataProductDescriptor dataProduct)
    {
        ArgumentNullException.ThrowIfNull(dataProduct);

        if (!string.Equals(dataProduct.SourceModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Data product '{dataProduct.Id}' declared source module '{dataProduct.SourceModuleId}', but it was contributed by module '{moduleId}'.");
        }

        dataProducts.Add(dataProduct);
    }
}
