using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.MongoDbDependencies.Services;

internal sealed class MongoDbDependencyHealthDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => MongoDbDependencyHealthDiagnosticsConventions.Convention;
}

internal static class MongoDbDependencyHealthDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ProbeTimedOut = new(
        Id: 3130,
        Name: "ProbeTimedOut",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "MongoDB dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s.",
        Description: "Emitted when a MongoDB dependency probe does not complete before its timeout.");

    public static readonly DiagnosticEventDefinition ProbeFailed = new(
        Id: 3131,
        Name: "ProbeFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "MongoDB dependency probe '{DependencyId}' failed.",
        Description: "Emitted when a MongoDB dependency probe fails because the configured document database endpoint cannot be reached or queried successfully.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.MongoDbDependencies",
        LoggerCategoryPrefix: "Cephalon.Observability.MongoDbDependencies",
        Description: "Structured diagnostics for MongoDB dependency probes.",
        Events:
        [
            ProbeTimedOut,
            ProbeFailed
        ]);
}
