using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.CassandraDependencies.Services;

internal sealed class CassandraDependencyHealthDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => CassandraDependencyHealthDiagnosticsConventions.Convention;
}

internal static class CassandraDependencyHealthDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ProbeTimedOut = new(
        Id: 3146,
        Name: "ProbeTimedOut",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Cassandra dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s.",
        Description: "Emitted when a Cassandra dependency probe does not complete before its timeout.");

    public static readonly DiagnosticEventDefinition ProbeFailed = new(
        Id: 3147,
        Name: "ProbeFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Cassandra dependency probe '{DependencyId}' failed.",
        Description: "Emitted when a Cassandra dependency probe fails because the configured cluster cannot be reached or queried successfully.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.CassandraDependencies",
        LoggerCategoryPrefix: "Cephalon.Observability.CassandraDependencies",
        Description: "Structured diagnostics for Cassandra dependency probes.",
        Events:
        [
            ProbeTimedOut,
            ProbeFailed
        ]);
}
