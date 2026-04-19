using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes one configuration-driven external provider binding for a feature flag.
/// </summary>
public sealed class FeatureFlagProviderBindingSettings
{
    /// <summary>
    /// Creates provider-binding settings.
    /// </summary>
    /// <param name="providerId">The stable external provider identifier.</param>
    /// <param name="providerFeatureId">The provider-specific feature identifier.</param>
    /// <param name="metadata">Optional provider-specific binding metadata.</param>
    public FeatureFlagProviderBindingSettings(
        string providerId,
        string? providerFeatureId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("Provider id is required.", nameof(providerId));
        }

        ProviderId = providerId.Trim();
        ProviderFeatureId = string.IsNullOrWhiteSpace(providerFeatureId)
            ? null
            : providerFeatureId.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable external provider identifier.
    /// </summary>
    public string ProviderId { get; }

    /// <summary>
    /// Gets the provider-specific feature identifier when one was supplied.
    /// </summary>
    public string? ProviderFeatureId { get; }

    /// <summary>
    /// Gets provider-specific binding metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Reads one provider binding from configuration.
    /// </summary>
    /// <param name="section">The configuration section that contains the provider binding.</param>
    /// <returns>The parsed provider binding settings.</returns>
    public static FeatureFlagProviderBindingSettings FromSection(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new FeatureFlagProviderBindingSettings(
            providerId: section["ProviderId"]
                ?? throw new InvalidOperationException("Feature flag provider id is required."),
            providerFeatureId: section["ProviderFeatureId"],
            metadata: ReadMetadata(section.GetSection("Metadata")));
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
