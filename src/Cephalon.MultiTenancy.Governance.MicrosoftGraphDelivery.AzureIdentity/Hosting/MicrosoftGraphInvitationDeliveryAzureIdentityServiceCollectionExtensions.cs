using Azure.Core;
using Azure.Identity;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity.Configuration;
using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity.Services;
using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity.Hosting;

/// <summary>
/// Adds Azure Identity token acquisition for Microsoft Graph invitation delivery.
/// </summary>
public static class MicrosoftGraphInvitationDeliveryAzureIdentityServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Azure Identity token provider using configuration as the primary source of token-acquisition settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">An optional callback that can extend or override configuration-driven settings.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<MicrosoftGraphInvitationDeliveryAzureIdentityOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = MicrosoftGraphInvitationDeliveryAzureIdentityOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(options, credential: null);
    }

    /// <summary>
    /// Adds the Azure Identity token provider using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures token acquisition.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(
        this IServiceCollection services,
        Action<MicrosoftGraphInvitationDeliveryAzureIdentityOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new MicrosoftGraphInvitationDeliveryAzureIdentityOptions();
        configure?.Invoke(options);

        return services.AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(options, credential: null);
    }

    /// <summary>
    /// Adds the Azure Identity token provider with an explicit credential instance.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="credential">The credential used to acquire Microsoft Graph access tokens.</param>
    /// <param name="configure">An optional callback that configures token acquisition.</param>
    /// <returns>The same service collection for further registration.</returns>
    /// <remarks>
    /// This overload is useful for tests, shared host credential factories, or applications that want to provide a
    /// specific <see cref="TokenCredential" /> such as <see cref="ManagedIdentityCredential" />.
    /// </remarks>
    public static IServiceCollection AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(
        this IServiceCollection services,
        TokenCredential credential,
        Action<MicrosoftGraphInvitationDeliveryAzureIdentityOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(credential);

        var options = new MicrosoftGraphInvitationDeliveryAzureIdentityOptions();
        configure?.Invoke(options);

        return services.AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(options, credential);
    }

    private static IServiceCollection AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(
        this IServiceCollection services,
        MicrosoftGraphInvitationDeliveryAzureIdentityOptions options,
        TokenCredential? credential)
    {
        if (!options.Enabled)
        {
            return services;
        }

        if (options.GetScopes().Length == 0)
        {
            throw new InvalidOperationException("Microsoft Graph Azure Identity token acquisition requires at least one scope.");
        }

        if (!string.IsNullOrWhiteSpace(options.AuthorityHost) && options.TryGetAuthorityHost() is null)
        {
            throw new InvalidOperationException("Microsoft Graph Azure Identity token acquisition requires an HTTPS authority host or a supported Azure authority alias.");
        }

        services.Replace(ServiceDescriptor.Singleton(options));
        services.Replace(ServiceDescriptor.Singleton(provider =>
        {
            var registeredOptions = provider.GetRequiredService<MicrosoftGraphInvitationDeliveryAzureIdentityOptions>();
            var resolvedCredential = credential ?? new DefaultAzureCredential(registeredOptions.CreateDefaultAzureCredentialOptions());
            return new MicrosoftGraphInvitationDeliveryAzureIdentityCredentialSource(resolvedCredential);
        }));
        services.Replace(ServiceDescriptor.Singleton<IMicrosoftGraphInvitationDeliveryAccessTokenProvider, MicrosoftGraphInvitationDeliveryAzureIdentityAccessTokenProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, MicrosoftGraphInvitationDeliveryAzureIdentityDiagnosticsConventionContributor>());

        return services;
    }
}
