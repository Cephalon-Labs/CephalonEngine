using Cephalon.Engine.Configuration;
using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Services;
using Microsoft.Extensions.Configuration;

namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Configuration;

/// <summary>
/// Configures Microsoft Graph <c>sendMail</c> delivery for tenant invitations dispatched by the governance companion pack.
/// </summary>
/// <remarks>
/// The Microsoft Graph sender posts one JSON <c>sendMail</c> request to Microsoft Graph. It does not own OAuth credential
/// issuance, mailbox provisioning, Graph change notifications, provider polling, public onboarding, SMS, chat, CRM, or
/// identity-provider invitation flows.
/// </remarks>
public sealed class MicrosoftGraphInvitationDeliveryOptions
{
    private static readonly int[] DefaultAcceptedStatusCodes = [202];

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftGraphInvitationDeliveryOptions" /> class.
    /// </summary>
    public MicrosoftGraphInvitationDeliveryOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Microsoft Graph invitation sender should be registered.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the sender identifier used by <c>TenantInvitationDeliveryRequest.SenderId</c>.
    /// </summary>
    public string SenderId { get; set; } = "microsoft-graph-email";

    /// <summary>
    /// Gets or sets the Microsoft Graph API base URL.
    /// </summary>
    /// <remarks>
    /// The default targets the global Microsoft Graph cloud. Sovereign-cloud hosts can set this to the appropriate
    /// Graph endpoint while keeping the same <c>sendMail</c> request contract.
    /// </remarks>
    public string BaseUrl { get; set; } = "https://graph.microsoft.com";

    /// <summary>
    /// Gets or sets the Microsoft Graph API version segment.
    /// </summary>
    /// <remarks>
    /// The default is <c>v1.0</c>. Preview or beta endpoints should be used only by hosts that deliberately accept that
    /// external API stability posture.
    /// </remarks>
    public string ApiVersion { get; set; } = "v1.0";

    /// <summary>
    /// Gets or sets the mailbox user id or user principal name used in <c>/users/{id | userPrincipalName}/sendMail</c>.
    /// </summary>
    /// <remarks>
    /// When omitted, the sender posts to <c>/me/sendMail</c>. Application-permission hosts normally configure this value
    /// and provide a token with Microsoft Graph <c>Mail.Send</c> permission.
    /// </remarks>
    public string? SenderUserId { get; set; }

    /// <summary>
    /// Gets or sets an optional static Microsoft Graph bearer token.
    /// </summary>
    /// <remarks>
    /// Production hosts should usually register <see cref="IMicrosoftGraphInvitationDeliveryAccessTokenProvider" />
    /// instead of storing a short-lived token in configuration.
    /// </remarks>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the metadata key used to resolve the recipient email address when the invitee id is not an email address.
    /// </summary>
    /// <remarks>
    /// The sender checks dispatch metadata first and invitation metadata second. If neither contains a value and
    /// <c>InviteeKind</c> is <c>email</c>, the invitee id is treated as the recipient address.
    /// </remarks>
    public string RecipientEmailMetadataKey { get; set; } = "email";

    /// <summary>
    /// Gets or sets delivery channels accepted by this sender.
    /// </summary>
    /// <remarks>
    /// When empty, the sender accepts every requested channel.
    /// </remarks>
    public IReadOnlyList<string> SupportedChannels { get; set; } = ["email"];

    /// <summary>
    /// Gets or sets the Microsoft Graph message subject template.
    /// </summary>
    public string SubjectTemplate { get; set; } = "Tenant invitation for {tenantId}";

    /// <summary>
    /// Gets or sets the plain-text Microsoft Graph message body template.
    /// </summary>
    public string TextBodyTemplate { get; set; } =
        "You have been invited to tenant {tenantId}.\n\nInvitation: {invitationId}\nInvitee: {displayName}\nRoles: {roles}\nCorrelation: {correlationId}";

    /// <summary>
    /// Gets or sets the optional HTML Microsoft Graph message body template.
    /// </summary>
    /// <remarks>
    /// When configured, the sender uses a Graph message body with <c>contentType = HTML</c>. Otherwise it sends a plain
    /// text body.
    /// </remarks>
    public string? HtmlBodyTemplate { get; set; }

    /// <summary>
    /// Gets or sets Microsoft Graph message categories added to each request.
    /// </summary>
    public IReadOnlyList<string> Categories { get; set; } = ["cephalon-invitation"];

    /// <summary>
    /// Gets or sets custom Microsoft Graph internet message headers added to each request.
    /// </summary>
    /// <remarks>
    /// The sender keeps only single-line custom <c>x-*</c> headers and filters message-core headers before sending.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets a value indicating whether safe Cephalon context headers should be added to the Graph message.
    /// </summary>
    public bool IncludeContextHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether Microsoft Graph should save the message to Sent Items.
    /// </summary>
    /// <remarks>
    /// The default is <see langword="false" /> so service-style invitation dispatch does not fill the sender mailbox by
    /// default. Hosts can opt in when mailbox history is part of their compliance posture.
    /// </remarks>
    public bool SaveToSentItems { get; set; }

    /// <summary>
    /// Gets or sets response status codes that indicate Microsoft Graph accepted the request.
    /// </summary>
    /// <remarks>
    /// The default accepts <c>202 Accepted</c>, which means Graph accepted the send request but not that downstream mail
    /// delivery has completed.
    /// </remarks>
    public IReadOnlyList<int> AcceptedStatusCodes { get; set; } = DefaultAcceptedStatusCodes;

    /// <summary>
    /// Gets or sets the maximum time allowed for the Microsoft Graph API request.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Binds Microsoft Graph invitation delivery options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound Microsoft Graph invitation delivery options.</returns>
    public static MicrosoftGraphInvitationDeliveryOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("MultiTenancy")
            .GetSection("Governance")
            .GetSection("MicrosoftGraphInvitationDelivery");

        return new MicrosoftGraphInvitationDeliveryOptions
        {
            Enabled = GetBoolean(section["Enabled"], defaultValue: true),
            SenderId = section["SenderId"]?.Trim() ?? "microsoft-graph-email",
            BaseUrl = section["BaseUrl"]?.Trim() ?? "https://graph.microsoft.com",
            ApiVersion = section["ApiVersion"]?.Trim() ?? "v1.0",
            SenderUserId = section["SenderUserId"]?.Trim(),
            AccessToken = section["AccessToken"],
            RecipientEmailMetadataKey = section["RecipientEmailMetadataKey"]?.Trim() ?? "email",
            SupportedChannels = ParseStringList(section.GetSection("SupportedChannels"), ["email"]),
            SubjectTemplate = section["SubjectTemplate"] ?? "Tenant invitation for {tenantId}",
            TextBodyTemplate = section["TextBodyTemplate"] ??
                "You have been invited to tenant {tenantId}.\n\nInvitation: {invitationId}\nInvitee: {displayName}\nRoles: {roles}\nCorrelation: {correlationId}",
            HtmlBodyTemplate = section["HtmlBodyTemplate"],
            Categories = ParseStringList(section.GetSection("Categories"), ["cephalon-invitation"]),
            Headers = ParseDictionary(section.GetSection("Headers")),
            IncludeContextHeaders = GetBoolean(section["IncludeContextHeaders"], defaultValue: true),
            SaveToSentItems = GetBoolean(section["SaveToSentItems"], defaultValue: false),
            AcceptedStatusCodes = ParseInt32List(section.GetSection("AcceptedStatusCodes"), DefaultAcceptedStatusCodes),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 10)
        };
    }

    internal Uri? TryGetBaseUrl()
    {
        return Uri.TryCreate(BaseUrl?.Trim(), UriKind.Absolute, out var baseUrl) &&
            (baseUrl.Scheme == Uri.UriSchemeHttp || baseUrl.Scheme == Uri.UriSchemeHttps)
            ? baseUrl
            : null;
    }

    internal Uri GetSendMailEndpoint()
    {
        var baseUrl = TryGetBaseUrl() ?? new Uri("https://graph.microsoft.com", UriKind.Absolute);
        var apiVersion = GetApiVersion();

        if (string.IsNullOrWhiteSpace(SenderUserId))
        {
            return new Uri(baseUrl, $"/{apiVersion}/me/sendMail");
        }

        return new Uri(baseUrl, $"/{apiVersion}/users/{Uri.EscapeDataString(SenderUserId.Trim())}/sendMail");
    }

    internal TimeSpan GetTimeout() => TimeSpan.FromSeconds(Math.Clamp(TimeoutSeconds, 1, 300));

    internal string GetApiVersion() => string.IsNullOrWhiteSpace(ApiVersion) ? "v1.0" : ApiVersion.Trim().Trim('/');

    internal string GetSenderMailboxScope() => string.IsNullOrWhiteSpace(SenderUserId) ? "me" : SenderUserId.Trim();

    internal int[] GetAcceptedStatusCodes()
    {
        return AcceptedStatusCodes
            .Where(static statusCode => statusCode is >= 100 and <= 599)
            .Distinct()
            .Order()
            .ToArray();
    }

    private static bool GetBoolean(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static int GetInt32(string? value, int defaultValue)
    {
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static int[] ParseInt32List(IConfigurationSection section, IReadOnlyList<int>? defaultValues = null)
    {
        var parsed = ParseConfiguredInt32List(section);
        return parsed.Length > 0 || section.Exists() || defaultValues is null
            ? parsed
            : defaultValues.ToArray();
    }

    private static int[] ParseConfiguredInt32List(IConfigurationSection section)
    {
        if (!string.IsNullOrWhiteSpace(section.Value))
        {
            return section.Value!
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(static value => int.TryParse(value, out var parsed) ? parsed : -1)
                .Where(static value => value is >= 100 and <= 599)
                .Distinct()
                .Order()
                .ToArray();
        }

        return section
            .GetChildren()
            .Select(static child => int.TryParse(child.Value, out var parsed) ? parsed : -1)
            .Where(static value => value is >= 100 and <= 599)
            .Distinct()
            .Order()
            .ToArray();
    }

    private static string[] ParseStringList(IConfigurationSection section, IReadOnlyList<string>? defaultValues = null)
    {
        var parsed = ParseConfiguredStringList(section);
        return parsed.Length > 0 || section.Exists() || defaultValues is null
            ? parsed
            : defaultValues
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
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
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return section
            .GetChildren()
            .Select(static child => child.Value?.Trim())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Dictionary<string, string> ParseDictionary(IConfigurationSection section)
    {
        return section
            .GetChildren()
            .Where(static child => !string.IsNullOrWhiteSpace(child.Key) && !string.IsNullOrWhiteSpace(child.Value))
            .ToDictionary(
                static child => child.Key.Trim(),
                static child => child.Value!,
                StringComparer.OrdinalIgnoreCase);
    }
}
