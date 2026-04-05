namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Provides lookup helpers over selected and available technology profiles.
/// </summary>
public sealed class TechnologySelection
{
    private readonly Dictionary<string, TechnologyDescriptor> selectedIndex;
    private readonly Dictionary<string, TechnologyDescriptor> catalogIndex;

    /// <summary>
    /// Creates a technology-selection view.
    /// </summary>
    /// <param name="selected">The technology profiles currently selected for the app.</param>
    /// <param name="catalog">The technology profiles available to the runtime.</param>
    public TechnologySelection(
        IReadOnlyList<TechnologyDescriptor> selected,
        IReadOnlyList<TechnologyDescriptor> catalog)
    {
        Selected = selected ?? throw new ArgumentNullException(nameof(selected));
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        selectedIndex = CreateIndex(selected);
        catalogIndex = CreateIndex(catalog);
    }

    /// <summary>
    /// Gets the technology profiles currently selected for the app.
    /// </summary>
    public IReadOnlyList<TechnologyDescriptor> Selected { get; }

    /// <summary>
    /// Gets the technology profiles available to the runtime.
    /// </summary>
    public IReadOnlyList<TechnologyDescriptor> Catalog { get; }

    /// <summary>
    /// Determines whether a technology is selected.
    /// </summary>
    /// <param name="value">The technology identifier or display name to match.</param>
    /// <returns><see langword="true"/> when the technology is selected; otherwise <see langword="false"/>.</returns>
    public bool IsSelected(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return selectedIndex.ContainsKey(NormalizeKey(value));
    }

    /// <summary>
    /// Determines whether a technology is available in the runtime catalog.
    /// </summary>
    /// <param name="value">The technology identifier or display name to match.</param>
    /// <returns><see langword="true"/> when the technology is available; otherwise <see langword="false"/>.</returns>
    public bool IsAvailable(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return catalogIndex.ContainsKey(NormalizeKey(value));
    }

    /// <summary>
    /// Attempts to resolve one selected technology.
    /// </summary>
    /// <param name="value">The technology identifier or display name to match.</param>
    /// <param name="technology">The resolved selected technology when one is found.</param>
    /// <returns><see langword="true"/> when the technology is selected; otherwise <see langword="false"/>.</returns>
    public bool TryGetSelected(string value, out TechnologyDescriptor technology)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return selectedIndex.TryGetValue(NormalizeKey(value), out technology!);
    }

    /// <summary>
    /// Attempts to resolve one available technology from the runtime catalog.
    /// </summary>
    /// <param name="value">The technology identifier or display name to match.</param>
    /// <param name="technology">The resolved available technology when one is found.</param>
    /// <returns><see langword="true"/> when the technology is available; otherwise <see langword="false"/>.</returns>
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

            foreach (var alias in technology.Aliases)
            {
                index[NormalizeKey(alias)] = technology;
            }
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
