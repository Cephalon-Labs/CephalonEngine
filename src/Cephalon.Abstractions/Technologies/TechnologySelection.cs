namespace Cephalon.Abstractions.Technologies;

public sealed class TechnologySelection
{
    private readonly Dictionary<string, TechnologyDescriptor> selectedIndex;
    private readonly Dictionary<string, TechnologyDescriptor> catalogIndex;

    public TechnologySelection(
        IReadOnlyList<TechnologyDescriptor> selected,
        IReadOnlyList<TechnologyDescriptor> catalog)
    {
        Selected = selected ?? throw new ArgumentNullException(nameof(selected));
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        selectedIndex = CreateIndex(selected);
        catalogIndex = CreateIndex(catalog);
    }

    public IReadOnlyList<TechnologyDescriptor> Selected { get; }

    public IReadOnlyList<TechnologyDescriptor> Catalog { get; }

    public bool IsSelected(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return selectedIndex.ContainsKey(NormalizeKey(value));
    }

    public bool IsAvailable(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return catalogIndex.ContainsKey(NormalizeKey(value));
    }

    public bool TryGetSelected(string value, out TechnologyDescriptor technology)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return selectedIndex.TryGetValue(NormalizeKey(value), out technology!);
    }

    public bool TryGetAvailable(string value, out TechnologyDescriptor technology)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return catalogIndex.TryGetValue(NormalizeKey(value), out technology!);
    }

    private static Dictionary<string, TechnologyDescriptor> CreateIndex(IEnumerable<TechnologyDescriptor> technologies)
    {
        var index = new Dictionary<string, TechnologyDescriptor>(StringComparer.Ordinal);

        foreach (var technology in technologies)
        {
            index[NormalizeKey(technology.Id)] = technology;
            index[NormalizeKey(technology.DisplayName)] = technology;
        }

        return index;
    }

    private static string NormalizeKey(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
    }
}
