namespace Cephalon.Sample.Microservice.Modules.Customers.Features.Welcome.Policies;

public static class WelcomePolicy
{
    public static string ResolveTenantPolicy(string tenant)
    {
        return string.Equals(tenant, "enterprise", StringComparison.OrdinalIgnoreCase)
            ? "enterprise-boundary"
            : "single-service-boundary";
    }
}
