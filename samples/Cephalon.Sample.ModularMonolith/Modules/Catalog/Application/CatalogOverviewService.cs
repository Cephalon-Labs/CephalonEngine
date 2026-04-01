using Cephalon.Sample.ModularMonolith.Modules.Catalog.Domain;

namespace Cephalon.Sample.ModularMonolith.Modules.Catalog.Application;

public sealed class CatalogOverviewService
{
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

public sealed record CatalogOverviewEnvelope(
    string Architecture,
    IReadOnlyList<ProductSnapshot> Products);
