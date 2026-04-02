namespace Cephalon.Sample.Microservice.Modules.Customers.Features.Welcome.Policies;

/// <summary>
/// Resolves sample tenant policies for the microservice welcome flow.
/// </summary>
public static class WelcomePolicy
{
    /// <summary>
    /// Resolves the tenant policy name used by the welcome response.
    /// </summary>
    /// <param name="tenant">
    /// The tenant identifier supplied to the sample endpoint.
    /// </param>
    /// <returns>
    /// The policy label associated with the requested tenant.
    /// </returns>
    public static string ResolveTenantPolicy(string tenant)
    {
        return string.Equals(tenant, "enterprise", StringComparison.OrdinalIgnoreCase)
            ? "enterprise-boundary"
            : "single-service-boundary";
    }
}
