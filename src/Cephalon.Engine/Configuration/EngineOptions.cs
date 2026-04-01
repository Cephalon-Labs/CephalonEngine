using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Captures module and capability enablement overrides for the runtime.
/// </summary>
public sealed class EngineOptions
{
    /// <summary>
    /// Gets an empty options instance with no explicit overrides.
    /// </summary>
    public static EngineOptions Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EngineOptions" /> class.
    /// </summary>
    /// <param name="modules">Module enablement overrides keyed by module identifier.</param>
    /// <param name="capabilities">Capability enablement overrides keyed by capability key.</param>
    public EngineOptions(
        IReadOnlyDictionary<string, bool>? modules = null,
        IReadOnlyDictionary<string, bool>? capabilities = null)
    {
        Modules = Normalize(modules);
        Capabilities = Normalize(capabilities);
    }

    /// <summary>
    /// Gets module enablement overrides keyed by module identifier.
    /// </summary>
    public IReadOnlyDictionary<string, bool> Modules { get; }

    /// <summary>
    /// Gets capability enablement overrides keyed by capability key.
    /// </summary>
    public IReadOnlyDictionary<string, bool> Capabilities { get; }

    /// <summary>
    /// Gets a value indicating whether any explicit option overrides are present.
    /// </summary>
    public bool HasValues => Modules.Count > 0 || Capabilities.Count > 0;

    /// <summary>
    /// Determines whether a module is enabled under the current option set.
    /// </summary>
    /// <param name="moduleId">The module identifier to evaluate.</param>
    /// <returns><see langword="true" /> when the module is enabled; otherwise, <see langword="false" />.</returns>
    public bool IsModuleEnabled(string moduleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);

        return !Modules.TryGetValue(moduleId.Trim(), out var enabled) || enabled;
    }

    /// <summary>
    /// Determines whether a capability is enabled under the current option set.
    /// </summary>
    /// <param name="capabilityKey">The capability key to evaluate.</param>
    /// <returns><see langword="true" /> when the capability is enabled; otherwise, <see langword="false" />.</returns>
    public bool IsCapabilityEnabled(string capabilityKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capabilityKey);

        return !Capabilities.TryGetValue(capabilityKey.Trim(), out var enabled) || enabled;
    }

    /// <summary>
    /// Merges another option set into the current instance.
    /// </summary>
    /// <param name="other">The option set to overlay on top of the current values.</param>
    /// <returns>A merged option set.</returns>
    public EngineOptions Merge(EngineOptions? other)
    {
        if (other is null || !other.HasValues)
        {
            return this;
        }

        if (!HasValues)
        {
            return other;
        }

        var modules = new Dictionary<string, bool>(Modules, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in other.Modules)
        {
            modules[pair.Key] = pair.Value;
        }

        var capabilities = new Dictionary<string, bool>(Capabilities, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in other.Capabilities)
        {
            capabilities[pair.Key] = pair.Value;
        }

        return new EngineOptions(modules, capabilities);
    }

    /// <summary>
    /// Reads engine options from configuration.
    /// </summary>
    /// <param name="configuration">The configuration source that contains the engine section.</param>
    /// <param name="sectionPath">The root configuration section path to read from.</param>
    /// <returns>The parsed engine options.</returns>
    public static EngineOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var optionsSection = configuration
            .GetSection(sectionPath)
            .GetSection("Options");

        var modules = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var moduleSection in optionsSection.GetSection("Modules").GetChildren())
        {
            if (!TryParseBoolean(moduleSection["Enabled"], out var enabled))
            {
                continue;
            }

            modules[moduleSection.Key] = enabled;
        }

        var capabilities = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var capabilitySection in optionsSection.GetSection("Capabilities").GetChildren())
        {
            if (!TryParseBoolean(capabilitySection.Value, out var enabled))
            {
                continue;
            }

            capabilities[capabilitySection.Key] = enabled;
        }

        return new EngineOptions(modules, capabilities);
    }

    private static Dictionary<string, bool> Normalize(IReadOnlyDictionary<string, bool>? source)
    {
        var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        if (source is null)
        {
            return result;
        }

        foreach (var pair in source)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                continue;
            }

            result[pair.Key.Trim()] = pair.Value;
        }

        return result;
    }

    private static bool TryParseBoolean(string? value, out bool enabled)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            enabled = default;
            return false;
        }

        return bool.TryParse(value.Trim(), out enabled);
    }
}
