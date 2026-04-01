namespace Cephalon.Abstractions.Localization;

public interface ILocalizedResourceRegistry
{
    void Add(string culture, string key, string value);

    void Add(string culture, IReadOnlyDictionary<string, string> resources);
}
