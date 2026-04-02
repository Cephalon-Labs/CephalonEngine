using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.NatsDependencies.Services;

internal sealed class NatsDependencyHealthDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => NatsDependencyHealthDiagnosticsConventions.Convention;
}

internal static class NatsDependencyHealthDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ProbeTimedOut = new(
        Id: 3134,
        Name: "ProbeTimedOut",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "NATS dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s.",
        Description: "Emitted when a NATS dependency probe does not complete before its timeout.");

    public static readonly DiagnosticEventDefinition ProbeFailed = new(
        Id: 3135,
        Name: "ProbeFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "NATS dependency probe '{DependencyId}' failed.",
        Description: "Emitted when a NATS dependency probe fails because the configured broker endpoint cannot complete the CONNECT and PING/PONG exchange.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.NatsDependencies",
        LoggerCategoryPrefix: "Cephalon.Observability.NatsDependencies",
        Description: "Structured diagnostics for NATS dependency probes.",
        Events:
        [
            ProbeTimedOut,
            ProbeFailed
        ]);
}
