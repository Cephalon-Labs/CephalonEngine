using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.Neo4jDependencies.Services;

internal sealed class Neo4jDependencyHealthDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => Neo4jDependencyHealthDiagnosticsConventions.Convention;
}

internal static class Neo4jDependencyHealthDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ProbeTimedOut = new(
        Id: 3148,
        Name: "ProbeTimedOut",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Neo4j dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s.",
        Description: "Emitted when a Neo4j dependency probe does not complete before its timeout.");

    public static readonly DiagnosticEventDefinition ProbeFailed = new(
        Id: 3149,
        Name: "ProbeFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Neo4j dependency probe '{DependencyId}' failed.",
        Description: "Emitted when a Neo4j dependency probe fails because the configured graph endpoint cannot be reached or queried successfully.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.Neo4jDependencies",
        LoggerCategoryPrefix: "Cephalon.Observability.Neo4jDependencies",
        Description: "Structured diagnostics for Neo4j dependency probes.",
        Events:
        [
            ProbeTimedOut,
            ProbeFailed
        ]);
}
