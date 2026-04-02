using Cephalon.Sample.ModularMonolith.Modules.Catalog.Domain;

namespace Cephalon.Sample.ModularMonolith.Modules.Catalog.Application;

/// <summary>
/// Builds catalog overview payloads for the modular monolith sample.
/// </summary>
public sealed class CatalogOverviewService
{
    /// <summary>
    /// Creates the sample catalog overview payload.
    /// </summary>
    /// <returns>
    /// The catalog overview returned by the sample endpoint.
    /// </returns>
    public CatalogOverviewEnvelope Build()
    {
        return new CatalogOverviewEnvelope(
            Architecture: "ModularMonolith",
            Products:
            [
                new ProductSnapshot("starter-kit", "Starter Kit", "composable"),
                new ProductSnapshot("ops-console", "Operations Console", "observable")
            ]);
    }
}

/// <summary>
/// Represents the catalog overview returned by the modular monolith sample.
/// </summary>
/// <param name="Architecture">
/// The application blueprint represented by the sample.
/// </param>
/// <param name="Products">
/// The product snapshots included in the overview.
/// </param>
public sealed record CatalogOverviewEnvelope(
    string Architecture,
    IReadOnlyList<ProductSnapshot> Products);
