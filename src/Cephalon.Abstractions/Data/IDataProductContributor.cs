namespace Cephalon.Abstractions.Data;

/// <summary>
/// Contributes one or more data product descriptors to the active runtime.
/// </summary>
public interface IDataProductContributor
{
    /// <summary>
    /// Registers one or more data product descriptors with the supplied registry.
    /// </summary>
    /// <param name="dataProducts">The registry that collects contributed data product descriptors.</param>
    void RegisterDataProducts(IDataProductRegistry dataProducts);
}
