namespace Cephalon.Sample.ModularVerticalSlice.Modules.Orders.Features.Checkout.Policies;

/// <summary>
/// Resolves checkout policies for the modular vertical-slice sample.
/// </summary>
public static class CheckoutPolicy
{
    /// <summary>
    /// Determines whether the supplied customer qualifies for expedited checkout.
    /// </summary>
    /// <param name="customerId">
    /// The customer identifier supplied by the request.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the customer should receive expedited handling; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsExpedited(string customerId)
    {
        return customerId.StartsWith("vip", StringComparison.OrdinalIgnoreCase);
    }
}
