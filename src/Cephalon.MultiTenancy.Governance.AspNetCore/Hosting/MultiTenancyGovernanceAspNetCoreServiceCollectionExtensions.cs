using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.AspNetCore.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

/// <summary>
/// Registers the ASP.NET Core governance adapter services used by Cephalon multi-tenancy governance.
/// </summary>
public static class MultiTenancyGovernanceAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Cephalon ASP.NET Core multi-tenancy governance adapter to the service collection.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration">
    /// The optional configuration root used to populate <see cref="MultiTenancyGovernanceAspNetCoreOptions" /> from
    /// <c>Engine:MultiTenancy:Governance:AspNetCore</c>.
    /// </param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven ASP.NET Core governance adapter options.
    /// </param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonMultiTenancyGovernanceAspNetCore(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<MultiTenancyGovernanceAspNetCoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = MultiTenancyGovernanceAspNetCoreOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        services.RemoveAll<MultiTenancyGovernanceAspNetCoreOptions>();
        services.AddSingleton(options);
        services.TryAddSingleton<TenantAdministrationEndpointRuntimeCatalog>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, MultiTenancyGovernanceAspNetCoreAdministrationRuntimeSurfaceContributor>());
        return services;
    }
}
