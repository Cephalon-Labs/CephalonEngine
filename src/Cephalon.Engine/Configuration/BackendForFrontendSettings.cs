using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven backend-for-frontend settings for a Cephalon app.
/// </summary>
public sealed class BackendForFrontendSettings
{
    /// <summary>
    /// Gets an empty backend-for-frontend settings instance.
    /// </summary>
    public static BackendForFrontendSettings Empty { get; } = new();

    /// <summary>
    /// Creates backend-for-frontend settings.
    /// </summary>
    /// <param name="bindings">The client-specific transport bindings configured for the app.</param>
    public BackendForFrontendSettings(IReadOnlyList<BackendForFrontendClientBindingSettings>? bindings = null)
    {
        Bindings = bindings?.ToArray() ?? [];
    }

    /// <summary>
    /// Gets the client-specific transport bindings configured for the app.
    /// </summary>
    public IReadOnlyList<BackendForFrontendClientBindingSettings> Bindings { get; }

    /// <summary>
    /// Gets a value indicating whether any backend-for-frontend settings were explicitly supplied.
    /// </summary>
    public bool HasValues => Bindings.Count > 0;

    /// <summary>
    /// Reads backend-for-frontend settings from configuration.
    /// </summary>
    /// <param name="configuration">The configuration source that contains the engine section.</param>
    /// <param name="sectionPath">The root configuration section path to read from.</param>
    /// <returns>The parsed backend-for-frontend settings.</returns>
    public static BackendForFrontendSettings FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("BackendForFrontend");
        if (!section.Exists())
        {
            return Empty;
        }

        var bindings = section
            .GetSection("Bindings")
            .GetChildren()
            .Where(static child => child.Exists())
            .Select(BackendForFrontendClientBindingSettings.FromSection)
            .ToArray();

        return new BackendForFrontendSettings(bindings);
    }
}
