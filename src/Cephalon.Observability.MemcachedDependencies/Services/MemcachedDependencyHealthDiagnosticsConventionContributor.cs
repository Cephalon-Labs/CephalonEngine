using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.MemcachedDependencies.Services;

internal sealed class MemcachedDependencyHealthDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => MemcachedDependencyHealthDiagnosticsConventions.Convention;
}

internal static class MemcachedDependencyHealthDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ProbeTimedOut = new(
        Id: 3140,
        Name: "ProbeTimedOut",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Memcached dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s against {Host}:{Port}.",
        Description: "Emitted when a Memcached dependency probe does not complete before its timeout.");

    public static readonly DiagnosticEventDefinition ProbeFailed = new(
        Id: 3141,
        Name: "ProbeFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Memcached dependency probe '{DependencyId}' failed against {Host}:{Port}.",
        Description: "Emitted when a Memcached dependency probe fails because the configured cache endpoint cannot be reached or does not respond to a version probe.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.MemcachedDependencies",
        LoggerCategoryPrefix: "Cephalon.Observability.MemcachedDependencies",
        Description: "Structured diagnostics for Memcached dependency probes.",
        Events:
        [
            ProbeTimedOut,
            ProbeFailed
        ]);
}
