using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.SendGridDelivery.Configuration;
using Cephalon.MultiTenancy.Governance.SendGridDelivery.Services;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.Hosting;

/// <summary>
/// Adds SendGrid Mail Send API invitation delivery services to a Cephalon host.
/// </summary>
public static class SendGridInvitationDeliveryServiceCollectionExtensions
{
    /// <summary>
    /// The named HTTP client used by the SendGrid invitation delivery client.
    /// </summary>
    public const string HttpClientName = "Cephalon.MultiTenancy.Governance.SendGridDelivery";

    /// <summary>
    /// Adds SendGrid invitation delivery using configuration as the primary source of Mail Send API settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">An optional callback that can extend or override the configuration-driven setup.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonSendGridInvitationDelivery(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<SendGridInvitationDeliveryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = SendGridInvitationDeliveryOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonSendGridInvitationDelivery(options);
    }

    /// <summary>
    /// Adds SendGrid invitation delivery using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures the SendGrid invitation delivery sender.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonSendGridInvitationDelivery(
        this IServiceCollection services,
        Action<SendGridInvitationDeliveryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SendGridInvitationDeliveryOptions();
        configure?.Invoke(options);

        return services.AddCephalonSendGridInvitationDelivery(options);
    }

    private static IServiceCollection AddCephalonSendGridInvitationDelivery(
        this IServiceCollection services,
        SendGridInvitationDeliveryOptions options)
    {
        if (!options.Enabled)
        {
            return services;
        }

        if (string.IsNullOrWhiteSpace(options.SenderId))
        {
            throw new InvalidOperationException("SendGrid invitation delivery requires a non-empty sender id.");
        }

        if (options.TryGetBaseUrl() is null)
        {
            throw new InvalidOperationException("SendGrid invitation delivery requires an absolute HTTP or HTTPS base URL.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("SendGrid invitation delivery requires an API key.");
        }

        if (string.IsNullOrWhiteSpace(options.FromEmail) ||
            !SendGridInvitationDeliveryAddress.TryCreate(options.FromEmail, options.FromName, out _))
        {
            throw new InvalidOperationException("SendGrid invitation delivery requires a valid from email address.");
        }

        services.TryAddSingleton(options);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, SendGridInvitationDeliveryDiagnosticsConventionContributor>());
        services.TryAddSingleton<ISendGridInvitationDeliveryClient, SendGridInvitationDeliveryClient>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantInvitationDeliverySender, SendGridInvitationDeliverySender>());

        services.AddHttpClient(HttpClientName)
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan);

        return services;
    }
}
