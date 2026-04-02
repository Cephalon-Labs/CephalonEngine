namespace Cephalon.Abstractions.Localization;

/// <summary>
/// Reads localized text resolved by the runtime.
/// </summary>
public interface ILocalizedTextCatalog
{
    /// <summary>
    /// Gets the default culture used by the catalog.
    /// </summary>
    string DefaultCulture { get; }

    /// <summary>
    /// Gets the cultures currently available in the catalog.
    /// </summary>
    IReadOnlyList<string> SupportedCultures { get; }

    /// <summary>
    /// Attempts to resolve one localized text value.
    /// </summary>
    /// <param name="key">The resource key to resolve.</param>
    /// <param name="culture">The preferred culture, or <see langword="null"/> to use the default resolution flow.</param>
    /// <param name="value">The resolved text value when one is found.</param>
    /// <returns><see langword="true"/> when the value was resolved; otherwise <see langword="false"/>.</returns>
    bool TryGet(string key, string? culture, out string value);

    /// <summary>
    /// Resolves one localized text value with an optional fallback.
    /// </summary>
    /// <param name="key">The resource key to resolve.</param>
    /// <param name="culture">The preferred culture, or <see langword="null"/> to use the default resolution flow.</param>
    /// <param name="fallback">The fallback value to use when the key cannot be resolved.</param>
    /// <returns>The resolved localized text value.</returns>
    string ResolveText(string key, string? culture = null, string? fallback = null);

    /// <summary>
    /// Returns the localized resources visible for one culture.
    /// </summary>
    /// <param name="culture">The preferred culture, or <see langword="null"/> to use the default resolution flow.</param>
    /// <returns>The localized resources visible for the requested culture.</returns>
    IReadOnlyDictionary<string, string> GetResources(string? culture = null);

    /// <summary>
    /// Creates an introspectable snapshot of the currently resolved localized resources.
    /// </summary>
    /// <param name="culture">The preferred culture, or <see langword="null"/> to use the default resolution flow.</param>
    /// <returns>The localized-resource snapshot.</returns>
    LocalizedResourcesSnapshot CreateSnapshot(string? culture = null);
}
