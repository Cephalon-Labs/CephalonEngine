using Cephalon.Identity.AspNetCore.Configuration;
using Cephalon.Identity.AspNetCore.Services;
using Cephalon.Abstractions.Technologies;
using Cephalon.Audit.Services;
using Microsoft.AspNetCore.Http;
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
    /// <remarks>
    /// When <c>Cephalon.Audit</c> is also active and the host has not already supplied a custom
    /// <see cref="IAuditActorAccessor" />, this registration also bridges the current authenticated
    /// <see cref="Microsoft.AspNetCore.Http.HttpContext.User" /> into the ambient audit actor contract.
    /// </remarks>
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
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.TryAddSingleton<IdentityPrincipalDescriptorFactory>();
        services.TryAddSingleton<HttpContextAuditActorAccessor>();
        services.TryAddSingleton<HttpContextAuthorizationRequestFactory>();
        services.TryAddSingleton<CephalonAuthorizationBoundaryExecutor>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, IdentityAspNetCoreRuntimeSurfaceContributor>());
        TryRegisterAuditActorBridge(services);
        return services;
    }

    private static void TryRegisterAuditActorBridge(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var existingDescriptors = services
            .Where(static descriptor => descriptor.ServiceType == typeof(IAuditActorAccessor))
            .ToArray();
        if (existingDescriptors.Length == 0)
        {
            services.AddSingleton<IAuditActorAccessor>(serviceProvider =>
                serviceProvider.GetRequiredService<HttpContextAuditActorAccessor>());
            return;
        }

        if (existingDescriptors.Any(static descriptor => !IsDefaultAuditActorAccessor(descriptor)))
        {
            return;
        }

        foreach (var descriptor in existingDescriptors)
        {
            services.Remove(descriptor);
        }

        services.AddSingleton<IAuditActorAccessor>(serviceProvider =>
            serviceProvider.GetRequiredService<HttpContextAuditActorAccessor>());
    }

    private static bool IsDefaultAuditActorAccessor(ServiceDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return string.Equals(
            descriptor.ImplementationType?.FullName,
            "Cephalon.Audit.Services.DefaultAuditActorAccessor",
            StringComparison.Ordinal);
    }
}
