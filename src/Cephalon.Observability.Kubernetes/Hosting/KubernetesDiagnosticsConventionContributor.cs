using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.Kubernetes.Hosting;

internal sealed class KubernetesDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => KubernetesDiagnosticsConventions.Convention;
}

internal static class KubernetesDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ExportSummary = new(
        Id: 3117,
        Name: "KubernetesTelemetrySummary",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Kubernetes observability export uses endpoint mode {EndpointMode}, collector target {CollectorTarget}, resource context {ResourceContext}, trust mode {TrustMode}, headers mode {HeadersMode}, signals {Signals}.",
        Description: "Emitted once on host startup to summarize the active Kubernetes observability defaults without logging secrets.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.Kubernetes",
        LoggerCategoryPrefix: "Cephalon.Observability.Kubernetes",
        Description: "Kubernetes OTLP collector defaults, trust handling, and resource-default diagnostics.",
        Events:
        [
            ExportSummary
        ]);
}
