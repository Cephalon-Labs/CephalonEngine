namespace Cephalon.Engine.Diagnostics;

internal sealed class RuntimeDiagnosticsCatalogSnapshot : IRuntimeDiagnosticsCatalog
{
    private readonly IReadOnlyList<DiagnosticsConvention> conventions;
    private readonly Dictionary<string, IReadOnlyList<DiagnosticsConvention>> conventionsBySource;

    public RuntimeDiagnosticsCatalogSnapshot(IEnumerable<IDiagnosticsConventionContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(contributors);

        conventions = contributors
            .Select(static contributor => contributor.DescribeDiagnosticsConvention())
            .OrderBy(static convention => convention.Source, StringComparer.Ordinal)
            .ThenBy(static convention => convention.MinimumEventId ?? int.MaxValue)
            .ToArray();

        conventionsBySource = conventions
            .GroupBy(static convention => convention.Source, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<DiagnosticsConvention>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<DiagnosticsConvention> Conventions => conventions;

    public IReadOnlyList<DiagnosticsConvention> GetBySource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return [];
        }

        return conventionsBySource.TryGetValue(source.Trim(), out var matches)
            ? matches
            : [];
    }
}
