using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.OracleDependencies.Services;

internal sealed class OracleDependencyHealthDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => OracleDependencyHealthDiagnosticsConventions.Convention;
}

internal static class OracleDependencyHealthDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ProbeTimedOut = new(
        Id: 3144,
        Name: "ProbeTimedOut",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Oracle dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s.",
        Description: "Emitted when an Oracle dependency probe does not complete before its timeout.");

    public static readonly DiagnosticEventDefinition ProbeFailed = new(
        Id: 3145,
        Name: "ProbeFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Oracle dependency probe '{DependencyId}' failed.",
        Description: "Emitted when an Oracle dependency probe fails because the configured database endpoint cannot be reached or queried successfully.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.OracleDependencies",
        LoggerCategoryPrefix: "Cephalon.Observability.OracleDependencies",
        Description: "Structured diagnostics for Oracle dependency probes.",
        Events:
        [
            ProbeTimedOut,
            ProbeFailed
        ]);
}
