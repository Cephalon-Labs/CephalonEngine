using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.KafkaDependencies.Services;

internal sealed class KafkaDependencyHealthDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => KafkaDependencyHealthDiagnosticsConventions.Convention;
}

internal static class KafkaDependencyHealthDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ProbeTimedOut = new(
        Id: 3132,
        Name: "ProbeTimedOut",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Kafka dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s.",
        Description: "Emitted when a Kafka dependency probe does not complete before its timeout.");

    public static readonly DiagnosticEventDefinition ProbeFailed = new(
        Id: 3133,
        Name: "ProbeFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Kafka dependency probe '{DependencyId}' failed.",
        Description: "Emitted when a Kafka dependency probe fails because the configured broker metadata cannot be retrieved successfully.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.KafkaDependencies",
        LoggerCategoryPrefix: "Cephalon.Observability.KafkaDependencies",
        Description: "Structured diagnostics for Kafka dependency probes.",
        Events:
        [
            ProbeTimedOut,
            ProbeFailed
        ]);
}
