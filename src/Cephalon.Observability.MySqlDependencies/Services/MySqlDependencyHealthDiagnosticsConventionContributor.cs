using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.MySqlDependencies.Services;

internal sealed class MySqlDependencyHealthDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => MySqlDependencyHealthDiagnosticsConventions.Convention;
}

internal static class MySqlDependencyHealthDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ProbeTimedOut = new(
        Id: 3128,
        Name: "ProbeTimedOut",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "MySQL dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s.",
        Description: "Emitted when a MySQL dependency probe does not complete before its timeout.");

    public static readonly DiagnosticEventDefinition ProbeFailed = new(
        Id: 3129,
        Name: "ProbeFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "MySQL dependency probe '{DependencyId}' failed.",
        Description: "Emitted when a MySQL dependency probe fails because the configured database endpoint cannot be reached or queried successfully.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.MySqlDependencies",
        LoggerCategoryPrefix: "Cephalon.Observability.MySqlDependencies",
        Description: "Structured diagnostics for MySQL dependency probes.",
        Events:
        [
            ProbeTimedOut,
            ProbeFailed
        ]);
}
