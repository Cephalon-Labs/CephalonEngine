using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Configures opt-in HTTP request and response logging for Cephalon ASP.NET Core hosts.
/// </summary>
/// <remarks>
/// These settings are read from <c>Engine:Observability:HttpLogging</c> by default. Request and
/// response bodies are captured only for textual content types such as JSON, XML, GraphQL, form
/// payloads, and <c>text/*</c> responses, and body capture is truncated to the configured limits.
/// Sensitive query-string and payload fields can also be redacted before the log event is written,
/// including JSON, form, and header-style plain-text key/value content.
/// </remarks>
public sealed class HttpRequestResponseLoggingOptions
{
    private static readonly string[] DefaultRedactedFieldNames =
    [
        "access_token",
        "apiKey",
        "api_key",
        "authorization",
        "client_secret",
        "cookie",
        "password",
        "pin",
        "refresh_token",
        "secret",
        "set-cookie",
        "token"
    ];

    /// <summary>
    /// Creates request and response logging options with body capture disabled by default.
    /// </summary>
    public HttpRequestResponseLoggingOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the ASP.NET Core host should log request and response summaries.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether textual request bodies should be logged.
    /// </summary>
    public bool LogRequestBody { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether textual response bodies should be logged.
    /// </summary>
    public bool LogResponseBody { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of request-body characters to log before the payload is truncated.
    /// </summary>
    public int RequestBodyLimit { get; set; } = 4096;

    /// <summary>
    /// Gets or sets the maximum number of response-body characters to log before the payload is truncated.
    /// </summary>
    public int ResponseBodyLimit { get; set; } = 4096;

    /// <summary>
    /// Gets or sets a value indicating whether known-sensitive query-string and payload fields should be redacted before logging.
    /// </summary>
    public bool RedactSensitiveValues { get; set; } = true;

    /// <summary>
    /// Gets or sets the field names that should be treated as sensitive when request and response content is logged.
    /// </summary>
    public IReadOnlyList<string> RedactedFieldNames { get; set; } = DefaultRedactedFieldNames;

    /// <summary>
    /// Gets or sets the placeholder written to logs when a sensitive value is redacted.
    /// </summary>
    public string RedactionValue { get; set; } = "[REDACTED]";

    /// <summary>
    /// Binds request and response logging options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound request and response logging options.</returns>
    public static HttpRequestResponseLoggingOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("HttpLogging");

        return new HttpRequestResponseLoggingOptions
        {
            Enabled = GetBoolean(section["Enabled"], defaultValue: false),
            LogRequestBody = GetBoolean(section["LogRequestBody"], defaultValue: false),
            LogResponseBody = GetBoolean(section["LogResponseBody"], defaultValue: false),
            RequestBodyLimit = GetInt32(section["RequestBodyLimit"], defaultValue: 4096),
            ResponseBodyLimit = GetInt32(section["ResponseBodyLimit"], defaultValue: 4096),
            RedactSensitiveValues = GetBoolean(section["RedactSensitiveValues"], defaultValue: true),
            RedactedFieldNames = GetStringArray(section, "RedactedFieldNames", DefaultRedactedFieldNames),
            RedactionValue = GetString(section["RedactionValue"], "[REDACTED]")
        };
    }

    private static bool GetBoolean(string? value, bool defaultValue) =>
        bool.TryParse(value, out var parsed) ? parsed : defaultValue;

    private static int GetInt32(string? value, int defaultValue) =>
        int.TryParse(value, out var parsed) ? Math.Max(0, parsed) : defaultValue;

    private static string GetString(string? value, string defaultValue) =>
        string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();

    private static IReadOnlyList<string> GetStringArray(
        IConfigurationSection section,
        string key,
        IReadOnlyList<string> defaultValue)
    {
        var values = section
            .GetSection(key)
            .GetChildren()
            .Select(static child => child.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return values.Length > 0 ? values : defaultValue;
    }
}
