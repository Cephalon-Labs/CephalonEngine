using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.Tanzu.Hosting;

internal sealed class TanzuDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => TanzuDiagnosticsConventions.Convention;
}

internal static class TanzuDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ExportSummary = new(
        Id: 3116,
        Name: "TanzuTelemetrySummary",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "VMware Tanzu observability export uses endpoint mode {EndpointMode}, hosted platform {HostedPlatform}, collector target {CollectorTarget}, trust mode {TrustMode}, handoff mode {HandoffMode}, signals {Signals}.",
        Description: "Emitted once on host startup to summarize the active VMware Tanzu observability defaults without logging secrets.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.Tanzu",
        LoggerCategoryPrefix: "Cephalon.Observability.Tanzu",
        Description: "VMware Tanzu OTLP defaults, Wavefront proxy trace handoff, and hosted resource-default diagnostics.",
        Events:
        [
            ExportSummary
        ]);
}
