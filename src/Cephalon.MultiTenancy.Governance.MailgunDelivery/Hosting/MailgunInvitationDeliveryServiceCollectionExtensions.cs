using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.MailgunDelivery.Configuration;
using Cephalon.MultiTenancy.Governance.MailgunDelivery.Services;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.Hosting;

/// <summary>
/// Adds Mailgun Messages API invitation delivery services to a Cephalon host.
/// </summary>
public static class MailgunInvitationDeliveryServiceCollectionExtensions
{
    /// <summary>
    /// The named HTTP client used by the Mailgun invitation delivery client.
    /// </summary>
    public const string HttpClientName = "Cephalon.MultiTenancy.Governance.MailgunDelivery";

    /// <summary>
    /// Adds Mailgun invitation delivery using configuration as the primary source of Messages API settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">An optional callback that can extend or override the configuration-driven setup.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonMailgunInvitationDelivery(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<MailgunInvitationDeliveryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = MailgunInvitationDeliveryOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonMailgunInvitationDelivery(options);
    }

    /// <summary>
    /// Adds Mailgun invitation delivery using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures the Mailgun invitation delivery sender.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonMailgunInvitationDelivery(
        this IServiceCollection services,
        Action<MailgunInvitationDeliveryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new MailgunInvitationDeliveryOptions();
        configure?.Invoke(options);

        return services.AddCephalonMailgunInvitationDelivery(options);
    }

    private static IServiceCollection AddCephalonMailgunInvitationDelivery(
        this IServiceCollection services,
        MailgunInvitationDeliveryOptions options)
    {
        if (!options.Enabled)
        {
            return services;
        }

        if (string.IsNullOrWhiteSpace(options.SenderId))
        {
            throw new InvalidOperationException("Mailgun invitation delivery requires a non-empty sender id.");
        }

        if (options.TryGetBaseUrl() is null)
        {
            throw new InvalidOperationException("Mailgun invitation delivery requires an absolute HTTP or HTTPS base URL.");
        }

        if (string.IsNullOrWhiteSpace(options.DomainName))
        {
            throw new InvalidOperationException("Mailgun invitation delivery requires a sending domain name.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("Mailgun invitation delivery requires an API key.");
        }

        if (string.IsNullOrWhiteSpace(options.FromEmail) ||
            !MailgunInvitationDeliveryAddress.TryCreate(options.FromEmail, options.FromName, out _))
        {
            throw new InvalidOperationException("Mailgun invitation delivery requires a valid from email address.");
        }

        services.TryAddSingleton(options);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, MailgunInvitationDeliveryDiagnosticsConventionContributor>());
        services.TryAddSingleton<IMailgunInvitationDeliveryClient, MailgunInvitationDeliveryClient>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantInvitationDeliverySender, MailgunInvitationDeliverySender>());

        services.AddHttpClient(HttpClientName)
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan);

        return services;
    }
}
