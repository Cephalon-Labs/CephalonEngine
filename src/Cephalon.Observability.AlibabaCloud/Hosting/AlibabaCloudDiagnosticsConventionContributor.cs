using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.AlibabaCloud.Hosting;

internal sealed class AlibabaCloudDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => AlibabaCloudDiagnosticsConventions.Convention;
}

internal static class AlibabaCloudDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ExportSummary = new(
        Id: 3113,
        Name: "AlibabaCloudTelemetrySummary",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Alibaba Cloud observability export uses endpoint mode {EndpointMode}, hosted platform {HostedPlatform}, authentication mode {AuthenticationMode}, managed OpenTelemetry ingestion {UseManagedOpenTelemetryIngestion}, signals {Signals}.",
        Description: "Emitted once on host startup to summarize the active Alibaba Cloud observability defaults without logging secrets.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.AlibabaCloud",
        LoggerCategoryPrefix: "Cephalon.Observability.AlibabaCloud",
        Description: "Alibaba Cloud OTLP defaults, managed OpenTelemetry ingestion, and hosted resource-default diagnostics.",
        Events:
        [
            ExportSummary
        ]);
}
