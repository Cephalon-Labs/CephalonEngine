using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.ClickHouseDependencies.Services;

internal sealed class ClickHouseDependencyHealthDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => ClickHouseDependencyHealthDiagnosticsConventions.Convention;
}

internal static class ClickHouseDependencyHealthDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ProbeTimedOut = new(
        Id: 3152,
        Name: "ProbeTimedOut",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "ClickHouse dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s.",
        Description: "Emitted when a ClickHouse dependency probe does not complete before its timeout.");

    public static readonly DiagnosticEventDefinition ProbeFailed = new(
        Id: 3153,
        Name: "ProbeFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "ClickHouse dependency probe '{DependencyId}' failed.",
        Description: "Emitted when a ClickHouse dependency probe fails because the endpoint is unreachable, unauthorized, or does not complete the health query.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.ClickHouseDependencies",
        LoggerCategoryPrefix: "Cephalon.Observability.ClickHouseDependencies",
        Description: "Structured diagnostics for ClickHouse dependency probes.",
        Events:
        [
            ProbeTimedOut,
            ProbeFailed
        ]);
}
