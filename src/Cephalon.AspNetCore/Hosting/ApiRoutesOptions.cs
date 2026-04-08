using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Configures host-level HTTP route prefixes for Cephalon ASP.NET Core transports.
/// </summary>
/// <remarks>
/// These settings describe the public HTTP surface of the ASP.NET Core adapter. They intentionally stay out of the engine core.
/// </remarks>
public sealed class ApiRoutesOptions
{
    /// <summary>
    /// Gets the configuration section used for API route settings.
    /// </summary>
    public const string SectionName = "ApiRoutes";

    /// <summary>
    /// Gets or sets the root prefix used by the built-in REST transport mapper.
    /// </summary>
    public string RestPrefix { get; set; } = "/api";

    /// <summary>
    /// Binds and normalizes API route settings from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">The configuration section path to bind.</param>
    /// <returns>The normalized route settings.</returns>
    public static ApiRoutesOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionPath);
        var restPrefix = section["RestPrefix"] ?? section["Prefixes:Rest"];

        return new ApiRoutesOptions
        {
            RestPrefix = NormalizePrefix(restPrefix, "/api")
        };
    }

    private static string NormalizePrefix(string? value, string defaultValue)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value.Trim();

        if (!normalized.StartsWith('/'))
        {
            normalized = $"/{normalized}";
        }

        normalized = normalized.Length > 1
            ? normalized.TrimEnd('/')
            : normalized;

        if (normalized == "/")
        {
            throw new InvalidOperationException(
                "ApiRoutes route prefixes must resolve to non-root paths such as '/api'.");
        }

        return normalized;
    }
}
