using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.HttpDelivery.Configuration;
using Cephalon.MultiTenancy.Governance.HttpDelivery.Services;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.MultiTenancy.Governance.HttpDelivery.Hosting;

/// <summary>
/// Adds HTTP webhook invitation delivery services to a Cephalon host.
/// </summary>
public static class HttpInvitationDeliveryServiceCollectionExtensions
{
    /// <summary>
    /// The named HTTP client used by the HTTP invitation delivery sender.
    /// </summary>
    public const string HttpClientName = "Cephalon.MultiTenancy.Governance.HttpDelivery";

    /// <summary>
    /// Adds HTTP invitation delivery using configuration as the primary source of webhook settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">An optional callback that can extend or override the configuration-driven setup.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonHttpInvitationDelivery(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<HttpInvitationDeliveryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = HttpInvitationDeliveryOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonHttpInvitationDelivery(options);
    }

    /// <summary>
    /// Adds HTTP invitation delivery using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures the HTTP invitation delivery sender.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonHttpInvitationDelivery(
        this IServiceCollection services,
        Action<HttpInvitationDeliveryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new HttpInvitationDeliveryOptions();
        configure?.Invoke(options);

        return services.AddCephalonHttpInvitationDelivery(options);
    }

    private static IServiceCollection AddCephalonHttpInvitationDelivery(
        this IServiceCollection services,
        HttpInvitationDeliveryOptions options)
    {
        if (!options.Enabled)
        {
            return services;
        }

        if (string.IsNullOrWhiteSpace(options.SenderId))
        {
            throw new InvalidOperationException("HTTP invitation delivery requires a non-empty sender id.");
        }

        if (options.TryGetEndpoint() is null)
        {
            throw new InvalidOperationException("HTTP invitation delivery requires an absolute HTTP or HTTPS endpoint.");
        }

        services.TryAddSingleton(options);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, HttpInvitationDeliveryDiagnosticsConventionContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantInvitationDeliverySender, HttpInvitationDeliverySender>());

        services.AddHttpClient(HttpClientName)
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan);

        return services;
    }
}
