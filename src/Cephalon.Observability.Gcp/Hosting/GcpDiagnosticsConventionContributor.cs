using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.Gcp.Hosting;

internal sealed class GcpDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => GcpDiagnosticsConventions.Convention;
}

internal static class GcpDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ExportSummary = new(
        Id: 3111,
        Name: "GcpTelemetrySummary",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "GCP observability export uses endpoint mode {EndpointMode}, hosted platform {HostedPlatform}, credential mode {CredentialMode}, direct managed ingestion {UseGoogleManagedIngestion}, signals {Signals}.",
        Description: "Emitted once on host startup to summarize the active GCP observability defaults without logging secrets.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.Gcp",
        LoggerCategoryPrefix: "Cephalon.Observability.Gcp",
        Description: "GCP OTLP export defaults, Google-managed ingestion, and hosted resource-default diagnostics.",
        Events:
        [
            ExportSummary
        ]);
}
