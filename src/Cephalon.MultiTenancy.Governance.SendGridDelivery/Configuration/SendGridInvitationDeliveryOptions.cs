using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.Configuration;

/// <summary>
/// Configures SendGrid Mail Send API delivery for tenant invitations dispatched by the governance companion pack.
/// </summary>
/// <remarks>
/// The SendGrid sender sends one transactional email request to the SendGrid Mail Send API. It does not own SendGrid
/// Event Webhook callbacks, bounce translation, provider polling, public onboarding, SMS, chat, CRM, or identity-provider
/// invitation flows.
/// </remarks>
public sealed class SendGridInvitationDeliveryOptions
{
    private static readonly int[] DefaultAcceptedStatusCodes = [202];

    /// <summary>
    /// Initializes a new instance of the <see cref="SendGridInvitationDeliveryOptions" /> class.
    /// </summary>
    public SendGridInvitationDeliveryOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the SendGrid invitation sender should be registered.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the sender identifier used by <c>TenantInvitationDeliveryRequest.SenderId</c>.
    /// </summary>
    public string SenderId { get; set; } = "sendgrid-email";

    /// <summary>
    /// Gets or sets the SendGrid v3 API base URL.
    /// </summary>
    /// <remarks>
    /// The default is the global SendGrid endpoint. Hosts that use EU regional sending can set this value to
    /// <c>https://api.eu.sendgrid.com</c>.
    /// </remarks>
    public string BaseUrl { get; set; } = "https://api.sendgrid.com";

    /// <summary>
    /// Gets or sets the SendGrid API key used as the bearer token.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the verified sender email address used in the SendGrid message.
    /// </summary>
    public string? FromEmail { get; set; }

    /// <summary>
    /// Gets or sets the optional sender display name used in the SendGrid message.
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
    /// Gets or sets the SendGrid message subject template.
    /// </summary>
    public string SubjectTemplate { get; set; } = "Tenant invitation for {tenantId}";

    /// <summary>
    /// Gets or sets the plain-text SendGrid message body template.
    /// </summary>
    public string TextBodyTemplate { get; set; } =
        "You have been invited to tenant {tenantId}.\n\nInvitation: {invitationId}\nInvitee: {displayName}\nRoles: {roles}\nCorrelation: {correlationId}";

    /// <summary>
    /// Gets or sets the optional HTML SendGrid message body template.
    /// </summary>
    public string? HtmlBodyTemplate { get; set; }

    /// <summary>
    /// Gets or sets SendGrid message categories added to each request.
    /// </summary>
    /// <remarks>
    /// SendGrid allows up to ten category names. Values beyond that limit are ignored by the sender.
    /// </remarks>
    public IReadOnlyList<string> Categories { get; set; } = ["cephalon-invitation"];

    /// <summary>
    /// Gets or sets SendGrid custom arguments added to each personalization.
    /// </summary>
    /// <remarks>
    /// These values are sent to SendGrid and may appear in provider activity or webhook data. Do not put secrets here.
    /// </remarks>
    public IReadOnlyDictionary<string, string> CustomArgs { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets additional SendGrid message headers added to each request.
    /// </summary>
    /// <remarks>
    /// The sender filters empty, multi-line, and SendGrid-reserved headers before sending.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets a value indicating whether safe Cephalon custom arguments should be added to the SendGrid request.
    /// </summary>
    public bool IncludeContextCustomArgs { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether safe Cephalon context headers should be added to the SendGrid request.
    /// </summary>
    public bool IncludeContextHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether SendGrid sandbox mode should be enabled.
    /// </summary>
    public bool EnableSandboxMode { get; set; }

    /// <summary>
    /// Gets or sets response status codes that indicate the SendGrid API accepted the request.
    /// </summary>
    /// <remarks>
    /// The default accepts <c>202 Accepted</c>. When sandbox mode is enabled, <c>200 OK</c> is also accepted.
    /// </remarks>
    public IReadOnlyList<int> AcceptedStatusCodes { get; set; } = DefaultAcceptedStatusCodes;

    /// <summary>
    /// Gets or sets the SendGrid response header that contains the provider message identifier.
    /// </summary>
    public string ProviderMessageIdHeaderName { get; set; } = "X-Message-ID";

    /// <summary>
    /// Gets or sets the maximum time allowed for the SendGrid API request.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Binds SendGrid invitation delivery options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound SendGrid invitation delivery options.</returns>
    public static SendGridInvitationDeliveryOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("MultiTenancy")
            .GetSection("Governance")
            .GetSection("SendGridInvitationDelivery");

        return new SendGridInvitationDeliveryOptions
        {
            Enabled = GetBoolean(section["Enabled"], defaultValue: true),
            SenderId = section["SenderId"]?.Trim() ?? "sendgrid-email",
            BaseUrl = section["BaseUrl"]?.Trim() ?? "https://api.sendgrid.com",
            ApiKey = section["ApiKey"],
            FromEmail = section["FromEmail"]?.Trim(),
            FromName = section["FromName"]?.Trim(),
            RecipientEmailMetadataKey = section["RecipientEmailMetadataKey"]?.Trim() ?? "email",
            SupportedChannels = ParseStringList(section.GetSection("SupportedChannels"), ["email"]),
            SubjectTemplate = section["SubjectTemplate"] ?? "Tenant invitation for {tenantId}",
            TextBodyTemplate = section["TextBodyTemplate"] ??
                "You have been invited to tenant {tenantId}.\n\nInvitation: {invitationId}\nInvitee: {displayName}\nRoles: {roles}\nCorrelation: {correlationId}",
            HtmlBodyTemplate = section["HtmlBodyTemplate"],
            Categories = ParseStringList(section.GetSection("Categories"), ["cephalon-invitation"]),
            CustomArgs = ParseDictionary(section.GetSection("CustomArgs")),
            Headers = ParseDictionary(section.GetSection("Headers")),
            IncludeContextCustomArgs = GetBoolean(section["IncludeContextCustomArgs"], defaultValue: true),
            IncludeContextHeaders = GetBoolean(section["IncludeContextHeaders"], defaultValue: true),
            EnableSandboxMode = GetBoolean(section["EnableSandboxMode"], defaultValue: false),
            AcceptedStatusCodes = ParseInt32List(section.GetSection("AcceptedStatusCodes"), DefaultAcceptedStatusCodes),
            ProviderMessageIdHeaderName = section["ProviderMessageIdHeaderName"]?.Trim() ?? "X-Message-ID",
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

    internal Uri GetMailSendEndpoint()
    {
        var baseUrl = TryGetBaseUrl() ?? new Uri("https://api.sendgrid.com", UriKind.Absolute);
        return new Uri(baseUrl, "/v3/mail/send");
    }

    internal TimeSpan GetTimeout() => TimeSpan.FromSeconds(Math.Clamp(TimeoutSeconds, 1, 300));

    internal string GetProviderMessageIdHeaderName()
    {
        return string.IsNullOrWhiteSpace(ProviderMessageIdHeaderName)
            ? "X-Message-ID"
            : ProviderMessageIdHeaderName.Trim();
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
