using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Configuration;
using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Services;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Hosting;

/// <summary>
/// Adds Microsoft Graph <c>sendMail</c> invitation delivery services to a Cephalon host.
/// </summary>
public static class MicrosoftGraphInvitationDeliveryServiceCollectionExtensions
{
    /// <summary>
    /// The named HTTP client used by the Microsoft Graph invitation delivery client.
    /// </summary>
    public const string HttpClientName = "Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery";

    /// <summary>
    /// Adds Microsoft Graph invitation delivery using configuration as the primary source of <c>sendMail</c> settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">An optional callback that can extend or override the configuration-driven setup.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonMicrosoftGraphInvitationDelivery(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<MicrosoftGraphInvitationDeliveryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = MicrosoftGraphInvitationDeliveryOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonMicrosoftGraphInvitationDelivery(options);
    }

    /// <summary>
    /// Adds Microsoft Graph invitation delivery using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures the Microsoft Graph invitation delivery sender.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonMicrosoftGraphInvitationDelivery(
        this IServiceCollection services,
        Action<MicrosoftGraphInvitationDeliveryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new MicrosoftGraphInvitationDeliveryOptions();
        configure?.Invoke(options);

        return services.AddCephalonMicrosoftGraphInvitationDelivery(options);
    }

    private static IServiceCollection AddCephalonMicrosoftGraphInvitationDelivery(
        this IServiceCollection services,
        MicrosoftGraphInvitationDeliveryOptions options)
    {
        if (!options.Enabled)
        {
            return services;
        }

        if (string.IsNullOrWhiteSpace(options.SenderId))
        {
            throw new InvalidOperationException("Microsoft Graph invitation delivery requires a non-empty sender id.");
        }

        if (options.TryGetBaseUrl() is null)
        {
            throw new InvalidOperationException("Microsoft Graph invitation delivery requires an absolute HTTP or HTTPS base URL.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiVersion))
        {
            throw new InvalidOperationException("Microsoft Graph invitation delivery requires a non-empty API version.");
        }

        services.TryAddSingleton(options);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, MicrosoftGraphInvitationDeliveryDiagnosticsConventionContributor>());
        services.TryAddSingleton<IMicrosoftGraphInvitationDeliveryAccessTokenProvider, MicrosoftGraphInvitationDeliveryStaticAccessTokenProvider>();
        services.TryAddSingleton<IMicrosoftGraphInvitationDeliveryClient, MicrosoftGraphInvitationDeliveryClient>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantInvitationDeliverySender, MicrosoftGraphInvitationDeliverySender>());

        services.AddHttpClient(HttpClientName)
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan);

        return services;
    }
}
