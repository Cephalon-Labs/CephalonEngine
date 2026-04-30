using Amazon;
using Amazon.SimpleEmailV2;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Configuration;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Services;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Hosting;

/// <summary>
/// Adds Amazon SES v2 invitation delivery services to a Cephalon host.
/// </summary>
public static class AmazonSesInvitationDeliveryServiceCollectionExtensions
{
    /// <summary>
    /// Adds Amazon SES invitation delivery using configuration as the primary source of SES settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">An optional callback that can extend or override the configuration-driven setup.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonAmazonSesInvitationDelivery(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AmazonSesInvitationDeliveryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = AmazonSesInvitationDeliveryOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonAmazonSesInvitationDelivery(options);
    }

    /// <summary>
    /// Adds Amazon SES invitation delivery using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures the Amazon SES invitation delivery sender.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonAmazonSesInvitationDelivery(
        this IServiceCollection services,
        Action<AmazonSesInvitationDeliveryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new AmazonSesInvitationDeliveryOptions();
        configure?.Invoke(options);

        return services.AddCephalonAmazonSesInvitationDelivery(options);
    }

    private static IServiceCollection AddCephalonAmazonSesInvitationDelivery(
        this IServiceCollection services,
        AmazonSesInvitationDeliveryOptions options)
    {
        if (!options.Enabled)
        {
            return services;
        }

        if (string.IsNullOrWhiteSpace(options.SenderId))
        {
            throw new InvalidOperationException("Amazon SES invitation delivery requires a non-empty sender id.");
        }

        if (!AmazonSesInvitationDeliveryAddress.TryCreate(options.FromEmail, options.FromName, out _))
        {
            throw new InvalidOperationException("Amazon SES invitation delivery requires a valid FromEmail address.");
        }

        services.TryAddSingleton(options);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, AmazonSesInvitationDeliveryDiagnosticsConventionContributor>());
        services.TryAddSingleton<IAmazonSimpleEmailServiceV2>(_ =>
        {
            var region = options.GetRegionSystemName();
            return string.IsNullOrWhiteSpace(region)
                ? new AmazonSimpleEmailServiceV2Client()
                : new AmazonSimpleEmailServiceV2Client(RegionEndpoint.GetBySystemName(region));
        });
        services.TryAddSingleton<IAmazonSesInvitationDeliveryClient, AmazonSesInvitationDeliveryClient>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantInvitationDeliverySender, AmazonSesInvitationDeliverySender>());

        return services;
    }
}
