using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.MqttDependencies.Services;

internal sealed class MqttDependencyHealthDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => MqttDependencyHealthDiagnosticsConventions.Convention;
}

internal static class MqttDependencyHealthDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ProbeTimedOut = new(
        Id: 3136,
        Name: "ProbeTimedOut",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "MQTT dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s.",
        Description: "Emitted when an MQTT dependency probe does not complete before its timeout.");

    public static readonly DiagnosticEventDefinition ProbeFailed = new(
        Id: 3137,
        Name: "ProbeFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "MQTT dependency probe '{DependencyId}' failed.",
        Description: "Emitted when an MQTT dependency probe fails because the configured broker endpoint cannot complete the CONNECT, CONNACK, and PINGREQ/PINGRESP exchange.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.MqttDependencies",
        LoggerCategoryPrefix: "Cephalon.Observability.MqttDependencies",
        Description: "Structured diagnostics for MQTT dependency probes.",
        Events:
        [
            ProbeTimedOut,
            ProbeFailed
        ]);
}
