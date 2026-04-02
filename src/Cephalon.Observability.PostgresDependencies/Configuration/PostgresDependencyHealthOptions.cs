using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.PostgresDependencies.Configuration;

/// <summary>
/// Configures Postgres dependency probes contributed to Cephalon runtime health.
/// </summary>
public sealed class PostgresDependencyHealthOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresDependencyHealthOptions" /> class.
    /// </summary>
    public PostgresDependencyHealthOptions()
    {
    }

    /// <summary>
    /// Gets or sets the interval, in seconds, between background refresh attempts.
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the configured Postgres dependencies that should contribute to runtime health.
    /// </summary>
    public IReadOnlyList<PostgresDependencyDefinition> Dependencies { get; set; } = Array.Empty<PostgresDependencyDefinition>();

    /// <summary>
    /// Binds Postgres dependency-health options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound dependency-health options.</returns>
    public static PostgresDependencyHealthOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("DependencyHealth")
            .GetSection("Postgres");

        return FromSection(section);
    }

    private static PostgresDependencyHealthOptions FromSection(IConfigurationSection section)
    {
        var dependencies = section
            .GetSection("Dependencies")
            .GetChildren()
            .Select(ParseDependency)
            .ToArray();

        return new PostgresDependencyHealthOptions
        {
            RefreshIntervalSeconds = GetInt32(section["RefreshIntervalSeconds"], defaultValue: 30),
            Dependencies = dependencies
        };
    }

    private static PostgresDependencyDefinition ParseDependency(IConfigurationSection section)
    {
        return new PostgresDependencyDefinition
        {
            Id = section["Id"]?.Trim() ?? string.Empty,
            DisplayName = section["DisplayName"]?.Trim(),
            ConnectionString = section["ConnectionString"]?.Trim(),
            Host = section["Host"]?.Trim() ?? string.Empty,
            Port = GetInt32(section["Port"], defaultValue: 5432),
            Database = section["Database"]?.Trim() ?? "postgres",
            Username = section["Username"]?.Trim(),
            Password = section["Password"],
            SslMode = section["SslMode"]?.Trim(),
            HealthQuery = section["HealthQuery"]?.Trim() ?? "SELECT 1;",
            Required = GetBoolean(section["Required"], defaultValue: false),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 5)
        };
    }

    private static bool GetBoolean(string? value, bool defaultValue) =>
        bool.TryParse(value, out var parsed) ? parsed : defaultValue;

    private static int GetInt32(string? value, int defaultValue) =>
        int.TryParse(value, out var parsed) && parsed > 0 ? parsed : defaultValue;
}
