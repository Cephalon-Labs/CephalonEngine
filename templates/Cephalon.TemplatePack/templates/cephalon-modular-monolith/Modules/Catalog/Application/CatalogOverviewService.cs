#pragma warning disable MA0048 // Starter keeps the service and small response DTO together for adoption clarity.

using System.Collections.Generic;
using CephalonTemplateApp.Modules.Catalog.Domain;

namespace CephalonTemplateApp.Modules.Catalog.Application;

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
