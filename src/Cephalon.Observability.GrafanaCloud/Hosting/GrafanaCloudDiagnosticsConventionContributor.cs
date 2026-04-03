using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.GrafanaCloud.Hosting;

internal sealed class GrafanaCloudDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => GrafanaCloudDiagnosticsConventions.Convention;
}

internal static class GrafanaCloudDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ExportSummary = new(
        Id: 3119,
        Name: "GrafanaCloudTelemetrySummary",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Grafana Cloud observability export uses endpoint mode {EndpointMode}, endpoint target {EndpointTarget}, authentication mode {AuthenticationMode}, resource context {ResourceContext}, signals {Signals}.",
        Description: "Emitted once on host startup to summarize the active Grafana Cloud observability defaults without logging secrets.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.GrafanaCloud",
        LoggerCategoryPrefix: "Cephalon.Observability.GrafanaCloud",
        Description: "Grafana Cloud OTLP endpoint and access-policy authentication diagnostics.",
        Events:
        [
            ExportSummary
        ]);
}
