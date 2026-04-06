using Cephalon.Engine.Configuration;
using Cephalon.Observability.DependencyHealth.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.HttpDependencies.Configuration;

/// <summary>
/// Configures HTTP and external API dependency probes contributed to Cephalon runtime health.
/// </summary>
public sealed class HttpDependencyHealthOptions : DependencyHealthOptionsBase<HttpDependencyDefinition>
{
    /// <summary>
    /// Binds HTTP dependency-health options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound dependency-health options.</returns>
    public static HttpDependencyHealthOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("DependencyHealth")
            .GetSection("Http");

        return FromSection(section);
    }

    private static HttpDependencyHealthOptions FromSection(IConfigurationSection section)
    {
        var dependencies = section
            .GetSection("Dependencies")
            .GetChildren()
            .Select(ParseDependency)
            .ToArray();

        return new HttpDependencyHealthOptions
        {
            RefreshIntervalSeconds = GetInt32(section["RefreshIntervalSeconds"], defaultValue: 30),
            Dependencies = dependencies
        };
    }

    private static HttpDependencyDefinition ParseDependency(IConfigurationSection section)
    {
        var expectedStatusCodes = ParseExpectedStatusCodes(section.GetSection("ExpectedStatusCodes"));

        return new HttpDependencyDefinition
        {
            Id = section["Id"]?.Trim() ?? string.Empty,
            DisplayName = section["DisplayName"]?.Trim(),
            Endpoint = section["Endpoint"]?.Trim() ?? string.Empty,
            Method = section["Method"]?.Trim() ?? "GET",
            Required = GetBoolean(section["Required"], defaultValue: false),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 5),
            ExpectedStatusCodes = expectedStatusCodes
        };
    }

    private static int[] ParseExpectedStatusCodes(IConfigurationSection section)
    {
        if (!string.IsNullOrWhiteSpace(section.Value))
        {
            return section.Value!
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => int.TryParse(value, out var parsed) ? parsed : -1)
                .Where(static value => value > 0)
                .ToArray();
        }

        return section
            .GetChildren()
            .Select(child => int.TryParse(child.Value, out var parsed) ? parsed : -1)
            .Where(static value => value > 0)
            .ToArray();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpDependencyHealthOptions" /> class.
    /// </summary>
    public HttpDependencyHealthOptions()
    {
    }
}
