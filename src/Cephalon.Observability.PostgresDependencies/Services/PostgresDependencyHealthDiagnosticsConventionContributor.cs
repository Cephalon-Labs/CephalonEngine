using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.PostgresDependencies.Services;

internal sealed class PostgresDependencyHealthDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => PostgresDependencyHealthDiagnosticsConventions.Convention;
}

internal static class PostgresDependencyHealthDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ProbeTimedOut = new(
        Id: 3122,
        Name: "ProbeTimedOut",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Postgres dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s.",
        Description: "Emitted when a Postgres dependency probe does not complete before its timeout.");

    public static readonly DiagnosticEventDefinition ProbeFailed = new(
        Id: 3123,
        Name: "ProbeFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Postgres dependency probe '{DependencyId}' failed.",
        Description: "Emitted when a Postgres dependency probe fails because the configured database endpoint cannot be reached or queried successfully.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.PostgresDependencies",
        LoggerCategoryPrefix: "Cephalon.Observability.PostgresDependencies",
        Description: "Structured diagnostics for Postgres dependency probes.",
        Events:
        [
            ProbeTimedOut,
            ProbeFailed
        ]);
}
