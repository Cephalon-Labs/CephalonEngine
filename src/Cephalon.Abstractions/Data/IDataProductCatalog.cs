namespace Cephalon.Abstractions.Data;

/// <summary>
/// Exposes the data products visible to the current runtime.
/// </summary>
public interface IDataProductCatalog
{
    /// <summary>
    /// Gets all data products visible to the current runtime.
    /// </summary>
    IReadOnlyList<DataProductDescriptor> DataProducts { get; }

    /// <summary>
    /// Gets one data product by its stable identifier.
    /// </summary>
    /// <param name="dataProductId">The data product identifier to resolve.</param>
    /// <returns>The matching data product, or <see langword="null" /> when it is not active.</returns>
    DataProductDescriptor? GetById(string dataProductId);

    /// <summary>
    /// Gets all data products contributed by the requested module.
    /// </summary>
    /// <param name="sourceModuleId">The source module identifier to filter by.</param>
    /// <returns>The matching data products, or an empty list when the module contributed none.</returns>
    IReadOnlyList<DataProductDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all data products that belong to the requested domain.
    /// </summary>
    /// <param name="domainId">The domain identifier to filter by.</param>
    /// <returns>The matching data products, or an empty list when the domain contributed none.</returns>
    IReadOnlyList<DataProductDescriptor> GetByDomainId(string domainId);

    /// <summary>
    /// Gets all data products that expose the requested contract identifier.
    /// </summary>
    /// <param name="contractId">The contract identifier to filter by.</param>
    /// <returns>The matching data products, or an empty list when no active data product exposes the contract.</returns>
    IReadOnlyList<DataProductDescriptor> GetByContractId(string contractId);
}
