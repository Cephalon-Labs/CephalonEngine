using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.OpenShift.Hosting;

internal sealed class OpenShiftDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => OpenShiftDiagnosticsConventions.Convention;
}

internal static class OpenShiftDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ExportSummary = new(
        Id: 3114,
        Name: "OpenShiftTelemetrySummary",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "OpenShift observability export uses endpoint mode {EndpointMode}, hosted platform {HostedPlatform}, collector target {CollectorTarget}, trust mode {TrustMode}, headers mode {HeadersMode}, signals {Signals}.",
        Description: "Emitted once on host startup to summarize the active OpenShift observability defaults without logging secrets.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.OpenShift",
        LoggerCategoryPrefix: "Cephalon.Observability.OpenShift",
        Description: "OpenShift OTLP collector defaults, trust handling, and hosted resource-default diagnostics.",
        Events:
        [
            ExportSummary
        ]);
}
