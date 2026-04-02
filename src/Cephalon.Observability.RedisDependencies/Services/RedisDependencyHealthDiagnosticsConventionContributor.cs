using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.RedisDependencies.Services;

internal sealed class RedisDependencyHealthDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => RedisDependencyHealthDiagnosticsConventions.Convention;
}

internal static class RedisDependencyHealthDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ProbeTimedOut = new(
        Id: 3120,
        Name: "ProbeTimedOut",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Redis dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s against {Host}:{Port}.",
        Description: "Emitted when a Redis dependency probe does not complete before its timeout.");

    public static readonly DiagnosticEventDefinition ProbeFailed = new(
        Id: 3121,
        Name: "ProbeFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Redis dependency probe '{DependencyId}' failed against {Host}:{Port}.",
        Description: "Emitted when a Redis dependency probe fails because the configured cache endpoint cannot be reached or authenticated.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.RedisDependencies",
        LoggerCategoryPrefix: "Cephalon.Observability.RedisDependencies",
        Description: "Structured diagnostics for Redis and cache dependency probes.",
        Events:
        [
            ProbeTimedOut,
            ProbeFailed
        ]);
}
