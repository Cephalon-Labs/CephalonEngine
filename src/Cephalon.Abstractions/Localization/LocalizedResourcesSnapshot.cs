namespace Cephalon.Abstractions.Localization;

/// <summary>
/// Captures the resolved localization state visible to the runtime.
/// </summary>
public sealed class LocalizedResourcesSnapshot
{
    /// <summary>
    /// Creates a localization snapshot.
    /// </summary>
    /// <param name="defaultCulture">The default catalog culture.</param>
    /// <param name="resolvedCulture">The culture actually resolved for the snapshot.</param>
    /// <param name="supportedCultures">The cultures currently supported by the catalog.</param>
    /// <param name="resources">The localized resources visible to the snapshot.</param>
    public LocalizedResourcesSnapshot(
        string defaultCulture,
        string resolvedCulture,
        IReadOnlyList<string> supportedCultures,
        IReadOnlyDictionary<string, string> resources)
    {
        if (string.IsNullOrWhiteSpace(defaultCulture))
        {
            throw new ArgumentException("Default culture is required.", nameof(defaultCulture));
        }

        if (string.IsNullOrWhiteSpace(resolvedCulture))
        {
            throw new ArgumentException("Resolved culture is required.", nameof(resolvedCulture));
        }

        DefaultCulture = defaultCulture.Trim();
        ResolvedCulture = resolvedCulture.Trim();
        SupportedCultures = supportedCultures ?? throw new ArgumentNullException(nameof(supportedCultures));
        Resources = resources ?? throw new ArgumentNullException(nameof(resources));
    }

    /// <summary>
    /// Gets the default catalog culture.
    /// </summary>
    public string DefaultCulture { get; }

    /// <summary>
    /// Gets the culture actually resolved for the snapshot.
    /// </summary>
    public string ResolvedCulture { get; }

    /// <summary>
    /// Gets the cultures currently supported by the catalog.
    /// </summary>
    public IReadOnlyList<string> SupportedCultures { get; }

    /// <summary>
    /// Gets the localized resources visible to the snapshot.
    /// </summary>
    public IReadOnlyDictionary<string, string> Resources { get; }
}
