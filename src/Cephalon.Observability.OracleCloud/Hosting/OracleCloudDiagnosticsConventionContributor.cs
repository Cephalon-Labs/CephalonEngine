using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.OracleCloud.Hosting;

internal sealed class OracleCloudDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => OracleCloudDiagnosticsConventions.Convention;
}

internal static class OracleCloudDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ExportSummary = new(
        Id: 3118,
        Name: "OracleCloudTelemetrySummary",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Oracle Cloud observability export uses endpoint mode {EndpointMode}, hosted platform {HostedPlatform}, authentication mode {AuthenticationMode}, managed OpenTelemetry ingestion {UseManagedOpenTelemetryIngestion}, signals {Signals}.",
        Description: "Emitted once on host startup to summarize the active Oracle Cloud observability defaults without logging secrets.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.OracleCloud",
        LoggerCategoryPrefix: "Cephalon.Observability.OracleCloud",
        Description: "Oracle Cloud APM OTLP defaults, managed ingestion, and hosted resource-default diagnostics.",
        Events:
        [
            ExportSummary
        ]);
}
