using Cephalon.Sample.MicroserviceSuite.Foundation.Contracts;
using Cephalon.Sample.MicroserviceSuite.Foundation.Conventions;

namespace Cephalon.Sample.MicroserviceSuite.CatalogService.Modules.Catalog.Features.Overview.Application;

/// <summary>
/// Builds catalog overview responses for the microservice-suite sample.
/// </summary>
public sealed class CatalogOverviewApplicationService
{
    /// <summary>
    /// Creates the shared suite summary returned by the catalog service.
    /// </summary>
    /// <returns>
    /// The shared suite summary for the catalog service boundary.
    /// </returns>
    public SuiteServiceSummaryContract Build()
    {
        return CommerceSuiteConventions.CreateServiceSummary(
            CommerceSuiteConventions.CatalogService,
            "Catalog ownership stays inside one Cephalon service while suite conventions stay shared.");
    }
}
