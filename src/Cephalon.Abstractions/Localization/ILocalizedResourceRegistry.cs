namespace Cephalon.Abstractions.Localization;

/// <summary>
/// Registers localized resources by culture and key.
/// </summary>
public interface ILocalizedResourceRegistry
{
    /// <summary>
    /// Adds one localized text value.
    /// </summary>
    /// <param name="culture">The culture the value belongs to.</param>
    /// <param name="key">The localized resource key.</param>
    /// <param name="value">The localized text value.</param>
    void Add(string culture, string key, string value);

    /// <summary>
    /// Adds a batch of localized text values for one culture.
    /// </summary>
    /// <param name="culture">The culture the values belong to.</param>
    /// <param name="resources">The localized resources to register.</param>
    void Add(string culture, IReadOnlyDictionary<string, string> resources);
}
