using Cephalon.Sample.ModularVerticalSlice.Modules.Orders.Features.Checkout.Policies;
using Cephalon.Sample.ModularVerticalSlice.Modules.Orders.Features.Checkout.Queries;

namespace Cephalon.Sample.ModularVerticalSlice.Modules.Orders.Features.Checkout.Commands;

public sealed class CheckoutPreviewService
{
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
