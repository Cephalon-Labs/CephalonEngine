using Cephalon.Sample.MicroserviceSuite.Foundation.Conventions;
using Cephalon.Sample.MicroserviceSuite.Governance.Contracts;
using Cephalon.Sample.MicroserviceSuite.Governance.Guidance;

namespace Cephalon.Sample.MicroserviceSuite.OrdersService.Modules.Orders.Features.Governance.Application;

/// <summary>
/// Builds additive gateway and control-plane guidance for the orders service.
/// </summary>
public sealed class OrdersGovernanceApplicationService
{
    /// <summary>
    /// Creates the governance snapshot returned by the orders service.
    /// </summary>
    /// <returns>
    /// The governance snapshot for the orders service boundary.
    /// </returns>
    public SuiteGovernanceSnapshotContract Build()
    {
        return CommerceSuiteGovernance.CreateSnapshot(CommerceSuiteConventions.OrdersService);
    }
}
