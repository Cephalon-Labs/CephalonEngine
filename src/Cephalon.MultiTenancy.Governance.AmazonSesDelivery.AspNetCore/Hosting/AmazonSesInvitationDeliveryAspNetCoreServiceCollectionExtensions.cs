using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Configuration;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Hosting;

/// <summary>
/// Registers ASP.NET Core Amazon SES over SNS callback translation services for tenant-invitation delivery status callbacks.
/// </summary>
public static class AmazonSesInvitationDeliveryAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds Amazon SES over SNS callback translation services using configuration as the primary setup source.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration">The optional configuration root.</param>
    /// <param name="configure">An optional callback that can extend or override configuration-driven options.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonAmazonSesInvitationDeliveryAspNetCore(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<AmazonSesInvitationDeliveryAspNetCoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = AmazonSesInvitationDeliveryAspNetCoreOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        services.RemoveAll<AmazonSesInvitationDeliveryAspNetCoreOptions>();
        services.AddSingleton(options);
        services.TryAddSingleton<AmazonSesInvitationDeliveryStatusCallbackRuntimeCatalog>();
        services.TryAddSingleton<AmazonSesSnsDeliveryStatusMapper>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, AmazonSesInvitationDeliveryStatusRuntimeSurfaceContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, AmazonSesInvitationDeliveryAspNetCoreDiagnosticsConventionContributor>());
        return services;
    }
}
