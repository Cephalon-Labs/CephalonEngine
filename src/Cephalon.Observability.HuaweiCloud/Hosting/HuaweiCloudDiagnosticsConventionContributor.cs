using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.HuaweiCloud.Hosting;

internal sealed class HuaweiCloudDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => HuaweiCloudDiagnosticsConventions.Convention;
}

internal static class HuaweiCloudDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ExportSummary = new(
        Id: 3112,
        Name: "HuaweiCloudTelemetrySummary",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Huawei Cloud observability export uses endpoint mode {EndpointMode}, hosted platform {HostedPlatform}, authentication mode {AuthenticationMode}, direct APM trace ingestion {UseApmManagedTraceIngestion}, signals {Signals}.",
        Description: "Emitted once on host startup to summarize the active Huawei Cloud observability defaults without logging secrets.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.HuaweiCloud",
        LoggerCategoryPrefix: "Cephalon.Observability.HuaweiCloud",
        Description: "Huawei Cloud OTLP defaults, managed APM trace ingestion, and hosted resource-default diagnostics.",
        Events:
        [
            ExportSummary
        ]);
}
