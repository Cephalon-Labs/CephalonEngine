using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Configuration;

/// <summary>
/// Configures Amazon SES v2 delivery for tenant invitations dispatched by the governance companion pack.
/// </summary>
/// <remarks>
/// The Amazon SES sender submits one SES v2 <c>SendEmail</c> simple message request. It does not own AWS account setup,
/// sender identity verification, bounce/complaint callbacks, provider polling, public onboarding, SMS, chat, CRM, or
/// identity-provider invitation flows.
/// </remarks>
public sealed class AmazonSesInvitationDeliveryOptions
{
    private static readonly int[] DefaultAcceptedStatusCodes = [200];

    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonSesInvitationDeliveryOptions" /> class.
    /// </summary>
    public AmazonSesInvitationDeliveryOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Amazon SES invitation sender should be registered.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the sender identifier used by <c>TenantInvitationDeliveryRequest.SenderId</c>.
    /// </summary>
    public string SenderId { get; set; } = "amazon-ses-email";

    /// <summary>
    /// Gets or sets the optional AWS region system name used when Cephalon creates the default SES v2 client.
    /// </summary>
    /// <remarks>
    /// When omitted, the AWS SDK default region resolution chain remains authoritative. Example values include
    /// <c>us-east-1</c> and <c>eu-west-1</c>.
    /// </remarks>
    public string? RegionSystemName { get; set; }

    /// <summary>
    /// Gets or sets the optional SES configuration set name attached to the request.
    /// </summary>
    public string? ConfigurationSetName { get; set; }

    /// <summary>
    /// Gets or sets the sender email address used in the SES message.
    /// </summary>
    public string? FromEmail { get; set; }

    /// <summary>
    /// Gets or sets the optional sender display name used in the SES message.
    /// </summary>
    public string? FromName { get; set; } = "Cephalon";

    /// <summary>
    /// Gets or sets reply-to addresses attached to the SES message.
    /// </summary>
    public IReadOnlyList<string> ReplyToAddresses { get; set; } = [];

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
    /// Gets or sets the Amazon SES message subject template.
    /// </summary>
    public string SubjectTemplate { get; set; } = "Tenant invitation for {tenantId}";

    /// <summary>
    /// Gets or sets the plain-text Amazon SES message body template.
    /// </summary>
    public string TextBodyTemplate { get; set; } =
        "You have been invited to tenant {tenantId}.\n\nInvitation: {invitationId}\nInvitee: {displayName}\nRoles: {roles}\nCorrelation: {correlationId}";

    /// <summary>
    /// Gets or sets the optional HTML Amazon SES message body template.
    /// </summary>
    public string? HtmlBodyTemplate { get; set; }

    /// <summary>
    /// Gets or sets Amazon SES message tags attached to the request.
    /// </summary>
    /// <remarks>
    /// These values are sent to Amazon SES and can appear in provider event publishing. Do not put secrets here.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["cephalon-source"] = "invitation"
    };

    /// <summary>
    /// Gets or sets a value indicating whether safe Cephalon context tags should be added to the SES request.
    /// </summary>
    public bool IncludeContextTags { get; set; } = true;

    /// <summary>
    /// Gets or sets response status codes that indicate Amazon SES accepted the request.
    /// </summary>
    /// <remarks>
    /// The default accepts <c>200 OK</c>, which the AWS SDK reports for a successful SES v2 <c>SendEmail</c> call.
    /// </remarks>
    public IReadOnlyList<int> AcceptedStatusCodes { get; set; } = DefaultAcceptedStatusCodes;

    /// <summary>
    /// Gets or sets the maximum time allowed for the Amazon SES SDK request.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Binds Amazon SES invitation delivery options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound Amazon SES invitation delivery options.</returns>
    public static AmazonSesInvitationDeliveryOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("MultiTenancy")
            .GetSection("Governance")
            .GetSection("AmazonSesInvitationDelivery");

        return new AmazonSesInvitationDeliveryOptions
        {
            Enabled = GetBoolean(section["Enabled"], defaultValue: true),
            SenderId = section["SenderId"]?.Trim() ?? "amazon-ses-email",
            RegionSystemName = section["RegionSystemName"]?.Trim(),
            ConfigurationSetName = section["ConfigurationSetName"]?.Trim(),
            FromEmail = section["FromEmail"]?.Trim(),
            FromName = section["FromName"]?.Trim(),
            ReplyToAddresses = ParseStringList(section.GetSection("ReplyToAddresses")),
            RecipientEmailMetadataKey = section["RecipientEmailMetadataKey"]?.Trim() ?? "email",
            SupportedChannels = ParseStringList(section.GetSection("SupportedChannels"), ["email"]),
            SubjectTemplate = section["SubjectTemplate"] ?? "Tenant invitation for {tenantId}",
            TextBodyTemplate = section["TextBodyTemplate"] ??
                "You have been invited to tenant {tenantId}.\n\nInvitation: {invitationId}\nInvitee: {displayName}\nRoles: {roles}\nCorrelation: {correlationId}",
            HtmlBodyTemplate = section["HtmlBodyTemplate"],
            Tags = ParseDictionary(section.GetSection("Tags"), new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["cephalon-source"] = "invitation"
            }),
            IncludeContextTags = GetBoolean(section["IncludeContextTags"], defaultValue: true),
            AcceptedStatusCodes = ParseInt32List(section.GetSection("AcceptedStatusCodes"), DefaultAcceptedStatusCodes),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 10)
        };
    }

    internal TimeSpan GetTimeout() => TimeSpan.FromSeconds(Math.Clamp(TimeoutSeconds, 1, 300));

    internal string? GetRegionSystemName() => string.IsNullOrWhiteSpace(RegionSystemName) ? null : RegionSystemName.Trim();

    internal string? GetConfigurationSetName() => string.IsNullOrWhiteSpace(ConfigurationSetName) ? null : ConfigurationSetName.Trim();

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

    private static Dictionary<string, string> ParseDictionary(
        IConfigurationSection section,
        IReadOnlyDictionary<string, string>? defaultValues = null)
    {
        var parsed = section
            .GetChildren()
            .Where(static child => !string.IsNullOrWhiteSpace(child.Key) && !string.IsNullOrWhiteSpace(child.Value))
            .ToDictionary(
                static child => child.Key.Trim(),
                static child => child.Value!,
                StringComparer.OrdinalIgnoreCase);

        if (parsed.Count > 0 || section.Exists() || defaultValues is null)
        {
            return parsed;
        }

        return defaultValues.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }
}
