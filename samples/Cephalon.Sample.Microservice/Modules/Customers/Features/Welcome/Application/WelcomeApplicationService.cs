using Cephalon.Sample.Microservice.Contracts;
using Cephalon.Sample.Microservice.Modules.Customers.Features.Welcome.Policies;

namespace Cephalon.Sample.Microservice.Modules.Customers.Features.Welcome.Application;

public sealed class WelcomeApplicationService
{
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
