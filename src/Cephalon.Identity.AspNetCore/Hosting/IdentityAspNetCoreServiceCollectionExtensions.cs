using Cephalon.Identity.AspNetCore.Configuration;
using Cephalon.Identity.AspNetCore.Services;
using Cephalon.Abstractions.Technologies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Identity.AspNetCore.Hosting;

/// <summary>
/// Registers the ASP.NET Core identity adapter services used by Cephalon.
/// </summary>
public static class IdentityAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Cephalon ASP.NET Core identity adapter to the service collection.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration">
    /// The optional configuration root used to populate <see cref="IdentityAspNetCoreOptions" /> from
    /// <c>Engine:Identity:AspNetCore</c>.
    /// </param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven ASP.NET Core identity adapter options.
    /// </param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonIdentityAspNetCore(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<IdentityAspNetCoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = IdentityAspNetCoreOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        services.RemoveAll<IdentityAspNetCoreOptions>();
        services.AddSingleton(options);
        services.TryAddSingleton<HttpContextAuthorizationRequestFactory>();
        services.TryAddSingleton<CephalonAuthorizationBoundaryExecutor>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, IdentityAspNetCoreRuntimeSurfaceContributor>());
        return services;
    }
}
