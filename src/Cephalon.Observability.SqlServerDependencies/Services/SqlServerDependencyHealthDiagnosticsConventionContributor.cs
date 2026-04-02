using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.SqlServerDependencies.Services;

internal sealed class SqlServerDependencyHealthDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => SqlServerDependencyHealthDiagnosticsConventions.Convention;
}

internal static class SqlServerDependencyHealthDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ProbeTimedOut = new(
        Id: 3126,
        Name: "ProbeTimedOut",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "SQL Server dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s.",
        Description: "Emitted when a SQL Server dependency probe does not complete before its timeout.");

    public static readonly DiagnosticEventDefinition ProbeFailed = new(
        Id: 3127,
        Name: "ProbeFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "SQL Server dependency probe '{DependencyId}' failed.",
        Description: "Emitted when a SQL Server dependency probe fails because the configured SQL endpoint cannot be reached or queried successfully.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.SqlServerDependencies",
        LoggerCategoryPrefix: "Cephalon.Observability.SqlServerDependencies",
        Description: "Structured diagnostics for SQL Server dependency probes.",
        Events:
        [
            ProbeTimedOut,
            ProbeFailed
        ]);
}
