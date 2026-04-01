namespace Cephalon.Abstractions.Localization;

public interface ILocalizedTextCatalog
{
    string DefaultCulture { get; }

    IReadOnlyList<string> SupportedCultures { get; }

    bool TryGet(string key, string? culture, out string value);

    string ResolveText(string key, string? culture = null, string? fallback = null);

    IReadOnlyDictionary<string, string> GetResources(string? culture = null);

    LocalizedResourcesSnapshot CreateSnapshot(string? culture = null);
}
