namespace Cephalon.Abstractions.Data;

/// <summary>
/// Receives data product descriptors contributed by active modules or packages.
/// </summary>
public interface IDataProductRegistry
{
    /// <summary>
    /// Adds a data product to the current runtime composition.
    /// </summary>
    /// <param name="dataProduct">The data product descriptor to register.</param>
    void Add(DataProductDescriptor dataProduct);
}
