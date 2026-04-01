using Cephalon.Sample.Microservice.Contracts;
using Cephalon.Sample.Microservice.Modules.Customers.Features.Welcome.Policies;

namespace Cephalon.Sample.Microservice.Modules.Customers.Features.Welcome.Application;

/// <summary>
/// Builds customer welcome responses for the microservice sample.
/// </summary>
public sealed class WelcomeApplicationService
{
    /// <summary>
    /// Creates the welcome contract returned by the customers module.
    /// </summary>
    /// <param name="name">
    /// The optional visitor name included in the response.
    /// </param>
    /// <param name="tenant">
    /// The optional tenant identifier used to resolve the sample policy.
    /// </param>
    /// <returns>
    /// The welcome payload returned by the sample endpoint.
    /// </returns>
    public WelcomeContract Build(string? name, string? tenant = null)
    {
        var visitor = string.IsNullOrWhiteSpace(name) ? "builder" : name.Trim();
        var tenantPolicy = WelcomePolicy.ResolveTenantPolicy(tenant ?? "single");

        return new WelcomeContract(
            Service: "Customers",
            TenantPolicy: tenantPolicy,
            Message: $"Welcome, {visitor}, from the Cephalon microservice sample.");
    }
}
