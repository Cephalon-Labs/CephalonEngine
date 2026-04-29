using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.Services;
using Cephalon.MultiTenancy.Governance.SmtpDelivery.Configuration;
using Cephalon.MultiTenancy.Governance.SmtpDelivery.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.MultiTenancy.Governance.SmtpDelivery.Hosting;

/// <summary>
/// Adds SMTP relay invitation delivery services to a Cephalon host.
/// </summary>
public static class SmtpInvitationDeliveryServiceCollectionExtensions
{
    /// <summary>
    /// Adds SMTP invitation delivery using configuration as the primary source of relay settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">An optional callback that can extend or override the configuration-driven setup.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonSmtpInvitationDelivery(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<SmtpInvitationDeliveryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = SmtpInvitationDeliveryOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonSmtpInvitationDelivery(options);
    }

    /// <summary>
    /// Adds SMTP invitation delivery using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures the SMTP invitation delivery sender.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonSmtpInvitationDelivery(
        this IServiceCollection services,
        Action<SmtpInvitationDeliveryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SmtpInvitationDeliveryOptions();
        configure?.Invoke(options);

        return services.AddCephalonSmtpInvitationDelivery(options);
    }

    private static IServiceCollection AddCephalonSmtpInvitationDelivery(
        this IServiceCollection services,
        SmtpInvitationDeliveryOptions options)
    {
        if (!options.Enabled)
        {
            return services;
        }

        if (string.IsNullOrWhiteSpace(options.SenderId))
        {
            throw new InvalidOperationException("SMTP invitation delivery requires a non-empty sender id.");
        }

        if (string.IsNullOrWhiteSpace(options.Host))
        {
            throw new InvalidOperationException("SMTP invitation delivery requires an SMTP host.");
        }

        if (string.IsNullOrWhiteSpace(options.FromAddress) ||
            !SmtpInvitationDeliveryAddress.TryCreate(options.FromAddress, options.FromDisplayName, out _))
        {
            throw new InvalidOperationException("SMTP invitation delivery requires a valid from address.");
        }

        services.TryAddSingleton(options);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, SmtpInvitationDeliveryDiagnosticsConventionContributor>());
        services.TryAddSingleton<ISmtpInvitationDeliveryClient, SmtpInvitationDeliveryClient>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantInvitationDeliverySender, SmtpInvitationDeliverySender>());

        return services;
    }
}
