using Cephalon.Abstractions.Features;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes one configuration-driven feature flag.
/// </summary>
public sealed class FeatureFlagSettings
{
    /// <summary>
    /// Creates feature-flag settings.
    /// </summary>
    /// <param name="id">The stable feature-flag identifier.</param>
    /// <param name="displayName">The operator-facing feature-flag name.</param>
    /// <param name="description">The human-readable feature-flag description.</param>
    /// <param name="enabled">Indicates whether the feature flag is enabled before targeting is applied.</param>
    /// <param name="sourceKind">Identifies whether the feature flag is host-owned or module-owned.</param>
    /// <param name="sourceModuleId">The source-module identifier when the feature flag is module-owned.</param>
    /// <param name="targeting">The optional targeting settings attached to the feature flag.</param>
    /// <param name="metadata">Optional operator-facing metadata.</param>
    public FeatureFlagSettings(
        string id,
        string displayName,
        string description,
        bool enabled = false,
        FeatureFlagSourceKind sourceKind = FeatureFlagSourceKind.Host,
        string? sourceModuleId = null,
        FeatureFlagTargetingSettings? targeting = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Feature flag id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Feature flag display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Feature flag description is required.", nameof(description));
        }

        var normalizedSourceModuleId = string.IsNullOrWhiteSpace(sourceModuleId)
            ? null
            : sourceModuleId.Trim();
        if (sourceKind == FeatureFlagSourceKind.Module &&
            normalizedSourceModuleId is null)
        {
            throw new ArgumentException(
                "Module-owned feature flags must declare a source module id.",
                nameof(sourceModuleId));
        }

        if (sourceKind == FeatureFlagSourceKind.Host &&
            normalizedSourceModuleId is not null)
        {
            throw new ArgumentException(
                "Host-owned feature flags cannot declare a source module id.",
                nameof(sourceModuleId));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Enabled = enabled;
        SourceKind = sourceKind;
        SourceModuleId = normalizedSourceModuleId;
        Targeting = targeting ?? FeatureFlagTargetingSettings.Empty;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable feature-flag identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing feature-flag name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable feature-flag description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets a value indicating whether the feature flag is enabled before targeting is applied.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Gets the ownership kind for this feature flag.
    /// </summary>
    public FeatureFlagSourceKind SourceKind { get; }

    /// <summary>
    /// Gets the source-module identifier when the feature flag is module-owned.
    /// </summary>
    public string? SourceModuleId { get; }

    /// <summary>
    /// Gets the optional targeting settings attached to the feature flag.
    /// </summary>
    public FeatureFlagTargetingSettings Targeting { get; }

    /// <summary>
    /// Gets optional operator-facing metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Reads one feature flag from configuration.
    /// </summary>
    /// <param name="section">The configuration section that contains the feature flag.</param>
    /// <returns>The parsed feature-flag settings.</returns>
    public static FeatureFlagSettings FromSection(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        var sourceModuleId = section["SourceModuleId"];
        var sourceKind = ParseSourceKind(section["SourceKind"], sourceModuleId);

        return new FeatureFlagSettings(
            id: section["Id"]
                ?? throw new InvalidOperationException("Feature flag id is required."),
            displayName: section["DisplayName"]
                ?? throw new InvalidOperationException("Feature flag display name is required."),
            description: section["Description"]
                ?? throw new InvalidOperationException("Feature flag description is required."),
            enabled: bool.TryParse(section["Enabled"], out var enabled) && enabled,
            sourceKind: sourceKind,
            sourceModuleId: sourceKind == FeatureFlagSourceKind.Module ? sourceModuleId : null,
            targeting: FeatureFlagTargetingSettings.FromSection(section.GetSection("Targeting")),
            metadata: ReadMetadata(section.GetSection("Metadata")));
    }

    private static FeatureFlagSourceKind ParseSourceKind(string? rawValue, string? sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.IsNullOrWhiteSpace(sourceModuleId)
                ? FeatureFlagSourceKind.Host
                : FeatureFlagSourceKind.Module;
        }

        if (Enum.TryParse<FeatureFlagSourceKind>(rawValue.Trim(), ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException(
            $"Feature flag source kind '{rawValue}' is invalid. Expected Host or Module.");
    }

    private static Dictionary<string, string> ReadMetadata(IConfigurationSection section)
    {
        if (!section.Exists())
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return section
            .GetChildren()
            .Where(static child => !string.IsNullOrWhiteSpace(child.Key) && !string.IsNullOrWhiteSpace(child.Value))
            .ToDictionary(
                static child => child.Key.Trim(),
                static child => child.Value!.Trim(),
                StringComparer.OrdinalIgnoreCase);
    }
}
