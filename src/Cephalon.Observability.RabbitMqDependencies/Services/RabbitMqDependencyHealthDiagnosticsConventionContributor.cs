using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.RabbitMqDependencies.Services;

internal sealed class RabbitMqDependencyHealthDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => RabbitMqDependencyHealthDiagnosticsConventions.Convention;
}

internal static class RabbitMqDependencyHealthDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ProbeTimedOut = new(
        Id: 3124,
        Name: "ProbeTimedOut",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "RabbitMQ dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s.",
        Description: "Emitted when a RabbitMQ dependency probe does not complete before its timeout.");

    public static readonly DiagnosticEventDefinition ProbeFailed = new(
        Id: 3125,
        Name: "ProbeFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "RabbitMQ dependency probe '{DependencyId}' failed.",
        Description: "Emitted when a RabbitMQ dependency probe fails because the configured broker endpoint cannot accept the probe connection.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.RabbitMqDependencies",
        LoggerCategoryPrefix: "Cephalon.Observability.RabbitMqDependencies",
        Description: "Structured diagnostics for RabbitMQ dependency probes.",
        Events:
        [
            ProbeTimedOut,
            ProbeFailed
        ]);
}
