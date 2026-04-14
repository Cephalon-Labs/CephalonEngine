using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Configures host-level governance for public REST endpoint publication in Cephalon ASP.NET Core hosts.
/// </summary>
/// <remarks>
/// These settings describe host-level publication governance for module-owned REST shorthand paths.
/// They intentionally stay out of the engine core because they govern the ASP.NET Core public REST surface.
/// </remarks>
public sealed class RestApiGovernanceOptions
{
    /// <summary>
    /// Gets the root configuration section used for REST API governance settings.
    /// </summary>
    public const string SectionName = "RestApi";

    /// <summary>
    /// Initializes a new instance of the <see cref="RestApiGovernanceOptions" /> class.
    /// </summary>
    /// <param name="suppressions">The configured suppression rules for shorthand REST candidates.</param>
    /// <param name="overrides">The configured override rules for shorthand REST candidates.</param>
    public RestApiGovernanceOptions(
        IReadOnlyList<RestEndpointSuppressionOptions>? suppressions = null,
        IReadOnlyList<RestEndpointOverrideOptions>? overrides = null)
    {
        Suppressions = suppressions?
            .Where(static value => value is not null)
            .ToArray() ?? [];
        Overrides = overrides?
            .Where(static value => value is not null)
            .ToArray() ?? [];
    }

    /// <summary>
    /// Gets the configured suppression rules for descriptor-backed REST shorthand candidates.
    /// </summary>
    public IReadOnlyList<RestEndpointSuppressionOptions> Suppressions { get; }

    /// <summary>
    /// Gets the configured override rules for descriptor-backed REST shorthand candidates.
    /// </summary>
    public IReadOnlyList<RestEndpointOverrideOptions> Overrides { get; }

    /// <summary>
    /// Gets a value indicating whether any REST governance values were explicitly supplied.
    /// </summary>
    public bool HasValues => Suppressions.Count > 0 || Overrides.Count > 0;

    /// <summary>
    /// Binds and normalizes REST governance settings from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">The configuration section path to bind.</param>
    /// <returns>The normalized REST governance settings.</returns>
    public static RestApiGovernanceOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var suppressions = configuration.GetSection(sectionPath)
            .GetSection("Suppressions")
            .GetChildren()
            .Select(static child => new RestEndpointSuppressionOptions(
                id: child.Key,
                behaviorIds: ReadStringArray(child.GetSection("Behaviors")),
                sourceModuleIds: ReadStringArray(child.GetSection("Modules")),
                authoringStyles: ReadStringArray(child.GetSection("AuthoringStyles"))))
            .ToArray();
        var overrides = configuration.GetSection(sectionPath)
            .GetSection("Overrides")
            .GetChildren()
            .Select(child => new RestEndpointOverrideOptions(
                id: child.Key,
                behaviorIds: ReadStringArray(child.GetSection("Behaviors")),
                sourceModuleIds: ReadStringArray(child.GetSection("Modules")),
                authoringStyles: ReadStringArray(child.GetSection("AuthoringStyles")),
                apiVersionMajor: ReadPositiveInt(child, "ApiVersionMajor"),
                method: child["Method"]?.Trim()))
            .ToArray();

        return new RestApiGovernanceOptions(suppressions, overrides);
    }

    private static string[] ReadStringArray(IConfiguration section)
    {
        return section.GetChildren()
            .Select(static child => child.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int? ReadPositiveInt(IConfigurationSection section, string key)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var rawValue = section[key]?.Trim();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (!int.TryParse(rawValue, out var parsedValue) || parsedValue <= 0)
        {
            throw new InvalidOperationException(
                $"REST API governance value '{section.Path}:{key}' must be a positive integer.");
        }

        return parsedValue;
    }
}
