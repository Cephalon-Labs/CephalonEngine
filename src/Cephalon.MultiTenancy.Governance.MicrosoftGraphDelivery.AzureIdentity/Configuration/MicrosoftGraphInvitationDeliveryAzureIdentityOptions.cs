using Azure.Identity;
using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity.Configuration;

/// <summary>
/// Configures Azure Identity token acquisition for Microsoft Graph tenant-invitation delivery.
/// </summary>
/// <remarks>
/// This companion package only supplies a Microsoft Graph bearer token through <c>Azure.Identity</c>. The Microsoft
/// Graph sender still owns the <c>sendMail</c> request, while Microsoft Entra application registration, permissions,
/// mailbox access policy, and credential lifecycle remain outside the Cephalon engine boundary.
/// </remarks>
public sealed class MicrosoftGraphInvitationDeliveryAzureIdentityOptions
{
    private static readonly string[] DefaultScopes = ["https://graph.microsoft.com/.default"];

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftGraphInvitationDeliveryAzureIdentityOptions" /> class.
    /// </summary>
    public MicrosoftGraphInvitationDeliveryAzureIdentityOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Azure Identity token provider should be registered.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the Microsoft Graph scopes requested from the configured Azure credential.
    /// </summary>
    /// <remarks>
    /// The default is <c>https://graph.microsoft.com/.default</c>, which asks Microsoft Entra ID for the app's
    /// configured application permissions such as Microsoft Graph <c>Mail.Send</c>.
    /// </remarks>
    public IReadOnlyList<string> Scopes { get; set; } = DefaultScopes;

    /// <summary>
    /// Gets or sets the Microsoft Entra tenant id used by <see cref="DefaultAzureCredential" />.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the client id for a user-assigned managed identity.
    /// </summary>
    /// <remarks>
    /// Leave this value empty to allow <see cref="DefaultAzureCredential" /> to use a system-assigned managed identity
    /// or another credential source from its chain.
    /// </remarks>
    public string? ManagedIdentityClientId { get; set; }

    /// <summary>
    /// Gets or sets the Microsoft Entra authority host used for token acquisition.
    /// </summary>
    /// <remarks>
    /// Supported aliases are <c>AzurePublicCloud</c>, <c>AzureGovernment</c>, and <c>AzureChina</c>. Hosts can also
    /// provide an absolute HTTPS authority URI for a sovereign or private cloud.
    /// </remarks>
    public string? AuthorityHost { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether interactive browser authentication should be excluded.
    /// </summary>
    /// <remarks>
    /// The default is <see langword="true" /> so production hosts do not accidentally launch browser prompts.
    /// </remarks>
    public bool ExcludeInteractiveBrowserCredential { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether managed identity authentication should be excluded.
    /// </summary>
    /// <remarks>
    /// The default keeps managed identity available because hosted Azure, workload identity, and service-style
    /// invitation delivery are the primary production scenarios for this package.
    /// </remarks>
    public bool ExcludeManagedIdentityCredential { get; set; }

    /// <summary>
    /// Binds Azure Identity token-provider options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound Azure Identity token-provider options.</returns>
    public static MicrosoftGraphInvitationDeliveryAzureIdentityOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("MultiTenancy")
            .GetSection("Governance")
            .GetSection("MicrosoftGraphInvitationDelivery")
            .GetSection("AzureIdentity");

        return new MicrosoftGraphInvitationDeliveryAzureIdentityOptions
        {
            Enabled = GetBoolean(section["Enabled"], defaultValue: true),
            Scopes = ParseStringList(section.GetSection("Scopes"), DefaultScopes),
            TenantId = section["TenantId"]?.Trim(),
            ManagedIdentityClientId = section["ManagedIdentityClientId"]?.Trim(),
            AuthorityHost = section["AuthorityHost"]?.Trim(),
            ExcludeInteractiveBrowserCredential = GetBoolean(section["ExcludeInteractiveBrowserCredential"], defaultValue: true),
            ExcludeManagedIdentityCredential = GetBoolean(section["ExcludeManagedIdentityCredential"], defaultValue: false)
        };
    }

    internal string[] GetScopes()
    {
        var scopes = Scopes
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return scopes.Length == 0 ? DefaultScopes : scopes;
    }

    internal DefaultAzureCredentialOptions CreateDefaultAzureCredentialOptions()
    {
        var options = new DefaultAzureCredentialOptions
        {
            ExcludeInteractiveBrowserCredential = ExcludeInteractiveBrowserCredential,
            ExcludeManagedIdentityCredential = ExcludeManagedIdentityCredential
        };

        if (!string.IsNullOrWhiteSpace(TenantId))
        {
            options.TenantId = TenantId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(ManagedIdentityClientId))
        {
            options.ManagedIdentityClientId = ManagedIdentityClientId.Trim();
        }

        var authorityHost = TryGetAuthorityHost();
        if (authorityHost is not null)
        {
            options.AuthorityHost = authorityHost;
        }

        return options;
    }

    internal Uri? TryGetAuthorityHost()
    {
        if (string.IsNullOrWhiteSpace(AuthorityHost))
        {
            return null;
        }

        var value = AuthorityHost.Trim();
        return value.ToLowerInvariant() switch
        {
            "azurepubliccloud" or "azure-public-cloud" or "public" or "global" => AzureAuthorityHosts.AzurePublicCloud,
            "azuregovernment" or "azure-government" or "government" or "usgovernment" or "us-government" => AzureAuthorityHosts.AzureGovernment,
            "azurechina" or "azure-china" or "china" => AzureAuthorityHosts.AzureChina,
            _ => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? uri : null
        };
    }

    private static bool GetBoolean(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static string[] ParseStringList(IConfigurationSection section, IReadOnlyList<string> defaultValues)
    {
        var parsed = ParseConfiguredStringList(section);
        return parsed.Length > 0 || section.Exists()
            ? parsed
            : defaultValues
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }

    private static string[] ParseConfiguredStringList(IConfigurationSection section)
    {
        if (!string.IsNullOrWhiteSpace(section.Value))
        {
            return section.Value!
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return section
            .GetChildren()
            .Select(static child => child.Value?.Trim())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
