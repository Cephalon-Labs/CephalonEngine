using Cephalon.Sample.MicroserviceSuite.Foundation.Contracts;
using Cephalon.Sample.MicroserviceSuite.Foundation.Conventions;

namespace Cephalon.Sample.MicroserviceSuite.OrdersService.Modules.Orders.Features.Coordination.Application;

/// <summary>
/// Builds order-coordination responses for the microservice-suite sample.
/// </summary>
public sealed class OrderCoordinationApplicationService
{
    /// <summary>
    /// Creates the shared suite summary returned by the orders service.
    /// </summary>
    /// <param name="orderId">
    /// The optional order identifier supplied by the caller.
    /// </param>
    /// <param name="fulfillmentRegion">
    /// The optional fulfillment region used to shape the sample response.
    /// </param>
    /// <returns>
    /// The shared suite summary for the orders service boundary.
    /// </returns>
    public SuiteServiceSummaryContract Build(string? orderId = null, string? fulfillmentRegion = null)
    {
        var normalizedOrderId = string.IsNullOrWhiteSpace(orderId) ? "PO-1000" : orderId.Trim();
        var normalizedRegion = string.IsNullOrWhiteSpace(fulfillmentRegion) ? "global-default" : fulfillmentRegion.Trim();

        return CommerceSuiteConventions.CreateServiceSummary(
            CommerceSuiteConventions.OrdersService,
            $"Order {normalizedOrderId} follows the {normalizedRegion} coordination path inside the commerce suite.");
    }
}
