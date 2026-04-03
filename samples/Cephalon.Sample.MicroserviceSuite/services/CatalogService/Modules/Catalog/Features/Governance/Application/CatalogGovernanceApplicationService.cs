using Cephalon.Sample.MicroserviceSuite.Foundation.Conventions;
using Cephalon.Sample.MicroserviceSuite.Governance.Contracts;
using Cephalon.Sample.MicroserviceSuite.Governance.Guidance;

namespace Cephalon.Sample.MicroserviceSuite.CatalogService.Modules.Catalog.Features.Governance.Application;

/// <summary>
/// Builds additive gateway and control-plane guidance for the catalog service.
/// </summary>
public sealed class CatalogGovernanceApplicationService
{
    /// <summary>
    /// Creates the governance snapshot returned by the catalog service.
    /// </summary>
    /// <returns>
    /// The governance snapshot for the catalog service boundary.
    /// </returns>
    public SuiteGovernanceSnapshotContract Build()
    {
        return CommerceSuiteGovernance.CreateSnapshot(CommerceSuiteConventions.CatalogService);
    }
}
