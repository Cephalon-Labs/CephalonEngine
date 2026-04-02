using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.HttpDependencies.Services;

internal sealed class HttpDependencyHealthDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => HttpDependencyHealthDiagnosticsConventions.Convention;
}

internal static class HttpDependencyHealthDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ProbeTimedOut = new(
        Id: 3100,
        Name: "ProbeTimedOut",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "HTTP dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s against {Endpoint}.",
        Description: "Emitted when an HTTP dependency probe does not complete before its timeout.");

    public static readonly DiagnosticEventDefinition ProbeFailed = new(
        Id: 3101,
        Name: "ProbeFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "HTTP dependency probe '{DependencyId}' failed against {Endpoint}.",
        Description: "Emitted when an HTTP dependency probe fails because the upstream is unreachable or returns an unexpected result.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.HttpDependencies",
        LoggerCategoryPrefix: "Cephalon.Observability.HttpDependencies",
        Description: "Structured diagnostics for external HTTP and API dependency probes.",
        Events:
        [
            ProbeTimedOut,
            ProbeFailed
        ]);
}
