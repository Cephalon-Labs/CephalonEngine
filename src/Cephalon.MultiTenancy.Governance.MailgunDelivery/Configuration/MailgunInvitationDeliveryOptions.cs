using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.Configuration;

/// <summary>
/// Configures Mailgun Messages API delivery for tenant invitations dispatched by the governance companion pack.
/// </summary>
/// <remarks>
/// The Mailgun sender posts one multipart Messages API request to Mailgun. It does not own Mailgun webhook callbacks,
/// bounce translation, provider polling, public onboarding, SMS, chat, CRM, or identity-provider invitation flows.
/// </remarks>
public sealed class MailgunInvitationDeliveryOptions
{
    private static readonly int[] DefaultAcceptedStatusCodes = [200];

    /// <summary>
    /// Initializes a new instance of the <see cref="MailgunInvitationDeliveryOptions" /> class.
    /// </summary>
    public MailgunInvitationDeliveryOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Mailgun invitation sender should be registered.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the sender identifier used by <c>TenantInvitationDeliveryRequest.SenderId</c>.
    /// </summary>
    public string SenderId { get; set; } = "mailgun-email";

    /// <summary>
    /// Gets or sets the Mailgun API base URL.
    /// </summary>
    /// <remarks>
    /// The default is the US endpoint. Hosts that use EU regional sending can set this value to
    /// <c>https://api.eu.mailgun.net</c>.
    /// </remarks>
    public string BaseUrl { get; set; } = "https://api.mailgun.net";

    /// <summary>
    /// Gets or sets the Mailgun sending domain name used in the Messages API route.
    /// </summary>
    public string? DomainName { get; set; }

    /// <summary>
    /// Gets or sets the Mailgun private API key used with basic authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the sender email address used in the Mailgun message.
    /// </summary>
    public string? FromEmail { get; set; }

    /// <summary>
    /// Gets or sets the optional sender display name used in the Mailgun message.
    /// </summary>
    public string? FromName { get; set; } = "Cephalon";

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
    /// Gets or sets the Mailgun message subject template.
    /// </summary>
    public string SubjectTemplate { get; set; } = "Tenant invitation for {tenantId}";

    /// <summary>
    /// Gets or sets the plain-text Mailgun message body template.
    /// </summary>
    public string TextBodyTemplate { get; set; } =
        "You have been invited to tenant {tenantId}.\n\nInvitation: {invitationId}\nInvitee: {displayName}\nRoles: {roles}\nCorrelation: {correlationId}";

    /// <summary>
    /// Gets or sets the optional HTML Mailgun message body template.
    /// </summary>
    public string? HtmlBodyTemplate { get; set; }

    /// <summary>
    /// Gets or sets Mailgun message tags added through <c>o:tag</c> form fields.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = ["cephalon-invitation"];

    /// <summary>
    /// Gets or sets Mailgun user variables added through <c>v:*</c> form fields.
    /// </summary>
    /// <remarks>
    /// These values are sent to Mailgun and may appear in provider activity or webhook data. Do not put secrets here.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Variables { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets additional Mailgun custom headers added through <c>h:*</c> form fields.
    /// </summary>
    /// <remarks>
    /// The sender filters empty, multi-line, and message-core headers before sending.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets a value indicating whether safe Cephalon user variables should be added to the Mailgun request.
    /// </summary>
    public bool IncludeContextVariables { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether safe Cephalon custom headers should be added to the Mailgun request.
    /// </summary>
    public bool IncludeContextHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether Mailgun test mode should be enabled through <c>o:testmode=yes</c>.
    /// </summary>
    public bool EnableTestMode { get; set; }

    /// <summary>
    /// Gets or sets response status codes that indicate the Mailgun API accepted the request.
    /// </summary>
    /// <remarks>
    /// The default accepts <c>200 OK</c>, which Mailgun returns when a message is queued.
    /// </remarks>
    public IReadOnlyList<int> AcceptedStatusCodes { get; set; } = DefaultAcceptedStatusCodes;

    /// <summary>
    /// Gets or sets the JSON property name that contains the Mailgun provider message identifier.
    /// </summary>
    public string ProviderMessageIdJsonPropertyName { get; set; } = "id";

    /// <summary>
    /// Gets or sets the maximum time allowed for the Mailgun API request.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Binds Mailgun invitation delivery options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound Mailgun invitation delivery options.</returns>
    public static MailgunInvitationDeliveryOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("MultiTenancy")
            .GetSection("Governance")
            .GetSection("MailgunInvitationDelivery");

        return new MailgunInvitationDeliveryOptions
        {
            Enabled = GetBoolean(section["Enabled"], defaultValue: true),
            SenderId = section["SenderId"]?.Trim() ?? "mailgun-email",
            BaseUrl = section["BaseUrl"]?.Trim() ?? "https://api.mailgun.net",
            DomainName = section["DomainName"]?.Trim(),
            ApiKey = section["ApiKey"],
            FromEmail = section["FromEmail"]?.Trim(),
            FromName = section["FromName"]?.Trim(),
            RecipientEmailMetadataKey = section["RecipientEmailMetadataKey"]?.Trim() ?? "email",
            SupportedChannels = ParseStringList(section.GetSection("SupportedChannels"), ["email"]),
            SubjectTemplate = section["SubjectTemplate"] ?? "Tenant invitation for {tenantId}",
            TextBodyTemplate = section["TextBodyTemplate"] ??
                "You have been invited to tenant {tenantId}.\n\nInvitation: {invitationId}\nInvitee: {displayName}\nRoles: {roles}\nCorrelation: {correlationId}",
            HtmlBodyTemplate = section["HtmlBodyTemplate"],
            Tags = ParseStringList(section.GetSection("Tags"), ["cephalon-invitation"]),
            Variables = ParseDictionary(section.GetSection("Variables")),
            Headers = ParseDictionary(section.GetSection("Headers")),
            IncludeContextVariables = GetBoolean(section["IncludeContextVariables"], defaultValue: true),
            IncludeContextHeaders = GetBoolean(section["IncludeContextHeaders"], defaultValue: true),
            EnableTestMode = GetBoolean(section["EnableTestMode"], defaultValue: false),
            AcceptedStatusCodes = ParseInt32List(section.GetSection("AcceptedStatusCodes"), DefaultAcceptedStatusCodes),
            ProviderMessageIdJsonPropertyName = section["ProviderMessageIdJsonPropertyName"]?.Trim() ?? "id",
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

    internal Uri GetMessagesEndpoint()
    {
        var baseUrl = TryGetBaseUrl() ?? new Uri("https://api.mailgun.net", UriKind.Absolute);
        var escapedDomain = Uri.EscapeDataString(GetDomainName());
        return new Uri(baseUrl, $"/v3/{escapedDomain}/messages");
    }

    internal TimeSpan GetTimeout() => TimeSpan.FromSeconds(Math.Clamp(TimeoutSeconds, 1, 300));

    internal string GetDomainName() => string.IsNullOrWhiteSpace(DomainName) ? string.Empty : DomainName.Trim();

    internal string GetProviderMessageIdJsonPropertyName()
    {
        return string.IsNullOrWhiteSpace(ProviderMessageIdJsonPropertyName)
            ? "id"
            : ProviderMessageIdJsonPropertyName.Trim();
    }

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
