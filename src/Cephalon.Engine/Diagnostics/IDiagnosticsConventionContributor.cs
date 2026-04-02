namespace Cephalon.Engine.Diagnostics;

/// <summary>
/// Contributes a diagnostics convention to the runtime-facing diagnostics catalog.
/// </summary>
public interface IDiagnosticsConventionContributor
{
    /// <summary>
    /// Describes the diagnostics convention owned by the contributor.
    /// </summary>
    /// <returns>The diagnostics convention published by the contributor.</returns>
    DiagnosticsConvention DescribeDiagnosticsConvention();
}
