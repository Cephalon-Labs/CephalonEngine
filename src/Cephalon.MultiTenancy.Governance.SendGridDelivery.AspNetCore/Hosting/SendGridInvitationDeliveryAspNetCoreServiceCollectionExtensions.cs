using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Configuration;
using Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Hosting;

/// <summary>
/// Registers ASP.NET Core SendGrid Event Webhook translation services for tenant-invitation delivery status callbacks.
/// </summary>
public static class SendGridInvitationDeliveryAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds SendGrid Event Webhook callback translation services using configuration as the primary setup source.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration">The optional configuration root.</param>
    /// <param name="configure">An optional callback that can extend or override configuration-driven options.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonSendGridInvitationDeliveryAspNetCore(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<SendGridInvitationDeliveryAspNetCoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = SendGridInvitationDeliveryAspNetCoreOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        services.RemoveAll<SendGridInvitationDeliveryAspNetCoreOptions>();
        services.AddSingleton(options);
        services.TryAddSingleton<SendGridInvitationDeliveryStatusCallbackRuntimeCatalog>();
        services.TryAddSingleton<SendGridEventWebhookDeliveryStatusMapper>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, SendGridInvitationDeliveryStatusRuntimeSurfaceContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, SendGridInvitationDeliveryAspNetCoreDiagnosticsConventionContributor>());
        return services;
    }
}
