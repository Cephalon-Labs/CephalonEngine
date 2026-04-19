using Cephalon.Abstractions.Data;

namespace Cephalon.Engine.Data;

internal sealed class DataProductCatalogSnapshot : IDataProductCatalog
{
    private readonly IReadOnlyList<DataProductDescriptor> dataProducts;
    private readonly Dictionary<string, DataProductDescriptor> dataProductsById;
    private readonly Dictionary<string, IReadOnlyList<DataProductDescriptor>> dataProductsBySourceModule;
    private readonly Dictionary<string, IReadOnlyList<DataProductDescriptor>> dataProductsByDomainId;
    private readonly Dictionary<string, IReadOnlyList<DataProductDescriptor>> dataProductsByContractId;

    public DataProductCatalogSnapshot(IEnumerable<DataProductDescriptor> dataProducts)
    {
        ArgumentNullException.ThrowIfNull(dataProducts);

        this.dataProducts = dataProducts.ToArray();
        dataProductsById = this.dataProducts.ToDictionary(static dataProduct => dataProduct.Id, StringComparer.OrdinalIgnoreCase);
        dataProductsBySourceModule = this.dataProducts
            .GroupBy(static dataProduct => dataProduct.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<DataProductDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        dataProductsByDomainId = this.dataProducts
            .GroupBy(static dataProduct => dataProduct.DomainId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<DataProductDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        dataProductsByContractId = this.dataProducts
            .GroupBy(static dataProduct => dataProduct.ContractId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<DataProductDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<DataProductDescriptor> DataProducts => dataProducts;

    public DataProductDescriptor? GetById(string dataProductId)
    {
        if (string.IsNullOrWhiteSpace(dataProductId))
        {
            return null;
        }

        return dataProductsById.TryGetValue(dataProductId.Trim(), out var dataProduct)
            ? dataProduct
            : null;
    }

    public IReadOnlyList<DataProductDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return dataProductsBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<DataProductDescriptor> GetByDomainId(string domainId)
    {
        if (string.IsNullOrWhiteSpace(domainId))
        {
            return [];
        }

        return dataProductsByDomainId.TryGetValue(domainId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<DataProductDescriptor> GetByContractId(string contractId)
    {
        if (string.IsNullOrWhiteSpace(contractId))
        {
            return [];
        }

        return dataProductsByContractId.TryGetValue(contractId.Trim(), out var matches)
            ? matches
            : [];
    }
}
