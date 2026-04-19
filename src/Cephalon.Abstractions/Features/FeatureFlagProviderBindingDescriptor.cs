namespace Cephalon.Abstractions.Features;

/// <summary>
/// Describes one external provider binding attached to a Cephalon-owned feature flag.
/// </summary>
public sealed class FeatureFlagProviderBindingDescriptor
{
    /// <summary>
    /// Creates a feature-flag provider binding.
    /// </summary>
    /// <param name="providerId">The stable external provider identifier.</param>
    /// <param name="providerFeatureId">
    /// The provider-specific feature identifier. When omitted, the owning Cephalon feature-flag id
    /// is used.
    /// </param>
    /// <param name="metadata">Optional provider-specific binding metadata.</param>
    public FeatureFlagProviderBindingDescriptor(
        string providerId,
        string? providerFeatureId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("Provider id is required.", nameof(providerId));
        }

        ProviderId = providerId.Trim();
        ProviderFeatureId = NormalizeOptional(providerFeatureId);
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
    /// Resolves the provider-side feature identifier for the supplied Cephalon feature flag.
    /// </summary>
    /// <param name="featureFlagId">The owning Cephalon feature-flag identifier.</param>
    /// <returns>The provider-side feature identifier.</returns>
    public string ResolveProviderFeatureId(string featureFlagId)
    {
        if (string.IsNullOrWhiteSpace(featureFlagId))
        {
            throw new ArgumentException("Feature flag id is required.", nameof(featureFlagId));
        }

        return ProviderFeatureId ?? featureFlagId.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
