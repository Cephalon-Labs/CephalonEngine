using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.RedisDependencies.Configuration;

/// <summary>
/// Configures Redis dependency probes contributed to Cephalon runtime health.
/// </summary>
public sealed class RedisDependencyHealthOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RedisDependencyHealthOptions" /> class.
    /// </summary>
    public RedisDependencyHealthOptions()
    {
    }

    /// <summary>
    /// Gets or sets the interval, in seconds, between background refresh attempts.
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the configured Redis dependencies that should contribute to runtime health.
    /// </summary>
    public IReadOnlyList<RedisDependencyDefinition> Dependencies { get; set; } = Array.Empty<RedisDependencyDefinition>();

    /// <summary>
    /// Binds Redis dependency-health options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound dependency-health options.</returns>
    public static RedisDependencyHealthOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("DependencyHealth")
            .GetSection("Redis");

        return FromSection(section);
    }

    private static RedisDependencyHealthOptions FromSection(IConfigurationSection section)
    {
        var dependencies = section
            .GetSection("Dependencies")
            .GetChildren()
            .Select(ParseDependency)
            .ToArray();

        return new RedisDependencyHealthOptions
        {
            RefreshIntervalSeconds = GetInt32(section["RefreshIntervalSeconds"], defaultValue: 30),
            Dependencies = dependencies
        };
    }

    private static RedisDependencyDefinition ParseDependency(IConfigurationSection section)
    {
        return new RedisDependencyDefinition
        {
            Id = section["Id"]?.Trim() ?? string.Empty,
            DisplayName = section["DisplayName"]?.Trim(),
            Host = section["Host"]?.Trim() ?? string.Empty,
            Port = GetInt32(section["Port"], defaultValue: 6379),
            Required = GetBoolean(section["Required"], defaultValue: false),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 5),
            Username = section["Username"]?.Trim(),
            Password = section["Password"],
            Database = GetNullableInt32(section["Database"])
        };
    }

    private static bool GetBoolean(string? value, bool defaultValue) =>
        bool.TryParse(value, out var parsed) ? parsed : defaultValue;

    private static int GetInt32(string? value, int defaultValue) =>
        int.TryParse(value, out var parsed) && parsed > 0 ? parsed : defaultValue;

    private static int? GetNullableInt32(string? value) =>
        int.TryParse(value, out var parsed) && parsed >= 0 ? parsed : null;
}
