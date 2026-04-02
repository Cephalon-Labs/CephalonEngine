using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.Aws.Hosting;

internal sealed class AwsDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => AwsDiagnosticsConventions.Convention;
}

internal static class AwsDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ExportSummary = new(
        Id: 3110,
        Name: "AwsTelemetrySummary",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "AWS observability export uses endpoint mode {EndpointMode}, hosted platform {HostedPlatform}, trace mode {TraceMode}, AWS SDK instrumentation {EnableAwsSdkInstrumentation}, signals {Signals}.",
        Description: "Emitted once on host startup to summarize the active AWS observability defaults without logging secrets.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.Aws",
        LoggerCategoryPrefix: "Cephalon.Observability.Aws",
        Description: "AWS OTLP export defaults, trace propagation, and hosted resource-default diagnostics.",
        Events:
        [
            ExportSummary
        ]);
}
