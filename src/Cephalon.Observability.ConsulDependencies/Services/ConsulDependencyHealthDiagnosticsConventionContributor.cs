using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.ConsulDependencies.Services;

internal sealed class ConsulDependencyHealthDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => ConsulDependencyHealthDiagnosticsConventions.Convention;
}

internal static class ConsulDependencyHealthDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ProbeTimedOut = new(
        Id: 3142,
        Name: "ProbeTimedOut",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Consul dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s against {Endpoint}.",
        Description: "Emitted when a Consul dependency probe does not complete before its timeout.");

    public static readonly DiagnosticEventDefinition ProbeFailed = new(
        Id: 3143,
        Name: "ProbeFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Consul dependency probe '{DependencyId}' failed against {Endpoint}.",
        Description: "Emitted when a Consul dependency probe fails because the configured control-plane endpoint cannot be reached or does not report a leader.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.ConsulDependencies",
        LoggerCategoryPrefix: "Cephalon.Observability.ConsulDependencies",
        Description: "Structured diagnostics for Consul dependency probes.",
        Events:
        [
            ProbeTimedOut,
            ProbeFailed
        ]);
}
