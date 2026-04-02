using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.ElasticsearchDependencies.Services;

internal sealed class ElasticsearchDependencyHealthDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => ElasticsearchDependencyHealthDiagnosticsConventions.Convention;
}

internal static class ElasticsearchDependencyHealthDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ProbeTimedOut = new(
        Id: 3138,
        Name: "ProbeTimedOut",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Elasticsearch dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s against {Endpoint}.",
        Description: "Emitted when an Elasticsearch dependency probe does not complete before its timeout.");

    public static readonly DiagnosticEventDefinition ProbeFailed = new(
        Id: 3139,
        Name: "ProbeFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Elasticsearch dependency probe '{DependencyId}' failed against {Endpoint}.",
        Description: "Emitted when an Elasticsearch dependency probe fails because the cluster-health endpoint is unreachable, unauthorized, or reports an unhealthy result.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.ElasticsearchDependencies",
        LoggerCategoryPrefix: "Cephalon.Observability.ElasticsearchDependencies",
        Description: "Structured diagnostics for Elasticsearch dependency probes.",
        Events:
        [
            ProbeTimedOut,
            ProbeFailed
        ]);
}
