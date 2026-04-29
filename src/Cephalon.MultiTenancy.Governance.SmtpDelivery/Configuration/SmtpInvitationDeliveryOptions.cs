using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.MultiTenancy.Governance.SmtpDelivery.Configuration;

/// <summary>
/// Configures SMTP relay delivery for tenant invitations dispatched by the governance companion pack.
/// </summary>
/// <remarks>
/// The SMTP sender sends one email message through a configured SMTP relay. It does not own provider-specific
/// transactional-email APIs, bounce handling, delivery callbacks, or public onboarding flows.
/// </remarks>
public sealed class SmtpInvitationDeliveryOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the SMTP invitation sender should be registered.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the sender identifier used by <c>TenantInvitationDeliveryRequest.SenderId</c>.
    /// </summary>
    public string SenderId { get; set; } = "smtp-email";

    /// <summary>
    /// Gets or sets the SMTP relay host name.
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// Gets or sets the SMTP relay port.
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Gets or sets a value indicating whether SSL/TLS should be enabled for the SMTP relay connection.
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// Gets or sets the SMTP username when the relay requires explicit credentials.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the SMTP password when the relay requires explicit credentials.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the sender email address used in the SMTP message.
    /// </summary>
    public string? FromAddress { get; set; }

    /// <summary>
    /// Gets or sets the optional sender display name used in the SMTP message.
    /// </summary>
    public string? FromDisplayName { get; set; } = "Cephalon";

    /// <summary>
    /// Gets or sets the metadata key used to resolve the recipient email address when the invitee id is not an email address.
    /// </summary>
    /// <remarks>
    /// The sender checks dispatch metadata first and invitation metadata second. If neither contains a value and
    /// <c>InviteeKind</c> is <c>email</c>, the invitee id is treated as the recipient address.
    /// </remarks>
    public string RecipientAddressMetadataKey { get; set; } = "email";

    /// <summary>
    /// Gets or sets delivery channels accepted by this sender.
    /// </summary>
    /// <remarks>
    /// When empty, the sender accepts every requested channel.
    /// </remarks>
    public IReadOnlyList<string> SupportedChannels { get; set; } = ["email"];

    /// <summary>
    /// Gets or sets the SMTP message subject template.
    /// </summary>
    public string SubjectTemplate { get; set; } = "Tenant invitation for {tenantId}";

    /// <summary>
    /// Gets or sets the plain-text SMTP message body template.
    /// </summary>
    public string TextBodyTemplate { get; set; } =
        "You have been invited to tenant {tenantId}.\n\nInvitation: {invitationId}\nInvitee: {displayName}\nRoles: {roles}\nCorrelation: {correlationId}";

    /// <summary>
    /// Gets or sets the optional HTML SMTP message body template.
    /// </summary>
    public string? HtmlBodyTemplate { get; set; }

    /// <summary>
    /// Gets or sets additional SMTP message headers added to every delivery message.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets a value indicating whether safe Cephalon context headers should be added to the SMTP message.
    /// </summary>
    public bool IncludeContextHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets the domain used for deterministic SMTP <c>Message-Id</c> values.
    /// </summary>
    public string MessageIdDomain { get; set; } = "cephalon.local";

    /// <summary>
    /// Gets or sets the maximum time allowed for the SMTP send operation.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Binds SMTP invitation delivery options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound SMTP invitation delivery options.</returns>
    public static SmtpInvitationDeliveryOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("MultiTenancy")
            .GetSection("Governance")
            .GetSection("SmtpInvitationDelivery");

        return new SmtpInvitationDeliveryOptions
        {
            Enabled = GetBoolean(section["Enabled"], defaultValue: true),
            SenderId = section["SenderId"]?.Trim() ?? "smtp-email",
            Host = section["Host"]?.Trim(),
            Port = GetInt32(section["Port"], defaultValue: 587),
            UseSsl = GetBoolean(section["UseSsl"], defaultValue: true),
            UserName = section["UserName"]?.Trim(),
            Password = section["Password"],
            FromAddress = section["FromAddress"]?.Trim(),
            FromDisplayName = section["FromDisplayName"]?.Trim(),
            RecipientAddressMetadataKey = section["RecipientAddressMetadataKey"]?.Trim() ?? "email",
            SupportedChannels = ParseStringList(section.GetSection("SupportedChannels"), ["email"]),
            SubjectTemplate = section["SubjectTemplate"] ?? "Tenant invitation for {tenantId}",
            TextBodyTemplate = section["TextBodyTemplate"] ??
                "You have been invited to tenant {tenantId}.\n\nInvitation: {invitationId}\nInvitee: {displayName}\nRoles: {roles}\nCorrelation: {correlationId}",
            HtmlBodyTemplate = section["HtmlBodyTemplate"],
            Headers = ParseDictionary(section.GetSection("Headers")),
            IncludeContextHeaders = GetBoolean(section["IncludeContextHeaders"], defaultValue: true),
            MessageIdDomain = section["MessageIdDomain"]?.Trim() ?? "cephalon.local",
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 10)
        };
    }

    internal int GetPort() => Math.Clamp(Port, 1, 65535);

    internal TimeSpan GetTimeout() => TimeSpan.FromSeconds(Math.Clamp(TimeoutSeconds, 1, 300));

    internal string GetMessageIdDomain()
    {
        return string.IsNullOrWhiteSpace(MessageIdDomain) ? "cephalon.local" : MessageIdDomain.Trim();
    }

    private static bool GetBoolean(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static int GetInt32(string? value, int defaultValue)
    {
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
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
