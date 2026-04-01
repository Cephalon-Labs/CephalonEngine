namespace CephalonTemplateApp.Modules.Orders.Features.Checkout.Policies;

public static class CheckoutPolicy
{
    public static bool IsExpedited(string customerId)
    {
        return customerId.StartsWith("vip", StringComparison.OrdinalIgnoreCase);
    }
}
