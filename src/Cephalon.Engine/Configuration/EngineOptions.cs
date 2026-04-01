using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

public sealed class EngineOptions
{
    public static EngineOptions Empty { get; } = new();

    public EngineOptions(
        IReadOnlyDictionary<string, bool>? modules = null,
        IReadOnlyDictionary<string, bool>? capabilities = null)
    {
        Modules = Normalize(modules);
        Capabilities = Normalize(capabilities);
    }

    public IReadOnlyDictionary<string, bool> Modules { get; }

    public IReadOnlyDictionary<string, bool> Capabilities { get; }

    public bool HasValues => Modules.Count > 0 || Capabilities.Count > 0;

    public bool IsModuleEnabled(string moduleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);

        return !Modules.TryGetValue(moduleId.Trim(), out var enabled) || enabled;
    }

    public bool IsCapabilityEnabled(string capabilityKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capabilityKey);

        return !Capabilities.TryGetValue(capabilityKey.Trim(), out var enabled) || enabled;
    }

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
