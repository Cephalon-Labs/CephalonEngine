namespace Cephalon.Engine.Diagnostics;

/// <summary>
/// Exposes the merged diagnostics conventions visible to the current runtime.
/// </summary>
public interface IRuntimeDiagnosticsCatalog
{
    /// <summary>
    /// Gets the published diagnostics conventions visible to the runtime.
    /// </summary>
    IReadOnlyList<DiagnosticsConvention> Conventions { get; }

    /// <summary>
    /// Gets the diagnostics conventions owned by the requested source.
    /// </summary>
    /// <param name="source">The diagnostics source or package identifier.</param>
    /// <returns>The matching conventions, or an empty list when the source is not active.</returns>
    IReadOnlyList<DiagnosticsConvention> GetBySource(string source);
}
