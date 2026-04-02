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
/// </remarks>
public sealed class HttpRequestResponseLoggingOptions
{
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
            ResponseBodyLimit = GetInt32(section["ResponseBodyLimit"], defaultValue: 4096)
        };
    }

    private static bool GetBoolean(string? value, bool defaultValue) =>
        bool.TryParse(value, out var parsed) ? parsed : defaultValue;

    private static int GetInt32(string? value, int defaultValue) =>
        int.TryParse(value, out var parsed) ? Math.Max(0, parsed) : defaultValue;
}
