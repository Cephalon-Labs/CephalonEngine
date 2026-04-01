using Cephalon.Sample.ModularVerticalSlice.Modules.Orders.Features.Checkout.Policies;
using Cephalon.Sample.ModularVerticalSlice.Modules.Orders.Features.Checkout.Queries;

namespace Cephalon.Sample.ModularVerticalSlice.Modules.Orders.Features.Checkout.Commands;

/// <summary>
/// Builds checkout preview payloads for the modular vertical-slice sample.
/// </summary>
public sealed class CheckoutPreviewService
{
    /// <summary>
    /// Creates the checkout preview returned by the orders module.
    /// </summary>
    /// <param name="customerId">
    /// The optional customer identifier supplied by the request.
    /// </param>
    /// <returns>
    /// The checkout preview payload for the requested customer.
    /// </returns>
    public CheckoutPreviewEnvelope Build(string? customerId)
    {
        var resolvedCustomerId = string.IsNullOrWhiteSpace(customerId) ? "builder" : customerId.Trim();
        var expedited = CheckoutPolicy.IsExpedited(resolvedCustomerId);

        return new CheckoutPreviewEnvelope(
            Architecture: "ModularVerticalSlice",
            CustomerId: resolvedCustomerId,
            Expedited: expedited,
            Policy: expedited ? "vip-fast-lane" : "standard-lane");
    }
}
