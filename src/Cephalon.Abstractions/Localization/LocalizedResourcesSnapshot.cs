namespace Cephalon.Abstractions.Localization;

public sealed class LocalizedResourcesSnapshot
{
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

    public string DefaultCulture { get; }

    public string ResolvedCulture { get; }

    public IReadOnlyList<string> SupportedCultures { get; }

    public IReadOnlyDictionary<string, string> Resources { get; }
}
