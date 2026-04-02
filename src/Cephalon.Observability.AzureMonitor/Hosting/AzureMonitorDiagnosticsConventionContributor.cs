using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.AzureMonitor.Hosting;

internal sealed class AzureMonitorDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => AzureMonitorDiagnosticsConventions.Convention;
}

internal static class AzureMonitorDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ExportSummary = new(
        Id: 3100,
        Name: "AzureMonitorExportSummary",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Azure Monitor export uses authentication {AuthenticationMode}, hosted platform {HostedPlatform}, logs {ExportLogs}, metrics {ExportMetrics}, traces {ExportTraces}.",
        Description: "Emitted once on host startup to summarize the active Azure Monitor exporter settings without logging secrets.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.AzureMonitor",
        LoggerCategoryPrefix: "Cephalon.Observability.AzureMonitor",
        Description: "Azure Monitor exporter wiring and hosted Azure resource-default diagnostics.",
        Events:
        [
            ExportSummary
        ]);
}
