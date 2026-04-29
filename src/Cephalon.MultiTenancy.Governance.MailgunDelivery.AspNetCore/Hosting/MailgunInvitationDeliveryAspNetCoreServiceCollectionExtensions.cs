using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Configuration;
using Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Hosting;

/// <summary>
/// Registers ASP.NET Core Mailgun webhook translation services for tenant-invitation delivery status callbacks.
/// </summary>
public static class MailgunInvitationDeliveryAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds Mailgun webhook callback translation services using configuration as the primary setup source.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration">The optional configuration root.</param>
    /// <param name="configure">An optional callback that can extend or override configuration-driven options.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonMailgunInvitationDeliveryAspNetCore(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<MailgunInvitationDeliveryAspNetCoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = MailgunInvitationDeliveryAspNetCoreOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        services.RemoveAll<MailgunInvitationDeliveryAspNetCoreOptions>();
        services.AddSingleton(options);
        services.TryAddSingleton<MailgunInvitationDeliveryStatusCallbackRuntimeCatalog>();
        services.TryAddSingleton<MailgunInvitationDeliveryStatusCallbackReplayGuard>();
        services.TryAddSingleton<MailgunWebhookDeliveryStatusMapper>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, MailgunInvitationDeliveryStatusRuntimeSurfaceContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, MailgunInvitationDeliveryAspNetCoreDiagnosticsConventionContributor>());
        return services;
    }
}
