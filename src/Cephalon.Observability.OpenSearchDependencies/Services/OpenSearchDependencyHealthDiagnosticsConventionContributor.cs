using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.OpenSearchDependencies.Services;

internal sealed class OpenSearchDependencyHealthDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => OpenSearchDependencyHealthDiagnosticsConventions.Convention;
}

internal static class OpenSearchDependencyHealthDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ProbeTimedOut = new(
        Id: 3150,
        Name: "ProbeTimedOut",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "OpenSearch dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s against {Endpoint}.",
        Description: "Emitted when an OpenSearch dependency probe does not complete before its timeout.");

    public static readonly DiagnosticEventDefinition ProbeFailed = new(
        Id: 3151,
        Name: "ProbeFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "OpenSearch dependency probe '{DependencyId}' failed against {Endpoint}.",
        Description: "Emitted when an OpenSearch dependency probe fails because the cluster-health endpoint is unreachable, unauthorized, or reports an unhealthy result.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.OpenSearchDependencies",
        LoggerCategoryPrefix: "Cephalon.Observability.OpenSearchDependencies",
        Description: "Structured diagnostics for OpenSearch dependency probes.",
        Events:
        [
            ProbeTimedOut,
            ProbeFailed
        ]);
}
